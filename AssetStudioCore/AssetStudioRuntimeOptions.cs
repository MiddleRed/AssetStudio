#nullable enable

using AssetStudio;
using AssetStudioCore.Options;
using CubismLive2DExtractor;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace AssetStudioCore.Runtime
{
    internal static class AssetStudioRuntimeOptions
    {
        private static readonly List<ClassIDType> ExportableAssetTypes = new List<ClassIDType>
        {
            ClassIDType.Texture2D,
            ClassIDType.Texture2DArray,
            ClassIDType.Sprite,
            ClassIDType.TextAsset,
            ClassIDType.MonoBehaviour,
            ClassIDType.Font,
            ClassIDType.Shader,
            ClassIDType.AudioClip,
            ClassIDType.VideoClip,
            ClassIDType.MovieTexture,
            ClassIDType.Mesh,
        };

        private static readonly List<ClassIDType> CoreDefaultAssetTypes = ExportableAssetTypes
            .Concat(new[] { ClassIDType.Animator })
            .ToList();

        private static readonly AsyncLocal<RuntimeOptionsState> CurrentOptions = new AsyncLocal<RuntimeOptionsState>();

        public static RuntimeOptionsState Current
        {
            get
            {
                var options = CurrentOptions.Value;
                if (options == null)
                {
                    options = RuntimeOptionsState.CreateDefaults();
                    CurrentOptions.Value = options;
                }
                return options;
            }
            private set => CurrentOptions.Value = value;
        }

        public static bool HasParsedCoreOptions => Current.IsParsed;

        public static bool HasActiveFilter => Current.FilterBy != FilterBy.None;

        public static FilterBy CurrentFilterBy => Current.FilterBy;

        public static bool FilterWithRegex => Current.FilterWithRegex;

        public static bool FilterExcludeMode => Current.FilterExcludeMode;

        public static List<string> FilterByNameValues => Current.FilterByNameValues;

        public static List<string> FilterByContainerValues => Current.FilterByContainerValues;

        public static List<string> FilterByPathIdValues => Current.FilterByPathIdValues;

        public static List<string> FilterByTextValues => Current.FilterByTextValues;

        public static WorkMode CurrentWorkMode => Current.WorkMode;

        public static string CurrentWorkModeName => Current.WorkMode.ToString();

        public static bool ShouldExportAssetList => Current.ExportAssetListType != ExportListType.None;

        public static LoggerEvent LogLevel => Current.LogLevel;

        public static bool ShouldWriteDebugLog => Current.LogLevel <= LoggerEvent.Debug;

        public static string PrimaryInputPath => Current.InputPaths[0];

        public static string OutputFolder => Current.OutputFolder;

        public static bool ShouldLoadAllAssets => Current.LoadAllAssets;

        public static bool IsLive2DMode => Current.WorkMode == WorkMode.Live2D;

        public static bool IsExportMode => Current.WorkMode == WorkMode.Export;

        public static AssetGroupOption GroupAssetsBy => Current.GroupAssetsBy;

        public static int MaxParallelExportTasks => Current.MaxParallelExportTasks;

        public static string? StripPathPrefix
        {
            get => Current.StripPathPrefix;
            set => Current.StripPathPrefix = value ?? string.Empty;
        }

        public static ExportListType ExportAssetListType => Current.ExportAssetListType;

        public static AnimationExportMode FbxAnimationMode => Current.FbxAnimationMode;

        public static Live2DMotionMode Live2DMotionMode => Current.Live2DMotionMode;

        public static bool Live2DForceBezier => Current.Live2DForceBezier;

        public static Live2DModelGroupOption Live2DModelGroupOption => Current.Live2DModelGroupOption;

        public static bool Live2DSearchByFilename => Current.Live2DSearchByFilename;

        public static bool HasAssemblyPath => !string.IsNullOrEmpty(Current.AssemblyPath);

        public static bool ShouldBuildTreeStructure =>
            Current.WorkMode == WorkMode.SplitObjects ||
            Current.GroupAssetsBy == AssetGroupOption.SceneHierarchy;

        public static List<string> InputPaths => Current.InputPaths;

        public static List<ClassIDType> ExportAssetTypes => Current.ExportAssetTypes;

        public static LogOutputMode LogOutput => Current.LogOutput;

        public static LoggerEvent LogMinLevel => Current.LogLevel;

        public static FilenameFormat FilenameFormat => Current.FilenameFormat;

        public static bool OverwriteExisting => Current.OverwriteExisting;

        public static bool ConvertTexture => Current.ConvertTexture;

        public static ImageFormat ImageFormat => Current.ImageFormat;

        public static AudioFormat AudioFormat => Current.AudioFormat;

        public static int FbxBoneSize => Current.FbxBoneSize;

        public static float FbxScaleFactor => Current.FbxScaleFactor;

        public static bool FbxUvsAsDiffuseMaps => Current.FbxUvsAsDiffuseMaps;

        public static bool RestoreTextAssetExtension => Current.RestoreTextAssetExtension;

        public static bool RawByteArrayFromMonoBehaviour => Current.RawByteArrayFromMonoBehaviour;

        public static void ApplySnapshot(AssetStudioRuntimeOptionsSnapshot snapshot)
        {
            Current = CreateFromSnapshot(snapshot);
        }

        public static RuntimeOptionsState CreateFromSnapshot(AssetStudioRuntimeOptionsSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            return new RuntimeOptionsState
            {
                IsParsed = snapshot.IsParsed,
                FilterBy = snapshot.FilterBy,
                FilterWithRegex = snapshot.FilterWithRegex,
                FilterExcludeMode = snapshot.FilterExcludeMode,
                FilterByNameValues = new List<string>(snapshot.FilterByNameValues),
                FilterByContainerValues = new List<string>(snapshot.FilterByContainerValues),
                FilterByPathIdValues = new List<string>(snapshot.FilterByPathIdValues),
                FilterByTextValues = new List<string>(snapshot.FilterByTextValues),
                WorkMode = snapshot.WorkMode,
                ExportAssetListType = snapshot.ExportAssetListType,
                LogLevel = snapshot.LogLevel,
                InputPaths = new List<string>(snapshot.InputPaths),
                OutputFolder = snapshot.OutputFolder,
                LoadAllAssets = snapshot.LoadAllAssets,
                GroupAssetsBy = snapshot.GroupAssetsBy,
                MaxParallelExportTasks = snapshot.MaxParallelExportTasks,
                StripPathPrefix = snapshot.StripPathPrefix,
                FbxAnimationMode = snapshot.FbxAnimationMode,
                Live2DMotionMode = snapshot.Live2DMotionMode,
                Live2DForceBezier = snapshot.Live2DForceBezier,
                Live2DModelGroupOption = snapshot.Live2DModelGroupOption,
                Live2DSearchByFilename = snapshot.Live2DSearchByFilename,
                AssemblyPath = snapshot.AssemblyPath,
                ExportAssetTypes = new List<ClassIDType>(snapshot.ExportAssetTypes),
                LogOutput = snapshot.LogOutput,
                FilenameFormat = snapshot.FilenameFormat,
                OverwriteExisting = snapshot.OverwriteExisting,
                ConvertTexture = snapshot.ConvertTexture,
                ImageFormat = snapshot.ImageFormat,
                AudioFormat = snapshot.AudioFormat,
                FbxBoneSize = snapshot.FbxBoneSize,
                FbxScaleFactor = snapshot.FbxScaleFactor,
                FbxUvsAsDiffuseMaps = snapshot.FbxUvsAsDiffuseMaps,
                RestoreTextAssetExtension = snapshot.RestoreTextAssetExtension,
                RawByteArrayFromMonoBehaviour = snapshot.RawByteArrayFromMonoBehaviour,
                LoadViaTypeTree = snapshot.LoadViaTypeTree,
                UnityVersion = snapshot.UnityVersion,
                BundleBlockInfoCompression = snapshot.BundleBlockInfoCompression,
                BundleBlockCompression = snapshot.BundleBlockCompression,
                DecompressToDisk = snapshot.DecompressToDisk,
            };
        }

        public static void ApplyCoreOptions(AssetStudioCoreOptions options)
        {
            Current = CreateFromCoreOptions(options);
        }

        public static RuntimeOptionsState CreateFromCoreOptions(AssetStudioCoreOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.InputPath))
            {
                throw new ArgumentException("input_path is required");
            }

            var inputPath = Path.GetFullPath(options.InputPath).Replace("\"", "");
            if (!Directory.Exists(inputPath) && !File.Exists(inputPath))
            {
                throw new FileNotFoundException($"input_path does not exist: {inputPath}", inputPath);
            }

            var runtimeOptions = RuntimeOptionsState.CreateDefaults();
            runtimeOptions.IsParsed = true;
            runtimeOptions.InputPaths.Add(inputPath);
            runtimeOptions.WorkMode = WorkMode.Info;
            runtimeOptions.OutputFolder = string.IsNullOrWhiteSpace(options.OutputDir)
                ? Path.Combine(Path.GetTempPath(), "assetstudio-inspect-" + Guid.NewGuid().ToString("N"))
                : Path.GetFullPath(options.OutputDir);
            runtimeOptions.ExportAssetListType = ExportListType.None;
            runtimeOptions.LoadAllAssets = options.LoadAllAssets;
            runtimeOptions.FilterWithRegex = options.FilterWithRegex;
            runtimeOptions.FilterExcludeMode = options.FilterExcludeMode;

            if (!string.IsNullOrWhiteSpace(options.UnityVersion))
            {
                runtimeOptions.UnityVersion = new UnityVersion(options.UnityVersion);
            }

            var assetTypes = ParseAssetTypes(options.AssetTypes, options.LoadAllAssets, runtimeOptions.ExportAssetTypes);
            if (assetTypes.Count > 0)
            {
                runtimeOptions.ExportAssetTypes = assetTypes;
            }

            ApplyFilters(options, runtimeOptions);
            return runtimeOptions;
        }

        public static void Reset()
        {
            Current = RuntimeOptionsState.CreateDefaults();
        }

        public static void Use(RuntimeOptionsState runtimeOptions)
        {
            Current = runtimeOptions ?? throw new ArgumentNullException(nameof(runtimeOptions));
        }

        public static void ApplyToAssetsManager(AssetsManager assetsManager)
        {
            ApplyToAssetsManager(assetsManager, Current);
        }

        public static void ApplyToAssetsManager(AssetsManager assetsManager, RuntimeOptionsState runtimeOptions)
        {
            assetsManager.LoadViaTypeTree = runtimeOptions.LoadViaTypeTree;
            assetsManager.Options.CustomUnityVersion = runtimeOptions.UnityVersion;
            assetsManager.Options.BundleOptions.CustomBlockInfoCompression = runtimeOptions.BundleBlockInfoCompression;
            assetsManager.Options.BundleOptions.CustomBlockCompression = runtimeOptions.BundleBlockCompression;
            assetsManager.Options.BundleOptions.DecompressToDisk = runtimeOptions.DecompressToDisk;
            assetsManager.OptionLoaders.Clear();
        }

        public static void ApplyToAssemblyLoader(AssemblyLoader assemblyLoader)
        {
            ApplyToAssemblyLoader(assemblyLoader, Current);
        }

        public static void ApplyToAssemblyLoader(AssemblyLoader assemblyLoader, RuntimeOptionsState runtimeOptions)
        {
            if (!string.IsNullOrEmpty(runtimeOptions.AssemblyPath))
            {
                assemblyLoader.Load(runtimeOptions.AssemblyPath);
            }
            if (!assemblyLoader.Loaded)
            {
                assemblyLoader.Loaded = true;
            }
        }

        private static List<ClassIDType> ParseAssetTypes(
            IReadOnlyCollection<string>? assetTypes,
            bool loadAllAssets,
            List<ClassIDType> defaultAssetTypes)
        {
            if (assetTypes == null || assetTypes.Count == 0)
            {
                return new List<ClassIDType>(defaultAssetTypes);
            }

            var parsed = new List<ClassIDType>();
            foreach (var rawType in assetTypes)
            {
                if (string.IsNullOrWhiteSpace(rawType))
                {
                    continue;
                }

                foreach (var token in rawType.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var normalized = token.Trim();
                    if (normalized.Equals("all", StringComparison.OrdinalIgnoreCase))
                    {
                        return new List<ClassIDType>(ExportableAssetTypes);
                    }

                    if (TryParseAssetType(normalized, out var assetType))
                    {
                        parsed.Add(assetType);
                        continue;
                    }

                    throw new ArgumentException($"unsupported asset type `{normalized}`");
                }
            }

            return parsed.Count > 0 || loadAllAssets
                ? parsed.Distinct().ToList()
                : new List<ClassIDType>(defaultAssetTypes);
        }

        private static bool TryParseAssetType(string value, out ClassIDType assetType)
        {
            switch (value.Trim().ToLowerInvariant())
            {
                case "tex2d":
                    assetType = ClassIDType.Texture2D;
                    return true;
                case "tex2darray":
                    assetType = ClassIDType.Texture2DArray;
                    return true;
                case "audio":
                    assetType = ClassIDType.AudioClip;
                    return true;
                case "video":
                    assetType = ClassIDType.VideoClip;
                    return true;
                case "textasset":
                    assetType = ClassIDType.TextAsset;
                    return true;
                case "monobehaviour":
                    assetType = ClassIDType.MonoBehaviour;
                    return true;
                default:
                    return Enum.TryParse(value, ignoreCase: true, out assetType);
            }
        }

        private static void ApplyFilters(AssetStudioCoreOptions options, RuntimeOptionsState runtimeOptions)
        {
            var hasName = !string.IsNullOrWhiteSpace(options.FilterByName);
            var hasContainer = !string.IsNullOrWhiteSpace(options.FilterByContainer);
            var hasPathIds = options.FilterByPathIds != null && options.FilterByPathIds.Count > 0;

            if (hasName)
            {
                runtimeOptions.FilterByNameValues.Add(options.FilterByName!);
            }
            if (hasContainer)
            {
                runtimeOptions.FilterByContainerValues.Add(options.FilterByContainer!);
            }
            if (hasPathIds)
            {
                runtimeOptions.FilterByPathIdValues.AddRange(options.FilterByPathIds!.Select(x => x.ToString(CultureInfo.InvariantCulture)));
            }

            runtimeOptions.FilterBy = (hasName, hasContainer, hasPathIds) switch
            {
                (_, _, true) => FilterBy.PathID,
                (true, true, _) => FilterBy.NameAndContainer,
                (true, false, _) => FilterBy.Name,
                (false, true, _) => FilterBy.Container,
                _ => FilterBy.None,
            };
        }

        internal sealed class RuntimeOptionsState
        {
            public bool IsParsed { get; set; }
            public bool HasActiveFilter => FilterBy != FilterBy.None;
            public FilterBy CurrentFilterBy => FilterBy;
            public WorkMode CurrentWorkMode => WorkMode;
            public string CurrentWorkModeName => WorkMode.ToString();
            public bool ShouldExportAssetList => ExportAssetListType != ExportListType.None;
            public bool ShouldWriteDebugLog => LogLevel <= LoggerEvent.Debug;
            public string PrimaryInputPath => InputPaths[0];
            public bool IsLive2DMode => WorkMode == WorkMode.Live2D;
            public bool IsExportMode => WorkMode == WorkMode.Export;
            public bool ShouldLoadAllAssets => LoadAllAssets;
            public bool HasAssemblyPath => !string.IsNullOrEmpty(AssemblyPath);
            public bool ShouldBuildTreeStructure =>
                WorkMode == WorkMode.SplitObjects ||
                GroupAssetsBy == AssetGroupOption.SceneHierarchy;
            public FilterBy FilterBy { get; set; }
            public bool FilterWithRegex { get; set; }
            public bool FilterExcludeMode { get; set; }
            public List<string> FilterByNameValues { get; set; } = new List<string>();
            public List<string> FilterByContainerValues { get; set; } = new List<string>();
            public List<string> FilterByPathIdValues { get; set; } = new List<string>();
            public List<string> FilterByTextValues { get; set; } = new List<string>();
            public WorkMode WorkMode { get; set; }
            public ExportListType ExportAssetListType { get; set; }
            public LoggerEvent LogLevel { get; set; }
            public List<string> InputPaths { get; set; } = new List<string>();
            public string OutputFolder { get; set; } = string.Empty;
            public bool LoadAllAssets { get; set; }
            public AssetGroupOption GroupAssetsBy { get; set; }
            public int MaxParallelExportTasks { get; set; }
            public string? StripPathPrefix { get; set; } = string.Empty;
            public AnimationExportMode FbxAnimationMode { get; set; }
            public Live2DMotionMode Live2DMotionMode { get; set; }
            public bool Live2DForceBezier { get; set; }
            public Live2DModelGroupOption Live2DModelGroupOption { get; set; }
            public bool Live2DSearchByFilename { get; set; }
            public string AssemblyPath { get; set; } = string.Empty;
            public List<ClassIDType> ExportAssetTypes { get; set; } = new List<ClassIDType>();
            public LogOutputMode LogOutput { get; set; }
            public FilenameFormat FilenameFormat { get; set; }
            public bool OverwriteExisting { get; set; }
            public bool ConvertTexture { get; set; }
            public ImageFormat ImageFormat { get; set; }
            public AudioFormat AudioFormat { get; set; }
            public int FbxBoneSize { get; set; }
            public float FbxScaleFactor { get; set; }
            public bool FbxUvsAsDiffuseMaps { get; set; }
            public bool RestoreTextAssetExtension { get; set; }
            public bool RawByteArrayFromMonoBehaviour { get; set; }
            public bool LoadViaTypeTree { get; set; }
            public UnityVersion? UnityVersion { get; set; }
            public CompressionType BundleBlockInfoCompression { get; set; }
            public CompressionType BundleBlockCompression { get; set; }
            public bool DecompressToDisk { get; set; }

            public static RuntimeOptionsState CreateDefaults()
            {
                return new RuntimeOptionsState
                {
                    IsParsed = false,
                    FilterBy = FilterBy.None,
                    WorkMode = WorkMode.Export,
                    ExportAssetListType = ExportListType.None,
                    LogLevel = LoggerEvent.Info,
                    OutputFolder = "ASExport",
                    GroupAssetsBy = AssetGroupOption.ContainerPath,
                    MaxParallelExportTasks = Environment.ProcessorCount - 1,
                    ExportAssetTypes = new List<ClassIDType>(CoreDefaultAssetTypes),
                    LogOutput = LogOutputMode.Console,
                    FilenameFormat = FilenameFormat.AssetName,
                    ConvertTexture = true,
                    ImageFormat = ImageFormat.Png,
                    AudioFormat = AudioFormat.Wav,
                    FbxBoneSize = 10,
                    FbxScaleFactor = 1f,
                    FbxAnimationMode = AnimationExportMode.Auto,
                    RestoreTextAssetExtension = true,
                    LoadViaTypeTree = true,
                    BundleBlockInfoCompression = CompressionType.Auto,
                    BundleBlockCompression = CompressionType.Auto,
                    Live2DModelGroupOption = Live2DModelGroupOption.ContainerPath,
                    Live2DMotionMode = Live2DMotionMode.MonoBehaviour,
                };
            }
        }
    }
}
