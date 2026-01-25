using System;
using System.IO;
using System.Text;
using System.Linq;

namespace CsharpToC.Roundtrip.Tests
{
    public static class CdrDumper
    {
        private static readonly string OutputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "cdr_dumps");

        static CdrDumper()
        {
            if (!Directory.Exists(OutputDir))
            {
                Directory.CreateDirectory(OutputDir);
            }
        }

        public static void SaveHexArgs(string topicName, int seed, string suffix, byte[] data)
        {
            string cleanTopic = topicName.Replace("::", "_").Replace(":", "_");
            string filename = $"{cleanTopic}_{seed}_{suffix}.hex";
            string path = Path.Combine(OutputDir, filename);
            File.WriteAllText(path, ToHexString(data));
            Console.WriteLine($"   [CDR Dump] Saved {path}");
        }
        
        public static void SaveBin(string topicName, int seed, string suffix, byte[] data)
        {
            string cleanTopic = topicName.Replace("::", "_").Replace(":", "_");
            string filename = $"{cleanTopic}_{seed}_{suffix}.bin";
            string path = Path.Combine(OutputDir, filename);
            File.WriteAllBytes(path, data);
        }

        public static string ToHexString(byte[] data)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in data)
            {
                sb.AppendFormat("{0:x2} ", b);
            }
            return sb.ToString().Trim();
        }

        public static bool Compare(byte[] received, byte[] serialized, out string error)
        {
            if (received.Length != serialized.Length)
            {
                error = $"Length mismatch: Received {received.Length} bytes, Serialized {serialized.Length} bytes.";
                return false;
            }

            for (int i = 0; i < received.Length; i++)
            {
                if (received[i] != serialized[i])
                {
                    error = $"Byte mismatch at index {i}: Received {received[i]:x2}, Serialized {serialized[i]:x2}";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }
    }
}
