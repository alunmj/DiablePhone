using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Diable.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : ContentPage
    {
        private bool isSave;
        public bool FoldedSwitch { get => _FoldedSwitch.IsToggled; set => _FoldedSwitch.IsToggled = value; }
        public string Pin0 { get => _Pin0.Text; set => _Pin0.Text = value; }
        public string Pin1 { get => _Pin1.Text; set => _Pin1.Text = value; }
        public string DiaBLEName { get => _DiaBLEName.Text; set => _DiaBLEName.Text = value; }
        public string DiaBLEVersion { get => _DiaBLEVersion.Text; set => _DiaBLEVersion.Text = value; }
        public SettingsPage()
        {
            InitializeComponent();
            isSave = false; // Default 'back' is like a 'cancel'
        }

        private async void Save_Clicked(object sender, EventArgs e)
        {
            isSave = true;
            await Navigation.PopAsync();
        }

        internal bool IsSave()
        {
            return isSave;
        }
    }
}