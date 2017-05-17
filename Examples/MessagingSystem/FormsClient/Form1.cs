using System;
using System.IO;
using System.Linq;
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

        private User _user;

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
            messagesBox.Invoke((MethodInvoker) delegate
            {
                messagesBox.Text = "SYSTEM: DataDownloaded" + Environment.NewLine;
                var realm = GetRealm();

                foreach (var message in realm.All<Message>())
                {
                    messagesBox.AppendText($"{message.UserId} : {message.Text}"+ Environment.NewLine);
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
            var email = "test@test.com";
            var key = "123";
            var deviceId = clientID.Text;

            var syncService = SyncServiceFactory.CreateUsingSignalR(
                GetRealm,
                new Uri(_url + $"/signalr?email={email}&authKey={key}&deviceId={deviceId}"),
                "SignalRSyncHub",
                new[]
                {
                    typeof(User),
                    typeof(Message)
                });

            syncService.Unauthorized += (sender, response) => { messagesBox.AppendText("SYSTEM: User not authorized!" + Environment.NewLine); };
            
            return syncService;
        }

        private void CreateNewUser_Click(object sender, EventArgs e)
        {
            _user = new User {Nickname = usernameBox.Text};
            var realm = GetRealm();

            realm.Write(() => realm.Add(_user));
            realm.Refresh();
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
            var msg = new Message {Text = messageBox.Text, UserId = clientID.Text };
            var realm = GetRealm();

            realm.Write(() => realm.Add(msg));
            realm.Refresh();

            messagesBox.AppendText($"{msg.UserId} : {msg.Text}" + Environment.NewLine);
        }
    }
}
