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
using Shiny;

// NOTE THE USE OF THE FULL TYPE NAME INCLUDING NAMESPACE
[assembly: ShinyApplication(
    ShinyStartupTypeName = "Diable.DiableShinyStartup",
    XamarinFormsAppTypeName = "Diable.App"
)]

namespace Diable.Droid
{
    [Activity(Label = "Diable", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, 
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public partial class MainActivity : Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            Xamarin.Forms.Forms.SetFlags("RadioButton_Experimental");
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            // I need fine location support to be able to list nearby BLE endpoints.
            ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.AccessFineLocation }, 1);
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);
            LoadApplication(new App());
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            this.ShinyOnRequestPermissionsResult(requestCode, permissions, grantResults);

            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}