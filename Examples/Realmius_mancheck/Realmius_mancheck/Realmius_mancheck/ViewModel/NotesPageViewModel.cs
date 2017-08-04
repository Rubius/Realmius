using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Realmius;
using Realmius_mancheck.RealmEntities;
using Realms;
using Xamarin.Forms;

namespace Realmius_mancheck.ViewModel
{
    public class NotesPageViewModel : RootViewModel
    {
        public IRealmCollection<NoteRealm> Notes { get; set; } 

        public string NewNoteTitle { get; set; }

        public string NewNoteDescription { get; set; }

        public ICommand AddNoteCommand { get; set; }

        public ICommand RemoveNoteCommand { get; set; }

        public NotesPageViewModel()
        {
            AddNoteCommand = new Command(AddNote);
            RemoveNoteCommand  = new Command<string>(RemoveNote);
            InitData();
        }

        private void InitData()
        {
            var realmNotes = App.GetRealm().All<NoteRealm>();
            realmNotes.SubscribeForNotifications((collection, o, e) =>
            {
            });
            Notes = realmNotes.AsRealmCollection();
        }

        private void AddNote()
        {
            string title = !String.IsNullOrWhiteSpace(NewNoteTitle) ? NewNoteTitle : "<none>";
            string description = !String.IsNullOrWhiteSpace(NewNoteDescription) ? NewNoteDescription : "<none>";

            var realm = App.GetRealm();
            realm.Write(() =>
            {
                realm.Add(new NoteRealm()
                {
                    Title = title,
                    Description = description,
                    Id = Guid.NewGuid().ToString(),
                    PostTime = DateTimeOffset.Now
                });
            });
            
            NewNoteDescription = "";
            NewNoteTitle = "";
            OnPropertyChanged(nameof(NewNoteTitle));
            OnPropertyChanged(nameof(NewNoteDescription));
        }

        private void RemoveNote(string id)
        {
            var realm = App.GetRealm();
            realm.Write(() =>
                {
                    realm.RemoveAndSync(Notes.First(x => x.Id == id));
                }
            );
        }

        public void Refresh()
        {
            InitData();
            OnPropertyChanged(nameof(Notes));
        }
    }
}
