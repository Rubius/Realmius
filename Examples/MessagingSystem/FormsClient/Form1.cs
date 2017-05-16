﻿using System;
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
            messagesBox.Invoke((MethodInvoker) delegate { messagesBox.AppendText("SYSTEM: DataDownloaded"); });
        }

        private void SyncServiceOnUnauthorized(object sender, UnauthorizedResponse e)
        {
            messagesBox.Invoke((MethodInvoker)delegate { messagesBox.AppendText("SYSTEM: DataDownloaded"); });
        }

        protected internal virtual IRealmiusSyncService CreateSyncService()
        {
            var email = "test@test.com";
            var key = "123";
            var deviceId = "Device1";

            var syncService = SyncServiceFactory.CreateUsingSignalR(
                GetRealm,
                new Uri(_url + $"/signalr?email={email}&authKey={key}&deviceId={deviceId}"),
                "SignalRSyncHub",
                new[]
                {
                    typeof(User),
                    typeof(Message)
                });

            syncService.Unauthorized += (sender, response) => { messagesBox.AppendText("SYSTEM: User not authorized!"); };
            
            return syncService;
        }

        private void CreateNewUser_Click(object sender, EventArgs e)
        {
            var tmpUser = new User {Name = usernameBox.Text};
            var realm = GetRealm();

            realm.Write(() => realm.Add(tmpUser));
            realm.Refresh();
        }

        private void Connect_Click(object sender, EventArgs e)
        {
            _realmFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            RealmiusSyncService.RealmiusDbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "sync");
            InitializeRealmSync();
        }
    }
}
