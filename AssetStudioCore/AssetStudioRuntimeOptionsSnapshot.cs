#nullable enable

using AssetStudio;
using AssetStudioCore.Options;
using CubismLive2DExtractor;
using System.Collections.Generic;

namespace AssetStudioCore.Runtime
{
    /// <summary>
    /// Front-end-agnostic snapshot of runtime options. Front-ends (CLI, FFI hosts)
    /// fill this DTO from their own option sources and hand it to
    /// <see cref="AssetStudioRuntimeOptions.ApplySnapshot"/>.
    /// </summary>
    internal sealed class AssetStudioRuntimeOptionsSnapshot
    {
        public bool IsParsed { get; set; }
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
        public string StripPathPrefix { get; set; } = string.Empty;
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
    }
}
