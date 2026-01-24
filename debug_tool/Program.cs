using System;
using System.Reflection;
using System.Runtime.InteropServices;
using CycloneDDS.Core;
using CycloneDDS.Runtime;

namespace DebugTool
{
    public class Program
    {
        public static void Main()
        {
            try {
                // Seed 1
                TestCompositeKey("Str_1", 1, 1, "Str_1", 1.25, 1);
                
                // Seed 100
                TestCompositeKey("Str_100", 1, 1, "Str_100", 1.25, 1);
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }

        private static void TestCompositeKey(
            string Region, 
            int Zone, 
            short Sector,
            string Name,
            double Value,
            int Priority)
        {
            Console.WriteLine($"\n--- TestCompositeKey: Region='{Region}', Name='{Name}' ---");
            
            var encoding = CdrEncoding.Xcdr2;
            int initialOffset = 4;
            var sizer = new CdrSizer(initialOffset, encoding);
            bool isXcdr2 = encoding == CdrEncoding.Xcdr2;

            Console.WriteLine($"Start Sizer at {sizer.Position}");

            // DHEADER
            if (encoding == CdrEncoding.Xcdr2)
            {
                sizer.Align(4);
                Console.WriteLine($"After Align(4) for DHEADER: {sizer.Position}");
                sizer.WriteUInt32(0);
                Console.WriteLine($"After DHEADER (4 bytes): {sizer.Position}");
            }

            // Struct body
            sizer.Align(4); 
            Console.WriteLine($"After Align(4) for Region: {sizer.Position}");
            
            // Region String
            int preStr = sizer.Position;
            sizer.WriteString(Region, isXcdr2);
            Console.WriteLine($"After Region ({Region.Length} chars): {sizer.Position} (Used {sizer.Position - preStr})");

            sizer.Align(4); 
            Console.WriteLine($"After Align(4) for Zone: {sizer.Position}");
            sizer.WriteInt32(Zone); 
            Console.WriteLine($"After Zone (4 bytes): {sizer.Position}");
            
            sizer.Align(2); 
            Console.WriteLine($"After Align(2) for Sector: {sizer.Position}");
            sizer.WriteInt16(Sector); 
            Console.WriteLine($"After Sector (2 bytes): {sizer.Position}");
            
            sizer.Align(4); 
            Console.WriteLine($"After Align(4) for Name: {sizer.Position}");
            
            // Name String
            preStr = sizer.Position;
            sizer.WriteString(Name, isXcdr2); 
            Console.WriteLine($"After Name ({Name.Length} chars): {sizer.Position} (Used {sizer.Position - preStr})");
            
            sizer.Align(8); 
            Console.WriteLine($"After Align(8) for Value: {sizer.Position}");
            sizer.WriteDouble(Value); 
            Console.WriteLine($"After Value (8 bytes): {sizer.Position}");
            
            sizer.Align(4); 
            Console.WriteLine($"After Align(4) for Priority: {sizer.Position}");
            sizer.WriteInt32(Priority); 
            Console.WriteLine($"After Priority (4 bytes): {sizer.Position}");

            int delta = sizer.GetSizeDelta(initialOffset);
            int total = delta + 4; // Start + DHEADER... wait, delta includes DHEADER written by sizer?
            // DdsWriter does: payloadSize = _sizer(sample, 4, encoding);
            // totalSize = payloadSize + 4.
            
            Console.WriteLine($"Sizer Delta: {delta}");
            Console.WriteLine($"Computed TotalSize (Delta+4): {total}");
        }
    }
}
