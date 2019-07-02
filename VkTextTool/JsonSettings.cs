using System;

namespace VampireKnightDS
{
    public class JsonBlockSettings
    {
        public string Description { get; set; }
        public uint Offset { get; set; }
        public uint Length { get; set; }
        public bool Sorted { get; set; } = false;
    }

    public class JsonSettings
    {
        public string PathToArm9Bin { get; set; }
        public string PathToSaveModifiedArm9Bin { get; set; }
        public string PathToFontMap { get; set; }
        public string ExportDirectory { get; set; }
        public JsonIgnoreRegionsForPointerScanSettings[] IgnoreRegionsForPointerScan { get; set; }
        public JsonInjectionSettings Injection { get; set; }
        public JsonBlockSettings[] Blocks { get; set; }
        public string[] PathsToTranslatedFiles { get; set; }
    }

    public class JsonInjectionSettings
    {
        public uint InjectionAddressPointer { get; set; }
        public uint EndOfFilePointer { get; set; }
        public uint FileFooterPointer { get; set; }
    }

    public class JsonIgnoreRegionsForPointerScanSettings
    {
        public uint From { get; set; }
        public uint To { get; set; }
    }
}
