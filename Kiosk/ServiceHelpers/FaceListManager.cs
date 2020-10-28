// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Cognitive Services: http://www.microsoft.com/cognitive
// 
// Microsoft Cognitive Services Github:
// https://github.com/Microsoft/Cognitive
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceHelpers
{
    class FaceListInfo
    {
        public string FaceListId { get; set; }
        public DateTime LastMatchTimestamp { get; set; }
        public bool IsFull { get; set; }
    }

    public class FaceListManager
    {
        private const int MaxFaceListCount = 64;
        private static Dictionary<string, FaceListInfo> faceLists;
        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public static string FaceListsUserDataFilter { get; set; }

        private FaceListManager() { }

        public static async Task ResetFaceLists()
        {
            faceLists = new Dictionary<string, FaceListInfo>();

            try
            {
                IEnumerable<FaceList> metadata = await FaceServiceHelper.GetFaceListsAsync(FaceListsUserDataFilter);
                foreach (var item in metadata)
                {
                    await FaceServiceHelper.DeleteFaceListAsync(item.FaceListId);
                }
            }
            catch (Exception e)
            {
                ErrorTrackingHelper.TrackException(e, "Error resetting face lists");
            }
        }

        public static async Task Initialize()
        {
            faceLists = new Dictionary<string, FaceListInfo>();

            try
            {
                IEnumerable<FaceList> metadata = await FaceServiceHelper.GetFaceListsAsync(FaceListsUserDataFilter);
                foreach (var item in metadata)
                {
                    faceLists.Add(item.FaceListId, new FaceListInfo { FaceListId = item.FaceListId, LastMatchTimestamp = DateTime.Now });
                }
            }
            catch (Exception e)
            {
                ErrorTrackingHelper.TrackException(e, "Face API GetFaceListsAsync error");
            }
        }

        public static Task<SimilarFace> FindSimilarPersistedFaceAsync(string imageUrl, Guid faceId, DetectedFace face)
        {
            return FindSimilarPersistedFaceAsync(imageUrl, null, faceId, face);
        }

        public static Task<SimilarFace> FindSimilarPersistedFaceAsync(Func<Task<Stream>> imageStreamCallback, Guid faceId, DetectedFace face)
        {
            return FindSimilarPersistedFaceAsync(null, imageStreamCallback, faceId, face);
        }

        private static async Task<SimilarFace> FindSimilarPersistedFaceAsync(string imageUrl, Func<Task<Stream>> imageStreamCallback, Guid faceId, DetectedFace face)
        {
            await semaphore.WaitAsync();
            try
            {
                return await FindSimilarOrInsertAsync(imageUrl, imageStreamCallback, faceId, face);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private static async Task<SimilarFace> FindSimilarOrInsertAsync(string imageUrl, Func<Task<Stream>> imageStreamCallback, Guid faceId, DetectedFace face)
        {
            if (faceLists == null)
            {
                await Initialize();
            }

            Tuple<SimilarFace, string> bestMatch = null;

            bool foundMatch = false;
            foreach (var faceListId in faceLists.Keys)
            {
                try
                {
                    SimilarFace similarFace = (await FaceServiceHelper.FindSimilarAsync(faceId, faceListId))?.FirstOrDefault();
                    if (similarFace == null)
                    {
                        continue;
                    }

                    foundMatch = true;

                    if (bestMatch != null)
                    {
                        // We already found a match for this face in another list. Replace the previous one if the new confidence is higher.
                        if (bestMatch.Item1.Confidence < similarFace.Confidence)
                        {
                            bestMatch = new Tuple<SimilarFace, string>(similarFace, faceListId);
                        }
                    }
                    else
                    {
                        bestMatch = new Tuple<SimilarFace, string>(similarFace, faceListId);
                    }
                }
                catch (Exception e)
                {
                    // Catch errors with individual face lists so we can continue looping through all lists. Maybe an answer will come from
                    // another one.
                    ErrorTrackingHelper.TrackException(e, "Face API FindSimilarAsync error");
                }
            }

            if (!foundMatch)
            {
                // If we are here we didnt' find a match, so let's add the face to the first FaceList that we can add it to. We
                // might create a new list if none exist, and if all lists are full we will delete the oldest face list (based on when we  
                // last match anything on) so that we can add the new one.

                double maxAngle = 30;
                if (face.FaceAttributes.HeadPose != null &&
                    (Math.Abs(face.FaceAttributes.HeadPose.Yaw) > maxAngle ||
                     Math.Abs(face.FaceAttributes.HeadPose.Pitch) > maxAngle ||
                     Math.Abs(face.FaceAttributes.HeadPose.Roll) > maxAngle))
                {
                    // This isn't a good frontal shot, so let's not use it as the primary example face for this person
                    return null;
                }

                if (!faceLists.Any())
                {
                    // We don't have any FaceLists yet. Create one
                    string newFaceListId = Guid.NewGuid().ToString();
                    await FaceServiceHelper.CreateFaceListAsync(newFaceListId, "ManagedFaceList", FaceListsUserDataFilter);

                    faceLists.Add(newFaceListId, new FaceListInfo { FaceListId = newFaceListId, LastMatchTimestamp = DateTime.Now });
                }

                PersistedFace addResult = null;
                bool failedToAddToNonFullList = false;
                foreach (var faceList in faceLists)
                {
                    if (faceList.Value.IsFull)
                    {
                        continue;
                    }

                    try
                    {
                        if (imageUrl != null)
                        {
                            addResult = await FaceServiceHelper.AddFaceToFaceListFromUrlAsync(faceList.Key, imageUrl, face.FaceRectangle);
                        }
                        else
                        {
                            addResult = await FaceServiceHelper.AddFaceToFaceListFromStreamAsync(faceList.Key, imageStreamCallback, face.FaceRectangle);
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (ex is APIErrorException && ((APIErrorException)ex).Response.StatusCode == (System.Net.HttpStatusCode)403)
                        {
                            // FaceList is full. Continue so we can try again with the next FaceList
                            faceList.Value.IsFull = true;
                            continue;
                        }
                        else
                        {
                            failedToAddToNonFullList = true;
                            break;
                        }
                    }
                }

                if (addResult == null && !failedToAddToNonFullList)
                {
                    // We were not able to add the face to an existing list because they were all full. 

                    // If possible, let's create a new list now and add the new face to it. If we can't (e.g. we already maxed out on list count), 
                    // let's delete an old list, create a new one and add the new face to it.

                    if (faceLists.Count == MaxFaceListCount)
                    {
                        // delete oldest face list
                        var oldestFaceList = faceLists.OrderBy(fl => fl.Value.LastMatchTimestamp).FirstOrDefault();
                        faceLists.Remove(oldestFaceList.Key);
                        await FaceServiceHelper.DeleteFaceListAsync(oldestFaceList.Key);
                    }

                    // create new list
                    string newFaceListId = Guid.NewGuid().ToString();
                    await FaceServiceHelper.CreateFaceListAsync(newFaceListId, "ManagedFaceList", FaceListsUserDataFilter);
                    faceLists.Add(newFaceListId, new FaceListInfo { FaceListId = newFaceListId, LastMatchTimestamp = DateTime.Now });

                    // Add face to new list
                    if (imageUrl != null)
                    {
                        addResult = await FaceServiceHelper.AddFaceToFaceListFromUrlAsync(newFaceListId, imageUrl, face.FaceRectangle);
                    }
                    else
                    {
                        addResult = await FaceServiceHelper.AddFaceToFaceListFromStreamAsync(newFaceListId, imageStreamCallback, face.FaceRectangle);
                    }
                }

                if (addResult != null)
                {
                    bestMatch = new Tuple<SimilarFace, string>(new SimilarFace { Confidence = 1, PersistedFaceId = addResult.PersistedFaceId }, null);
                }
            }

            return bestMatch?.Item1;
        }
    }
}
