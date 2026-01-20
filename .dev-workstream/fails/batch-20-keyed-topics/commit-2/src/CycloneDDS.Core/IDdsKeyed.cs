using System;

namespace CycloneDDS.Core
{
    public interface IDdsKeyed<T>
    {
        void SerializeKey(ref CdrWriter writer);
        int GetKeySerializedSize();
    }
}
