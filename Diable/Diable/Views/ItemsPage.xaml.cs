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
using Shiny;
using Shiny.BluetoothLE;

namespace Diable.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class ItemsPage : ContentPage
    {
        ItemsViewModel viewModel;
        Dictionary<string, IPeripheral> peripheralMap = new Dictionary<string, IPeripheral>();

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
            await Navigation.PushAsync(new ItemDetailPage(new ItemDetailViewModel(item), peripheralMap[item.Id]));
        }

        async void AddItem_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new NavigationPage(new NewItemPage()));
        }

        private async Task RefreshBTLEDevices()
        {
            var bleManager = ShinyHost.Resolve<IBleManager>();
            if (bleManager != null && bleManager.CanControlAdapterState())
            {
                bleManager.TrySetAdapterState(true);
            }
            // TODO: Clear all items.
            viewModel.Items.Clear();
            peripheralMap.Clear();
#if DEBUG
            string fakeId = "01-f0-10-d0-01";
            viewModel.Items.Add(new Item() { Description = "Fake test item", Text = "00000-GA-B000-GAAAAA", Id = fakeId });
            peripheralMap[fakeId] = null;
#endif // DEBUG
            // Connecting to Device Name "Adafruit Bluefruit LE", service = 6e400001-b5a3-f393-e0a9-e50e24dcca9e
            // Scan for BLE devices
            if (!bleManager.IsScanning)
            {
                var scanner = bleManager.Scan(new ScanConfig { ServiceUuids = new List<string>() { "6e400001-b5a3-f393-e0a9-e50e24dcca9e" } })
                    .Subscribe(scanResult => {
                        IAdvertisementData adv = scanResult.AdvertisementData;
                        /*
                         * adv.IsConnectable = true
                         * adv.LocalName = "RayTac"
                         * adv.ManufacturerData = null
                         * adv.ServiceData = Shiny.BluetoothLE.AdvertisementServiceData[0]
                         * adv.ServiceUuids = { "6e400001-b5a3-f393-e0a9-e50e24dcca9e" }
                         * adv.TxPower = 0
                         * 
                         */
                        /*
                         * scanResult.Rssi = -83
                         */
                        IPeripheral per = scanResult.Peripheral;
                        /*
                         * per.Name = "RayTac"
                         * per.Uuid = "00000000-0000-0000-0000-d8d4f9fcc04b"
                         * per.MtuSize = 20
                         * per.Native = Android.Bluetooth.BluetoothDevice
                         * per.PairingStatus = Shiny.BluetoothLE.PairingState.NotPaired
                         * per.Status = Shiny.BluetoothLE.ConnectionState.Disconnected
                         */
                        string textId = per.Uuid;
                        if (!peripheralMap.ContainsKey(textId))
                        {
                            viewModel.Items.Add(new Item()
                            {
                                Description = adv.LocalName, // Could also just be per.Name?
                                Text = per.Uuid,
                                Id = textId
                            });
                            peripheralMap[textId] = per;
                        }

                    }
                );
            }
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