#nullable enable

using AssetStudioCore.Runtime;
using AssetStudioCore.Options;
using System;
using System.Collections.Generic;

namespace AssetStudioCLI
{
    internal static class AssetStudioCliOptionsSnapshot
    {
        public static AssetStudioRuntimeOptionsSnapshot CreateFromCurrent()
        {
            return new AssetStudioRuntimeOptionsSnapshot
            {
                IsParsed = CLIOptions.isParsed,
                FilterBy = CLIOptions.filterBy,
                FilterWithRegex = CLIOptions.f_filterWithRegex.Value,
                FilterExcludeMode = CLIOptions.f_filterExcludeMode.Value,
                FilterByNameValues = new List<string>(CLIOptions.o_filterByName.Value),
                FilterByContainerValues = new List<string>(CLIOptions.o_filterByContainer.Value),
                FilterByPathIdValues = new List<string>(CLIOptions.o_filterByPathID.Value),
                FilterByTextValues = new List<string>(CLIOptions.o_filterByText.Value),
                WorkMode = CLIOptions.o_workMode.Value,
                ExportAssetListType = CLIOptions.o_exportAssetList.Value,
                LogLevel = CLIOptions.o_logLevel.Value,
                InputPaths = new List<string>(CLIOptions.inputPathList),
                OutputFolder = CLIOptions.o_outputFolder.Value,
                LoadAllAssets = CLIOptions.f_loadAllAssets.Value,
                GroupAssetsBy = CLIOptions.o_groupAssetsBy.Value,
                MaxParallelExportTasks = CLIOptions.o_maxParallelExportTasks.Value,
                StripPathPrefix = CLIOptions.o_stripPathPrefix.Value,
                FbxAnimationMode = CLIOptions.o_fbxAnimMode.Value,
                Live2DMotionMode = CLIOptions.o_l2dMotionMode.Value,
                Live2DForceBezier = CLIOptions.f_l2dForceBezier.Value,
                Live2DModelGroupOption = CLIOptions.o_l2dGroupOption.Value,
                Live2DSearchByFilename = CLIOptions.f_l2dAssetSearchByFilename.Value,
                AssemblyPath = CLIOptions.o_assemblyPath.Value,
                ExportAssetTypes = new List<AssetStudio.ClassIDType>(CLIOptions.o_exportAssetTypes.Value),
                LogOutput = CLIOptions.o_logOutput.Value,
                FilenameFormat = CLIOptions.o_filenameFormat.Value,
                OverwriteExisting = CLIOptions.f_overwriteExisting.Value,
                ConvertTexture = CLIOptions.convertTexture,
                ImageFormat = CLIOptions.o_imageFormat.Value,
                AudioFormat = CLIOptions.o_audioFormat.Value,
                FbxBoneSize = CLIOptions.o_fbxBoneSize.Value,
                FbxScaleFactor = CLIOptions.o_fbxScaleFactor.Value,
                FbxUvsAsDiffuseMaps = CLIOptions.f_fbxUvsAsDiffuseMaps.Value,
                RestoreTextAssetExtension = !CLIOptions.f_notRestoreExtensionName.Value,
                RawByteArrayFromMonoBehaviour = CLIOptions.f_rawByteArrayFromMono.Value,
                LoadViaTypeTree = !CLIOptions.f_avoidLoadingViaTypetree.Value,
                UnityVersion = CLIOptions.o_unityVersion.Value,
                BundleBlockInfoCompression = CLIOptions.o_bundleBlockInfoCompression.Value,
                BundleBlockCompression = CLIOptions.o_bundleBlockCompression.Value,
                DecompressToDisk = CLIOptions.f_decompressToDisk.Value,
            };
        }
    }
}
