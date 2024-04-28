using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SRXDFolderSwitcher.Classes;
using XDMenuPlay;

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
            Harmony.CreateAndPatchAll(typeof(SelectionPatches));

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

        public class SelectionPatches
        {

            private const string deleteButtonPath = "GameScene/MenuScenes(Clone)/MainMenuWorldSpaceContainer/Canvas/XDSelectionMenu/Container/Content/TabPanelContainer/TabPanelColumn/TabsOffset/TabsContainer/TabPanelDisplayList/TabPanel_ManageCustoms(Clone)/Scroll List Tab Prefab/Scroll View/Viewport/Content/ManageTrackPopout/";

            private static GameObject _clonedButton;

            private static GameObject _folderSwitchChoice;

            private static bool GameObjectExists(string gameObjPath)
            {
                return GameObject.Find(gameObjPath) == null ? false : true;
            }

            private static void OpenFolderChoiceDialog()
            {
                ModalMessageDialog.ModalMessage msg = new ModalMessageDialog.ModalMessage();
                msg.message = "balls";
                msg.cancelCallback += () =>
                {
                    NotificationSystemGUI.AddMessage("no balls :(");
                    _folderSwitchChoice?.SetActive(false);
                };
                msg.cancelText = new TranslationReference("UI_No", false);
                msg.affirmativeCallback += () =>
                {
                    NotificationSystemGUI.AddMessage("yo ballin :D");
                    _folderSwitchChoice?.SetActive(false);
                };
                msg.affirmativeText = new TranslationReference("UI_Yes", false);
                ModalMessageDialog.Instance.AddMessage(msg);
                _folderSwitchChoice?.SetActive(true);
                if (_folderSwitchChoice != null) return;
                _folderSwitchChoice = Object.Instantiate(BuildSettingsAsset.Instance.multiChoiceOptionPrefab,
                    ModalMessageDialog.Instance.transform.Find("Container/Body"));
                _folderSwitchChoice.transform.SetSiblingIndex(4);
                _folderSwitchChoice.name = "FolderSwitchOptions";
                Object.Destroy(_folderSwitchChoice.GetComponent<XDNavigableOptionMultiChoice_IntValue>());
                var multiChoice = _folderSwitchChoice.GetComponent<XDNavigableOptionMultiChoice>();
                //multiChoice.state.callbacks = new XDNavigableOptionMultiChoice.Callbacks();
                multiChoice.SetCallbacksAndValue(
                    0,
                    v => { NotificationSystemGUI.AddMessage("New value: " + v); },
                    () => new IntRange(0, customPaths.Count),
                    v => customPaths[v].Key
                );
                _folderSwitchChoice.transform.Find("OptionLabel").GetComponent<CustomTextMeshProUGUI>().text = "cursed";
            }

            [HarmonyPatch(typeof(XDSelectionListMenu), nameof(XDSelectionListMenu.UpdatePreviewHandle)), HarmonyPostfix]
            private static void Update_Postfix()
            {

                if (XDSelectionListMenu.Instance == null) return;

                if (Input.GetKeyDown(KeyCode.F2))
                {
                    NotificationSystemGUI.AddMessage($"Current track: {XDSelectionListMenu.Instance.CurrentPreviewTrack.Item1.TrackInfoRef.customFile.FilePath}");
                }

            }

            [HarmonyPatch(typeof(XDSelectionListMenu), nameof(XDSelectionListMenu.OpenSidePanel)), HarmonyPostfix]
            private static void CloneButton_Postfix()
            {

                if (!GameObjectExists(deleteButtonPath + "DeleteSelected")) return;

                if (_clonedButton != null) return;

                var deleteButton = GameObject.Find(deleteButtonPath + "DeleteSelected");
                _clonedButton = GameObject.Instantiate(deleteButton, deleteButton.transform.parent);

                _clonedButton.gameObject.name = "MoveToFolder";

                GameObject.Destroy(_clonedButton.transform.Find("IconContainer/ButtonText").GetComponent<TranslatedTextMeshPro>());
                _clonedButton.transform.Find("IconContainer/ButtonText").GetComponent<CustomTextMeshProUGUI>().text = "Move to folder";
                _clonedButton.transform.SetSiblingIndex(1);

                _clonedButton.GetComponent<XDNavigableButton>().onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                _clonedButton.GetComponent<XDNavigableButton>().onClick.AddListener(OpenFolderChoiceDialog);

            }

        }

    }
}
