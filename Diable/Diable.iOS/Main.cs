using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;
// NOTE THE USE OF THE FULL TYPE NAME INCLUDING NAMESPACE
[assembly: Shiny.ShinyApplication(
    ShinyStartupTypeName = "Diable.DiableShinyStartup",
    XamarinFormsAppTypeName = "Diable.App"
)]
namespace Diable.iOS
{
    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            UIApplication.Main(args, null, "AppDelegate");
        }
    }
}
