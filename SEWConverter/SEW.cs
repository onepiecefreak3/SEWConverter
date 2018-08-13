using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SEWConverter.IO;
using SEWConverter.Models;

namespace SEWConverter
{
    public class SEW
    {
        private int[] index_table = new int[] { 8, 6, 4, 2, -1, -1, -1, -1, -1, -1, -1, -1, 2, 4, 6, 8 };
        private int[] step_table = new int[] { 7, 8, 9, 10, 11, 12, 13, 14, 16, 17, 19, 21, 23, 25, 28, 31, 34, 37, 41, 45, 50, 55, 60, 66, 73, 80, 88, 97, 107, 118, 130, 143, 157, 173, 190, 209, 230, 253, 279, 307, 337, 371, 408, 449, 494, 544, 598, 658, 724, 796, 876, 963, 1060, 1166, 1282, 1411, 1552, 1707, 1878, 2066, 2272, 2499, 2749, 3024, 3327, 3660, 4026, 4428, 4871, 5358, 5894, 6484, 7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899, 15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767 };

        private Stream _stream = null;
        private SEWHeader _header = null;
        private SEWFooter _footer = null;
        private bool _fileInitialized = false;

        private int _predSample = 0;
        private int _index = 0;

        private int[] _loopPredSample = new int[6];
        private int[] _loopIndex = new int[6];

        public int ChannelCount { get => (CheckInitialization()) ? _header.channelCount : -1; }
        public int SampleRate { get => (CheckInitialization()) ? _header.sampleRate : -1; }
        public int LoopStart { get => (CheckInitialization()) ? _header.loopStart : -1; }
        public int LoopEnd { get => (CheckInitialization()) ? _header.loopEnd : -1; }

        #region Constructor
        public SEW()
        {

        }

        public SEW(string file)
        {
            SetFile(file);
        }
        #endregion

        #region Init
        public void SetFile(string file)
        {
            Initialize(file);
        }

        private void Initialize(string file)
        {
            if (!File.Exists(file))
                throw new Exception($"File {file} doesn't exist.");

            _stream = File.OpenRead(file);

            ParseHeader();

            ParseFooter();

            _fileInitialized = true;
        }

        private bool CheckInitialization()
        {
            if (!_fileInitialized || _stream == null)
                Console.WriteLine("File is not initialized. Process can't advance.");

            if (_header == null)
                Console.WriteLine("Header could not be read.");

            if (_footer == null)
                Console.WriteLine("Footer could not be read.");

            return _fileInitialized && _stream != null && _header != null && _footer != null;
        }

        private void ParseHeader()
        {
            using (var br = new BinaryReaderX(_stream, true))
            {
                _header = br.ReadStruct<SEWHeader>();

                //check info
                if (_header.magic != "FWSE")
                    Program.ExitWithError("This is no valid FWSE file.");
                if (_header.channelCount > 2)
                    Program.ExitWithError("Only sew's with 1 or 2 channels are supported.");
                if (_header.version != 3)
                    Program.ExitWithError("Only version 3 is supported.");
            }
        }

        private void ParseFooter()
        {
            using (var br = new BinaryReaderX(_stream, true))
            {
                br.BaseStream.Position = _header.footerOffset;
                _footer = br.ReadStruct<SEWFooter>();

                //check info
                if (_footer.magic != "tIME" || _footer.magic2 != "ver.")
                    Program.ExitWithError("This is no valid FWSE file.");
                if (_footer.version != 3)
                    Program.ExitWithError("Only version 3 is supported.");
            }
        }

        private void InitializeCreation(int audioLength, int channelCount, int sampleRate, int loopStart, int loopEnd)
        {
            _header = new SEWHeader
            {
                footerOffset = 0x400 + audioLength / 2 + audioLength % 2,
                channelCount = channelCount,
                sampleRate = sampleRate,
                sampleCount = audioLength / channelCount,
                loopStart = loopStart,
                loopEnd = loopEnd
            };

            _footer = new SEWFooter
            {
                year = (short)DateTime.Today.Year,
                month = (byte)DateTime.Today.Month,
                day = (byte)DateTime.Today.Day,

                hour = (byte)DateTime.Today.Hour,
                minute = (byte)DateTime.Today.Minute,
                second = (byte)DateTime.Today.Second
            };
        }
        #endregion

        public void PrintMeta()
        {
            if (!CheckInitialization())
                return;

            //Output meta
            Console.WriteLine($"\nMeta:\n" +
                    $"  Version: {_header.version}\n" +
                    $"\n" +
                    $"  Channels: {_header.channelCount}\n" +
                    $"  SampleRate: {_header.sampleRate}\n" +
                    $"  Samples: {_header.sampleCount}\n" +
                    $"\n" +
                    $"  LoopStart: {_header.loopStart}\n" +
                    $"  LoopEnd: {_header.loopEnd}");
        }

        #region Decoding
        public int[] Decode()
        {
            if (!CheckInitialization())
                return null;

            using (var br = new BinaryReaderX(_stream, true))
            {
                br.BaseStream.Position = _header.dataOffset;

                (_predSample, _index) = (0, 0);
                List<int> result = new List<int>();

                var input = br.ReadBytes(_header.footerOffset - _header.dataOffset);
                if (_header.channelCount <= 1)
                    foreach (var n in input.SelectMany(i => new[] { i / 16, i % 16 }))
                        result.Add(DecodeNibble(n));
                else
                {
                    var channelDecode = new List<List<int>>();

                    channelDecode.Add(new List<int>());
                    foreach (var n in input.Select(i => i / 16))
                        channelDecode[0].Add(DecodeNibble(n));

                    (_predSample, _index) = (0, 0);
                    channelDecode.Add(new List<int>());
                    foreach (var n in input.Select(i => i % 16))
                        channelDecode[1].Add(DecodeNibble(n));

                    for (int i = 0; i < _header.sampleCount; i++)
                        for (int j = 0; j < _header.channelCount; j++)
                            result.Add(channelDecode[j][i]);
                }

                return result.ToArray();
            }
        }

        private int DecodeNibble(int n)
        {
            _predSample = _predSample + (2 * n - 15) * step_table[_index];
            _index = Program.Clamp(_index + index_table[n], 0, 88);

            //one sample has 20 bits
            return _predSample << 12;
        }
        #endregion

        #region Encoding
        public byte[] Encode(int[] audioData, int channel = -1)
        {
            List<byte> result = new List<byte>();

            (_predSample, _index) = (0, 0);
            var s = false;
            var r = 0;
            for (int i = 0; i < audioData.Length; i++)
            {
                if ((_header?.loopStart + 0x20 ?? -1) == i && channel >= 0)
                {
                    _loopPredSample[channel] = _predSample;
                    _loopIndex[channel] = _index;
                }
                var encNibble = EncodeNibble(audioData[i]);

                if (!s)
                {
                    r |= encNibble << 4;
                }
                else
                {
                    r |= encNibble;
                    result.Add((byte)r);
                    r = 0;
                }
                s = !s;
            }
            if (s)
                result.Add((byte)r);

            return result.ToArray();
        }

        private byte EncodeNibble(int sample)
        {
            var n = (byte)Program.Clamp(((int)Math.Floor(((sample >> 12) - _predSample) / 2.0 / step_table[_index]) + 8), 0, 15);

            DecodeNibble(n);

            return n;
        }
        #endregion

        #region Create
        public void Create(string file, int[] audioData, int channelCount, int sampleRate, int loopStart = -1, int loopEnd = -1)
        {
            InitializeCreation(audioData.Length, channelCount, sampleRate, loopStart, loopEnd);

            using (var bw = new BinaryWriterX(File.Create(file)))
            {
                bw.WriteStruct(_header);

                var encodedData = CreateEncodedData(audioData);

                //Loop data
                bw.Write(CreateLoopData(audioData));

                //Crossfaded data
                bw.Write(CreateCrossfadedData(audioData));

                bw.WriteAlignment(0x100);
                bw.Write(encodedData);

                bw.WriteStruct(_footer);
            }

            SetFile(file);
        }

        private byte[] CreateLoopData(int[] audioData)
        {
            if (_header.loopStart < 0 && _header.loopEnd < 0)
                return new byte[12 * 4];

            int[] loopData = new int[12];
            for (int i = 0; i < _header.channelCount; i++)
            {
                loopData[i] = _loopPredSample[i];
                loopData[i + 6] = _loopIndex[i];
            }

            var ms = new MemoryStream();
            using (var bw = new BinaryWriterX(ms, true))
                bw.WriteMultiple(loopData);

            return ms.ToArray();
        }

        private byte[] CreateCrossfadedData(int[] audioData)
        {
            if (_header.loopStart < 0 && _header.loopEnd < 0)
                return new byte[6 * 32 * 4];

            int[] crossfadedData = new int[6 * 32];

            //Crossfading
            for (int j = 0; j < 32; j++)
                for (int i = 0; i < _header.channelCount; i++)
                {
                    var loopStartSample = audioData[(_header.loopStart + j) * _header.channelCount + i];
                    var loopEndSample = audioData[(_header.loopEnd + j) * _header.channelCount + i];

                    crossfadedData[j * _header.channelCount + i] = ((loopStartSample >> 12) * j + (loopEndSample >> 12) * (32 - j)) / 32;
                }

            var ms = new MemoryStream();
            using (var bw = new BinaryWriterX(ms, true))
                bw.WriteMultiple(crossfadedData);

            return ms.ToArray();
        }

        private byte[] CreateEncodedData(int[] audioData)
        {
            var ms = new MemoryStream();
            using (var bw = new BinaryWriterX(ms, true))
                if (_header.channelCount <= 1)
                    bw.Write(Encode(audioData, 0));
                else
                {
                    var channelData = new List<int[]>();
                    channelData.Add(audioData.Where((s, i) => i % 2 == 0).ToArray());
                    channelData.Add(audioData.Where((s, i) => i % 2 == 1).ToArray());

                    var encoded = new List<int[]>();
                    encoded.Add(Encode(channelData[0], 0).SelectMany((i) => new[] { i / 16, i % 16 }).ToArray());
                    encoded.Add(Encode(channelData[1], 1).SelectMany((i) => new[] { i / 16, i % 16 }).ToArray());

                    for (int i = 0; i < encoded[0].Length; i++)
                    {
                        byte res = 0;
                        for (int j = 0; j < _header.channelCount; j++)
                            res |= (byte)(encoded[j][i] << ((_header.channelCount - 1 - j) * 4));
                        bw.Write(res);
                    }
                }

            return ms.ToArray();
        }
        #endregion
    }
}
