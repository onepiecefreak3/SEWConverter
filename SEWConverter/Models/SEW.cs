using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEWConverter.IO;

namespace SEWConverter.Models
{
    public class SEWHeader
    {
        [FieldLength(4)]
        public string magic = "FWSE";
        public int version = 3;
        public int footerOffset;
        public int dataOffset = 0x400;
        public int channelCount;
        public int sampleCount;
        public int sampleRate;
        public int unk1 = 0x10;
        public int loopStart;
        public int loopEnd;
    }

    public class SEWFooter
    {
        [FieldLength(4)]
        public string magic = "tIME";
        public int timeLength = 8;
        public short year;
        public byte month;
        public byte day;
        public byte hour;
        public byte minute;
        public byte second;
        public byte padding1 = 0;
        [FieldLength(4)]
        public string magic2 = "ver.";
        public int verLength = 4;
        public int version = 3;
    }
}
