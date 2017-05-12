////////////////////////////////////////////////////////////////////////////
//
// Copyright 2017 Rubius
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;
using Realmius.Contracts.Models;
using Realmius.SyncService.RealmModels;

namespace Realmius.SyncService
{
    public interface IRealmiusService : IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Raised by the SyncServer when backend tells us we are not authorized to upload
        /// </summary>
        event EventHandler<UnauthorizedResponse> Unauthorized;
        event EventHandler DataDownloaded;
        event EventHandler<FileUploadedEventArgs> FileUploaded;

        bool UploadInProgress { get; }

        Uri ServerUri { get; set; }
        SyncState GetSyncState(Type type, string mobilePrimaryKey);

        /// <summary>
        /// those objects that were added to Realm before Realmius started
        /// </summary>
        void AttachNotLoadedObjects();

        /// <summary>
        /// delete database and start sync from scratch
        /// </summary>
        void DeleteDatabase();

        /// <summary>
        /// Upload to the server: "the object was removed"
        /// </summary>
        /// <param name="realmObject"></param>
        void RemoveObject(IRealmiusObjectClient realmObject);

        /// <summary>
        /// do not sync to the server changes made to the passed object
        /// </summary>
        void SkipUpload(IRealmiusObjectClient realmObject);

        #region File uploading
        string FileUploadUrl { get; set; }
        SyncState GetFileSyncState(string mobilePrimaryKey);

        /// <summary>
        /// puts the file uploading task to the queue
        /// </summary>
        /// <param name="pathToFile">path to file</param>
        /// <param name="queryParams">query parameters to pass to the server</param>
        /// <param name="fileUploadUrl">uri to upload file to (FileUploadUrl is used as default)</param>
        /// <param name="fileParameterName">server-side parameter name for a file (FileParameterName is used as default)</param>
        /// <param name="additionalInfo">any information that will be stored along with the file and passed back in </param>
        void QueueFileUpload(string pathToFile, string queryParams = "", string fileUploadUrl = null, string fileParameterName = null, string additionalInfo = null);
        #endregion
    }
}