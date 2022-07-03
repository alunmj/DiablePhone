﻿#define do2
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Diable.Models;
using Diable.ViewModels;
using nexus.protocols.ble;
using nexus.protocols.ble.scan;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using SkiaSharp;

namespace Diable.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class ItemDetailPage : ContentPage
    {
        ItemDetailViewModel viewModel;
        private IBlePeripheral myper;
        private BlePeripheralConnectionRequest connection;
        private IBleGattServerConnection gattServer;
        private readonly IBluetoothLowEnergyAdapter ble;
        private int brightness = 30;
        private SettingsPage settingsPage = null;

        public ItemDetailPage(ItemDetailViewModel viewModel, IBlePeripheral peripheral)
        {
            InitializeComponent();

            BindingContext = this.viewModel = viewModel;
            // Text is Device ID, Id is address.
            ble = ((App)(App.Current)).myble;
            myper = peripheral;
        }
        private Guid kUartSvcId = new Guid("6e400001-b5a3-f393-e0a9-e50e24dcca9e");
        private Guid kUartTxCharId = new Guid("6e400002-b5a3-f393-e0a9-e50e24dcca9e");
        private Guid kUartRxCharId = new Guid("6e400003-b5a3-f393-e0a9-e50e24dcca9e");
        private int kUartTxMaxBytes = 240; // ble.net supports the big MTU.

        private static readonly byte[] red_color = { 255, 0, 0 };
        private static readonly byte[] green_color = { 0, 255, 0 };
        private static readonly byte[] blue_color = { 0, 0, 255 };
        private static readonly byte[] white_color = { 255, 255, 255 };
        private static readonly byte[] black_color = { 0, 0, 0 };
        private static readonly byte[] yellow_color = { 255, 255, 0 };
        private static readonly byte[] cyan_color = { 0, 255, 255 };
        private static readonly byte[] magenta_color = { 255, 0, 255 };

        private Dictionary<Guid, string> imageSources = new Dictionary<Guid, string>();

        protected async override void OnAppearing()
        {
            base.OnAppearing();
            // Connect to Bluetooth if we can...
            if (myper != null && (gattServer == null))
            {
                connection = await ble.ConnectToDevice(myper, new TimeSpan(0, 0, 30));
                if (connection.IsSuccessful())
                {
                    // Don't forget to split into max MTU bytes per send.
                    gattServer = connection.GattServer;
                    _ = gattServer.NotifyCharacteristicValue(kUartSvcId, kUartRxCharId, OnBLEReceive);

                    await SendBLECmd(new byte[] { (byte)'B', (byte)brightness }); // Set brightness!
                    await SendBLECmd("V"); // Get version info!
                }
            }
            if (ImageButtonStack.Children.Count == 0)
            {
                var assembly = typeof(App).GetTypeInfo().Assembly;
                var MyAssemblyName = assembly.GetName().Name; //now this is for the future
                                                              //if you want acces files by a generated name would be:
                                                              // MyAssemblyName + ".Media."+filename
                                                              //anyway now we are building the whole list:
                foreach (var res in assembly.GetManifestResourceNames())
                {
                    if (res.Contains(".Media."))
                    {
                        ImageButton newImageButton = new ImageButton();
                        newImageButton.WidthRequest = 100;
                        newImageButton.Source = ImageSource.FromResource(res, typeof(ItemDetailPage).GetTypeInfo().Assembly);
                        imageSources.Add(newImageButton.Id, res);
                        newImageButton.Clicked += ImageButtonClick;
                        ImageButtonStack.Children.Add(newImageButton);
                    }
                }
            }
            if (settingsPage != null)
            {
                // We got here returning from a settings page, so if the "save" value is set in that, we need to save the data.
                if (settingsPage.IsSave())
                {
                    // Get name, pin layout & fold state, send to DiaBLE unit.
                    byte[] command = new byte[] { (byte)'L', 5, 6, (byte)'F' };
                    Byte.TryParse(settingsPage.Pin0, out command[1]);
                    Byte.TryParse(settingsPage.Pin1, out command[2]);
                    command[3] = (byte)(settingsPage.FoldedSwitch ? 'F' : 'W');
                    await SendBLECmd(command);
                    // TODO: Send name using T command?
                }
            }
        }

        private async void ImageButtonClick(object sender, EventArgs e)
        {
            ImageButton myButt = sender as ImageButton;
            if (myButt == null) return;
            Guid id = myButt.Id;
            if (imageSources.ContainsKey(id))
            {
                await SendResourceImage(imageSources[id]);
            }
        }

        private async Task SendPokeball()
        {
            // 4000 rpm = 0.015s per rev.
            // Therefore one half a revolution is 7500 micros
            FrameCommands f = new FrameCommands();
            // From inside out on each side, 1 LED at white, one LED off, 6 LEDs at colour.
            byte[][] blackline = { white_color, white_color, black_color, black_color, black_color, black_color, black_color, black_color,
                black_color, black_color, black_color, black_color, black_color, black_color, black_color, black_color};
            byte[][] redline = { white_color, white_color, black_color, black_color, red_color, white_color, red_color, white_color,
                red_color, white_color, red_color, white_color, red_color, white_color, red_color, white_color};
            byte[][] wtfline = { white_color, white_color, black_color, black_color, white_color, red_color, white_color, red_color,
                white_color, red_color, white_color, red_color, white_color, red_color, white_color, red_color};

            f.AddFrame(500, blackline);
            f.AddFrame(7000, redline);
            f.AddFrame(500, blackline);
            f.AddFrame(7000, wtfline);
            await SendBLECmd(f);
        }

        private async Task SendColourCircles(byte[] ColorPicked)
        {
            // 1/10th of a second for each circle, then 'a long time' for solid colour.
            long microsfillrow = 100000L;
            FrameCommands f = new FrameCommands();
            for (int i = 0; i < 16; i++)
            {
                if (i == 15)
                    microsfillrow = 100000000L;
                FrameCommands.Frames frame = new FrameCommands.Frames(microsfillrow);
                for (int j = 0; j < 16; j++)
                {
                    byte[] colors = new byte[3] { 0, 0, 0 };
                    if (j <= i)
                    {
                        colors = ColorPicked;
                    }
                    frame.frame[j] = colors;
                }
                f.AddFrame(frame);
            }

            await SendBLECmd(f);
        }

        private DateTime firstCall = DateTime.Now;

        private async Task SendBLECmd(String theString)
        {
            await SendBLECmd(Encoding.ASCII.GetBytes(theString));
        }

        private async Task SendBLECmd(byte[] bytes)
        {
            await SendBLECmd(new MemoryStream(bytes));
        }

        private async Task SendBLECmd(MemoryStream commandStream)
        {
            Debug.WriteLine($"Time: {(DateTime.Now - firstCall).TotalSeconds}");
            Debug.WriteLine($"Begin:\n{Convert.ToBase64String(commandStream.ToArray())}\n:End");
            commandStream.Position = 0;
            if (gattServer != null)
            {
                for (int i = 0; i < commandStream.Length; i += kUartTxMaxBytes)
                {
                    int toRead = kUartTxMaxBytes;
                    if (commandStream.Length - i < kUartTxMaxBytes)
                        toRead = (int)(commandStream.Length - i);
                    byte[] into = new byte[toRead];
                    int readthem = commandStream.Read(into, 0, toRead);
                    byte[] outof = await gattServer.WriteCharacteristicValue(kUartSvcId, kUartTxCharId, into);
                    bool outisin = outof.SequenceEqual(into);
                    if (!outisin)
                    {
                        Debug.WriteLine("That's not good - out is not the same as in.");
                    }
                }
            }
            commandStream.SetLength(0);
            commandStream.Position = 0;
        }

        private async void CircleButton_Clicked(object sender, EventArgs e)
        {
            byte[] colors = black_color;
            Dictionary<Button, byte[]> buttdict = new Dictionary<Button, byte[]>
            {
                [RedCircleButton] = red_color,
                [GreenCircleButton] = green_color,
                [BlueCircleButton] = blue_color,
                [WhiteCircleButton] = white_color,
                [YellowCircleButton] = yellow_color,
                [CyanCircleButton] = cyan_color,
                [MagentaCircleButton] = magenta_color
            };

            Button butt = sender as Button;
            Color bg = butt.BackgroundColor;
            string hex = bg.ToHex();

            if (buttdict.ContainsKey(butt))
                colors = buttdict[butt];

            await SendColourCircles(colors);
        }

        protected async override void OnDisappearing()
        {
            base.OnDisappearing();
            if (gattServer != null)
            {
                await gattServer.Disconnect();
            }
            gattServer = null;
        }

        private void Brightness_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            brightness = (int)e.NewValue;

        }

        private async void Brightness_DragCompleted(object sender, EventArgs e)
        {
            await SendBLECmd(new BrightnessCommands(brightness));
        }

        private async Task SendBoom(byte[] vs)
        {
            // 1/20th of a second for each brightness, then 'a long time' for solid colour.
            long microsfillrow = 500000L;
            // Set color to black
            await SendBLECmd(new ColorCommands(black_color));
            await SendBLECmd(new BrightnessCommands(0xff));
            FrameCommands f = new FrameCommands();

            // Boom - sudden increase in light strength, then bright light for a half second, then black.
            FrameCommands.Frames frame;
            for (int i = 0; i < 8; i++)
            {
                if (i != 0)
                    microsfillrow = 50000L;
                frame = new FrameCommands.Frames(microsfillrow);
                for (int j = 0; j < 16; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        frame.frame[j][k] = (byte)(vs[k] >> i);
                    }
                }
                f.AddFrame(frame);
            }
            f.AddFrame(new FrameCommands.Frames(microsfillrow * 100));
            // Full brightness!
            await SendBLECmd(f);
        }


        private async void BoomButton_Clicked(object sender, EventArgs e)
        {
            byte[] colors = black_color;
            if (sender.Equals(RedBoomButton)) { colors = red_color; }
            else if (sender.Equals(GreenBoomButton)) { colors = green_color; }
            else if (sender.Equals(BlueBoomButton)) { colors = blue_color; }
            else if (sender.Equals(WhiteBoomButton)) { colors = white_color; }
            else if (sender.Equals(YellowBoomButton)) { colors = yellow_color; }
            else if (sender.Equals(CyanBoomButton)) { colors = cyan_color; }
            else if (sender.Equals(MagentaBoomButton)) { colors = magenta_color; }
            await SendBoom(colors);
        }

        private async void PokeButton_Clicked(object sender, EventArgs e)
        {
            await SendPokeball();
        }

        private async Task SendResourceImage(string resourceID)
        {

            SKBitmap resourceBitmap;
            Assembly assembly = GetType().GetTypeInfo().Assembly;

            using (Stream stream = assembly.GetManifestResourceStream(resourceID))
            {
                resourceBitmap = SKBitmap.Decode(stream);
            }

            FrameCommands f = new FrameCommands();
            // f.AddFrame(micros,bytes)
            // micros add up to 15000 for a 4000rpm diabolo
            double diabolo_speed = 4000.0; // RPM - put this in a setting?
            double total_micros = 1000000.0 * 60.0 / diabolo_speed; // # of microseconds per revolution
            // Minimum micros value? Tests suggest if above 450, we get +/- 45-50 on timing.
            // This is supported by a comment in the NeoPixel code, which says 300us is required silent
            // between comms to the stick. https://github.com/adafruit/Adafruit_NeoPixel/blob/master/Adafruit_NeoPixel.h#L222
            // 15000 / 375 = 40 segments, so 9 degrees per segment, which seems likely good enough.
            // 15000 / 500 = 30 segments, which is 12 degrees per segment, which works.
            // We've tried 500, which is nice, but more segments is probably better.
            long each_micros = 375L; // Number of microseconds for each segment - put this in a setting?
            int max_steps = (int)Math.Floor(total_micros / each_micros);
            double st, ct;
            int xc, yc;
            int x, y;
            int w = resourceBitmap.Width / 2;
            int h = resourceBitmap.Height / 2;
            SKColor pixel_color;
            xc = w;
            yc = h;
            if (w > h) h = w; else w = h; // Choose the maximum (maintains aspect ratio)!
            for (int step = 0; step < max_steps; step++)
            {
                byte[][] step_frame = new byte[16][];
                // Diabolo spins clockwise only, so no point in trying to do this in any direction OTHER than clockwise.
                // Which means we're going in steps from 0 to 29, and the angle is -2.0 * pi * step / 30.0
                // bytes array is an array of pixels, going from inside to outside, alternating left stick, right, left, right, etc.
                // each pixel is an RGB triple, 0 to 255, e.g. {255,255,0} is yellow.
                // eca8642013579bdf
                // OK, it's a weird structure, sorry.
                // We will allocate each frame array new each time, because this is not a copy, it's a reference.
                double theta = -2.0 * Math.PI * step / max_steps;
                st = Math.Sin(theta);
                ct = Math.Cos(theta);

                double radius = 16.0; // I tried 17, so as not to reach the edges, but I think 16 may be better.

                for (int hop = 0; hop < 16; hop++)
                {
                    if (hop % 2 == 0)
                    {
                        x = (int)Math.Floor(xc + hop * ct * w / radius);
                        y = (int)Math.Floor(yc + hop * st * h / radius);
                    }
                    else
                    {
                        x = (int)Math.Floor(xc - hop * ct * w / radius);
                        y = (int)Math.Floor(yc - hop * st * h / radius);
                    }
                    if (x < 0 || x >= resourceBitmap.Width || y < 0 || y >= resourceBitmap.Height)
                    {
                        pixel_color = SKColor.Empty;
                    }
                    else
                    {
                        pixel_color = resourceBitmap.GetPixel(x, y);
                    }

                    step_frame[hop] = new byte[] { pixel_color.Red, pixel_color.Green, pixel_color.Blue };
                }
                f.AddFrame(each_micros, step_frame);
            }
            // Set color to black
            await SendBLECmd(new ColorCommands(black_color));
            // Set brightness up
            brightness = 0xff;
            await SendBLECmd(new BrightnessCommands(0xff));
            // Send image.
            await SendBLECmd(f);
        }

        private async void TestButton_Clicked(object sender, EventArgs e)
        {
            // Simulate sending as much data as a 30 frame animation.
            // That's (16*3+4)*30+2+1 = 1563 bytes
            // Not yet implemented in the firmware, so this is disabled in the app.
            // This was written to allow testing if the async change didn't fix the problem.
            int tlen = 1560;
            byte[] outstr = new byte[tlen + 3];
            outstr[0] = (byte)'T';
            outstr[1] = (byte)(tlen & 0xff);
            outstr[2] = (byte)((tlen >> 8) & 0xff);
            for (int i = 0; i < tlen; i++)
            {
                outstr[i + 3] = (byte)(i % 0xff);
            }
            await SendBLECmd(outstr);
        }

        private void OnBLEReceive(byte[] bytes)
        {
            // If this crashes, maybe it needs Device.BeginInvokeOnMainThread(Action)
            Debug.WriteLine($"We got some bytes of {bytes.Count()} length.");
            Debug.WriteLine($"{Encoding.ASCII.GetString(bytes)}");
            switch ((char)bytes[0])
            {
                case 'O':
                    // Should be "OK\r\n"
                    break;
                case 'L':
                    // Should be "LDiaBLE v2.10:0506W for version 2.10, pin0=5, pin1=6 (decimal), wing-forme
                    break;
            }
        }

        private async void SetupItem_Clicked(object sender, EventArgs e)
        {
            settingsPage = new SettingsPage();
            settingsPage.DiaBLEName = "Fake DiaBLE";
            settingsPage.DiaBLEVersion = "2.1.3";
            settingsPage.Pin0 = "7";
            settingsPage.Pin1 = "8";
            // TODO: These are all dumb settings, so instead, we should put up the values we got from the DiaBLE unit itself.

            await Navigation.PushAsync(settingsPage);

        }
    }
}