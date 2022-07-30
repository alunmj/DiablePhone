using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Diable.Models
{
    class Commands
    {
        protected MemoryStream ms;
        protected char commandtype;
        protected Commands(char _type)
        {
            commandtype = _type;
            ms = new MemoryStream();
        }
        static Commands Command(char _type)
        {
            switch (_type)
            {
                case 'F': // Frame type
                    return new FrameCommands();
                case 'B': // Brightness
                    return new BrightnessCommands();
            }
            return null;
        }
        protected virtual void PreSend() { ms.Seek(0, SeekOrigin.Begin); ms.WriteByte((byte)commandtype); }

        public MemoryStream GetStream()
        {
            PreSend();
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
        public static implicit operator MemoryStream(Commands cs) { return cs.GetStream(); }
    }

    class FrameCommands : Commands
    {
        public class Frames
        {
            public long microsFrame = 0;
            public int numLights = 16;
            public byte[][] frame = new byte[16][] {
                new byte[3], new byte[3], new byte[3], new byte[3],
                new byte[3], new byte[3], new byte[3], new byte[3],
                new byte[3], new byte[3], new byte[3], new byte[3],
                new byte[3], new byte[3], new byte[3], new byte[3],
             };
            private void AllocateLights(int nLights)
            {
                if (nLights == numLights)
                {
                    return;
                }
                frame = new byte[nLights][];
                numLights = nLights;
                for (int i = 0; i < numLights; i++)
                {
                    frame[i] = new byte[3];
                }
            }
            public Frames(int nLights)
            {
                AllocateLights(nLights);
            }
            public Frames(long _micros, int nLights)
            {
                microsFrame = _micros;
                AllocateLights(nLights);
            }
            public Frames(long _micros, byte[][] _frame)
            {
                microsFrame = _micros;
                frame = _frame;
            }
        }
        int frameCount = 0;
        readonly List<Frames> frames = new List<Frames>();
        public FrameCommands() : base('F') { }
        static private int numLights;
        static public void SetLightCount(int nLights) { numLights = nLights*2; }
        static public int GetLightCount() { return numLights/2; }
        public void AddFrame(Frames _frame)
        {
            Debug.Assert(_frame.frame.Length == numLights);
            // It causes the Arduino to crash if we don't supply exactly the right number of frames, so let's fix that!
            if (_frame.frame.Length != numLights)
            {
                Frames adjustedFrame = new Frames(_frame.microsFrame, numLights);
                for (int i = 0; i < numLights; i++)
                {
                    if (i < _frame.frame.Length)
                    {
                        adjustedFrame.frame[i] = _frame.frame[i];
                    }
                    else
                    {
                        adjustedFrame.frame[i] = new byte[] { 0, 0, 0 };
                    }
                }
                _frame = adjustedFrame;
            }
            frameCount++;
            frames.Add(_frame);
        }
        public void AddFrame(long _microsFrame, byte[][] _frame)
        {
            AddFrame(new Frames(_microsFrame, _frame));
        }
        protected override void PreSend()
        {
            base.PreSend();
            Debug.Assert(frameCount > 0 && frameCount < 32768);
            Debug.Assert(frameCount == frames.Count);
            byte[] bframeCount = BitConverter.GetBytes((short)frameCount).Reverse().ToArray();
            ms.Write(bframeCount, 0, 2);
            foreach (var x in frames)
            {
                byte[] microsFrame = BitConverter.GetBytes((int)x.microsFrame).Reverse().ToArray();

                ms.Write(microsFrame, 0, 4);
                foreach (var y in x.frame)
                {
                    ms.Write(y, 0, y.Length);
                }
            }
        }
    }
    class BrightnessCommands : Commands
    {
        byte brightness = 255;
        public BrightnessCommands() : base('B') { }
        public BrightnessCommands(int _brightness) : base('B')
        {
            commandtype = 'B';
            Set(_brightness);
        }
        public void Set(int _brightness)
        {
            brightness = (byte)_brightness;
        }
        protected override void PreSend()
        {
            base.PreSend();
            ms.WriteByte(brightness);
        }
    }
    class ColorCommands : Commands
    {
        byte[] color = new byte[] { 0, 0, 0 };
        public ColorCommands() : base('C') { }
        public ColorCommands(byte[] _color) : base('C') { color = _color; }
        protected override void PreSend()
        {
            base.PreSend();
            ms.Write(color, 0, 3);
        }
    }
}
