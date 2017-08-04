using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Realmius_mancheck.RealmEntities;
using Realms;
using Xamarin.Forms;

namespace Realmius_mancheck.ViewModel
{
    public class ChatPageViewModel : RootViewModel
    {
        public IRealmCollection<ChatMessageRealm> SourceMessages { get; set; }

        public List<ChatMessageRealm> Messages { get; set; }

        public string NewMessageText { get; set; }

        public ICommand SendCommand { get; set; }

        public ChatPageViewModel()
        {
            SendCommand = new Command(SendMessage);
            InitData();
        }

        public ChatMessageRealm SelectedMessage
        {
            get { return null; }
            set { }
        }

        private void InitData()
        {
            var realmMessages = App.GetRealm().All<ChatMessageRealm>();
            SourceMessages = realmMessages.AsRealmCollection();

            realmMessages.SubscribeForNotifications((o, y, e) =>
            {
                Messages = SourceMessages.OrderByDescending(x => x.CreatingDateTime).ToList();
                OnPropertyChanged(nameof(Messages));
            });
        }

        private void SendMessage()
        {
            if (String.IsNullOrWhiteSpace(NewMessageText))
                return;

            var newMessage = new ChatMessageRealm(NewMessageText);

            var realm = App.GetRealm();
            realm.Write(() =>
            {
                realm.Add(newMessage);
            });
            NewMessageText = String.Empty;
            OnPropertyChanged(nameof(NewMessageText));
        }

        public void Refresh()
        {
            InitData();
            OnPropertyChanged(nameof(Messages));
        }
    }
}
