using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace VampireKnightDS
{ 
    /// <summary>
    /// Binary data block containing 1 or more lines.
    /// </summary>
    public class DataBlock
    {
        internal string description;
        internal uint offset;
        internal uint length;
        internal bool sorted;
        internal List<DataLine> lines;

        public void ReadLines(List<Pointer> pointers, ref byte[] sourceBytes, bool sorted)
        {
            Console.WriteLine($"Loading block {description}...");

            var orderedPointers = pointers.OrderBy(p => p.PointsAt).ToArray();
            uint endOffset = offset + length;
            uint lineOffset = offset;
            uint lineEndOffset;
            uint lastLineSize = 0;
            int pointerIndex = 0;

            lines = new List<DataLine>();

            while (orderedPointers[pointerIndex].PointsAt < lineOffset) pointerIndex++;
            var pointerOffsets = new List<uint>();

            while (lineOffset < endOffset)
            {
                pointerOffsets.Clear();
                while (orderedPointers[pointerIndex].PointsAt == lineOffset)
                {
                    pointerOffsets.Add(orderedPointers[pointerIndex].At);
                    pointerIndex++;
                    if (pointerIndex >= orderedPointers.Length) break;
                }

                if (pointerIndex < orderedPointers.Length)
                {
                    while ((orderedPointers[pointerIndex].PointsAt < lineOffset) || (sorted && (orderedPointers[pointerIndex].PointsAt - lineOffset + 1 < lastLineSize)))
                    {
                        pointerIndex++;
                        if (pointerIndex >= orderedPointers.Length) break;
                    }
                }

                lineEndOffset = pointerIndex >= orderedPointers.Length ? endOffset - 1 : orderedPointers[pointerIndex].PointsAt - 1;
                lastLineSize = lineEndOffset - lineOffset + 1;

                lines.Add(new DataLine
                {
                    PointerAddresses = pointerOffsets.ToArray(),
                    Offset = lineOffset,
                    Data = sourceBytes.Skip((int)lineOffset).Take((int)lastLineSize).ToArray()
                });

                lineOffset = lineEndOffset + 1;
            }
        }

        public void DumpTextData(string destinationFile, FontMap fontMap)
        {
            Console.WriteLine($"Printing block {description}");
            File.WriteAllLines(destinationFile,
            lines.Select(line =>
                $"{string.Join(',', line.PointerAddresses)}|{line.Offset}|{fontMap.FromBinary(line.Data)}"));
        }

        public IEnumerable<DataLine> GetExcessiveLines()
        {
            IEnumerable<DataLine> enumerator = sorted ? lines.AsEnumerable() : lines.OrderBy(l => l.Data.Length);
            var newLines = new List<DataLine>();
            var excessiveLines = new List<DataLine>();

            int totalLength = 0;
            enumerator.Aggregate(totalLength, (accum, line) =>
            {
                if (accum + line.Data.Length <= length)
                {
                    newLines.Add(line);
                }
                else
                {
                    excessiveLines.Add(line);
                }
                return accum + line.Data.Length;
            });
            lines = newLines;
            return excessiveLines;
        }
    }

    /// <summary>
    /// A single line from script.
    /// </summary>
    public class DataLine
    {
        public uint[] PointerAddresses { get; set; }
        public uint Offset { get; set; }
        public byte[] Data { get; set; }
    }
}