# Folder Switcher
### Installation
1. Place SRXDFolderSwitcher.dll, ChartHelper.Parsing.dll, Newtonsoft.Json.dll and SpinCore.dll in **Spin Rhythm/BepInEx/plugins**.
2. Run the game with the mod installed once. This will create a JSON configuration file in **Spin Rhythm/BepInEx/config**.
3. Close the game, edit the configuration file to your liking.
4. Restart.

## Usage instructions
### Setting up the configuration file

Remember to follow the **JSON format** when adding custom paths, as well as properly **escaping the backslash characters** in the path strings.

### Preload path

Because switching folders is done from within the track list through custom UI, you have the option to choose one of your custom folders as the first one to be loaded upon 
pressing Play in the main menu (to avoid loading your default custom folder which likely has thousands of charts in it, causing longer initial load times).

```json
{
  "Custom paths": [
  {
    "Folder label": "Work in progress!",
    "Folder path": "C:\\Users\\USER\\AppData\\LocalLow\\Super Spin Digital\\Spin Rhythm XD\\WIP"
  },
  {
    "Folder label": "SSSO Summer 2024",
    "Folder path": "D:\\SRXD Customs\\SSSO Summer 2024"
  }
],
  "Preloaded path": "Work in progress!"
}
```
### or

```json
{
  "Custom paths": [
  {
    "Folder label": "Work in progress!",
    "Folder path": "C:/Users/USER/AppData/LocalLow/Super Spin Digital/Spin Rhythm XD/WIP"
  },
  {
    "Folder label": "SSSO Summer 2024",
    "Folder path": "D:/SRXD Customs/SSSO Summer 2024"
  }
],
  "Preloaded path": "Work in progress!"
}
```

⚠️ If a folder specified in the file doesn't physically exist, the game will create it for you upon switching to it.

⚠️ If you have a custom_path launch argument set up in SRXD's Steam properties, it will be ignored in favor of the paths declared in the config file.

### In-game

All of Folder Switchers' functions can be found in the dedicated side panel.

Select as many charts as you'd like with the **Copy to clipboard** button, or by using the **Ctrl + C** shortcut, then press **Move selected** to choose the target directory.
Alternatively, use **Clear clipboard** to start the selection process over.

Additionally, a Refresh Folder button is included in case you need the game to reload an SRTB you're editing with an external program, or if a SpinShare download didn't register as a newly added chart. 

![image](https://i.imgur.com/ZFSwrOd.jpeg)

### ⚠️IMPORTANT

The process of moving charts involves the album art and audio file(s) as well, as long as the selected chart **SRTBs** aren't also present in the target directory.

Examples: 
- Copy FinalBoss.srtb to "WIP" folder -> FinalBoss.srtb exists in "WIP"? -> Cancel operation.
- Copy FinalBoss.srtb to "Showcase" folder -> FinalBoss.srtb does **NOT** exit in "Showcase"? -> SRTB and related custom files will be moved here.

## TODOs

- ~~Switching directories from within the track list menu through UI~~ ✅
- ~~Better UI (SpinCore integration)~~ ✅
- "Separation of concerns" mode: assign custom directories exclusively for SRTBs, images and audio files.
- Config file reloading and editing without closing the game (SpinCore integration)

## Dependencies:
- [SpinCore](https://github.com/Raoul1808/SpinCore/releases/tag/v1.0.0)
- [ChartHelper](https://github.com/SRXDModdingGroup/SRXDChartStatistics/releases/tag/ChartHelper-1.0.1.0)
- Newtonsoft.Json (included)
