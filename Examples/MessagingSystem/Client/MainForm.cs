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
using System.IO;
using System.Windows.Forms;
using Realmius;
using Realmius.Contracts.Models;
using Realmius.SyncService;
using Realms;
using Message = Client.Models.Message;

namespace Client
{
    public partial class MainForm : Form
    {
        private string _url = "http://localhost:45000";
        
        private string _realmFileName;

        private static IRealmiusSyncService _syncService;

        public MainForm()
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
        }

        private void SyncServiceOnDataDownloaded(object sender, EventArgs e)
        {
            messagesBox.Invoke((MethodInvoker) delegate
            {
                messagesBox.Text = "SYSTEM: DataDownloaded" + Environment.NewLine;
                var realm = GetRealm();

                foreach (var message in realm.All<Message>())
                {
                    messagesBox.AppendText($"{message.ClientId} : {message.Text}"+ Environment.NewLine);
                }
                
                realm.Refresh();
            });
        }

        private void SyncServiceOnUnauthorized(object sender, UnauthorizedResponse e)
        {
            messagesBox.Invoke((MethodInvoker) delegate
            {
                messagesBox.AppendText("SYSTEM: DataDownloaded" + Environment.NewLine);
            });
        }

        protected internal virtual IRealmiusSyncService CreateSyncService()
        {
            var clientId = clientID.Text;

            var syncService = SyncServiceFactory.CreateUsingSignalR(
                GetRealm,
                new Uri(_url + $"/signalr?clientId={clientId}"),
                "SignalRSyncHub",
                new[]
                {
                    typeof(Message)
                },
                deleteDatabase:true);

            syncService.Unauthorized += (sender, response) => { messagesBox.AppendText("SYSTEM: User not authorized!" + Environment.NewLine); };
            
            return syncService;
        }

        private void Connect_Click(object sender, EventArgs e)
        {
            var path = @"D:\realms\"; // Path.GetTempPath();
            var name = $"client-{clientID.Text}.db"; // Guid.NewGuid().ToString();
            _realmFileName = Path.Combine(path, name);
            RealmiusSyncService.RealmiusDbPath = Path.Combine(path, name + "sync");
            //RealmiusSyncService.RealmiusDbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "sync");
            InitializeRealmSync();
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            var msg = new Message {Text = messageBox.Text, ClientId = clientID.Text };
            var realm = GetRealm();

            realm.Write(() => realm.Add(msg));
            realm.Refresh();

            messagesBox.AppendText($"{msg.ClientId} : {msg.Text}" + Environment.NewLine);

            messageBox.Text = string.Empty;
        }

        private void messageBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                sendButton_Click(this, e);
            }
        }
    }
}
