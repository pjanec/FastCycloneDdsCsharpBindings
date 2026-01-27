using System;
using System.Threading.Tasks;
using Xunit;
using CycloneDDS.Core;
using CycloneDDS.Runtime;

namespace CsharpToC.Roundtrip.Tests
{
    public class RoundtripFixture : IAsyncLifetime
    {
        public DdsParticipant? Participant { get; private set; }

        public Task InitializeAsync()
        {
            Console.WriteLine("[INFO] Initializing Native DLL...");
            NativeMethods.Native_Init(0); // Domain 0

            Console.WriteLine("[INFO] Initializing DDS Participant...");
            Participant = new DdsParticipant();
            
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            Participant?.Dispose();
            NativeMethods.Native_Cleanup();
            return Task.CompletedTask;
        }
    }

    [CollectionDefinition("Roundtrip Collection")]
    public class RoundtripCollection : ICollectionFixture<RoundtripFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
