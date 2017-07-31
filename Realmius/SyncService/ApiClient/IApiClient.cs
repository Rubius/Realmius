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
using System.Threading.Tasks;
using Realmius.Contracts.Models;

namespace Realmius.SyncService.ApiClient
{
    public interface IApiClient
    {
        event EventHandler<DownloadDataResponse> NewDataDownloaded;
        event EventHandler<UnauthorizedResponse> Unauthorized;

        Task Start(ApiClientStartOptions startOptions);
        void Stop();
        Task<UploadDataResponse> UploadData(UploadDataRequest request);

        bool IsConnected { get; }
        /// <summary>
        /// Should be raised when IsConnected flag is changed (especially important when it changes from false to true) 
        /// </summary>
        event EventHandler ConnectedStateChanged;

        void UpdateOptions(ApiClientStartOptions options);
    }
}