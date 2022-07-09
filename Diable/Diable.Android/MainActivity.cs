using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Bluetooth;
using Android.Support.V4.App;
using Android;
using AndroidX.Core.App;

namespace Diable.Droid
{
    [Activity(Label = "Diable", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            Xamarin.Forms.Forms.SetFlags("RadioButton_Experimental");
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            //BluetoothLowEnergyAdapter.Init(this);
            //-BIBluetoothLowEnergyAdapter bluetooth = null;
            // I need fine location support to be able to list nearby BLE endpoints.
            ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.AccessFineLocation }, 1);
            // If we're running in an emulator, no bluetooth.
            if (BluetoothAdapter.DefaultAdapter != null)
            {
                //-B bluetooth = BluetoothLowEnergyAdapter.ObtainDefaultAdapter(ApplicationContext);
            }
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);
            LoadApplication(new App(/*-B bluetooth*/));
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}