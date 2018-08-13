using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEWConverter.IO;

namespace SEWConverter.Models
{
    public class WAVHeader
    {
        [FieldLength(4)]
        public string riffMagic = "RIFF";
        public int fileSize;
        [FieldLength(4)]
        public string riffType = "WAVE";

        [FieldLength(4)]
        public string fmtTag = "fmt ";
        public int chunkSize = 0x10;
        public short formatTag = 0x1;
        public short channelCount;
        public int sampleRate;
        public int avgBytesPerSec;
        public short blockAlign = 0x4;
        public short bitsPerSample = 0x20;

        [FieldLength(4)]
        public string dataMagic = "data";
        public int dataSize;
    }
}
