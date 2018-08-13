using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEWConverter.Models;
using SEWConverter.IO;
using System.IO;

namespace SEWConverter
{
    public class WAV
    {
        private Stream _stream;
        private WAVHeader _header;

        private bool _fileInitialized = false;

        public int ChannelCount { get => _header.channelCount; }
        public int SampleRate { get => _header.sampleRate; }

        #region Constructors
        public WAV()
        {

        }

        public WAV(string file)
        {
            SetFile(file);
        }
        #endregion

        #region Init
        public void SetFile(string file)
        {
            if (!File.Exists(file))
                Program.ExitWithError($"File {file} doesn't exist.");

            _stream = File.OpenRead(file);

            ParseHeader();

            _fileInitialized = true;
        }

        private void ParseHeader()
        {
            using (var br = new BinaryReaderX(_stream, true))
            {
                _header = br.ReadStruct<WAVHeader>();

                if (_header.channelCount > 2)
                    Program.ExitWithError("ChannelCount can't exceed 2.");

                if (_header.formatTag != 1)
                    Program.ExitWithError("FormatTag must be 1.");
            }
        }

        private void InitializeHeader(int audioLength, int channelCount, int sampleRate)
        {
            _header = new WAVHeader
            {
                fileSize = audioLength * 4 + 0x2C - 0x8,
                channelCount = (short)channelCount,
                sampleRate = sampleRate,
                avgBytesPerSec = sampleRate * 4,

                dataSize = audioLength * 4
            };
        }

        private bool CheckInitialization()
        {
            if (!_fileInitialized || _stream == null)
                Console.WriteLine("File is not initialized. Process can't advance.");

            if (_header == null)
                Console.WriteLine("Header could not be read.");

            return _fileInitialized && _stream != null && _header != null;
        }
        #endregion

        public int[] GetAudioData()
        {
            if (!CheckInitialization())
                return null;

            using (var br = new BinaryReaderX(_stream, true))
            {
                br.BaseStream.Position = 0x2c;
                return br.ReadMultiple<int>((int)(br.BaseStream.Length - br.BaseStream.Position) / 4).ToArray();
            }
        }

        public void Create(string file, int[] audioData, int channelCount, int sampleRate)
        {
            InitializeHeader(audioData.Length, channelCount, sampleRate);

            using (var bw = new BinaryWriterX(File.Create(file)))
            {
                bw.WriteStruct(_header);
                bw.WriteMultiple(audioData);
            }

            SetFile(file);
        }
    }
}
