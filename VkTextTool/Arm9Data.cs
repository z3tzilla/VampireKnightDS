using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VampireKnightDS
{
    /// <summary>
    /// Collection of data blocks from arm9.bin
    /// </summary>
    public class Arm9Data
    {
        JsonSettings settings;
        List<DataBlock> blocks;
        DataBlock blockToInject;
        byte[] sourceBytes;

        public Arm9Data(JsonSettings settings)
        {
            this.settings = settings;
            LoadData();
        }

        private void LoadData()
        {
            sourceBytes = File.ReadAllBytes(settings.PathToArm9Bin);

            blocks = settings.Blocks.Select(block => new DataBlock
            {
                description = block.Description,
                offset = block.Offset,
                length = block.Length,
                sorted = block.Sorted
            }).ToList();

            var pointers = ScanForPointers();

            for (int i = 0; i < blocks.Count; i++)
            {
                blocks[i].ReadLines(pointers, ref sourceBytes, blocks[i].sorted);
            }
        }

        public void DumpTextBlocks()
        {
            var fontMap = new FontMap(settings.PathToFontMap);

            for (int i = 0; i < blocks.Count; i++)
            {
                string destinationFile = Path.Combine(settings.ExportDirectory, blocks[i].description + ".txt");
                blocks[i].DumpTextData(destinationFile, fontMap);
            }
        }

        public void LoadTranslationsAndMakeANewBin()
        {
            LoadTranslations();
            ReorganizeLinesInBlocks();
            WriteNewData();
            InjectData();

            Console.WriteLine("Writing new bin...");
            File.WriteAllBytes(settings.PathToSaveModifiedArm9Bin, sourceBytes);
        }

        private void LoadTranslations()
        {
            Console.WriteLine("Loading translation files...");
            var fontMap = new FontMap(settings.PathToFontMap);
            foreach (string pathToTranslatedFile in settings.PathsToTranslatedFiles)
            {
                var translatedLines = File.ReadAllLines(pathToTranslatedFile);

                foreach (string translatedLine in translatedLines)
                {
                    uint offset = Convert.ToUInt32(translatedLine.Split('|')[0]);
                    byte[] binaryLine = fontMap.ToBinary(translatedLine.Split('|')[1]);

                    var blockLine = blocks
                        .Where(block => (block.offset <= offset) && (block.offset + block.length > offset))
                        .Select(block => block.lines)
                        .First()
                        .First(line => line.Offset == offset);

                    blockLine.Data = binaryLine;
                }
            }
        }

        private void ReorganizeLinesInBlocks()
        {
            Console.WriteLine("Recreating Data Blocks...");
            uint injectionPointer = settings.Injection.InjectionAddressPointer;
            uint injectionAddress = Convert.ToUInt32(
                sourceBytes[injectionPointer] +
                sourceBytes[injectionPointer + 1] * 0x100 +
                sourceBytes[injectionPointer + 2] * 0x10000);

            blockToInject = new DataBlock
            {
                description = "ArmInjectionData",
                offset = injectionAddress,
                length = 0,
                lines = new List<DataLine>()
            };
            foreach (var block in blocks)
            {
                foreach (var line in block.GetExcessiveLines())
                {
                    line.Offset = blockToInject.offset + blockToInject.length;
                    blockToInject.length += (uint)line.Data.Length;
                    blockToInject.lines.Add(line);
                }
            }
        }

        private void WriteNewData()
        {
            Console.WriteLine("Updating data blocks...");
            foreach (var block in blocks)
            {
                WriteBlock(block);
            }
        }

        private void InjectData()
        {
            Console.WriteLine("Injecting new data...");

            uint injectionSize = blockToInject.length;
            while ((injectionSize & 0x1F) > 0) injectionSize++;

            byte[] newBytes = new byte[sourceBytes.Length + injectionSize];
            for (uint i = 0; i < sourceBytes.Length; i++)
            {
                if (i < blockToInject.offset)
                {
                    newBytes[i] = sourceBytes[i];
                }
                else
                {
                    newBytes[i + injectionSize] = sourceBytes[i];
                }
            }

            uint injectionPointer = settings.Injection.InjectionAddressPointer;
            uint unknownPointer = settings.Injection.InjectionAddressPointer + 8;
            uint footerPointer = settings.Injection.FileFooterPointer;
            uint endOfFilePointer = settings.Injection.EndOfFilePointer;

            int newEndOfFileAddress = newBytes.Length;
            int newFooterAddress = 
                sourceBytes[footerPointer] +
                sourceBytes[footerPointer + 1] * 0x100 +
                sourceBytes[footerPointer + 2] * 0x10000 +
                (int)injectionSize;
            int newInjectionAddress =
                sourceBytes[injectionPointer] +
                sourceBytes[injectionPointer + 1] * 0x100 +
                sourceBytes[injectionPointer + 2] * 0x10000 +
                (int)injectionSize;
            int newUnknownAddress =
                sourceBytes[unknownPointer] +
                sourceBytes[unknownPointer + 1] * 0x100 +
                sourceBytes[unknownPointer + 2] * 0x10000 +
                (int)injectionSize;

            newBytes[footerPointer] = Convert.ToByte(newFooterAddress & 0xFF);
            newBytes[footerPointer + 1] = Convert.ToByte((newFooterAddress & 0xFF00) >> 8);
            newBytes[footerPointer + 2] = Convert.ToByte((newFooterAddress & 0xFF0000) >> 16);

            newBytes[endOfFilePointer] = Convert.ToByte(newEndOfFileAddress & 0xFF);
            newBytes[endOfFilePointer + 1] = Convert.ToByte((newEndOfFileAddress & 0xFF00) >> 8);
            newBytes[endOfFilePointer + 2] = Convert.ToByte((newEndOfFileAddress & 0xFF0000) >> 16);

            newBytes[injectionPointer] = Convert.ToByte(newInjectionAddress & 0xFF);
            newBytes[injectionPointer + 1] = Convert.ToByte((newInjectionAddress & 0xFF00) >> 8);
            newBytes[injectionPointer + 2] = Convert.ToByte((newInjectionAddress & 0xFF0000) >> 16);

            newBytes[injectionPointer + 4] = Convert.ToByte(newInjectionAddress & 0xFF);
            newBytes[injectionPointer + 5] = Convert.ToByte((newInjectionAddress & 0xFF00) >> 8);
            newBytes[injectionPointer + 6] = Convert.ToByte((newInjectionAddress & 0xFF0000) >> 16);

            newBytes[unknownPointer] = Convert.ToByte(newUnknownAddress & 0xFF);
            newBytes[unknownPointer + 1] = Convert.ToByte((newUnknownAddress & 0xFF00) >> 8);
            newBytes[unknownPointer + 2] = Convert.ToByte((newUnknownAddress & 0xFF0000) >> 16);

            sourceBytes = newBytes;
            WriteBlock(blockToInject);
        }

        private void WriteBlock(DataBlock block)
        {
            Console.WriteLine($"Writing block {block.description} back to bin...");

            uint blockOffset = block.offset;
            foreach (var line in block.lines)
            {
                int newPointer = (int)blockOffset;

                // Copy bytes
                for (int i = 0; i < line.Data.Length; i++)
                {
                    sourceBytes[blockOffset++] = line.Data[i];
                }

                // Redo pointers
                foreach (var pointer in line.PointerAddresses)
                {
                    sourceBytes[pointer] = Convert.ToByte(newPointer & 0xFF);
                    sourceBytes[pointer + 1] = Convert.ToByte((newPointer & 0xFF00) >> 8);
                    sourceBytes[pointer + 2] = Convert.ToByte((newPointer & 0xFF0000) >> 16);
                }
            }
        }

        public List<string> DumpTextByPointer(uint initialPointer)
        {
            var pointers = ScanForPointers();

            int startingIndex = -1;

            for (int i = 0; i < pointers.Count; i++)
            {
                if (pointers[i].At == initialPointer)
                {
                    startingIndex = i;
                    break;
                }
            }
            if (startingIndex == -1) throw new ArgumentException("No such pointer in file");

            Console.WriteLine("Looking for adjacent text...");

            int dumpStartIndex = startingIndex;
            // Look back
            while (true)
            {
                var pointerToPointer = pointers
                    .DefaultIfEmpty(null)
                    .FirstOrDefault(ptr => ptr.PointsAt == pointers[dumpStartIndex].At);

                if (pointerToPointer != null) break;
                if (dumpStartIndex == 0) break;
                if (pointers[dumpStartIndex].At - pointers[dumpStartIndex - 1].At != 4) break;
                dumpStartIndex--;
            }

            // Look ahead
            int dumpEndIndex = startingIndex;
            while (true)
            {
                if (dumpEndIndex >= pointers.Count - 1) break;
                if (pointers[dumpEndIndex + 1].At - pointers[dumpEndIndex].At != 4) break;

                var pointerToPointer = pointers
                    .DefaultIfEmpty(null)
                    .FirstOrDefault(ptr => ptr.PointsAt == pointers[dumpEndIndex + 1].At);

                if (pointerToPointer != null) break;
                dumpEndIndex++;
            }

            Console.WriteLine("Writing result...");
            var results = new List<string>();
            var fontMap = new FontMap(settings.PathToFontMap);
            for (int i = dumpStartIndex; i <= dumpEndIndex; i++)
            {
                uint offset = pointers[i].PointsAt;

                var blockLine = blocks
                    .Where(block => (block.offset <= offset) && (block.offset + block.length > offset))
                    .Select(block => block.lines)
                    .First()
                    .First(line => line.Offset == offset);

                string textLine = fontMap.FromBinary(blockLine.Data);
                results.Add($"{pointers[i].At}|{offset}|{textLine}");
            }

            return results;
        }

        private List<Pointer> ScanForPointers()
        {
            return Pointer.ScanForPointers(ref sourceBytes,
                settings.IgnoreRegionsForPointerScan.Select(ir => new Tuple<uint, uint>(ir.From, ir.To)).ToList());
        }

        public void ExportPointersToFile(string destinationFile)
        {
            File.WriteAllLines(
                destinationFile,
                Pointer.GetPointerGroups(
                    ref sourceBytes,
                    settings.IgnoreRegionsForPointerScan.Select(ir => new Tuple<uint, uint>(ir.From, ir.To)).ToList())
                );
        }
    }
}
