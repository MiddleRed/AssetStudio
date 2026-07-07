#nullable enable

using AssetStudioCore;
using System;

namespace AssetStudioCLI
{
    /// <summary>
    /// Renders core progress reports as an in-place console percentage.
    /// </summary>
    internal sealed class AssetStudioConsoleProgressAdapter : IProgress<int>
    {
        public void Report(int value)
        {
            Console.Write($"[{value:000}%]\r");
        }

        public static IProgress<int>[] CreatePair()
        {
            return new IProgress<int>[]
            {
                new AssetStudioConsoleProgressAdapter(),
                new AssetStudioConsoleProgressAdapter(),
            };
        }
    }

    /// <summary>
    /// Renders core status updates (e.g. the export counter) as an in-place console line.
    /// </summary>
    internal sealed class AssetStudioConsoleStatusSink : IAssetStudioStatusSink
    {
        public void ReportStatus(string message)
        {
            Console.Write($"{message}\r");
        }

        public void CompleteStatus()
        {
            Console.WriteLine("");
        }
    }
}
