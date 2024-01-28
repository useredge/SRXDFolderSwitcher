using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SRXDFolderSwitcher.Classes;

namespace SRXDFolderSwitcher
{
    [BepInPlugin("com.useredge.srxdfolderswitcher", "SRXD Folder Switcher", "1.0.0")]
    public class FolderSwitcherPlugin : BaseUnityPlugin
    {
        public static new ManualLogSource Logger;

        private static string configFilePath = Path.Combine(Paths.ConfigPath, "FolderSwitcherConfig.json");

        private static List<JsonKeyValuePair<string, string>> customPaths = new List<JsonKeyValuePair<string, string>>();
        private static JsonKeyValuePair<string, string> defaultPath = new JsonKeyValuePair<string, string>("Default", "");

        private static int folderIndex;

        private static void cycleList()
        {

            folderIndex = folderIndex + 1;
            if (folderIndex > customPaths.Count - 1)
            {
                folderIndex = 0;
            }

        }

        private void Awake()
        {

            Logger = base.Logger;
            Harmony.CreateAndPatchAll(typeof(PathPatches));

            if (File.Exists(configFilePath))
            {
                Logger.LogInfo($"File exists. Reading.");

                customPaths = JsonConvert.DeserializeObject<List<JsonKeyValuePair<string, string>>>(File.ReadAllText(configFilePath));

                Logger.LogInfo($"Discovered paths: ");
                foreach (JsonKeyValuePair<string, string> path in customPaths)
                {
                    Logger.LogInfo(path.Key);
                }

                if (!customPaths.Contains(defaultPath)) customPaths.Add(defaultPath);
            }
            else
            {
                Logger.LogWarning($"Configuration file not found. Creating...");

                customPaths.Add(defaultPath);
                File.WriteAllText(configFilePath, JsonConvert.SerializeObject(customPaths, Formatting.Indented));

                Logger.LogDebug($"-- File created at: {configFilePath} --");
            }

            folderIndex = customPaths.Count - 1;

        }

        public class PathPatches
        {

            [HarmonyPatch(typeof(AssetBundleSystem), nameof(AssetBundleSystem.CUSTOM_DATA_PATH), MethodType.Getter), HarmonyPrefix]
            private static bool ForceAssetBundleFolder_Prefix(ref string __result)
            {

                if (customPaths[folderIndex].Value != "")
                {
                    __result = customPaths[folderIndex].Value;
                    return false;
                }

                return true;

            }

            [HarmonyPatch(typeof(ExternalAudioClipAsset), nameof(ExternalAudioClipAsset.CustomsRawFilePath), MethodType.Getter), HarmonyPrefix]
            private static bool ForceAudioFolder_Prefix(ref string __result)
            {

                if (customPaths[folderIndex].Value != "")
                {
                    __result = customPaths[folderIndex].Value + "\\AudioClips";
                    return false;
                }

                return true;

            }

            [HarmonyPatch(typeof(ExternalTexture2DAsset), nameof(ExternalTexture2DAsset.ExternalRawFilePath), MethodType.Getter), HarmonyPrefix]
            private static bool ForceArtFolder_Prefix(ref string __result)
            {
                if (customPaths[folderIndex].Value != "")
                {
                    __result = customPaths[folderIndex].Value + "\\AlbumArt";
                    return false;
                }

                return true;
            }

            [HarmonyPatch(typeof(XDMainMenu), "Update"), HarmonyPostfix]
            private static void SwitchFolder_Postfix()
            {

                if (Game.Instance.State != Game.GameState.Initialised) return;

                if (Input.GetKeyDown(KeyCode.F1))
                {
                    cycleList();
                    NotificationSystemGUI.AddMessage($"Switched folder to: {customPaths[folderIndex].Key}.");
                }

            }
            [HarmonyPatch(typeof(XDMainMenu), nameof(XDMainMenu.OpenMenu)), HarmonyPostfix]
            private static void OpenMenu_Postfix()
            {

                NotificationSystemGUI.AddMessage("Press F1 to cycle between custom folders!");

            }

        }
    }
}
