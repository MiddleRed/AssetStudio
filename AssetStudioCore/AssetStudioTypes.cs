#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace AssetStudioCore
{
    public enum AssetStudioLogLevel
    {
        Verbose,
        Debug,
        Info,
        Warning,
        Error,
    }

    public interface IAssetStudioLogSink
    {
        void Log(AssetStudioLogLevel level, string message);
    }

    public interface IAssetStudioProgressSink
    {
        void Report(int value, int index);
    }

    /// <summary>
    /// Receives transient single-line status updates (e.g. an export counter).
    /// Front-ends decide how to render them; the core never writes to the console.
    /// </summary>
    public interface IAssetStudioStatusSink
    {
        void ReportStatus(string message);
        void CompleteStatus();
    }

    public class AssetStudioCoreOptions
    {
        public string InputPath { get; set; } = "";
        public IReadOnlyCollection<string>? AssetTypes { get; set; }
        public string? UnityVersion { get; set; }
        public bool FilterExcludeMode { get; set; }
        public bool FilterWithRegex { get; set; }
        public string? FilterByName { get; set; }
        public string? FilterByContainer { get; set; }
        public IReadOnlyCollection<long>? FilterByPathIds { get; set; }
        public bool LoadAllAssets { get; set; }
        public string? OutputDir { get; set; }
        public IAssetStudioLogSink? LogSink { get; set; }
        public IAssetStudioProgressSink? ProgressSink { get; set; }
    }

    public sealed class AssetStudioRunResult
    {
        public IReadOnlyDictionary<string, long> PhaseMs { get; set; } = new Dictionary<string, long>();
        public IReadOnlyDictionary<string, long> Metrics { get; set; } = new Dictionary<string, long>();
    }

    public sealed class AssetStudioObjectReadOptions
    {
        public long PathId { get; set; }
        public int ObjectIndex { get; set; } = -1;
        public string Kind { get; set; } = "auto";
        public string ImageFormat { get; set; } = "raw_rgba";
    }

    public sealed class AssetStudioObjectReadResult
    {
        public AssetStudioAssetInfo Asset { get; set; } = new AssetStudioAssetInfo();
        public byte[] Payload { get; set; } = Array.Empty<byte>();
        public string PayloadKind { get; set; } = "";
        public string SuggestedExtension { get; set; } = "";
        public IReadOnlyDictionary<string, long> PhaseMs { get; set; } = new Dictionary<string, long>();
    }

    public enum AssetStudioObjectReadErrorKind
    {
        None = 0,
        InvalidRequest = 2,
        AssetNotFound = 6,
        UnsupportedKind = 7,
        InternalError = 100,
    }

    public sealed class AssetStudioObjectReadBatchItemResult
    {
        public int Index { get; set; }
        public int Status { get; set; }
        public AssetStudioObjectReadErrorKind ErrorKind { get; set; }
        public AssetStudioAssetInfo? Asset { get; set; }
        public long PathId { get; set; }
        public int TypeId { get; set; }
        public long Size { get; set; }
        public string? PayloadKind { get; set; }
        public string? SuggestedExtension { get; set; }
        public string? ErrorMessage { get; set; }
        public byte[]? Payload { get; set; }
        public long PayloadOffset { get; set; }
        public long PayloadLen { get; set; }
        public AssetStudioPayloadStreamingTier StreamingTier { get; set; }
    }

    public sealed class AssetStudioObjectReadBatchResult
    {
        public IReadOnlyList<AssetStudioObjectReadBatchItemResult> Reads { get; set; } = Array.Empty<AssetStudioObjectReadBatchItemResult>();
        public int FailedCount { get; set; }
        public long PayloadLen { get; set; }
    }

    public interface IAssetStudioPayloadWriter
    {
        Stream PayloadStream { get; }
    }

    public enum AssetStudioPayloadStreamingTier
    {
        ManagedPayload = 0,
        DirectBufferResident = 1,
        GeneratedStreaming = 2,
        SourceStreaming = 3,
        TempFileStreaming = 4,
    }

    public sealed class AssetStudioStreamPayloadWriter : IAssetStudioPayloadWriter
    {
        public AssetStudioStreamPayloadWriter(Stream payloadStream)
        {
            PayloadStream = payloadStream ?? throw new ArgumentNullException(nameof(payloadStream));
        }

        public Stream PayloadStream { get; }
    }

    internal sealed class AssetStudioObjectPayload
    {
        public byte[] Payload { get; set; } = Array.Empty<byte>();
        public string PayloadKind { get; set; } = "";
        public string SuggestedExtension { get; set; } = "";
        public IReadOnlyDictionary<string, long> PhaseMs { get; set; } = new Dictionary<string, long>();
    }

    internal readonly struct AssetStudioObjectStreamPayload
    {
        public AssetStudioObjectStreamPayload(long payloadLen, string payloadKind, string suggestedExtension, AssetStudioPayloadStreamingTier streamingTier)
        {
            PayloadLen = payloadLen;
            PayloadKind = payloadKind;
            SuggestedExtension = suggestedExtension;
            StreamingTier = streamingTier;
        }

        public long PayloadLen { get; }
        public string PayloadKind { get; }
        public string SuggestedExtension { get; }
        public AssetStudioPayloadStreamingTier StreamingTier { get; }
    }

    public sealed class AssetStudioInspectOptions : AssetStudioCoreOptions
    {
        public bool IncludeAssets { get; set; } = true;
    }

    public sealed class AssetStudioObjectListOptions
    {
        public int Offset { get; set; }
        public int Limit { get; set; }
        public IReadOnlyCollection<string>? AssetTypes { get; set; }
    }

    public enum AssetStudioObjectLookupKind
    {
        PathId = 1,
        Name = 2,
        Container = 3,
        Type = 4,
    }

    public sealed class AssetStudioObjectLookupOptions
    {
        public AssetStudioObjectLookupKind LookupKind { get; set; }
        public long PathId { get; set; }
        public string? Query { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }
        public bool Contains { get; set; }
        public IReadOnlyCollection<string>? AssetTypes { get; set; }
    }

    public sealed class AssetStudioObjectLookupResult
    {
        public int Offset { get; set; }
        public int Limit { get; set; }
        public int TotalCount { get; set; }
        public AssetStudioAssetInfo[] Assets { get; set; } = Array.Empty<AssetStudioAssetInfo>();
    }

    public sealed class AssetStudioInspectResult
    {
        public int AssetsFileCount { get; set; }
        public int ExportableAssetCount { get; set; }
        public string? UnityVersion { get; set; }
        public IReadOnlyCollection<AssetStudioAssetInfo> Assets { get; set; } = Array.Empty<AssetStudioAssetInfo>();
        public IReadOnlyDictionary<string, long> PhaseMs { get; set; } = new Dictionary<string, long>();
    }

    public sealed class AssetStudioAssetInfo
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("container")]
        public string? Container { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("type_id")]
        public int TypeId { get; set; }

        [JsonPropertyName("path_id")]
        public long PathId { get; set; }

        [JsonPropertyName("unique_id")]
        public string? UniqueId { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("estimated_payload_capacity")]
        public long EstimatedPayloadCapacity { get; set; }

        [JsonPropertyName("raw_payload_capacity")]
        public long RawPayloadCapacity { get; set; }

        [JsonPropertyName("image_payload_capacity")]
        public long ImagePayloadCapacity { get; set; }

        [JsonPropertyName("text_payload_capacity")]
        public long TextPayloadCapacity { get; set; }

        [JsonPropertyName("payload_capacity_flags")]
        public int PayloadCapacityFlags { get; set; }

        [JsonPropertyName("source_file")]
        public string? SourceFile { get; set; }
    }
}
