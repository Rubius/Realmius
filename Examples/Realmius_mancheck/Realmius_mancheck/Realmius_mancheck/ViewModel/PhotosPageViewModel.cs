using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Realmius_mancheck.RealmEntities;
using Realms;
using Xamarin.Forms;

namespace Realmius_mancheck.ViewModel
{
    public class PhotosPageViewModel : RootViewModel
    {
        public List<PhotoRealm> Photos { get; set; }

        public PhotosPageViewModel()
        {
            InitData();;
        }

        private void InitData()
        {
            var realmPhotos = App.GetRealm().All<PhotoRealm>();
            realmPhotos.SubscribeForNotifications((collection, y, e) =>
            {
                Photos = realmPhotos.ToList();
                OnPropertyChanged(nameof(Photos));
            });
        }

        public void Refresh()
        {
            InitData();
            OnPropertyChanged(nameof(Photos));
        }
    }
}
