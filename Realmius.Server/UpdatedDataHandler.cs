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
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR;
using Realmius.Contracts;
using Realmius.Contracts.Models;

namespace Realmius.Server
{
    internal class UpdatedDataHandler
    {
        internal static void HandleDataChanges(object sender, UpdatedDataBatch updatedDataBatch)
        {
            var hubContext = GlobalHost.ConnectionManager.GetHubContext(Constants.SignalRHubName);
            foreach (var item in updatedDataBatch.Items)
            {
                var download = new DownloadDataResponse()
                {
                };
                download.ChangedObjects.Add(item.DownloadResponseItem);
                var date = DateTimeOffset.UtcNow;

                var tags = new List<string>()
                {
                    item.Tag0, item.Tag1, item.Tag2, item.Tag3
                };
                
                download.LastChange = tags.Where(x => x != null).Distinct().ToDictionary(x => x, x => date);
                hubContext.Clients.Groups(tags).DataDownloaded(download);
            }
        }

    }
}