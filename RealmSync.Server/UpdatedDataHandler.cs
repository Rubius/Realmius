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