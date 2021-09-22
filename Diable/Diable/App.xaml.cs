using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Diable.Services;
using Diable.Views;
using nexus.protocols.ble;

namespace Diable
{
    public partial class App : Application
    {
        public IBluetoothLowEnergyAdapter myble;

        public App(IBluetoothLowEnergyAdapter ble)
        {
            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            MainPage = new MainPage();
            myble = ble;
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
