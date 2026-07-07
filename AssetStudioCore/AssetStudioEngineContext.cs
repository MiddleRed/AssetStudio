#nullable enable

using AssetStudio;
using AssetStudioCore.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssetStudioCore.Runtime
{
    internal interface IAssetStudioEngineContext : IDisposable
    {
        bool Loaded { get; }
        IReadOnlyList<AssetItem> ParsedAssets { get; }
        int AssetsFileCount { get; }
        string? UnityVersion { get; }
        AssetStudioRuntimeOptions.RuntimeOptionsState Options { get; }
        void LogWarning(string message);
        AssetStudioRunResult ExportCurrent(
            AssetStudioRuntimeOptions.RuntimeOptionsState? runtimeOptions = null,
            IReadOnlyCollection<long>? exactPathIds = null,
            Dictionary<string, long>? phases = null,
            Dictionary<string, long>? metrics = null,
            Action? showCurrentOptions = null,
            IProgress<int>[]? progressOverride = null,
            IAssetStudioStatusSink? statusSink = null);
    }

    internal static class AssetStudioEngine
    {
        public static IAssetStudioEngineContext Open(AssetStudioCoreOptions options, Dictionary<string, long> phases)
        {
            return AssetStudioEngineContext.Open(options, phases);
        }

        public static AssetStudioRunResult RunParsed(
            ILogger logger,
            IProgress<int>[] progress,
            IAssetStudioStatusSink? statusSink,
            bool catchExceptions,
            IReadOnlyCollection<long>? exactPathIds = null,
            Dictionary<string, long>? phases = null,
            Dictionary<string, long>? metrics = null,
            Action? showCurrentOptions = null)
        {
            return AssetStudioEngineContext.RunParsed(logger, progress, statusSink, catchExceptions, exactPathIds, phases, metrics, showCurrentOptions);
        }

        public static void ResetProcessLocalState()
        {
            AssetStudioEngineContext.ResetProcessLocalState();
        }
    }

    internal sealed class AssetStudioEngineContext : IAssetStudioEngineContext
    {
        private readonly AssetStudioEngineInstance studioEngine;
        private readonly AssetStudioCoreLogger logger;
        private readonly IAssetStudioProgressSink? progressSink;
        private readonly IReadOnlyList<AssetItem> parsedAssets;
        private readonly int assetsFileCount;
        private readonly string? unityVersion;
        private bool disposed;

        private AssetStudioEngineContext(
            AssetStudioEngineInstance studioEngine,
            AssetStudioCoreLogger logger,
            IAssetStudioProgressSink? progressSink,
            bool loaded)
        {
            this.studioEngine = studioEngine;
            this.logger = logger;
            this.progressSink = progressSink;
            Loaded = loaded;
            parsedAssets = studioEngine.ParsedAssets.ToArray();
            assetsFileCount = studioEngine.AssetsFileCount;
            unityVersion = studioEngine.UnityVersion;
        }

        public bool Loaded { get; }

        public IReadOnlyList<AssetItem> ParsedAssets => parsedAssets;

        public int AssetsFileCount => assetsFileCount;

        public string? UnityVersion => unityVersion;

        public AssetStudioRuntimeOptions.RuntimeOptionsState Options => studioEngine.Options;

        public void LogWarning(string message)
        {
            logger.Log(LoggerEvent.Warning, message, ignoreLevel: false);
        }

        public static AssetStudioEngineContext Open(AssetStudioCoreOptions options, Dictionary<string, long> phases)
        {
            var runtimeOptions = AssetStudioRuntimeOptions.CreateFromCoreOptions(options);
            AssetStudioRuntimeOptions.Use(runtimeOptions);
            var studioEngine = AssetStudioEngineInstance.Create(runtimeOptions);
            if (!runtimeOptions.IsParsed)
            {
                throw new InvalidOperationException("AssetStudio core arguments could not be parsed.");
            }

            var logger = new AssetStudioCoreLogger(options.LogSink);
            using (var processScope = AssetStudioProcessState.EnterCore(logger, options.ProgressSink))
            {
                AssetStudioSession.Measure(phases, "prepare_run", studioEngine.PrepareForRun);

                try
                {
                    if (!AssetStudioSession.Measure(phases, "load_assets", studioEngine.LoadAssets))
                    {
                        return new AssetStudioEngineContext(studioEngine, logger, options.ProgressSink, loaded: false);
                    }

                    AssetStudioSession.Measure(phases, "parse_assets", studioEngine.ParseAssets);
                    if (runtimeOptions.HasActiveFilter)
                    {
                        AssetStudioSession.Measure(phases, "filter", studioEngine.Filter);
                    }

                    return new AssetStudioEngineContext(studioEngine, logger, options.ProgressSink, loaded: true);
                }
                catch
                {
                    ResetProcessLocalState();
                    logger.LogToFile(LoggerEvent.Verbose, "---Context open failed---");
                    throw;
                }
            }
        }

        public static AssetStudioRunResult RunParsed(
            ILogger logger,
            IProgress<int>[] progress,
            IAssetStudioStatusSink? statusSink,
            bool catchExceptions,
            IReadOnlyCollection<long>? exactPathIds = null,
            Dictionary<string, long>? phases = null,
            Dictionary<string, long>? metrics = null,
            Action? showCurrentOptions = null)
        {
            phases ??= new Dictionary<string, long>();
            metrics ??= new Dictionary<string, long>();
            var runtimeOptions = AssetStudioRuntimeOptions.Current;
            var studioEngine = AssetStudioEngineInstance.Create(runtimeOptions);
            using (AssetStudioProcessState.Enter(logger, progress, statusSink))
            {
                AssetStudioSession.Measure(phases, "prepare_run", studioEngine.PrepareForRun);
                if (showCurrentOptions != null)
                {
                    AssetStudioSession.Measure(phases, "show_options", showCurrentOptions);
                }

                try
                {
                    if (runtimeOptions.CurrentWorkMode == WorkMode.Extract)
                    {
                        AssetStudioSession.Measure(phases, "extract_bundles", studioEngine.ExtractBundles);
                    }
                    else if (AssetStudioSession.Measure(phases, "load_assets", studioEngine.LoadAssets))
                    {
                        AssetStudioSession.Measure(phases, "parse_assets", studioEngine.ParseAssets);
                        if (runtimeOptions.HasActiveFilter)
                        {
                            AssetStudioSession.Measure(phases, "filter", studioEngine.Filter);
                        }
                        AssetStudioSession.Measure(phases, "exact_path_filter", () => ApplyExactPathIdFilter(studioEngine, exactPathIds));
                        if (runtimeOptions.ShouldExportAssetList)
                        {
                            AssetStudioSession.Measure(phases, "export_asset_list", studioEngine.ExportAssetList);
                        }
                        ExportCurrentMode(studioEngine, phases, metrics);
                    }
                }
                catch (Exception ex) when (catchExceptions)
                {
                    Logger.Error(ex.ToString());
                }
                finally
                {
                    AssetStudioSession.Measure(phases, "clear", studioEngine.Clear);
                }
            }

            return new AssetStudioRunResult { PhaseMs = phases, Metrics = metrics };
        }

        public static void ResetProcessLocalState()
        {
            AssetStudioProcessState.Reset();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            studioEngine.Clear();
            logger.LogToFile(LoggerEvent.Verbose, "---Context ended---");
            ResetProcessLocalState();
        }

        public AssetStudioRunResult ExportCurrent(
            AssetStudioRuntimeOptions.RuntimeOptionsState? runtimeOptions = null,
            IReadOnlyCollection<long>? exactPathIds = null,
            Dictionary<string, long>? phases = null,
            Dictionary<string, long>? metrics = null,
            Action? showCurrentOptions = null,
            IProgress<int>[]? progressOverride = null,
            IAssetStudioStatusSink? statusSink = null)
        {
            phases ??= new Dictionary<string, long>();
            metrics ??= new Dictionary<string, long>();
            if (runtimeOptions != null)
            {
                studioEngine.UseOptions(runtimeOptions);
            }
            using var progressScope = progressOverride != null
                ? AssetStudioProcessState.EnterProgress(progressOverride, statusSink)
                : AssetStudioProcessState.EnterCoreProgress(progressSink);
            if (showCurrentOptions != null)
            {
                AssetStudioSession.Measure(phases, "show_options", showCurrentOptions);
            }
            AssetStudioSession.Measure(phases, "exact_path_filter", () => ApplyExactPathIdFilter(studioEngine, exactPathIds));
            if (studioEngine.Options.ShouldExportAssetList)
            {
                AssetStudioSession.Measure(phases, "export_asset_list", studioEngine.ExportAssetList);
            }
            ExportCurrentMode(studioEngine, phases, metrics);
            return new AssetStudioRunResult { PhaseMs = phases, Metrics = metrics };
        }

        private static void ExportCurrentMode(AssetStudioEngineInstance studioEngine, Dictionary<string, long> phases, Dictionary<string, long> metrics)
        {
            switch (studioEngine.Options.CurrentWorkMode)
            {
                case WorkMode.Info:
                    AssetStudioSession.Measure(phases, "show_exportable_assets_info", studioEngine.ShowExportableAssetsInfo);
                    break;
                case WorkMode.Live2D:
                    AssetStudioSession.Measure(phases, "export_live2d", studioEngine.ExportLive2D);
                    break;
                case WorkMode.SplitObjects:
                    AssetStudioSession.Measure(phases, "export_split_objects", studioEngine.ExportSplitObjects);
                    break;
                case WorkMode.Animator:
                    AssetStudioSession.Measure(phases, "export_animator", studioEngine.ExportAnimator);
                    break;
                default:
                    studioEngine.ResetParallelExportDiagnostics();
                    AssetStudioSession.Measure(phases, "export_assets", studioEngine.ExportAssets);
                    foreach (var phase in studioEngine.SnapshotParallelExportTimingMs())
                    {
                        AddPhase(phases, phase.Key, phase.Value);
                    }
                    foreach (var metric in studioEngine.SnapshotParallelExportMetrics())
                    {
                        AddPhase(metrics, metric.Key, metric.Value);
                    }
                    break;
            }
        }

        private static void ApplyExactPathIdFilter(AssetStudioEngineInstance studioEngine, IReadOnlyCollection<long>? exactPathIds)
        {
            if (exactPathIds == null || exactPathIds.Count == 0)
            {
                return;
            }

            studioEngine.ApplyExactPathIdFilter(exactPathIds);
        }

        private static void AddPhase(Dictionary<string, long> phases, string name, long elapsedMs)
        {
            phases[name] = phases.TryGetValue(name, out var current)
                ? current + elapsedMs
                : elapsedMs;
        }
    }

    internal sealed class AssetStudioCoreLogger : ILogger
    {
        private readonly IAssetStudioLogSink? sink;

        public AssetStudioCoreLogger(IAssetStudioLogSink? sink)
        {
            this.sink = sink;
        }

        public void Log(LoggerEvent logMsgLevel, string message, bool ignoreLevel)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            sink?.Log(ToCoreLevel(logMsgLevel), message);
        }

        public void LogToFile(LoggerEvent logMsgLevel, string message)
        {
            Log(logMsgLevel, message, ignoreLevel: true);
        }

        private static AssetStudioLogLevel ToCoreLevel(LoggerEvent level)
        {
            return level switch
            {
                LoggerEvent.Verbose => AssetStudioLogLevel.Verbose,
                LoggerEvent.Debug => AssetStudioLogLevel.Debug,
                LoggerEvent.Info => AssetStudioLogLevel.Info,
                LoggerEvent.Warning => AssetStudioLogLevel.Warning,
                LoggerEvent.Error => AssetStudioLogLevel.Error,
                _ => AssetStudioLogLevel.Info,
            };
        }
    }

    internal sealed class AssetStudioProgressAdapter : IProgress<int>
    {
        private readonly IAssetStudioProgressSink? sink;
        private readonly int index;

        public AssetStudioProgressAdapter(IAssetStudioProgressSink? sink, int index)
        {
            this.sink = sink;
            this.index = index;
        }

        public void Report(int value)
        {
            sink?.Report(value, index);
        }
    }

}
