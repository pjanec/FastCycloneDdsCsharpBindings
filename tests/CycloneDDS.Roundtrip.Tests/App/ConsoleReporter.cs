using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CycloneDDS.Roundtrip.App;

/// <summary>
/// Formats test output for console display and CI/CD integration.
/// </summary>
internal static class ConsoleReporter
{
    private static readonly object ConsoleLock = new();
    
    #region Sections

    public static void PrintHeader(string title)
    {
        lock (ConsoleLock)
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine(title);
            Console.WriteLine("========================================");
        }
    }

    public static void PrintDivider()
    {
        lock (ConsoleLock)
        {
            Console.WriteLine("========================================");
        }
    }

    #endregion

    #region Messages

    public static void Info(string message)
    {
        lock (ConsoleLock)
        {
            Console.WriteLine($"[Info] {message}");
        }
    }

    public static void Warning(string message)
    {
        lock (ConsoleLock)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[Warning] {message}");
            Console.ResetColor();
        }
    }

    public static void Error(string message)
    {
        lock (ConsoleLock)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Error] {message}");
            Console.ResetColor();
        }
    }

    public static void Success(string message)
    {
        lock (ConsoleLock)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[Success] {message}");
            Console.ResetColor();
        }
    }

    #endregion

    #region Test Progress

    public static void StartTest(int testNumber, int totalTests, string testName)
    {
        lock (ConsoleLock)
        {
            Console.WriteLine();
            Console.WriteLine($"[Test {testNumber}/{totalTests}] {testName}");
        }
    }

    public static void TestStep(string description)
    {
        lock (ConsoleLock)
        {
            Console.WriteLine($"  {description}");
        }
    }

    public static void TestResult(bool passed)
    {
        lock (ConsoleLock)
        {
            if (passed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  Result: PASS");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  Result: FAIL");
            }
            Console.ResetColor();
        }
    }

    #endregion

    #region Summary

    public static void PrintSummary(int total, int passed, int failed, TimeSpan elapsed)
    {
        lock (ConsoleLock)
        {
            Console.WriteLine();
            PrintDivider();
            Console.WriteLine("Summary");
            PrintDivider();
            Console.WriteLine($"Total:   {total} tests");
            
            if (passed > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Passed:  {passed} tests");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine($"Passed:  {passed} tests");
            }

            if (failed > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed:  {failed} tests");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine($"Failed:  {failed} tests");
            }

            Console.WriteLine($"Time:    {elapsed.TotalSeconds:F1} seconds");
            Console.WriteLine();

            if (failed == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("All tests PASSED!");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{failed} test(s) FAILED!");
            }
            Console.ResetColor();
        }
    }

    #endregion

    #region Initialization

    public static void PrintInitialization(int domainId)
    {
        lock (ConsoleLock)
        {
            Console.WriteLine($"[Roundtrip] Initializing (Domain {domainId})...");
        }
    }

    public static void PrintNativeInfo()
    {
        lock (ConsoleLock)
        {
            Console.WriteLine("[Native] Initialization complete.");
        }
    }

    public static void PrintRegisteredTypes(IEnumerable<string> types)
    {
        lock (ConsoleLock)
        {
            PrintHeader("Registered Types");
            int index = 1;
            foreach (var type in types)
            {
                Console.WriteLine($"  [{index}] {type}");
                index++;
            }
            PrintDivider();
        }
    }

    #endregion

    #region Exit

    public static void PrintExitCode(int code)
    {
        lock (ConsoleLock)
        {
            Console.WriteLine();
            if (code == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Exit Code: {code}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Exit Code: {code}");
            }
            Console.ResetColor();
        }
    }

    #endregion
}
