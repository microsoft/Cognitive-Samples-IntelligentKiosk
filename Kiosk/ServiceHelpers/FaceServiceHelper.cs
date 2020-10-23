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
using Windows.Storage;

namespace ServiceHelpers
{
    public class FaceServiceHelper
    {
        public const string LatestRecognitionModelName = "recognition_02";

        public static readonly FaceAttributeType[] AllFaceAttributeTypes = new FaceAttributeType[]
        {
            FaceAttributeType.Age,
            FaceAttributeType.Gender,
            FaceAttributeType.HeadPose,
            FaceAttributeType.Smile,
            FaceAttributeType.FacialHair,
            FaceAttributeType.Glasses,
            FaceAttributeType.Emotion,
            FaceAttributeType.Hair,
            FaceAttributeType.Makeup,
            FaceAttributeType.Occlusion,
            FaceAttributeType.Accessories,
            FaceAttributeType.Blur,
            FaceAttributeType.Exposure,
            FaceAttributeType.Noise
        };

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
            await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.PersonGroup.CreateAsync(personGroupId, name, userData, recognitionModel: LatestRecognitionModelName));
        }

        public static async Task<IList<Person>> GetPersonsAsync(string personGroupId)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.PersonGroupPerson.ListAsync(personGroupId));
        }

        public static async Task<IList<DetectedFace>> DetectWithStreamAsync(Func<Task<Stream>> imageStreamCallback, bool returnFaceId = true, bool returnFaceLandmarks = false, IList<FaceAttributeType> returnFaceAttributes = null)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await faceClient.Face.DetectWithStreamAsync(await imageStreamCallback(), returnFaceId, returnFaceLandmarks, returnFaceAttributes, recognitionModel: LatestRecognitionModelName, returnRecognitionModel: true));
        }

        public static async Task<IList<DetectedFace>> DetectWithUrlAsync(string url, bool returnFaceId = true, bool returnFaceLandmarks = false, IList<FaceAttributeType> returnFaceAttributes = null)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.Face.DetectWithUrlAsync(url, returnFaceId, returnFaceLandmarks, returnFaceAttributes, recognitionModel: LatestRecognitionModelName, returnRecognitionModel: true));
        }

        public static async Task<PersistedFace> GetPersonFaceAsync(string personGroupId, Guid personId, Guid face)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.PersonGroupPerson.GetFaceAsync(personGroupId, personId, face));
        }

        public static async Task<IEnumerable<PersonGroup>> ListPersonGroupsAsync(string userDataFilter = null)
        {
            return (await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.PersonGroup.ListAsync(returnRecognitionModel: true))).Where(group => string.Equals(group.UserData, userDataFilter));
        }

        public static async Task<IEnumerable<FaceList>> GetFaceListsAsync(string userDataFilter = null)
        {
            return (await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.FaceList.ListAsync(returnRecognitionModel: true))).Where(list => string.Equals(list.UserData, userDataFilter));
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
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.LargeFaceList.GetAsync(faceId, returnRecognitionModel: true));
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

        public static async Task<PersistedFace> AddFaceToFaceListFromUrlAsync(string faceListId, string imageUrl, FaceRectangle targetFaceRect)
        {
            IList<int> targetFace = targetFaceRect != null ? new List<int>() { targetFaceRect.Left, targetFaceRect.Top, targetFaceRect.Width, targetFaceRect.Height } : null;
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await faceClient.FaceList.AddFaceFromUrlAsync(faceListId, imageUrl, null, targetFace));
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
            await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.FaceList.CreateAsync(faceListId, name, userData, recognitionModel: LatestRecognitionModelName));
        }

        public static async Task CreateLargeFaceListAsync(string faceListId, string name, string userData)
        {
            await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.LargeFaceList.CreateAsync(faceListId, name, userData, recognitionModel: LatestRecognitionModelName));
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

        public static async Task UpdatePersonGroupsWithNewRecModelAsync(PersonGroup oldPersonGroup, string userDataFilder, IProgress<FaceIdentificationModelUpdateStatus> progress = null)
        {
            try
            {

                bool allPeopleHaveAtLeastOneFaceMigrated = true;

                // just make sure the person group use previous recognition model
                bool isOldPersonGroup = oldPersonGroup?.RecognitionModel != null && !oldPersonGroup.RecognitionModel.Equals(LatestRecognitionModelName, StringComparison.OrdinalIgnoreCase);

                // get persons
                IList<Person> personsInGroup = isOldPersonGroup ? await GetPersonsAsync(oldPersonGroup.PersonGroupId) : new List<Person>();
                if (personsInGroup.Any())
                {
                    // create new person group
                    string newPersonGroupId = Guid.NewGuid().ToString();
                    await CreatePersonGroupAsync(newPersonGroupId, oldPersonGroup.Name, userDataFilder);

                    // create new persons
                    var newPersonList = new List<Tuple<Person, Person>>();
                    foreach (Person oldPerson in personsInGroup)
                    {
                        Person newPerson = await CreatePersonAsync(newPersonGroupId, oldPerson.Name);
                        newPersonList.Add(new Tuple<Person, Person>(oldPerson, newPerson));
                    }

                    // add face images
                    foreach (var (personItem, index) in newPersonList.Select((v, i) => (v, i)))
                    {
                        Person oldPerson = personItem.Item1;
                        Person newPerson = personItem.Item2;

                        // get face images from the old model
                        var personFaces = new List<PersistedFace>();
                        foreach (Guid face in oldPerson.PersistedFaceIds)
                        {
                            PersistedFace personFace = await GetPersonFaceAsync(oldPersonGroup.PersonGroupId, oldPerson.PersonId, face);
                            personFaces.Add(personFace);
                        }

                        bool addedAtLeastOneFaceImageForPerson = false;
                        // add face images to the new model
                        foreach (PersistedFace persistedFace in personFaces)
                        {
                            try
                            {
                                bool isUri = !string.IsNullOrEmpty(persistedFace.UserData) ? Uri.IsWellFormedUriString(persistedFace.UserData, UriKind.Absolute) : false;
                                if (isUri)
                                {
                                    await AddPersonFaceFromUrlAsync(newPersonGroupId, newPerson.PersonId, imageUrl: persistedFace.UserData, userData: persistedFace.UserData, targetFaceRect: null);
                                }
                                else
                                {
                                    StorageFile localImage = await StorageFile.GetFileFromPathAsync(persistedFace.UserData);
                                    await AddPersonFaceFromStreamAsync(newPersonGroupId, newPerson.PersonId, imageStreamCallback: localImage.OpenStreamForReadAsync, userData: localImage.Path, targetFaceRect: null);
                                }

                                addedAtLeastOneFaceImageForPerson = true;
                            }
                            catch { /* Ignore the error and continue. Other images might work */ }
                        }

                        if (!addedAtLeastOneFaceImageForPerson)
                        {
                            allPeopleHaveAtLeastOneFaceMigrated = false;
                        }

                        progress?.Report(new FaceIdentificationModelUpdateStatus { State = FaceIdentificationModelUpdateState.Running, Count = index + 1, Total = personsInGroup.Count });
                    }

                    // delete old person group
                    await DeletePersonGroupAsync(oldPersonGroup.PersonGroupId);

                    // train new person group
                    await TrainPersonGroupAsync(newPersonGroupId);
                }

                progress?.Report(new FaceIdentificationModelUpdateStatus { State = allPeopleHaveAtLeastOneFaceMigrated ? FaceIdentificationModelUpdateState.Complete : FaceIdentificationModelUpdateState.CompletedWithSomeEmptyPeople });
            }
            catch (Exception ex)
            {
                ErrorTrackingHelper.TrackException(ex, "Face API: Update PersonGroup using new recognition model error");
                progress?.Report(new FaceIdentificationModelUpdateStatus { State = FaceIdentificationModelUpdateState.Error });
            }
        }
    }

    public class FaceIdentificationModelUpdateStatus
    {
        public FaceIdentificationModelUpdateState State { get; set; }

        public int Count { get; set; }

        public int Total { get; set; }
    }

    public enum FaceIdentificationModelUpdateState
    {
        NotStarted,
        Running,
        Complete,
        Error,
        CompletedWithSomeEmptyPeople
    }
}
