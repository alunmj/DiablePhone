using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Diable.Models;
using Diable.Views;
using Diable.ViewModels;
using System.Threading;
using System.Diagnostics;

namespace Diable.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class ItemsPage : ContentPage
    {
        ItemsViewModel viewModel;
        Dictionary<string, object /*-B IBlePeripheral*/> peripheralMap = new Dictionary<string, object /*-B IBlePeripheral*/>();

        public ItemsPage()
        {
            InitializeComponent();

            BindingContext = viewModel = new ItemsViewModel();
        }

        async void OnItemSelected(object sender, EventArgs args)
        {
            // We clicked on a device. Connect to it. Subpage: Make sure it has the services we need, connect to the serial TX/RX service
            var layout = (BindableObject)sender;
            var item = (Item)layout.BindingContext;
            await Navigation.PushAsync(new ItemDetailPage(new ItemDetailViewModel(item)/*-B, peripheralMap[item.Id]*/));
        }

        async void AddItem_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new NavigationPage(new NewItemPage()));
        }

        private async Task RefreshBTLEDevices()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
/*-B            var ble = ((App)(App.Current)).myble;
            if (ble != null && ble.AdapterCanBeEnabled && ble.CurrentState.IsDisabledOrDisabling())
            {
                await ble.EnableAdapter();
            }
*/
            // TODO: Clear all items.
            viewModel.Items.Clear();
            peripheralMap.Clear();
#if DEBUG
            string fakeId = "01-f0-10-d0-01";
            viewModel.Items.Add(new Item() { Description = "Fake test item", Text = "00000-GA-B000-GAAAAA", Id = fakeId });
            peripheralMap[fakeId] = null;
#endif // DEBUG
            // Connecting to Device Name "Adafruit Bluefruit LE", service = 6e400001-b5a3-f393-e0a9-e50e24dcca9e
/*-B
            if (ble != null)
            {
                await ble.ScanForBroadcasts(
                    new ScanSettings()
                    {
                        Filter = new ScanFilter() { AdvertisedServiceIsInList = new List<Guid> { new Guid("6e400001-b5a3-f393-e0a9-e50e24dcca9e") } },
                        Mode = ScanMode.LowPower,
                        IgnoreRepeatBroadcasts = false
                    },
                     peripheral =>
                     {
                         var adv = peripheral.Advertisement;
                         Debug.WriteLine(adv.DeviceName ?? "(null device name)");
                         Debug.WriteLine(String.Join(", ", adv.Services.Select(x => x.ToString())));
                         Debug.WriteLine(adv.ManufacturerSpecificData.FirstOrDefault().CompanyName());
                         Debug.WriteLine(adv.ServiceData);
                         string textId = BitConverter.ToString(peripheral.Address);
                         if (!peripheralMap.ContainsKey(textId))
                         {
                             viewModel.Items.Add(new Item()
                             {
                                 Description = adv.DeviceName,
                                 Text = peripheral.DeviceId.ToString(),
                                 Id = textId
                             });
                             peripheralMap[textId] = peripheral;
                         }
                         cts.Cancel(); // Found what we needed? Really?
                     },
                     cts.Token);
            }
*/

        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            if (viewModel.Items.Count == 0)
                viewModel.IsBusy = true;
            await RefreshBTLEDevices();
        }

        private async void BTLERefresh_Button_Clicked(object sender, EventArgs e)
        {
            await RefreshBTLEDevices();
        }
    }
}