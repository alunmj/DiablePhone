#define do2
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Diable.Models;
using Diable.ViewModels;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using SkiaSharp;
using System.Text.RegularExpressions;
using Shiny.BluetoothLE;
using Shiny;

namespace Diable.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class ItemDetailPage : ContentPage
    {
        ItemDetailViewModel viewModel;
        private IPeripheral myper;
//-B        private BlePeripheralConnectionRequest connection;
//-B        private IBleGattServerConnection gattServer;
        private readonly IBleManager ble;
        private int brightness = 100;
        private SettingsPage settingsPage = null;

        // Settings related to the DiaBLE unit we're currently looking at - these could become a class, I dunno.
        private string _DiaBLEName;
        private string _DiaBLEVersion;
        private byte _DiaBLEPin0; // Which pin corresponds to stick 0
        private byte _DiaBLEPin1; // Which pin corresponds to stick 1
        private char _DiaBLEFold; // W or F.
        private byte _DiaBLELightCount = 8; // Number of lights per stick
        // TODO: Save the frame time somewhere, so personal preferences are kept.
        private long _DiaBLEFrameTime = 500; // 500 seems good, but we should be able to play with it!

        public ItemDetailPage(ItemDetailViewModel viewModel , IPeripheral peripheral)
        {
            InitializeComponent();
            FrameCommands.SetLightCount(_DiaBLELightCount);

            BindingContext = this.viewModel = viewModel;
            // Text is Device ID, Id is address.
            ble = ShinyHost.Resolve<IBleManager>();
            myper = peripheral;

            if (myper != null && myper.Name != null)
            {
                _DiaBLEName = myper.Name;
            }
            else
            {
                _DiaBLEName = "Fake test device";
            }
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
            // Connect to Bluetooth if we can, and need to...
            if (myper != null)
            {
                await myper.ConnectAsync(new ConnectionConfig { AutoConnect = false }, timeout: TimeSpan.FromSeconds(30));
                myper.TryRequestMtu(kUartTxMaxBytes);
                if (myper.IsConnected())
                {
                    // Don't forget to split into max MTU bytes per send.
                    myper.Notify(kUartSvcId.ToString(), kUartRxCharId.ToString()).SubscribeAsync(OnBLEReceive);

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
                    // Get name, pin layout & fold state, send to DiaBLE unit with 'L' command.
                    byte[] command = new byte[] { (byte)'L', 5, 6, (byte)'F' };
                    Byte.TryParse(settingsPage.Pin0, out command[1]);
                    _DiaBLEPin0 = command[1];
                    Byte.TryParse(settingsPage.Pin1, out command[2]);
                    _DiaBLEPin1 = command[2];
                    _DiaBLEFold = (settingsPage.FoldedSwitch ? 'F' : 'W');
                    command[3] = (byte)_DiaBLEFold;
                    await SendBLECmd(command);

                    // Set the LED count using the 'S' command.
                    byte[] stickcommand = new byte[] { (byte)'S', 8, 2, 1, 0, 0 };
                    Byte.TryParse(settingsPage.LightCount, out stickcommand[1]);
                    _DiaBLELightCount = stickcommand[1];
                    FrameCommands.SetLightCount(_DiaBLELightCount);
                    await SendBLECmd(stickcommand);

                    // Send name using T command?
                    if (_DiaBLEName != settingsPage.DiaBLEName)
                    {
                        await SendBLECmd($"T{settingsPage.DiaBLEName}");
                        _DiaBLEName = settingsPage.DiaBLEName;
                    }
                    long.TryParse(settingsPage.FrameTime, out _DiaBLEFrameTime);
                }
                settingsPage = null;
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
            // TODO: Allow for more than eight LEDs per stick!
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
            for (int i = 0; i < _DiaBLELightCount * 2; i++)
            {
                if (i == _DiaBLELightCount * 2 - 1)
                    microsfillrow = 100000000L;
                FrameCommands.Frames frame = new FrameCommands.Frames(microsfillrow, _DiaBLELightCount * 2);
                for (int j = 0; j < _DiaBLELightCount * 2; j++)
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
            if (myper != null && myper.IsConnected())
            {
                for (int i = 0; i < commandStream.Length; i += kUartTxMaxBytes)
                {
                    int toRead = kUartTxMaxBytes;
                    if (commandStream.Length - i < kUartTxMaxBytes)
                        toRead = (int)(commandStream.Length - i);
                    byte[] into = new byte[toRead];
                    int readthem = commandStream.Read(into, 0, toRead);
                    var outof = await myper.WriteCharacteristicAsync(kUartSvcId.ToString(), kUartTxCharId.ToString(), into);
                    bool outisin = outof.Data.SequenceEqual(into);
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
            myper.CancelConnection();
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
            for (int i = 0; i < 8 /* Not DiaBLELightCount */; i++)
            {
                if (i != 0)
                    microsfillrow = 50000L;
                frame = new FrameCommands.Frames(microsfillrow, _DiaBLELightCount * 2);
                for (int j = 0; j < _DiaBLELightCount * 2; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        frame.frame[j][k] = (byte)(vs[k] >> i);
                    }
                }
                f.AddFrame(frame);
            }
            f.AddFrame(new FrameCommands.Frames(microsfillrow * 100, _DiaBLELightCount * 2));
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
            long each_micros = _DiaBLEFrameTime; // Number of microseconds for each segment - put this in a setting?
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
                byte[][] step_frame = new byte[_DiaBLELightCount * 2][];
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

                double radius = _DiaBLELightCount * 2.0; // I tried 17, so as not to reach the edges, but I think 16 may be better.

                for (int hop = 0; hop < _DiaBLELightCount * 2; hop++)
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

        //private async void TestButton_Clicked(object sender, EventArgs e)
        //{
        //    // Simulate sending as much data as a 30 frame animation.
        //    // That's (16*3+4)*30+2+1 = 1563 bytes
        //    // Not yet implemented in the firmware, so this is disabled in the app.
        //    // This was written to allow testing if the async change didn't fix the problem.
        //    int tlen = 1560;
        //    byte[] outstr = new byte[tlen + 3];
        //    outstr[0] = (byte)'T';
        //    outstr[1] = (byte)(tlen & 0xff);
        //    outstr[2] = (byte)((tlen >> 8) & 0xff);
        //    for (int i = 0; i < tlen; i++)
        //    {
        //        outstr[i + 3] = (byte)(i % 0xff);
        //    }
        //    await SendBLECmd(outstr);
        //}




        private async Task OnBLEReceive(GattCharacteristicResult arg)
        {
            byte[] bytes = arg.Data;
            // If this crashes, maybe it needs Device.BeginInvokeOnMainThread(Action)
            Debug.WriteLine($"We got some bytes of {bytes.Count()} length.");
            Debug.WriteLine($"{Encoding.ASCII.GetString(bytes)}");
            switch ((char)bytes[0])
            {
                case 'O':
                    // Should be "OK\r\n"
                    break;
                case 'V':
                    // Should be "VDiaBLE v2.10:L0506W:Swhscz for version 2.10, pin0=5, pin1=6 (decimal), wing-forme
                    // (Maybe later, it'll have more values)
                    String input = Encoding.ASCII.GetString(bytes);
                    Regex re = new Regex("^VDiaBLE v([0-9.]+):L([0-9][0-9])([0-9][0-9])(.)(:S([0-9]+),...)?");
                    var matches = re.Match(input);
                    string version = matches.Groups[1].Value;
                    _DiaBLEVersion = version;
                    string pin0 = matches.Groups[2].Value;
                    Byte.TryParse(pin0, out _DiaBLEPin0);
                    string pin1 = matches.Groups[3].Value;
                    Byte.TryParse(pin1, out _DiaBLEPin1);
                    string fold = matches.Groups[4].Value;
                    _DiaBLEFold = fold[0];
                    if (matches.Groups.Count > 5)
                    {
                        if (matches.Groups[5].Value.StartsWith(":S"))
                        {
                            _DiaBLELightCount = (byte)int.Parse(matches.Groups[6].Value);
                            FrameCommands.SetLightCount(_DiaBLELightCount);
                        }
                    }
                    // TODO: Add number of LEDs, number of sticks (x * y)
                    // (You never know, we could get more lines by simply doing three sticks or four!)
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
            // These are all incorrect default settings, so instead, we should put up the values we got from the DiaBLE unit itself.
            settingsPage.DiaBLEVersion = _DiaBLEVersion;
            settingsPage.Pin0 = _DiaBLEPin0.ToString();
            settingsPage.Pin1 = _DiaBLEPin1.ToString();
            settingsPage.FoldedSwitch = _DiaBLEFold == 'F'; // Otherwise 'W'.
            settingsPage.DiaBLEName = _DiaBLEName;
            settingsPage.FrameTime = _DiaBLEFrameTime.ToString();
            settingsPage.LightCount = _DiaBLELightCount.ToString();

            await Navigation.PushAsync(settingsPage);

        }

        private async void Default1_Clicked(object sender, EventArgs e)
        {
            byte[][][] def1frames = new byte[12][][];
            for (int i = 0; i < 12; i++)
            {
                def1frames[i] = new byte[_DiaBLELightCount * 2][];
                for (int j = 0; j < _DiaBLELightCount * 2; j++)
                {
                    def1frames[i][j] = black_color;
                }
                // green, red, blue, green, red, blue at every fourth slot, starting at 0 - i
                int dc2 = _DiaBLELightCount * 2;

                for (int j=0;j<dc2 * 2;j+=4)
                {
                    int offset = j - i;
                    byte[] color = new byte[][]{ green_color, red_color, blue_color }[(j/4)%3];
                    if (offset > 0 && offset < dc2)
                    {
                        def1frames[i][offset] = color;
                    }
                }
            }
            FrameCommands f = new FrameCommands();
            foreach (var theFrame in def1frames)
                f.AddFrame(500, theFrame);

            // a colourful spiral design. Each leg of the spiral is a different colour.
            await SendBLECmd(f);
        }

        private async void Default2_Clicked(object sender, EventArgs e)
        {
            byte[][][] def2frames = new byte[6][][];
            for (int i = 0; i < 6; i++)
            {
                def2frames[i] = new byte[_DiaBLELightCount * 2][];
                if (i % 2 == 1)
                {
                    for (int j = 0; j < _DiaBLELightCount * 2; j++)
                    {
                        def2frames[i][j] = black_color;
                    }
                }
                else
                {
                    byte[][] tmp = new byte[][] { red_color, green_color, blue_color };
                    byte[] tmpColor = tmp[i / 2];
                    for (int j = 0; j < _DiaBLELightCount; j++)
                    {
                        def2frames[i][j] = tmpColor;
                    }
                    tmpColor = tmp[(i / 2 + 2) % 3];
                    for (int j = _DiaBLELightCount; j < _DiaBLELightCount * 2; j++)
                    {
                        def2frames[i][j] = tmpColor;
                    }
                }
            }
            FrameCommands f = new FrameCommands();
            foreach (var theFrame in def2frames)
                f.AddFrame(500, theFrame);
            await SendBLECmd(f);
        }

        private async void CycleButton_Clicked(object sender, EventArgs e)
        {
            // Cycle through the colours - also known as "Rainbow" or "Spectrum".
            int cycle_speed = (int)CycleSpeed.Value;
            int cycle_step = (int)CycleStep.Value;
            await SendBLECmd(new byte[] { (byte)'Y', (byte)cycle_speed, (byte)((cycle_step >> 8) & 0xff), (byte)(cycle_step & 0xff) }); // Set colour cycle!
        }

        private async void SparkleButton_Clicked(object sender, EventArgs e)
        {
            int sparkle_chance = (int)SparkleChance.Value;
            int sparkle_fred = (int)SparkleFRed.Value;
            int sparkle_fgreen = (int)SparkleFGreen.Value;
            int sparkle_fblue = (int)SparkleFBlue.Value;
            int sparkle_bred = (int)SparkleBRed.Value;
            int sparkle_bgreen = (int)SparkleBGreen.Value;
            int sparkle_bblue = (int)SparkleBBlue.Value;
            await SendBLECmd(new byte[] { (byte)'X', (byte)sparkle_chance, (byte)sparkle_fred, (byte) sparkle_fgreen, (byte)sparkle_fblue,
            (byte)sparkle_bred, (byte)sparkle_bgreen, (byte)sparkle_bblue});
        }

        private async void Gyro_Clicked(object sender, EventArgs e)
        {
            // Experimental gyro mode.
            await SendBLECmd(new byte[] { (byte)'G' });
        }
    }
}
