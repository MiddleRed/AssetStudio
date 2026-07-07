#nullable enable

using AssetStudio;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AssetStudioCore.Runtime
{
    internal static class AssetStudioProcessState
    {
        private static readonly AsyncLocal<IAssetStudioStatusSink?> CurrentStatusSink = new AsyncLocal<IAssetStudioStatusSink?>();

        public static IDisposable EnterCore(ILogger logger, IAssetStudioProgressSink? progressSink)
        {
            return new Scope(logger, CreateCoreProgress(progressSink), statusSink: null, configureLogger: true);
        }

        public static IDisposable Enter(ILogger logger, IProgress<int>[] progress, IAssetStudioStatusSink? statusSink)
        {
            return new Scope(logger, progress, statusSink, configureLogger: true);
        }

        public static IDisposable EnterCoreProgress(IAssetStudioProgressSink? progressSink)
        {
            return new Scope(logger: null, CreateCoreProgress(progressSink), statusSink: null, configureLogger: false);
        }

        public static IDisposable EnterProgress(IProgress<int>[] progress, IAssetStudioStatusSink? statusSink)
        {
            return new Scope(logger: null, progress, statusSink, configureLogger: false);
        }

        public static void ConfigureImageTimingSink(Action<string, long>? timingSink)
        {
            ImageSharpNativeAotGuard.TimingSink = timingSink;
        }

        public static void ReportStatus(string message)
        {
            CurrentStatusSink.Value?.ReportStatus(message);
        }

        public static void CompleteStatus()
        {
            CurrentStatusSink.Value?.CompleteStatus();
        }

        public static void Reset()
        {
            AssetStudioRuntimeOptions.Reset();
            Logger.Default = new DummyLogger();
            SetProgress(CreateCoreProgress(null));
            CurrentStatusSink.Value = null;
            ConfigureImageTimingSink(null);
            Progress.Reset();
            Progress.Reset(index: 1);
        }

        private static IProgress<int>[] CreateCoreProgress(IAssetStudioProgressSink? sink)
        {
            return new IProgress<int>[]
            {
                new AssetStudioProgressAdapter(sink, index: 0),
                new AssetStudioProgressAdapter(sink, index: 1),
            };
        }

        private static void SetProgress(IReadOnlyList<IProgress<int>> progress)
        {
            Progress.Default = progress[0];
            Progress.SetInstance(1, progress[1]);
        }

        private sealed class Scope : IDisposable
        {
            private readonly ILogger previousLogger;
            private readonly IProgress<int>[] previousProgress;
            private readonly IAssetStudioStatusSink? previousStatusSink;
            private readonly bool configureLogger;
            private bool disposed;

            public Scope(ILogger? logger, IProgress<int>[] progress, IAssetStudioStatusSink? statusSink, bool configureLogger)
            {
                this.configureLogger = configureLogger;
                previousLogger = Logger.Default;
                previousProgress = new[]
                {
                    Progress.GetInstance(0),
                    Progress.GetInstance(1),
                };
                previousStatusSink = CurrentStatusSink.Value;
                if (configureLogger)
                {
                    Logger.Default = logger ?? throw new ArgumentNullException(nameof(logger));
                }
                SetProgress(progress);
                CurrentStatusSink.Value = statusSink;
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                if (configureLogger)
                {
                    Logger.Default = previousLogger;
                }
                SetProgress(previousProgress);
                CurrentStatusSink.Value = previousStatusSink;
            }
        }
    }
}
