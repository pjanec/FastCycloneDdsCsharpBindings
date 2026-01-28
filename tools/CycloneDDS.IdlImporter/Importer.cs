using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CycloneDDS.IdlImporter;

/// <summary>
/// Core orchestrator for the IDL import process.
/// Manages recursive IDL file processing with folder structure preservation.
/// </summary>
/// <remarks>
/// Implementation planned in IDLIMP-004: Importer Core - File Queue and Recursion
/// See: tools/CycloneDDS.IdlImporter/IDLImport-TASK-DETAILS.md#idlimp-004
/// </remarks>
public class Importer
{
    private readonly HashSet<string> _processedFiles = new();
    private readonly Queue<string> _workQueue = new();
    private readonly bool _verbose;
    private readonly string? _idlcPath;
    
    private string _sourceRoot = string.Empty;
    private string _outputRoot = string.Empty;

    public Importer(bool verbose = false, string? idlcPath = null)
    {
        _verbose = verbose;
        _idlcPath = idlcPath;
    }

    /// <summary>
    /// Imports IDL files starting from a master file, replicating the directory structure.
    /// </summary>
    /// <param name="masterIdlPath">The entry point IDL (e.g. "src/App/main.idl")</param>
    /// <param name="sourceRoot">The common root for all IDLs (e.g. "src/")</param>
    /// <param name="outputRoot">The root for generated C# files (e.g. "generated/")</param>
    public void Import(string masterIdlPath, string sourceRoot, string outputRoot)
    {
        _sourceRoot = Path.GetFullPath(sourceRoot);
        _outputRoot = Path.GetFullPath(outputRoot);
        
        string fullMasterPath = Path.GetFullPath(masterIdlPath);

        Log($"Starting import from: {Path.GetRelativePath(_sourceRoot, fullMasterPath)}");
        
        // TODO: Implement recursive import logic
        // 1. EnqueueFile(fullMasterPath)
        // 2. While queue not empty:
        //    - ProcessSingleFile(dequeue)
        //    - Run idlc with include path
        //    - Parse JSON
        //    - Generate C# for types in current file
        //    - Enqueue dependencies
        
        throw new NotImplementedException("Import logic not yet implemented (IDLIMP-004, IDLIMP-005)");
    }

    private void ProcessSingleFile(string idlPath)
    {
        // TODO: Implement in IDLIMP-004
        // 1. Calculate relative path
        // 2. Run idlc -l json with include path
        // 3. Parse JSON output
        // 4. Generate C# code
        // 5. Enqueue dependencies
    }

    private void EnqueueFile(string path)
    {
        string fullPath = Path.GetFullPath(path);
        if (!_processedFiles.Contains(fullPath) && File.Exists(fullPath))
        {
            _processedFiles.Add(fullPath);
            _workQueue.Enqueue(fullPath);
            Log($"Enqueued: {Path.GetFileName(fullPath)}");
        }
    }

    private void Log(string message)
    {
        if (_verbose)
        {
            Console.WriteLine($"[Importer] {message}");
        }
    }
}
