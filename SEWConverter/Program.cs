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
    public class Program
    {
        public static int Clamp(int input, int min, int max) => Math.Min(max, Math.Max(min, input));

        public static void ExitWithError(string message)
        {
            Console.WriteLine(message);
            Environment.Exit(0);
        }

        static void Main(string[] args)
        {
            if (args.Count() < 2)
            {
                Console.WriteLine("Usage: SEWConverter.exe <mode> <path> [version=3] [loopstart=0] [loopend=0]\nThe optional parameters are only used by mode -e and have a default value.");
                Environment.Exit(0);
            }

            if (args[0] != "-d" && args[0] != "-e")
            {
                Console.WriteLine($"Unknown mode \"{args[0]}\".\n\nSupported modes:\n-d\tDecode a sew to wav\n-e\tEncode a wav to sew");
                Environment.Exit(0);
            }

            if (!File.Exists(args[1]))
            {
                Console.WriteLine($"Couldn't open file {args[1]}.");
                Environment.Exit(0);
            }

            var version = 3;
            if (args.Count() > 2)
            {
                if (!Int32.TryParse(args[2], out version))
                {
                    throw new Exception("Version isn't a valid number!");
                }
                else
                {
                    if (version < 0)
                    {
                        throw new Exception("Version can't be negative!");
                    }
                }
            }

            if (args[0] == "-d")
                DecodeSEWtoWAV(args[1]);
            else if (args[0] == "-e")
            {
                var loopStart = -1;
                var loopEnd = -1;
                if (args.Count() > 3)
                {
                    if (!Int32.TryParse(args[3], out loopStart))
                    {
                        loopStart = -1;
                    }
                    else
                    {
                        if (loopStart < 0) loopStart = -1;
                    }
                }
                if (args.Count() > 4)
                {
                    if (!Int32.TryParse(args[4], out loopEnd))
                    {
                        loopEnd = -1;
                    }
                    else
                    {
                        if (loopEnd < 0) loopEnd = -1;
                    }
                }

                EncodeWAVtoSEW(args[1], version, loopStart, loopEnd);
            }
        }

        public static void DecodeSEWtoWAV(string file)
        {
            using (var br = new BinaryReaderX(File.OpenRead(file)))
            {
                var sew = new SEW(file);
                var wav = new WAV();

                sew.PrintMeta();
                var decodedData = sew.Decode();

                wav.Create(Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".wav"), decodedData, sew.ChannelCount, sew.SampleRate);
            }
        }

        public static void EncodeWAVtoSEW(string file, int version, int loopStart, int loopEnd)
        {
            using (var br = new BinaryReaderX(File.OpenRead(file)))
            {
                var sew = new SEW();
                var wav = new WAV(file);

                var decodedData = wav.GetAudioData();

                sew.Create(Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".sew"), decodedData, wav.ChannelCount, wav.SampleRate, loopStart, loopEnd);
            }
        }
    }
}
