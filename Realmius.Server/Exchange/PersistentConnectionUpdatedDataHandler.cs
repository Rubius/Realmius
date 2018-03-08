using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Realmius.Contracts.Models;
using Realmius.Contracts.SignalR;
using Realmius.Server.QuickStart;

namespace Realmius.Server.Exchange
{
    internal class PersistentConnectionUpdatedDataHandler
    {
        internal static void HandleDataChanges<TUser>(object sender, UpdatedDataBatch updatedDataBatch)
        {
            var hubContext = RealmiusServer.ServiceProvider.GetService<IHubContext<RealmiusPersistentConnection<TUser>>>();

            foreach (var item in updatedDataBatch.Items)
            {
                var download = new DownloadDataResponse();
                download.ChangedObjects.Add(item.DownloadResponseItem);
                var date = DateTimeOffset.UtcNow;

                var tags = new List<string>
                {
                    item.Tag0, item.Tag1, item.Tag2, item.Tag3
                };

                download.LastChange = tags.Where(x => x != null).Distinct().ToDictionary(x => x, x => date);
                //var msg = RealmiusPersistentConnection<TUser>.Serialize(download);
                foreach (var tag in tags.Where(x => !string.IsNullOrEmpty(x)))
                {
                    hubContext.Clients.Group(tag).SendAsync("DataDownloaded", download);
                }
                //hubContext.Groups.Send(tags, $"{MethodConstants.ClientDataDownloaded}{msg}");
            }
        }
    }
}