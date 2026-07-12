using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace CycloneDDS.Compiler.Common
{
    public class IdlcRunner
    {
        public string? IdlcPathOverride { get; set; }
        public string? IdlcExtraArgs { get; set; }

        // The idlc executable and the native RID sub-directory both differ per
        // platform: idlc.exe under win-x64 on Windows, idlc under linux-x64 on
        // Linux. Every lookup below iterates these so the same search logic works
        // on either OS.
        private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static string[] IdlcNames => IsWindows ? new[] { "idlc.exe" } : new[] { "idlc" };
        private static string[] NativeRids => IsWindows ? new[] { "win-x64" } : new[] { "linux-x64" };

        public string FindIdlc()
        {
            if (!string.IsNullOrEmpty(IdlcPathOverride))
            {
                if (File.Exists(IdlcPathOverride)) return IdlcPathOverride;
                throw new FileNotFoundException($"idlc not found at override path: {IdlcPathOverride}");
            }

            // Check current directory (where DLLs / .so files are copied)
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            foreach (var name in IdlcNames)
            {
                string localIdlc = Path.Combine(currentDir, name);
                if (File.Exists(localIdlc)) return localIdlc;
            }

            // Check NuGet package location relative to tools/ (tools/ -> ../runtimes/{rid}/native/)
            foreach (var rid in NativeRids)
            {
                foreach (var name in IdlcNames)
                {
                    try
                    {
                        string nugetNativePath = Path.Combine(currentDir, "..", "runtimes", rid, "native", name);
                        if (File.Exists(nugetNativePath)) return Path.GetFullPath(nugetNativePath);
                    }
                    catch { }
                }
            }

            // DEV: Check workspace location (for tests/dev)
            // Iterate up 6 levels looking for cyclonedds/install/bin, cyclone-compiled/bin,
            // or artifacts/native/{rid} — trying each platform's executable name.
            var searchDir = new DirectoryInfo(currentDir);
            for (int i = 0; i < 6; i++)
            {
                if (searchDir == null) break;

                foreach (var name in IdlcNames)
                {
                    string checkPath = Path.Combine(searchDir.FullName, "cyclonedds", "install", "bin", name);
                    if (File.Exists(checkPath)) return checkPath;

                    string repoPath = Path.Combine(searchDir.FullName, "cyclone-compiled", "bin", name);
                    if (File.Exists(repoPath)) return repoPath;

                    foreach (var rid in NativeRids)
                    {
                        string artifactPath = Path.Combine(searchDir.FullName, "artifacts", "native", rid, name);
                        if (File.Exists(artifactPath)) return artifactPath;
                    }
                }

                searchDir = searchDir.Parent;
            }

            // Check environment variable
            string? cycloneHome = Environment.GetEnvironmentVariable("CYCLONEDDS_HOME");
            if (!string.IsNullOrEmpty(cycloneHome))
            {
                foreach (var name in IdlcNames)
                {
                    string path = Path.Combine(cycloneHome, "bin", name);
                    if (File.Exists(path))
                        return path;

                    // Try without bin?
                    path = Path.Combine(cycloneHome, name);
                    if (File.Exists(path))
                        return path;
                }
            }

            // Check PATH
            string? pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (pathEnv != null)
            {
                foreach (var dir in pathEnv.Split(Path.PathSeparator))
                {
                    foreach (var name in IdlcNames)
                    {
                        try
                        {
                            string path = Path.Combine(dir, name);
                            if (File.Exists(path))
                                return path;
                        }
                        catch { /* Ignore invalid paths in PATH */ }
                    }
                }
            }

            throw new FileNotFoundException("idlc executable not found. Set CYCLONEDDS_HOME or add to PATH.");
        }

        public IdlcResult RunIdlc(string idlFilePath, string outputDir, string? includePath = null)
        {
            string idlcPath = FindIdlc();
            
            // Ensure output directory exists
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = idlcPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            if (!IsWindows)
            {
                // NuGet packages do not preserve the Unix execute bit, so an idlc
                // restored from the package may not be runnable. Restore it (best effort).
                // The inner OperatingSystem guard is what the CA1416 analyzer recognizes.
                try
                {
                    if (!OperatingSystem.IsWindows())
                    {
                        var mode = File.GetUnixFileMode(idlcPath);
                        File.SetUnixFileMode(idlcPath,
                            mode | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
                    }
                }
                catch { /* best effort — may already be executable, or FS may not support it */ }

                // idlc depends on the libcycloneddsidl*.so libraries shipped alongside
                // it. The native build normally rewrites RPATH to $ORIGIN so they resolve,
                // but prepend the idlc directory to LD_LIBRARY_PATH as a fallback (e.g.
                // when patchelf was unavailable at native build time).
                string? idlcDir = Path.GetDirectoryName(Path.GetFullPath(idlcPath));
                if (!string.IsNullOrEmpty(idlcDir))
                {
                    string existing = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? string.Empty;
                    startInfo.Environment["LD_LIBRARY_PATH"] =
                        existing.Length == 0 ? idlcDir : idlcDir + Path.PathSeparator + existing;
                }
            }

            if (!string.IsNullOrWhiteSpace(IdlcExtraArgs))
            {
                // Simple split by whitespace is sufficient for most compiler flags like "-Werror"
                // But if they have spaces inside quotes, this simple split would break. 
                // System.CommandLine parsing is better handled by caller, so we assume caller provides simple args
                foreach(var arg in IdlcExtraArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    startInfo.ArgumentList.Add(arg);
                }
            }

            startInfo.ArgumentList.Add("-l");
            startInfo.ArgumentList.Add("json");
            startInfo.ArgumentList.Add("-o");
            startInfo.ArgumentList.Add(outputDir);
            
            if (!string.IsNullOrEmpty(includePath))
            {
                startInfo.ArgumentList.Add("-I");
                startInfo.ArgumentList.Add(includePath);
            }
            
            startInfo.ArgumentList.Add(idlFilePath);
            
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new Exception("Failed to start idlc process.");
            }
            
            var stdoutBuilder = new System.Text.StringBuilder();
            var stderrBuilder = new System.Text.StringBuilder();

            // 1. Subscribe to the events
            process.OutputDataReceived += (sender, e) => 
            {
                if (e.Data != null) stdoutBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) => 
            {
                if (e.Data != null) stderrBuilder.AppendLine(e.Data);
            };

            // 2. Begin reading asynchronously
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // 3. Wait for the process to finish
            process.WaitForExit();

            // 4. Extract the final strings
            string stdout = stdoutBuilder.ToString();
            string stderr = stderrBuilder.ToString();
            
            return new IdlcResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = stdout,
                StandardError = stderr,
                GeneratedFiles = FindGeneratedFiles(outputDir, idlFilePath)
            };
        }

        public string GetArguments(string idlFilePath, string outputDir, string? includePath)
        {
            var args = $"-l json -o \"{outputDir}\"";
            if (!string.IsNullOrEmpty(includePath))
            {
                args += $" -I \"{includePath}\"";
            }
            args += $" \"{idlFilePath}\"";
            return args;
        }
        
        private string[] FindGeneratedFiles(string outputDir, string idlFile)
        {
            // idlc -l json generates: <basename>.json
            string baseName = Path.GetFileNameWithoutExtension(idlFile);
            var jsonFile = Path.Combine(outputDir, baseName + ".json");
            
            var files = new System.Collections.Generic.List<string>();
            if (File.Exists(jsonFile)) files.Add(jsonFile);
            
            return files.ToArray();
        }
    }
}
