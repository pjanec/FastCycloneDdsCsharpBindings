namespace CycloneDDS.CodeGen
{
    public class KeyDescriptor
    {
        public string Name { get; set; } = string.Empty;
        public uint Index { get; set; }
        public uint Flags { get; set; }
    }

    public class DescriptorMetadata
    {
        public string TypeName { get; set; } = string.Empty;
        public string OpsArrayName { get; set; } = string.Empty;
        public uint[] OpsValues { get; set; } = System.Array.Empty<uint>();
        public string KeysArrayName { get; set; } = string.Empty;
        public List<KeyDescriptor> Keys { get; set; } = new List<KeyDescriptor>();
        public uint Flagset { get; set; }
        public bool HasDlcRemoved { get; set; }
    }
}
