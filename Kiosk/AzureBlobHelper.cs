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

using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace IntelligentKioskSample
{
    class AzureBlobHelper
    {
        public static CloudBlobContainer GetCloudBlobContainer(string storageAccount, string storageKey, string containerName)
        {
            CloudStorageAccount cloudStorageAccount = new CloudStorageAccount(new StorageCredentials(storageAccount, storageKey), true);
            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();
            return blobClient?.GetContainerReference(containerName);
        }

        public static async Task UploadStorageFilesToContainerAsync(IEnumerable<StorageFile> storageFiles, CloudBlobContainer container)
        {
            await container.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Blob, new BlobRequestOptions(), new OperationContext());

            // Upload our images
            foreach (StorageFile storageFile in storageFiles)
            {
                CloudBlockBlob imageBlob = container.GetBlockBlobReference(storageFile.Name);
                using (var stream = await storageFile.OpenStreamForReadAsync())
                {
                    await imageBlob.UploadFromStreamAsync(stream);
                }
            }
        }

        public static string GetContainerSasToken(CloudBlobContainer container, int sharedAccessStartTimeInMinutes, int sharedAccessExpiryTimeInMinutes)
        {
            SharedAccessBlobPolicy sasPolicy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-sharedAccessStartTimeInMinutes),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(sharedAccessExpiryTimeInMinutes)
            };

            return $"{container.Uri}{container.GetSharedAccessSignature(sasPolicy)}";
        }
    }
}
