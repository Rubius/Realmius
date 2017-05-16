using System;
using System.IO;
using System.Windows;
using MessageClient.Models;
using MessageClient.Properties;
using Realmius;
using Realmius.Contracts.Models;
using Realmius.SyncService;
using Realms;
using Realms.Schema;

namespace MessageClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private string _url = "http://localhost:53960";

        private string _realmFileName;

        private static IRealmiusSyncService _syncService;

        public App()
        {
            _realmFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            RealmiusSyncService.RealmiusDbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "sync");
            InitializeRealmSync();
        }

        public Realm GetRealm()
        {
            return Realm.GetInstance(_realmFileName);
        }

        protected internal virtual void InitializeRealmSync()
        {
            _syncService = CreateSyncService();
            _syncService.Unauthorized += SyncServiceOnUnauthorized;
            _syncService.DataDownloaded += SyncServiceOnDataDownloaded;
            //_syncService.FileUploaded += SyncServiceOnFileUploaded;
        }

        private void SyncServiceOnDataDownloaded(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void SyncServiceOnUnauthorized(object sender, UnauthorizedResponse e)
        {
            throw new NotImplementedException();
        }

        protected internal virtual IRealmiusSyncService CreateSyncService()
        {
            var syncService = SyncServiceFactory.CreateUsingSignalR(
                GetRealm,
                new Uri(_url + $"/signalr"), "SignalRSyncHub",
                new[]
                {
                    typeof(User),
                    typeof(Message)
                });

            //Task.Factory.StartNew(DownloadItemModels);
            
            return syncService;
        }
    }
}
