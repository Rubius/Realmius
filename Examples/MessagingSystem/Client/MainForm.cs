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
using Realmius.SyncService;
using Realms;
using Message = Client.Models.Message;

namespace Client
{
    public partial class MainForm : Form
    {
        private string _serverUrl = "http://localhost:45000";

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

        protected internal virtual void InitializeRealmius()
        {
            _syncService = CreateSyncService();
            var realm = GetRealm();
            realm.All<Message>().SubscribeForNotifications((collection, y, z) =>
              {
                  if (y?.InsertedIndices != null)
                  {
                      foreach (var change in y.InsertedIndices)
                      {
                          messagesBox.AppendText(FormatMessage(collection[change]));
                      }
                  }
              });
        }

        protected internal virtual IRealmiusSyncService CreateSyncService()
        {
            var clientId = clientID.Text;

            var syncService = SyncServiceFactory.CreateUsingSignalR(
                GetRealm,
                new Uri(_serverUrl + $"/Realmius?clientId={clientId}"),
                new[]
                {
                    typeof(Message)
                });

            return syncService;
        }

        private void Connect_Click(object sender, EventArgs e)
        {
            var path = Path.GetTempPath();
            var name = Guid.NewGuid().ToString();
            _realmFileName = Path.Combine(path, name);
            RealmiusSyncService.RealmiusDbPath = Path.Combine(path, name + "sync");
            InitializeRealmius();
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_realmFileName))
            {
                MessageBox.Show("Please hit Connect first");
                return;
            }

            var msg = new Message { Text = messageBox.Text, ClientId = clientID.Text, DateTime = DateTimeOffset.Now };
            var realm = GetRealm();

            realm.Write(() => realm.Add(msg));

            messageBox.Text = string.Empty;
        }

        private string FormatMessage(Message msg)
        {
            return $"{msg.ClientId} at {msg.DateTime} : {msg.Text}" + Environment.NewLine;
        }

        private void messageBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                sendButton_Click(this, e);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }
    }
}
