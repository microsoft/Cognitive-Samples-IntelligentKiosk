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

using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceHelpers
{
    public class FaceServiceHelper
    {
        public readonly static int RetryCountOnQuotaLimitError = 6;
        public readonly static int RetryDelayOnQuotaLimitError = 500;

        private static FaceClient faceClient { get; set; }

        public static Action Throttled;

        private static string apiKey;
        public static string ApiKey
        {
            get { return apiKey; }
            set
            {
                var changed = apiKey != value;
                apiKey = value;
                if (changed)
                {
                    InitializeFaceServiceClient();
                }
            }
        }

        private static string apiEndpoint;
        public static string ApiEndpoint
        {
            get { return apiEndpoint; }
            set
            {
                var changed = apiEndpoint != value;
                apiEndpoint = value;
                if (changed)
                {
                    InitializeFaceServiceClient();
                }
            }
        }

        static FaceServiceHelper()
        {
            InitializeFaceServiceClient();
        }

        private static void InitializeFaceServiceClient()
        {
            bool hasEndpoint = !string.IsNullOrEmpty(ApiEndpoint) ? Uri.IsWellFormedUriString(ApiEndpoint, UriKind.Absolute) : false;
            faceClient = !hasEndpoint
                ? new FaceClient(new ApiKeyServiceClientCredentials(ApiKey))
                : new FaceClient(new ApiKeyServiceClientCredentials(ApiKey))
                {
                    Endpoint = ApiEndpoint
                };
        }

        private static async Task<TResponse> RunTaskWithAutoRetryOnQuotaLimitExceededError<TResponse>(Func<Task<TResponse>> action)
        {
            int retriesLeft = RetryCountOnQuotaLimitError;
            int delay = RetryDelayOnQuotaLimitError;

            TResponse response = default(TResponse);

            while (true)
            {
                try
                {
                    response = await action();
                    break;
                }
                catch (APIErrorException exception) when (exception.Response.StatusCode == (System.Net.HttpStatusCode)429 && retriesLeft > 0)
                {
                    ErrorTrackingHelper.TrackException(exception, "Face API throttling error");
                    if (retriesLeft == 1 && Throttled != null)
                    {
                        Throttled();
                    }

                    await Task.Delay(delay);
                    retriesLeft--;
                    delay *= 2;
                    continue;
                }
            }

            return response;
        }

        private static async Task RunTaskWithAutoRetryOnQuotaLimitExceededError(Func<Task> action)
        {
            await RunTaskWithAutoRetryOnQuotaLimitExceededError<object>(async () => { await action(); return null; });
        }

        public static async Task CreatePersonGroupAsync(string personGroupId, string name, string userData)
        {
            await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.PersonGroup.CreateAsync(personGroupId, name, userData));
        }

        public static async Task<IList<Person>> GetPersonsAsync(string personGroupId)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.PersonGroupPerson.ListAsync(personGroupId));
        }

        public static async Task<IList<DetectedFace>> DetectWithStreamAsync(Func<Task<Stream>> imageStreamCallback, bool returnFaceId = true, bool returnFaceLandmarks = false, IList<FaceAttributeType> returnFaceAttributes = null)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await faceClient.Face.DetectWithStreamAsync(await imageStreamCallback(), returnFaceId, returnFaceLandmarks, returnFaceAttributes));
        }

        public static async Task<IList<DetectedFace>> DetectWithUrlAsync(string url, bool returnFaceId = true, bool returnFaceLandmarks = false, IList<FaceAttributeType> returnFaceAttributes = null)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.Face.DetectWithUrlAsync(url, returnFaceId, returnFaceLandmarks, returnFaceAttributes));
        }

        public static async Task<PersistedFace> GetPersonFaceAsync(string personGroupId, Guid personId, Guid face)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.PersonGroupPerson.GetFaceAsync(personGroupId, personId, face));
        }

        public static async Task<IEnumerable<PersonGroup>> ListPersonGroupsAsync(string userDataFilter = null)
        {
            return (await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.PersonGroup.ListAsync())).Where(group => string.Equals(group.UserData, userDataFilter));
        }

        public static async Task<IEnumerable<FaceList>> GetFaceListsAsync(string userDataFilter = null)
        {
            return (await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.FaceList.ListAsync())).Where(list => string.Equals(list.UserData, userDataFilter));
        }

        public static async Task<PersistedFace> GetFaceInLargeFaceListAsync(string largeFaceListId, Guid persistedFaceId)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.LargeFaceList.GetFaceAsync(largeFaceListId, persistedFaceId));
        }

        public static async Task<IList<PersistedFace>> ListFacesInLargeFaceListAsync(string largeFaceListId, string start, int count)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.LargeFaceList.ListFacesAsync(largeFaceListId, start, count));
        }

        public static async Task<LargeFaceList> GetLargeFaceListAsync(string faceId)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.LargeFaceList.GetAsync(faceId));
        }

        public static async Task DeleteLargeFaceListAsync(string faceId)
        {
            await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.LargeFaceList.DeleteAsync(faceId));
        }

        public static async Task<IList<SimilarFace>> FindSimilarAsync(Guid faceId, string faceListId, int maxNumOfCandidatesReturned = 1)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.Face.FindSimilarAsync(faceId, faceListId, maxNumOfCandidatesReturned: maxNumOfCandidatesReturned));
        }

        public static async Task<IList<SimilarFace>> FindSimilarAsync(Guid faceId, string faceListId, string largeFaceListId, FindSimilarMatchMode matchMode, int maxNumOfCandidatesReturned = 1)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.Face.FindSimilarAsync(faceId, faceListId, largeFaceListId, mode: matchMode, maxNumOfCandidatesReturned: maxNumOfCandidatesReturned));
        }

        public static async Task<PersistedFace> AddFaceToFaceListFromStreamAsync(string faceListId, Func<Task<Stream>> imageStreamCallback, FaceRectangle targetFaceRect)
        {
            IList<int> targetFace = targetFaceRect != null ? new List<int>() { targetFaceRect.Left, targetFaceRect.Top, targetFaceRect.Width, targetFaceRect.Height } : null;
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await faceClient.FaceList.AddFaceFromStreamAsync(faceListId, await imageStreamCallback(), null, targetFace));
        }

        public static async Task<PersistedFace> AddFaceToLargeFaceListFromStreamAsync(string faceListId, Func<Task<Stream>> imageStreamCallback, string userData, FaceRectangle targetFaceRect)
        {
            IList<int> targetFace = targetFaceRect != null ? new List<int>() { targetFaceRect.Left, targetFaceRect.Top, targetFaceRect.Width, targetFaceRect.Height } : null;
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await faceClient.LargeFaceList.AddFaceFromStreamAsync(faceListId, await imageStreamCallback(), null, targetFace));
        }

        public static async Task<PersistedFace> AddFaceToLargeFaceListFromUrlAsync(string faceListId, string imageUrl, string userData, FaceRectangle targetFaceRect)
        {
            IList<int> targetFace = targetFaceRect != null ? new List<int>() { targetFaceRect.Left, targetFaceRect.Top, targetFaceRect.Width, targetFaceRect.Height } : null;
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await faceClient.LargeFaceList.AddFaceFromUrlAsync(faceListId, imageUrl, userData, targetFace));
        }

        public static async Task CreateFaceListAsync(string faceListId, string name, string userData)
        {
            await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.FaceList.CreateAsync(faceListId, name, userData));
        }

        public static async Task CreateLargeFaceListAsync(string faceListId, string name, string userData)
        {
            await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.LargeFaceList.CreateAsync(faceListId, name, userData));
        }

        public static async Task DeleteFaceFromLargeFaceListAsync(string largeFaceListId, Guid persistedFaceId)
        {
            await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.LargeFaceList.DeleteFaceAsync(largeFaceListId, persistedFaceId));
        }

        public static async Task DeleteFaceListAsync(string faceListId)
        {
            await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.FaceList.DeleteAsync(faceListId));
        }

        public static async Task UpdatePersonGroupsAsync(string personGroupId, string name, string userData)
        {
            await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.PersonGroup.UpdateAsync(personGroupId, name, userData));
        }

        public static async Task<PersistedFace> AddPersonFaceFromUrlAsync(string personGroupId, Guid personId, string imageUrl, string userData, FaceRectangle targetFaceRect)
        {
            IList<int> targetFace = targetFaceRect != null ? new List<int>() { targetFaceRect.Left, targetFaceRect.Top, targetFaceRect.Width, targetFaceRect.Height } : null;
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await faceClient.PersonGroupPerson.AddFaceFromUrlAsync(personGroupId, personId, imageUrl, userData, targetFace));
        }

        public static async Task<PersistedFace> AddPersonFaceFromStreamAsync(string personGroupId, Guid personId, Func<Task<Stream>> imageStreamCallback, string userData, FaceRectangle targetFaceRect)
        {
            IList<int> targetFace = targetFaceRect != null ? new List<int>() { targetFaceRect.Left, targetFaceRect.Top, targetFaceRect.Width, targetFaceRect.Height } : null;
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await faceClient.PersonGroupPerson.AddFaceFromStreamAsync(personGroupId, personId, await imageStreamCallback(), userData, targetFace));
        }

        public static async Task<IList<IdentifyResult>> IdentifyAsync(string personGroupId, Guid[] detectedFaceIds)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.Face.IdentifyAsync(detectedFaceIds, personGroupId));
        }

        public static async Task DeletePersonAsync(string personGroupId, Guid personId)
        {
            await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.PersonGroupPerson.DeleteAsync(personGroupId, personId));
        }

        public static async Task<Person> CreatePersonAsync(string personGroupId, string name)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await faceClient.PersonGroupPerson.CreateAsync(personGroupId, name));
        }

        public static async Task<Person> GetPersonAsync(string personGroupId, Guid personId)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.PersonGroupPerson.GetAsync(personGroupId, personId));
        }

        public static async Task TrainPersonGroupAsync(string personGroupId)
        {
            await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.PersonGroup.TrainAsync(personGroupId));
        }

        public static async Task TrainLargeFaceListAsync(string largeFaceListId)
        {
            await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.LargeFaceList.TrainAsync(largeFaceListId));
        }

        public static async Task DeletePersonGroupAsync(string personGroupId)
        {
            await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.PersonGroup.DeleteAsync(personGroupId));
        }

        public static async Task DeletePersonFaceAsync(string personGroupId, Guid personId, Guid faceId)
        {
            await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.PersonGroupPerson.DeleteFaceAsync(personGroupId, personId, faceId));
        }

        public static async Task<TrainingStatus> GetPersonGroupTrainingStatusAsync(string personGroupId)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.PersonGroup.GetTrainingStatusAsync(personGroupId));
        }

        public static async Task<TrainingStatus> GetLargeFaceListTrainingStatusAsync(string largeFaceListId)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.LargeFaceList.GetTrainingStatusAsync(largeFaceListId));
        }
    }
}
