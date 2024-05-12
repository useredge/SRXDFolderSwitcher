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
using ChartHelper.Parsing;
using System;

namespace SRXDFolderSwitcher
{
    [BepInPlugin("com.useredge.srxdfolderswitcher", "SRXD Folder Switcher", "1.1.0")]
    public class FolderSwitcherPlugin : BaseUnityPlugin
    {
        public static new ManualLogSource Logger;

        private static string configFilePath = Path.Combine(Paths.ConfigPath, "FolderSwitcherConfig.json");

        public static List<JsonKeyValuePair<string, string>> customPaths = new List<JsonKeyValuePair<string, string>>();
        private static JsonKeyValuePair<string, string> defaultPath = new JsonKeyValuePair<string, string>("Default", FileHelper.CustomPath);

        public static int folderIndex;

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
                    __result = Path.Combine(customPaths[folderIndex].Value, "AudioClips");
                    return false;
                }

                return true;

            }

            [HarmonyPatch(typeof(ExternalTexture2DAsset), nameof(ExternalTexture2DAsset.ExternalRawFilePath), MethodType.Getter), HarmonyPrefix]
            private static bool ForceArtFolder_Prefix(ref string __result)
            {
                if (customPaths[folderIndex].Value != "")
                {
                    __result = Path.Combine(customPaths[folderIndex].Value, "AlbumArt");
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

            private static List<FileCollection> fileCollectionList = new List<FileCollection>();

            private static int intentToMoveIndex = 0;

            private static GameObject _moveButton;
            private static GameObject _copyButton;
            private static GameObject _pasteButton;

            private static GameObject _clipboardCount;

            private static GameObject _folderSwitchChoice;

            private static bool GameObjectExists(string gameObjPath)
            {
                return GameObject.Find(gameObjPath) == null ? false : true;
            }

            private static void TryMoveFileCollectionToDestination(FileCollection fileCollection, string destinationPath)
            {

                bool hasCopiedFile = false;

                try
                {
                    File.Copy(
                        Path.Combine(AssetBundleSystem.CUSTOM_DATA_PATH, fileCollection.SrtbName), Path.Combine(destinationPath, fileCollection.SrtbName)
                    );

                    if (fileCollection.AlbumArtFileName != null || fileCollection.AlbumArtFileName != String.Empty)
                    {
                        File.Copy(
                            Path.Combine(ExternalTexture2DAsset.ExternalRawFilePath, fileCollection.AlbumArtFileName), Path.Combine(destinationPath, "AlbumArt", fileCollection.AlbumArtFileName)
                        );
                    }

                    foreach (string file in fileCollection.AudioFileNames)
                    {
                        if (file == null || file == String.Empty) continue;

                        File.Copy(
                            Path.Combine(ExternalAudioClipAsset.CustomsRawFilePath, file), Path.Combine(destinationPath, "AudioClips", file)
                        );
                    }

                    hasCopiedFile = true;

                }
                catch (IOException error)
                {
                    //Logger.LogError(error.Message);
                    NotificationSystemGUI.AddMessage(error.Message, 6);
                }

                if (File.Exists(Path.Combine(destinationPath, fileCollection.SrtbName)))
                {

                    if (!hasCopiedFile) return;

                    try
                    {

                        File.Delete(Path.Combine(AssetBundleSystem.CUSTOM_DATA_PATH, fileCollection.SrtbName));

                        if (fileCollection.AlbumArtFileName != null || fileCollection.AlbumArtFileName != String.Empty)
                        {
                            File.Delete(Path.Combine(ExternalTexture2DAsset.ExternalRawFilePath, fileCollection.AlbumArtFileName));
                        }

                        foreach (string file in fileCollection.AudioFileNames)
                        {
                            if (file == null || file == String.Empty) continue;

                            File.Delete(Path.Combine(ExternalAudioClipAsset.CustomsRawFilePath, file));
                        }

                    }
                    catch (IOException error)
                    {
                        NotificationSystemGUI.AddMessage(error.Message, 6);
                    }

                    var refreshMethod = AccessTools.Method(typeof(CustomAssetLoadingHelper), "FullRefresh");

                    refreshMethod.Invoke(CustomAssetLoadingHelper.Instance, null);

                    NotificationSystemGUI.AddMessage($"{fileCollection.SrtbName} moved successfully.", 6);

                }

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

            private static void OpenFolderChoiceDialog()
            {

                UpdateClipboardCount();

                ModalMessageDialog.ModalMessage msg = new ModalMessageDialog.ModalMessage();

                msg.message = "Choose the destination folder";
                msg.cancelCallback += () =>
                {
                    NotificationSystemGUI.AddMessage("Cancelled move.");
                    _folderSwitchChoice?.SetActive(false);
                };
                msg.cancelText = new TranslationReference("UI_No", false);

                msg.affirmativeCallback += () =>
                {
                    foreach (FileCollection chart in fileCollectionList)
                    {
                        TryMoveFileCollectionToDestination(chart, customPaths[intentToMoveIndex].Value);
                    }

                    fileCollectionList.Clear();
                    UpdateClipboardCount();
                    _folderSwitchChoice?.SetActive(false);
                };
                msg.affirmativeText = new TranslationReference("UI_Yes", false);

                ModalMessageDialog.Instance.AddMessage(msg);

                _folderSwitchChoice?.SetActive(true);

                if (_folderSwitchChoice != null) return;

                _folderSwitchChoice = UnityEngine.Object.Instantiate(BuildSettingsAsset.Instance.multiChoiceOptionPrefab,
                    ModalMessageDialog.Instance.transform.Find("Container/Body"));

                _folderSwitchChoice.transform.SetSiblingIndex(4);
                _folderSwitchChoice.name = "FolderSwitchOptions";

                UnityEngine.Object.Destroy(_folderSwitchChoice.GetComponent<XDNavigableOptionMultiChoice_IntValue>());

                var multiChoice = _folderSwitchChoice.GetComponent<XDNavigableOptionMultiChoice>();
                //multiChoice.state.callbacks = new XDNavigableOptionMultiChoice.Callbacks();
                multiChoice.SetCallbacksAndValue(
                    intentToMoveIndex,
                    //v => { NotificationSystemGUI.AddMessage("New value: " + v); },
                    value => { intentToMoveIndex = value; },
                    () => new IntRange(0, customPaths.Count),
                    v => customPaths[v].Key
                );

                _folderSwitchChoice.transform.Find("OptionLabel").GetComponent<CustomTextMeshProUGUI>().text = "Available folders";

            }

            private static void GetCurrentTrackData()
            {

                if (fileCollectionList.Any(x => x.SrtbName == XDSelectionListMenu.Instance.CurrentPreviewTrack.Item1.TrackInfoRef.customFile.FileNameNoExtension + ".srtb")) return;

                FileCollection chart = new FileCollection();

                SRTB thisSrtb = SRTB.DeserializeFromFile(XDSelectionListMenu.Instance.CurrentPreviewTrack.Item1.TrackInfoRef.customFile.FilePath);

                string srtbName = XDSelectionListMenu.Instance.CurrentPreviewTrack.Item1.TrackInfoRef.customFile.FileNameNoExtension + ".srtb";

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

                //chart.Dump();

                fileCollectionList.Add(chart);

            }

            private static void UpdateClipboardCount()
            {
                _clipboardCount.transform.Find("Filename").GetComponent<CustomTextMeshProUGUI>().text = "Charts in clipboard: " + fileCollectionList.Count();

                Logger.LogWarning("------");
                foreach (var item in fileCollectionList)
                {
                    Logger.LogWarning(item.SrtbName);
                }
                Logger.LogWarning("------");

            }

            [HarmonyPatch(typeof(XDSelectionListMenu), nameof(XDSelectionListMenu.OpenSidePanel)), HarmonyPostfix]
            private static void CloneButton_Postfix()
            {

                if (!GameObjectExists(deleteButtonPath + "DeleteSelected")) return;

                if (_moveButton != null) return;

                //Move button

                var deleteButton = GameObject.Find(deleteButtonPath + "DeleteSelected");

                _moveButton = GameObject.Instantiate(deleteButton, deleteButton.transform.parent);

                _moveButton.gameObject.name = "MoveToFolder";

                GameObject.Destroy(_moveButton.transform.Find("IconContainer/ButtonText").GetComponent<TranslatedTextMeshPro>());
                _moveButton.transform.Find("IconContainer/ButtonText").GetComponent<CustomTextMeshProUGUI>().text = "Move selected";
                _moveButton.transform.SetSiblingIndex(1);

                _moveButton.GetComponent<XDNavigableButton>().onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                _moveButton.GetComponent<XDNavigableButton>().onClick.AddListener(OpenFolderChoiceDialog);

                //Copy to clipboard

                _copyButton = GameObject.Instantiate(deleteButton, deleteButton.transform.parent);

                _copyButton.gameObject.name = "CopyToClipboard";

                GameObject.Destroy(_copyButton.transform.Find("IconContainer/ButtonText").GetComponent<TranslatedTextMeshPro>());
                _copyButton.transform.Find("IconContainer/ButtonText").GetComponent<CustomTextMeshProUGUI>().text = "Copy to clipboard";
                _copyButton.transform.SetSiblingIndex(2);

                _copyButton.GetComponent<XDNavigableButton>().onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                _copyButton.GetComponent<XDNavigableButton>().onClick.AddListener(() => { GetCurrentTrackData(); NotificationSystemGUI.AddMessage($"Added chart to clipboard."); UpdateClipboardCount(); });

                // charts in clipboard

                var filenameString = GameObject.Find(deleteButtonPath + "FilenameContgainer");

                _clipboardCount = GameObject.Instantiate(filenameString, filenameString.transform.parent);
                _clipboardCount.gameObject.name = "ClipboardCount";

                GameObject.Destroy(_clipboardCount.transform.Find("Filename").GetComponent<TranslatedTextMeshPro>());
                _clipboardCount.transform.Find("Filename").GetComponent<CustomTextMeshProUGUI>().text = "Charts in clipboard: " + fileCollectionList.Count();
                _clipboardCount.transform.SetSiblingIndex(1);

                //paste from clipboard

                //_pasteButton = GameObject.Instantiate(deleteButton, deleteButton.transform.parent);

                //_pasteButton.gameObject.name = "PasteFromClipboard";

                //GameObject.Destroy(_pasteButton.transform.Find("IconContainer/ButtonText").GetComponent<TranslatedTextMeshPro>());
                //_pasteButton.transform.Find("IconContainer/ButtonText").GetComponent<CustomTextMeshProUGUI>().text = "Paste from clipboard";
                //_pasteButton.transform.SetSiblingIndex(3);

                //_pasteButton.GetComponent<XDNavigableButton>().onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                //_pasteButton.GetComponent<XDNavigableButton>().onClick.AddListener(
                //    () => { 
                //        foreach(FileCollection chart in currentFileCollection)
                //        {
                //            TryMoveFileCollectionToDestination(chart, customPaths[folderIndex].Value);
                //        };
                //        currentFileCollection.Clear();
                //    }
                //);


            }

        }

    }
}
