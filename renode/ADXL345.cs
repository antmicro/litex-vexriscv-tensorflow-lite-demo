//
// Copyright (c) 2010-2019 Antmicro
//
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.I2C;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.Sensors
{
    public class ADXL345 : II2CPeripheral
    {
        public ADXL345()
        {
            samplesFifo = new Queue<Sample>();
            rawDataFifo = new Queue<byte>();

            MaxFifoDepth = 32;
        }

        public void Reset()
        {
            range = Range.G2;
            fullResolution = false;
            lastRegister = 0;
            rawDataFifo.Clear();
            bufferedSamples = null;

            lock(samplesFifo)
            {
                samplesFifoEmptied = null;
                samplesFifo.Clear();
            }
        }

        public void Write(byte[] data)
        {
            if(data.Length == 0)
            {
                this.Log(LogLevel.Warning, "Write with no data. It's strange");
                return;
            }

            this.Log(LogLevel.Noisy, "Write with {0} bytes of data", data.Length);

            lastRegister = (Registers)data[0];
            this.Log(LogLevel.Noisy, "Setting register ID to 0x{0:X} - {0}", lastRegister);

            if(data.Length > 1)
            {
                this.Log(LogLevel.Noisy, "Handling register write");
                HandleRegisterWrite(lastRegister, data.Skip(1));
            }
            else
            {
                this.Log(LogLevel.Noisy, "Handling register read");
                if(lastRegister == Registers.Xdata0)
                {
                    lock(samplesFifo)
                    {
                        if(!samplesFifo.TryDequeue(out var sample))
                        {
                            this.Log(LogLevel.Warning, "Reading from Xdata0 register, but there are no samples");
                            return;
                        }

                        rawDataFifo.Clear();
                        rawDataFifo.EnqueueRange(sample.GetRepresentation(fullResolution, range));
                        if(samplesFifo.Count == 0 && samplesFifoEmptied != null)
                        {
                            samplesFifoEmptied();
                        }
                    }
                }
            }
        }

        public void FeedSample(short x, short y, short z, int repeat = 1)
        {
            if(repeat <= 0)
            {
                SamplesFifoEmptied = () => FeedSampleInner(x, y, z);
            }
            else
            {
                lock(samplesFifo)
                {
                    SamplesFifoEmptied = null;
                    FeedSampleInner(x, y, z, repeat);
                }
            }
        }

        public void FeedSample(string path, int repeat = 1)
        {
            if(!TryParseSamplesFile(path, out bufferedSamples))
            {
                return;
            }

            if(repeat <= 0)
            {
                SamplesFifoEmptied = () => FeedSampleInner(bufferedSamples);
            }
            else
            {
                lock(samplesFifo)
                {
                    SamplesFifoEmptied = null;
                    FeedSampleInner(bufferedSamples, repeat);
                }
            }
        }

        public byte[] Read(int count = 1)
        {
            this.Log(LogLevel.Noisy, "Reading {0} bytes from register 0x{1:X} - {1}", count, lastRegister);

            byte[] result = null;

            switch(lastRegister)
            {
                case Registers.DeviceID:
                    result = new[] { DevID };
                    break;

                case Registers.Xdata0:
                case Registers.Xdata1:
                case Registers.Ydata0:
                case Registers.Ydata1:
                case Registers.Zdata0:
                case Registers.Zdata1:
                    result = new byte[count];
                    for(int i = 0; i < count; i++) {
                        result[i] = rawDataFifo.TryDequeue(out var val) ? val : (byte)0 ;
                    }
                    break;

                case Registers.FifoStatus:
                    result = new[] { (byte)(Math.Min(samplesFifo.Count, MaxFifoDepth)) };
                    break;

                default:
                    result = new byte[0];
                    this.Log(LogLevel.Warning, "Reading from an unsupported or not-yet-implemented register: 0x{0:X} - {0}", lastRegister);
                    break;
            }

            lastRegister = (Registers)(((int)lastRegister + 1) % 0x40);
            this.Log(LogLevel.Noisy, "Auto-incrementing to the next register 0x{0:X} - {0}", lastRegister);

            this.Log(LogLevel.Noisy, "Read result: {0}", Misc.PrettyPrintCollection(result));
            return result;
        }

        public int MaxFifoDepth { get; set; }

        private void HandleRegisterWrite(Registers registerId, IEnumerable<byte> data)
        {
            this.Log(LogLevel.Noisy, "Writing value {0} to register {1}", Misc.PrettyPrintCollection(data), registerId);

            switch(registerId)
            {
                case Registers.DataFormatControl:
                    var b = data.FirstOrDefault();
                    range = (Range)(b & 0x3);
                    fullResolution = (b >> 3) != 0;
                    break;
                default:
                    this.Log(LogLevel.Warning, "Writing to an unsupported or not-yet-implemented register: 0x{0:X} - {0}", registerId);
                    break;
            }
        }

        private bool TryParseSamplesFile(string path, out IEnumerable<Sample> samples)
        {
            var localQueue = new Queue<Sample>();

            var lineNumber = 0;
            using(var reader = File.OpenText(path))
            {
                var line = "";
                while((line = reader.ReadLine()) != null)
                {
                    ++lineNumber;
                    var numbers = line.Split(new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToArray();

                    if(numbers.Length != 3
                            || !short.TryParse(numbers[0], out var x)
                            || !short.TryParse(numbers[1], out var y)
                            || !short.TryParse(numbers[2], out var z))
                    {
                        this.Log(LogLevel.Error, "Wrong data file format at line {0}: {1}", lineNumber, line);

                        samples = null;
                        return false;
                    }
                    localQueue.Enqueue(new Sample(x, y, z));
                }
            }

            samples = localQueue;
            return true;
        }

        private void FeedSampleInner(short x, short y, short z, int repeat = 1)
        {
            lock(samplesFifo)
            {
                var sample = new Sample(x, y, z);
                for(var i = 0; i < repeat; i++)
                {
                    samplesFifo.Enqueue(sample);
                }
            }
        }

        private void FeedSampleInner(IEnumerable<Sample> samples, int repeat = 1)
        {
            if(samples == null)
            {
                return;
            }

            lock(samplesFifo)
            {
                for(var i = 0; i < repeat; i++)
                {
                    samplesFifo.EnqueueRange(samples);
                }
            }
        }

        private Action SamplesFifoEmptied
        {
            get => samplesFifoEmptied;

            set
            {
                lock(samplesFifo)
                {
                    samplesFifoEmptied = value;
                    if(samplesFifoEmptied != null && samplesFifo.Count == 0)
                    {
                        samplesFifoEmptied();
                    }
                }
            }
        }

        private Registers lastRegister;
        private Range range;
        private bool fullResolution;
        private IEnumerable<Sample> bufferedSamples;
        private Action samplesFifoEmptied;

        private readonly Queue<Sample> samplesFifo;
        private readonly Queue<byte> rawDataFifo;

        private const byte DevID = 0xe5;

        private struct Sample
        {
            public Sample(short x, short y, short z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public byte[] GetRepresentation(bool fullRes, Range range)
            {
                return GetSingleRepresentation(X, fullRes, range).Concat(
                        GetSingleRepresentation(Y, fullRes, range)).Concat(
                        GetSingleRepresentation(Z, fullRes, range)).ToArray();
            }

            public override string ToString()
            {
                return $"[X: {X}, Y: {Y}, Z: {Z}]";
            }

            public short X;
            public short Y;
            public short Z;

            private static byte[] GetSingleRepresentation(short x, bool fullRes, Range range)
            {
                var shifter = 2;

                if(!fullRes)
                {
                    shifter = (int)range + 2;
                }

                var v = x >> shifter;
                return new [] { (byte)v, (byte)(v >> 8) };
            }
        }

        private enum Range
        {
            G2 = 0,
            G4 = 1,
            G8 = 2,
            G16 = 3
        }

        private enum Registers : byte
        {
            DeviceID = 0x00,
            // 0x01 to 0x1C are reserved
            TapThreshold = 0x1D,
            Xoffset = 0x1E,
            Yoffset = 0x1F,
            Zoffset = 0x20,
            TapDuration = 0x21,
            TapLatency = 0x22,
            TapWindow = 0x23,
            ActivityThreshold = 0x24,
            InactivityThreshold = 0x25,
            InactivityTime = 0x26,
            AxisEnableControlForActivityAndInactivityDetection = 0x27,
            FreeFallThreshold = 0x28,
            FreeFallTime = 0x29,
            AxisControlForSingleTapDoubleTap = 0x2A,
            SourceOfSingleTapDoubleTap = 0x2B,
            DataRateAndPowerModeControl = 0x2C,
            PowerSavingFeaturesControl = 0x2D,
            InterruptEnableControl = 0x2E,
            InterruptMappingControl = 0x2F,
            SourceOfInterrupts = 0x30,
            DataFormatControl = 0x31,
            Xdata0 = 0x32,
            Xdata1 = 0x33,
            Ydata0 = 0x34,
            Ydata1 = 0x35,
            Zdata0 = 0x36,
            Zdata1 = 0x37,
            FifoControl = 0x38,
            FifoStatus = 0x39
        }
    }
}
