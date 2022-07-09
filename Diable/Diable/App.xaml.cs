using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Diable.Services;
using Diable.Views;

namespace Diable
{
    public partial class App : Application
    {
        //-B public IBluetoothLowEnergyAdapter myble;

        public App(/*-B IBluetoothLowEnergyAdapter ble*/)
        {
            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            MainPage = new MainPage();
            //-B myble = ble;
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
