using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using RealmiusAdvancedExample.Model;
using Xamarin.Forms;

namespace RealmiusAdvancedExample.ViewModel
{
    public class TabbedPageViewModel : RootViewModel
    {
        public NotesPageViewModel NotesPageViewModel { get; } = new NotesPageViewModel();

        public PhotosPageViewModel PhotosPageViewModel { get; } = new PhotosPageViewModel();

        public SettingsPageViewModel SettingsPageViewModel { get; } = new SettingsPageViewModel();

        public ChatPageViewModel ChatPageViewModel { get; } = new ChatPageViewModel();

        public void RefreshViewModels()
        {
            NotesPageViewModel.Refresh();
            PhotosPageViewModel.Refresh();
            SettingsPageViewModel.Refresh();
            ChatPageViewModel.Refresh();
        }
    }
}
