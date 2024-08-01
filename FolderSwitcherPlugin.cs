using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

using UnityEngine;

using ChartHelper.Parsing;
using Newtonsoft.Json;

using SpinCore.Utility;
using SpinCore.UI;
using SpinCore.Translation;

using SRXDFolderSwitcher.Classes;
using SRXDFolderSwitcher.Patches;

namespace SRXDFolderSwitcher
{
    [BepInPlugin("com.useredge.srxdfolderswitcher", "SRXD Folder Switcher", "1.2.0")]
    [BepInDependency("srxd.raoul1808.spincore", BepInDependency.DependencyFlags.HardDependency)]
    public class FolderSwitcherPlugin : BaseUnityPlugin
    {

        internal static new ManualLogSource Logger;

        private protected static string configFilePath = Path.Combine(Paths.ConfigPath, "FolderSwitcherConfig.json");

        public static List<PathEntry<string, string>> customPaths = new List<PathEntry<string, string>>();
        public static PathEntry<string, string> defaultPath = new PathEntry<string, string>("Default", FileHelper.CustomPath);

        private static ConfigSchema configData = new ConfigSchema();

        public static PathEntry<string, string> currentCustomPath;

        internal static CustomTextComponent refCurrentFolder;
        internal static CustomTextComponent refFileCounter;

        internal static bool hasDoneFirstLoad = false;

        private Texture2D LoadImage(string name)
        {
            using (Stream imageStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SRXDFolderSwitcher.Resources." + name))
            {
                return RuntimeAssetLoader.Texture2DFromStream(imageStream);
            }
        }

        private void Awake()
        {

            Logger = base.Logger;
            Harmony.CreateAndPatchAll(typeof(PathPatches));
            Harmony.CreateAndPatchAll(typeof(SelectionPatches));

            // config file check step

            if (File.Exists(configFilePath))
            {

                Logger.LogWarning($"-- Configuration file found. Reading. --");

                configData = JsonConvert.DeserializeObject<ConfigSchema>(File.ReadAllText(configFilePath));

                Logger.LogWarning($"Discovered paths: ");
                foreach (PathEntry<string, string> path in configData.Paths)
                {
                    if (!Directory.Exists(path.Value))
                    {
                        Logger.LogWarning($"The folder \"{path.Key}\" does not currently exist. \nIt will be created when switched to.");
                    }
                    else
                    {
                        Logger.LogInfo(path.Key);
                    }
                }

                customPaths = configData.Paths;

                if (!customPaths.Contains(defaultPath)) customPaths.Insert(0, defaultPath);

                if (!customPaths.Any(p => p.Key == configData.DefaultPreloadPath))
                {
                    currentCustomPath = customPaths.FirstOrDefault();
                }
                else
                {
                    currentCustomPath = configData.Paths.Where(p => p.Key == configData.DefaultPreloadPath).FirstOrDefault();
                }

                Logger.LogWarning($"Preloaded path: {configData.DefaultPreloadPath}");

            }
            else
            {

                Logger.LogWarning($"-- Configuration file not found. Creating... --");

                customPaths.Insert(0, defaultPath);

                ConfigSchema schema = new ConfigSchema()
                {

                    Paths = customPaths,
                    DefaultPreloadPath = "Default",

                };

                File.WriteAllText(configFilePath, JsonConvert.SerializeObject(schema, Formatting.Indented));

                currentCustomPath = schema.Paths.FirstOrDefault();

                Logger.LogWarning($"-- Configuration file created at /BepinEx/config/FolderSwitcherConfig.json --");
                Logger.LogWarning($"-- Consider editing the configuration to set your default preloaded path --");

            }

            // create mod UI

            var modIcon = LoadImage("icon.png");

            var modIconSprite = Sprite.Create(modIcon, new Rect(0, 0, 450, 450), Vector2.zero);

            var sidePanel = UIHelper.CreateSidePanel("FolderSwitcherSettings", "FSP_TabName", modIconSprite);

            var keysStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SRXDFolderSwitcher.Resources.keys.json");
            TranslationHelper.LoadTranslationsFromStream(keysStream);

            sidePanel.OnSidePanelLoaded += parent =>
            {

                // header and current folder label

                UIHelper.CreateSectionHeader(parent, "StatusHeader", "FSP_Status", false);

                refCurrentFolder = UIHelper.CreateLabel(parent, "CurrentFolderText", "FSP_CurrentFolder");

                refCurrentFolder.ExtraText = currentCustomPath.Key;

                refFileCounter = UIHelper.CreateLabel(parent, "FileCollectionCount", "FSP_FileCollectionCount");

                refFileCounter.ExtraText = SelectionPatches.fileCollectionList.Count().ToString();

                // switch folder button

                UIHelper.CreateButton
                (
                    parent,
                    "SwitchFolder",
                    "FSP_SwitchFolderButton",
                    () =>
                    {

                        var intentToMoveIndex = 0;

                        ModalMessageDialog.ModalMessage msg = new ModalMessageDialog.ModalMessage();

                        msg.headerTranslation = new TranslationReference("FSP_Header_SwitchFolder", true);
                        msg.cancelText = new TranslationReference("Cancel", false);

                        msg.cancelCallback += () =>
                        {
                        };

                        msg.affirmativeCallback += () =>
                        {

                            currentCustomPath = customPaths[intentToMoveIndex];

                            SelectionPatches.CallTrackListRefresh();
                            SelectionPatches.UpdateCurrentFolderLabel();

                        };

                        msg.affirmativeText = new TranslationReference("Accept", false);

                        ModalMessageDialogExtensions.OpenWithCustomUI(msg, dialogParent =>
                        {

                            var _folderSwitcherMultiChoice = UIHelper.CreateLargeMultiChoiceButton(dialogParent, "FolderPickerInDialog", "FSP_FolderPickerInDialog",
                            defaultValue: intentToMoveIndex,
                            valueChanged: index => { intentToMoveIndex = index; },
                            valueRangeRequested: () => new IntRange(0, FolderSwitcherPlugin.customPaths.Count),
                            valueTextRequested: index => FolderSwitcherPlugin.customPaths[index].Key
                            );

                        });

                    }
                );

                UIHelper.CreateSectionHeader(parent, "ActionHeader", "FSP_Actions", false);

                // copy to clipboard

                UIHelper.CreateButton
                (
                    parent,
                    "CopyToClipboardButton",
                    "FSP_CopyToClipboard",
                    () =>
                    {
                        NotificationSystemGUI.AddMessage("Added chart to clipboard.");
                        SelectionPatches.GetCurrentTrackData();
                        SelectionPatches.UpdateClipboardCount();
                        SelectionPatches.UpdateCurrentFolderLabel();
                    }
                );

                // clear clipboard

                UIHelper.CreateButton
                (
                    parent,
                    "ClearClipboardButton",
                    "FSP_ClearClipboard",
                    () =>
                    {
                        SelectionPatches.fileCollectionList.Clear();
                        SelectionPatches.UpdateClipboardCount();
                        NotificationSystemGUI.AddMessage("Cleared.");
                    }
                );

                // move all selected charts

                UIHelper.CreateButton
                (
                    parent,
                    "MoveSelectedButton",
                    "FSP_MoveSelected",
                    () =>
                    {

                        var intentToMoveIndex = 0;

                        ModalMessageDialog.ModalMessage msg = new ModalMessageDialog.ModalMessage();

                        msg.headerTranslation = new TranslationReference("FSP_Header_MoveToFolder", true);
                        msg.cancelText = new TranslationReference("Cancel", false);

                        msg.affirmativeCallback += () =>
                        {

                            SelectionPatches.AffirmativeMoveCharts(intentToMoveIndex);

                        };

                        msg.cancelCallback += () => { };

                        msg.affirmativeText = new TranslationReference("Accept", false);

                        ModalMessageDialogExtensions.OpenWithCustomUI(msg, dialogParent =>
                        {

                            var _destinationMultiChoice = UIHelper.CreateLargeMultiChoiceButton(dialogParent, "FolderPickerInDialog", "FSP_FolderPickerInDialog",
                            defaultValue: intentToMoveIndex,
                            valueChanged: index => { intentToMoveIndex = index; },
                            valueRangeRequested: () => new IntRange(0, FolderSwitcherPlugin.customPaths.Count),
                            valueTextRequested: index => FolderSwitcherPlugin.customPaths[index].Key
                            );

                        });

                    }
                );

                UIHelper.CreateSectionHeader(parent, "UtilityHeader", "FSP_Utility", false);

                UIHelper.CreateButton
                (
                    parent,
                    "RefreshFolderButton",
                    "FSP_RefreshFolder",
                    () =>
                    {
                        SelectionPatches.CallTrackListRefresh();

                    }
                );
            };

        }

    }
}
