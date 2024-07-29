using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;
using UnityEngine;

namespace SRXDFolderSwitcher.Patches
{
    public class PathPatches
    {

        [HarmonyPatch(typeof(AssetBundleSystem), nameof(AssetBundleSystem.CUSTOM_DATA_PATH), MethodType.Getter), HarmonyPrefix]
        private static bool ForceAssetBundleFolder_Prefix(ref string __result)
        {

            if (FolderSwitcherPlugin.currentCustomPath.Value != "")
            {
                __result = FolderSwitcherPlugin.currentCustomPath.Value;
                return false;
            }

            return true;

        }

        [HarmonyPatch(typeof(ExternalAudioClipAsset), nameof(ExternalAudioClipAsset.CustomsRawFilePath), MethodType.Getter), HarmonyPrefix]
        private static bool ForceAudioFolder_Prefix(ref string __result)
        {

            if (FolderSwitcherPlugin.currentCustomPath.Value != "")
            {
                __result = Path.Combine(FolderSwitcherPlugin.currentCustomPath.Value, "AudioClips");
                return false;
            }

            return true;

        }

        [HarmonyPatch(typeof(ExternalTexture2DAsset), nameof(ExternalTexture2DAsset.ExternalRawFilePath), MethodType.Getter), HarmonyPrefix]
        private static bool ForceArtFolder_Prefix(ref string __result)
        {
            if (FolderSwitcherPlugin.currentCustomPath.Value != "")
            {
                __result = Path.Combine(FolderSwitcherPlugin.currentCustomPath.Value, "AlbumArt");
                return false;
            }

            return true;
        }

    }
}
