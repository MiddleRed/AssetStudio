#nullable enable

using AssetStudioCore;
using AssetStudioCore.Runtime;
using AssetStudioCore.Options;
using System;
using System.Collections.Generic;

namespace AssetStudioCLI
{
    public static class AssetStudioCliRunner
    {
        internal static AssetStudioRunResult RunParsed(
            bool catchExceptions,
            IReadOnlyCollection<long>? exactPathIds = null,
            Dictionary<string, long>? phases = null,
            Dictionary<string, long>? metrics = null)
        {
            AssetStudioRuntimeOptions.ApplySnapshot(AssetStudioCliOptionsSnapshot.CreateFromCurrent());
            var consoleLogger = new AssetStudioConsoleLogger(
                CLIOptions.o_logOutput.Value,
                CLIOptions.o_logLevel.Value,
                CLIOptions.cliArgs ?? Array.Empty<string>());
            try
            {
                return AssetStudioEngine.RunParsed(
                    consoleLogger,
                    AssetStudioConsoleProgressAdapter.CreatePair(),
                    new AssetStudioConsoleStatusSink(),
                    catchExceptions,
                    exactPathIds,
                    phases,
                    metrics,
                    CLIOptions.ShowCurrentOptions);
            }
            finally
            {
                consoleLogger.LogToFile(AssetStudio.LoggerEvent.Verbose, "---Program ended---");
            }
        }

    }

}
