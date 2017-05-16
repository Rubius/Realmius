using System;
using System.IO;
using System.Windows.Forms;
using MessageClient.Models;
using Realmius;
using Realmius.Contracts.Models;
using Realmius.SyncService;
using Realms;
using Message = MessageClient.Models.Message;

namespace FormsClient
{
    public partial class Form1 : Form
    {
        private string _url = "http://localhost:45000";

        private string _realmFileName;

        private static IRealmiusSyncService _syncService;

        public Form1()
        {
            InitializeComponent();
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
            
            return syncService;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var tmpUser = new User {Name = "Me"};
            var realm = GetRealm();

            realm.Write(() => realm.Add(tmpUser));
            realm.Refresh();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _realmFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            RealmiusSyncService.RealmiusDbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "sync");
            InitializeRealmSync();
        }
    }
}
