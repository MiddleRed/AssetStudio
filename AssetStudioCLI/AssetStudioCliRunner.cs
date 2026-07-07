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
        private static AssetStudioSession? activeSession;

        public static int ActiveObjectIndexCount => activeSession?.ObjectIndexCount ?? 0;

        public static AssetStudioRunResult Run(string[] args, IReadOnlyCollection<long>? exactPathIds = null)
        {
            var phases = new Dictionary<string, long>();
            Measure(phases, "parse_args", () =>
            {
                CLIOptions.Reset();
                CLIOptions.ParseArgs(args);
            });
            if (!CLIOptions.isParsed)
            {
                throw new InvalidOperationException("AssetStudio CLI arguments could not be parsed.");
            }
            AssetStudioRuntimeOptions.ApplySnapshot(AssetStudioCliOptionsSnapshot.CreateFromCurrent());
            return RunParsed(catchExceptions: false, exactPathIds, phases);
        }

        public static AssetStudioInspectResult Inspect(AssetStudioInspectOptions options)
        {
            using var session = AssetStudioSession.Open(options);
            return session.InspectResult;
        }

        public static AssetStudioLoadedSession BeginSession(AssetStudioInspectOptions options)
        {
            activeSession?.Dispose();
            activeSession = AssetStudioSession.Open(options);
            return new AssetStudioLoadedSession
            {
                Loaded = activeSession.Loaded,
                InspectResult = activeSession.InspectResult,
            };
        }

        public static AssetStudioRunResult ExportSession(string[] args, IReadOnlyCollection<long>? exactPathIds = null)
        {
            var phases = new Dictionary<string, long>();
            var metrics = new Dictionary<string, long>();
            Measure(phases, "parse_args", () =>
            {
                CLIOptions.Reset();
                CLIOptions.ParseArgs(args);
            });
            if (!CLIOptions.isParsed)
            {
                throw new InvalidOperationException("AssetStudio context export arguments could not be parsed.");
            }
            var runtimeOptions = AssetStudioRuntimeOptions.CreateFromSnapshot(AssetStudioCliOptionsSnapshot.CreateFromCurrent());
            AssetStudioRuntimeOptions.Use(runtimeOptions);

            return activeSession?.ExportCurrent(
                    runtimeOptions,
                    exactPathIds,
                    phases,
                    metrics,
                    CLIOptions.ShowCurrentOptions,
                    AssetStudioConsoleProgressAdapter.CreatePair(),
                    new AssetStudioConsoleStatusSink())
                ?? throw new InvalidOperationException("there is no active AssetStudio session");
        }

        public static AssetStudioObjectReadResult ReadObject(AssetStudioObjectReadOptions options)
        {
            return activeSession?.ReadObject(options)
                ?? throw new InvalidOperationException("there is no active AssetStudio session");
        }

        public static void EndSession()
        {
            activeSession?.Dispose();
            activeSession = null;
            AssetStudioEngine.ResetProcessLocalState();
        }

        public static void ResetProcessLocalState()
        {
            activeSession?.Dispose();
            activeSession = null;
            AssetStudioEngine.ResetProcessLocalState();
        }

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

        private static void Measure(Dictionary<string, long> phases, string name, Action action)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            action();
            AddPhase(phases, name, stopwatch.ElapsedMilliseconds);
        }

        private static T Measure<T>(Dictionary<string, long> phases, string name, Func<T> action)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = action();
            AddPhase(phases, name, stopwatch.ElapsedMilliseconds);
            return result;
        }

        private static void AddPhase(Dictionary<string, long> phases, string name, long elapsedMs)
        {
            phases[name] = phases.TryGetValue(name, out var current)
                ? current + elapsedMs
                : elapsedMs;
        }

    }

}
