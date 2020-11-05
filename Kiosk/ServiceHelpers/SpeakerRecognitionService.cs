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

using Microsoft.CognitiveServices.Speech;
using ServiceHelpers.Models;
using ServiceHelpers.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceHelpers
{
    public class SpeakerRecognitionService : ServiceBase
    {
        private readonly string HEADER_SUB_KEY = "Ocp-Apim-Subscription-Key";
        private readonly string SERVICE_URL_FORMAT = "{0}/speaker";
        private readonly string SPEAKER_VERIFICATION_URL = "/verification/v2.0/text-independent/profiles";
        private readonly string SPEAKER_IDENTIFICATION_URL = "/identification/v2.0/text-independent/profiles";

        public SpeakerRecognitionService(string subscriptionKey, string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint) == true)
            {
                throw new ArgumentNullException("Endpoint is not initialized.");
            }

            if (string.IsNullOrEmpty(subscriptionKey) == true)
            {
                throw new ArgumentNullException("Subscription key is not initialized.");
            }

            this.BaseServiceUrl = string.Format(SERVICE_URL_FORMAT, endpoint);
            this.RequestHeaders = new Dictionary<string, string>()
            {
                {  this.HEADER_SUB_KEY, subscriptionKey }
            };
        }

        public async Task<SpeakerProfileInfo> GetProfileAsync(string profileId, VoiceProfileType profileType)
        {
            string speakerUrl = profileType == VoiceProfileType.TextIndependentVerification ? this.SPEAKER_VERIFICATION_URL : this.SPEAKER_IDENTIFICATION_URL;
            Uri requestUri = new Uri($"{this.BaseServiceUrl}{speakerUrl}/{profileId}");
            return await HttpClientUtility.GetAsync<SpeakerProfileInfo>(requestUri, this.RequestHeaders);
        }

        public async Task<SpeakerProfilesResponse> GetProfilesAsync(VoiceProfileType profileType)
        {
            string speakerUrl = profileType == VoiceProfileType.TextIndependentVerification ? this.SPEAKER_VERIFICATION_URL : this.SPEAKER_IDENTIFICATION_URL;
            Uri requestUri = new Uri($"{this.BaseServiceUrl}{speakerUrl}");
            return await HttpClientUtility.GetAsync<SpeakerProfilesResponse>(requestUri, this.RequestHeaders);
        }

        public async Task DeleteProfileAsync(string profileId, VoiceProfileType profileType)
        {
            string speakerUrl = profileType == VoiceProfileType.TextIndependentVerification ? this.SPEAKER_VERIFICATION_URL : this.SPEAKER_IDENTIFICATION_URL;
            Uri requestUri = new Uri($"{this.BaseServiceUrl}{speakerUrl}/{profileId}");
            await HttpClientUtility.DeleteAsync(requestUri, this.RequestHeaders);
        }
    }
}
