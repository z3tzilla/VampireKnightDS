using System;
using System.Collections.Generic;
using System.IO;

namespace VampireKnightDS
{
    public class Pointer
    {
        public uint At;
        public uint PointsAt;
        public string TextAt;

        public static List<Pointer> ScanForPointers(ref byte[] sourceBytes, List<Tuple<uint, uint>> ignoreRegions)
        {
            Console.WriteLine("Scanning for pointers...");
            byte[] currentValue = { 0, sourceBytes[0], sourceBytes[1], sourceBytes[2] };
            var pointers = new List<Pointer>();

            for (uint i = 3; i < sourceBytes.Length; i++)
            {
                bool ignoreRegion = false;
                for (int j = 0; j < ignoreRegions.Count; j++)
                {
                    if ((ignoreRegions[j].Item1 >= i) && (ignoreRegions[j].Item2 <= i))
                    {
                        ignoreRegion = true;
                        break;
                    }
                }
                if (ignoreRegion) continue;

                currentValue[0] = currentValue[1];
                currentValue[1] = currentValue[2];
                currentValue[2] = currentValue[3];
                currentValue[3] = sourceBytes[i];

                if (currentValue[3] != 0x02) continue;
                if ((i - 3) % 4 != 0) continue;   // ALIGN by 4 bytes

                uint pointsAt = Convert.ToUInt32(currentValue[0] + (currentValue[1] * 0x100) + (currentValue[2] * 0x10000));

                if (pointsAt >= sourceBytes.Length - 2) continue;
                if (pointsAt % 4 != 0) continue;   // ALIGN by 4 bytes

                pointers.Add(new Pointer { At = i - 3, PointsAt = pointsAt });
            }

            return pointers;
        }

        public static List<string> GetPointerGroups(ref byte[] sourceBytes, List<Tuple<uint, uint>> ignoreRegions)
        {
            var pointers = ScanForPointers(ref sourceBytes, ignoreRegions);
            List<string> results = new List<string>();

            Console.WriteLine("Analysing pointers...");
            results.Add($"{pointers[0].At} -> {pointers[0].PointsAt}");
            uint lastAddress = pointers[0].At;
            for (int i = 1; i < pointers.Count; i++)
            {
                if (pointers[i].At - lastAddress != 4) results.Add("");
                results.Add($"{pointers[i].At} -> {pointers[i].PointsAt}");
                lastAddress = pointers[i].At;
            }

            return results;
        }
    }
}
