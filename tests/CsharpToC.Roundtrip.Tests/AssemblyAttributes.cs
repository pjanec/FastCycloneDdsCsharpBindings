using Xunit;

// Disable parallelization for all tests in this assembly
// This is critical for roundtrip tests that rely on shared native state or network ports
[assembly: CollectionBehavior(DisableTestParallelization = true)]
