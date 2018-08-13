using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEWConverter.IO
{
    public enum ByteOrder : ushort
    {
        LittleEndian = 0xFEFF,
        BigEndian = 0xFFFE
    }

    public enum BitOrder : byte
    {
        Inherit,
        LSBFirst,
        MSBFirst,
        LowestAddressFirst,
        HighestAddressFirst
    }

    public enum EffectiveBitOrder : byte
    {
        LSBFirst,
        MSBFirst
    }
}
