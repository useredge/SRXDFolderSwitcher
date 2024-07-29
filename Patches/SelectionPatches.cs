using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using UnityEngine;

using HarmonyLib;

using ChartHelper.Parsing;

using SRXDFolderSwitcher.Classes;

namespace SRXDFolderSwitcher.Patches
{
    public class SelectionPatches
    {

        public static List<FileCollection> fileCollectionList = new List<FileCollection>();

        public static void TryMoveFileCollectionToDestination(FileCollection fileCollection, string destinationPath, ref int successfulTransfers)
        {

            bool hasCopiedFile = false;

            try
            {
                File.Copy(
                    Path.Combine(AssetBundleSystem.CUSTOM_DATA_PATH, fileCollection.SrtbName), Path.Combine(destinationPath, fileCollection.SrtbName)
                , false);

                if (fileCollection.AlbumArtFileName != "" || fileCollection.AlbumArtFileName != String.Empty)
                {
                    File.Copy(
                        Path.Combine(ExternalTexture2DAsset.ExternalRawFilePath, fileCollection.AlbumArtFileName), Path.Combine(destinationPath, "AlbumArt", fileCollection.AlbumArtFileName)
                    , true);
                }

                foreach (string file in fileCollection.AudioFileNames)
                {
                    if (file == "" || file == String.Empty) continue;

                    File.Copy(
                        Path.Combine(ExternalAudioClipAsset.CustomsRawFilePath, file), Path.Combine(destinationPath, "AudioClips", file)
                    , true);
                }

                hasCopiedFile = true;
                successfulTransfers++;

            }
            catch (IOException error)
            {
                FolderSwitcherPlugin.Logger.LogError(error.Message);
                NotificationSystemGUI.AddMessage(String.Concat("COPY: ", error.Message), 6);
            }

            if (File.Exists(Path.Combine(destinationPath, fileCollection.SrtbName)))
            {

                if (!hasCopiedFile) return;

                try
                {

                    File.Delete(Path.Combine(AssetBundleSystem.CUSTOM_DATA_PATH, fileCollection.SrtbName));

                    if (fileCollection.AlbumArtFileName != "" || fileCollection.AlbumArtFileName != String.Empty)
                    {
                        File.Delete(Path.Combine(ExternalTexture2DAsset.ExternalRawFilePath, fileCollection.AlbumArtFileName));
                    }

                    foreach (string file in fileCollection.AudioFileNames)
                    {
                        if (file == "" || file == String.Empty) continue;

                        File.Delete(Path.Combine(ExternalAudioClipAsset.CustomsRawFilePath, file));
                    }

                }
                catch (IOException error)
                {
                    NotificationSystemGUI.AddMessage(String.Concat("DELETE: ", error.Message), 6);
                }

            }

        }

        public static void CallTrackListRefresh()
        {
            var refreshMethod = AccessTools.Method(typeof(CustomAssetLoadingHelper), "FullRefresh");

            refreshMethod.Invoke(CustomAssetLoadingHelper.Instance, null);
        }

        private static string GetAudioFilePathWithExtension(string audioFileAssetName)
        {
            return ExternalAudioClipAsset.GetPreferredFilePathWithExtension(
                ExternalAudioClipAsset.GetExternalRawFilePathWithoutExtension(audioFileAssetName)
            );
        }

        private static string GetAlbumArtPathWithExtension(string albumArtAssetName)
        {
            return ExternalTexture2DAsset.GetPreferredFilePathWithExtension(
                ExternalTexture2DAsset.GetExternalRawFilePathWithoutExtension(albumArtAssetName)
            );
        }

        public static void AffirmativeMoveCharts(int intentToMoveIndex)
        {

            int successfulTransfers = 0;

            foreach (FileCollection chart in fileCollectionList)
            {
                TryMoveFileCollectionToDestination(chart, FolderSwitcherPlugin.customPaths[intentToMoveIndex].Value, ref successfulTransfers);
            }

            if (successfulTransfers > 0) NotificationSystemGUI.AddMessage($"Successfully moved {successfulTransfers} charts!");

            fileCollectionList.Clear();
            UpdateClipboardCount();
            CallTrackListRefresh();

        }

        public static void GetCurrentTrackData()
        {

            string fileNameNoExt = XDSelectionListMenu.Instance.CurrentPreviewTrack.Item1.TrackInfoRef.customFile.FileNameNoExtension;

            if (fileCollectionList.Any(x => x.SrtbName == fileNameNoExt + ".srtb")) return;

            FileCollection chart = new FileCollection();

            SRTB thisSrtb = SRTB.DeserializeFromFile(XDSelectionListMenu.Instance.CurrentPreviewTrack.Item1.TrackInfoRef.customFile.FilePath);

            string srtbName = fileNameNoExt + ".srtb";

            string albumArtFilename = XDSelectionListMenu.Instance.CurrentPreviewTrack.Item1.AlbumArtReferenceCopy().AssetName;

            List<string> audioFileNames = new List<string>();

            for (int i = 0; i < thisSrtb.ClipInfoCount; i++)
            {
                string fullAudioPath = GetAudioFilePathWithExtension(thisSrtb.GetClipInfo(i).ClipAssetReference.AssetName);
                audioFileNames.Add(fullAudioPath == null ? String.Empty : fullAudioPath.Substring(fullAudioPath.LastIndexOf('\\') + 1));
            }

            chart.SrtbName = srtbName;

            string fullAlbumPath = GetAlbumArtPathWithExtension(albumArtFilename);
            chart.AlbumArtFileName = fullAlbumPath == null ? String.Empty : fullAlbumPath.Substring(fullAlbumPath.LastIndexOf('\\') + 1);

            chart.AudioFileNames = audioFileNames;

            chart.Dump();

            fileCollectionList.Add(chart);

        }

        internal static void UpdateClipboardCount()
        {

            FolderSwitcherPlugin.refFileCounter.ExtraText = fileCollectionList.Count().ToString();

            FolderSwitcherPlugin.Logger.LogWarning("--- Clipboard list ---");
            foreach (var item in fileCollectionList)
            {
                FolderSwitcherPlugin.Logger.LogWarning(item.SrtbName);
            }
            FolderSwitcherPlugin.Logger.LogWarning("------");

        }

        internal static void UpdateCurrentFolderLabel()
        {

            FolderSwitcherPlugin.refCurrentFolder.ExtraText = FolderSwitcherPlugin.currentCustomPath.Key;
            
        }

        [HarmonyPatch(typeof(XDMainMenu), nameof(XDMainMenu.OpenMenu)), HarmonyPostfix]
        private static void ClearClipboardOnOpen_Postfix()
        {
            if (fileCollectionList.Count == 0) return;
            fileCollectionList.Clear();
            UpdateClipboardCount();
        }

        [HarmonyPatch(typeof(XDSelectionListMenu), "Update"), HarmonyPostfix]
        private static void CtrlPlusC_Postfix()
        {

            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
            {
                GetCurrentTrackData();
                NotificationSystemGUI.AddMessage($"Added chart to clipboard.");
                UpdateClipboardCount();
            }

        }

    }
}
