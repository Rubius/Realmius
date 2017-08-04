using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using DLToolkit.Forms.Controls;
using Realmius;
using Realmius.Contracts.Logger;
using Realmius.SyncService;
using Realmius_mancheck.Model;
using Realmius_mancheck.RealmEntities;
using Realmius_mancheck.ViewModel;
using Realms;
using Xamarin.Forms;

namespace Realmius_mancheck
{
    public partial class App : Application
    {
        public static User CurrentUser { get; set; }

        public static bool UserAuthorised { get; set; }
        
        public static App Instance { get; set; }

        private readonly bool needAuthorisation = true;

        private static IRealmiusSyncService syncService;

        private ILogger _logger = new Logger();

        //Howto connect emulator to local server:https://stackoverflow.com/a/5249856
        private readonly string hostUrl = "http://192.168.10.109:9930";

        private Type[] TypesToSync = new Type[] {typeof(NoteRealm), typeof(PhotoRealm), typeof(ChatMessageRealm)};
        

        public App()
        {
            Instance = this;

            FlowListView.Init();

            GetUserCredentials();
            
            InitializeComponent();
            
            var tabbedPageViewModel = new TabbedPageViewModel();
            RefreshViews += (o, e) =>
            {
                tabbedPageViewModel.RefreshViewModels();
            };

            MainPage = new NavigationPage(new Realmius_mancheck.MainTabbedPage() {BindingContext = tabbedPageViewModel});

            if (!UserAuthorised)
            {
                InitAuthorisation();
            }
            else
            {
                realmPath = CurrentUser.Name;
                InitializeDatabasePaths();
                SetRealmConnection();
                RefreshViews?.Invoke(new object(), EventArgs.Empty);
            }
        }
        
        #region - REALM SERVICES -

        private static RealmConfiguration RealmConfiguration
        {
            get
            {
                var config = new RealmConfiguration(realmPath)
                {
                    MigrationCallback = (s, e) =>
                    {
                    },
                    ShouldDeleteIfMigrationNeeded = false,
                    SchemaVersion = 4,
                };
                return config;
            }
        }

        public static Realm GetRealm()
        {
            var instance = Realm.GetInstance(RealmConfiguration);
            return instance;
        }

        private void SetRealmConnection()
        {
            syncService = SyncServiceFactory.CreateUsingSignalR(
                GetRealm,
                new Uri(hostUrl + "/Realmius" + (needAuthorisation
                            ? "?userLogin=" + CurrentUser.Name + "&pass=" + CurrentUser.Password
                            : null)),
                TypesToSync);

            syncService.Logger = _logger;
            
            //InitStartData();
        }

        public void ReinitializeDatabases()
        {
            try
            {
                syncService.Dispose();
                //syncService.DeleteDatabase();
            }
            catch (Exception e)
            {
            }

            ResetDatabasePaths();
            InitializeDatabasePaths();
            SetRealmConnection();
            RefreshViews?.Invoke(new object(), EventArgs.Empty);
        }

        private static string realmPath = Guid.NewGuid().ToString();

        public static void ResetDatabasePaths()
        {
            if (CurrentUser == null)
            {
                realmPath = Guid.NewGuid().ToString();
            }
            else
            {
                realmPath = CurrentUser.Name;
            }
        }

        public static void InitializeDatabasePaths()
        {
            if (string.IsNullOrEmpty(realmPath))
            {
                ResetDatabasePaths();
            }
            RealmiusSyncService.RealmiusDbPath = realmPath + "_sync";
        }

        private void InitStartData()
        {
            var realm = App.GetRealm();
            realm.Write(() =>
            {
                realm.Add(new NoteRealm() { Id = "1001", Title = "Film", Description = "Fight club", PostTime = DateTimeOffset.Now }, true);

                realm.Add(new NoteRealm() { Id = "1002", Title = "Cleaning", Description = "Clean the room", PostTime = DateTimeOffset.Now }, true);

                realm.Add(new NoteRealm() { Id = "1003", Title = "Pet", Description = "Feed the dog", PostTime = DateTimeOffset.Now }, true);

                realm.Add(new PhotoRealm()
                {
                    Id = "1004",
                    Title = "Bike",
                    PhotoUri = "https://auto.ndtvimg.com/bike-images/gallery/honda/cbr-150r/exterior/bike-img.png",
                    PostTime = DateTimeOffset.Now
                }, true);

                realm.Add(new PhotoRealm()
                {
                    Id = "1005",
                    Title = "Plain",
                    PhotoUri = "http://az616578.vo.msecnd.net/files/2016/06/11/636012615152249351-1424983048_cover4.jpg",
                    PostTime = DateTimeOffset.Now
                }, true);

                realm.Add(new PhotoRealm()
                {
                    Id = "1006",
                    Title = "Helicopter",
                    PhotoUri = "https://i.ytimg.com/vi/_rLTPRGpA60/maxresdefault.jpg",
                    PostTime = DateTimeOffset.Now
                }, true);

                realm.Add(new ChatMessageRealm()
                {
                    AuthorName = "admin",
                    Id = "10000",
                    CreatingDateTime = DateTimeOffset.Now,
                    Text = "Hi all!"
                }, true);

                realm.Add(new ChatMessageRealm()
                {
                    AuthorName = "john",
                    Id = "10001",
                    CreatingDateTime = DateTimeOffset.Now,
                    Text = "Hi!"
                }, true);

                realm.Add(new ChatMessageRealm()
                {
                    AuthorName = "homer",
                    Id = "10002",
                    CreatingDateTime = DateTimeOffset.Now,
                    Text = "What's up?"
                }, true);
            });
        }

        #endregion // - REALM SERVICES -

        #region - PROCCESSING USER -

        public void InitAuthorisation()
        {
            if (UserAuthorised) return;

            var authorisationPageViewModel = new AuthorisationPageViewModel();
            var authorisationPage = new AuthorisationPage() { BindingContext = authorisationPageViewModel  };
            
            authorisationPageViewModel.OnAuthorisePageClosed += (o, e) =>
            {
                authorisationPage.Navigation.PopModalAsync();
                ReinitializeDatabases();
                SaveUserCredentials();
            };

            MainPage.Navigation.PushModalAsync(authorisationPage);
        }

        public EventHandler RefreshViews;
        
        private void GetUserCredentials()
        {
            IDictionary<string, object> properties = Application.Current.Properties;

            string name = string.Empty;
            string password = string.Empty;

            if (properties.ContainsKey(nameof(User.Name)))
            {
                name = properties[nameof(User.Name)] as string;
            }

            if (properties.ContainsKey(nameof(User.Password)))
            {
                password = properties[nameof(User.Password)] as string;
            }

            if (!String.IsNullOrWhiteSpace(name) && !String.IsNullOrWhiteSpace(password))
            {
                CurrentUser = new User(name, password);
                UserAuthorised = true;
            }
            else
            {
                UserAuthorised = false;
                CurrentUser = new User();
            }
            OnPropertyChanged(nameof(CurrentUser));
            OnPropertyChanged(nameof(UserAuthorised));
        }

        private void SaveUserCredentials()
        {
            IDictionary<string, object> properties = Application.Current.Properties;
            foreach (PropertyInfo prop in CurrentUser.GetType().GetRuntimeProperties())
            {
                if (!properties.ContainsKey(prop.Name))
                {
                    if (prop.GetValue(CurrentUser, null) != null)
                    {
                        properties.Add(prop.Name, prop.GetValue(CurrentUser, null).ToString());
                    }
                }
                else
                {
                    if (prop.GetValue(CurrentUser, null) != null)
                    {
                        properties[prop.Name] = prop.GetValue(CurrentUser, null).ToString();
                    }
                    else
                    {
                        properties.Remove(prop.Name);
                    }
                }
            }
            Application.Current.SavePropertiesAsync();
        }

 #endregion // - PROCESSING USER -
        
        #region - LIFECYCLE -

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
        #endregion //- LIFECYCLE -
    }
}
