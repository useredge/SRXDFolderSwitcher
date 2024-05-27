# Folder Switcher
### Installation
1. Place SRXDFolderSwitcher.dll, ChartHelper.Parsing.dll and Newtonsoft.Json.dll in **Spin Rhythm/BepInEx/plugins**.
2. Run the game with the mod installed once. This will create a JSON configuration file in **Spin Rhythm/BepInEx/config**.
3. Close the game, edit the configuration file to your liking.
4. Restart.

## Usage instructions
### Setting up the configuration file
Remember to follow the **JSON format** when adding custom paths, as well as properly **escaping the backslash characters** in the path strings.

```json
[
  {
    "Folder label": "SSSO charts",
    "Folder path": "C:/Users/USER/AppData/LocalLow/Super Spin Digital/Spin Rhythm XD/SSSO"
  },
  {
    "Folder label": "Work In Progress",
    "Folder path": "C:/Users/USER/AppData/LocalLow/Super Spin Digital/Spin Rhythm XD/WIP"
  }
]
```
### or

```json
[
  {
    "Folder label": "SSSO charts",
    "Folder path": "C:\\Users\\USER\\AppData\\LocalLow\\Super Spin Digital\\Spin Rhythm XD\\SSSO"
  },
  {
    "Folder label": "Work In Progress",
    "Folder path": "C:\\Users\\USER\\AppData\\LocalLow\\Super Spin Digital\\Spin Rhythm XD\\WIP"
  }
]
```

⚠️ If a folder specified in the file doesn't physically exist, the game will create it for you upon switching to it.

⚠️ If you have a custom_path launch argument set up in SRXD's Steam properties, it will be ignored in favor of the paths declared in the config file.

### In-game
To cycle between folders, press **F1** in the main menu.

![image](https://i.imgur.com/SamKcvl.jpeg)

In the **Customs** side panel, you'll find some new UI to help you select and move charts across your custom directories.

![image](https://i.imgur.com/SXpRy0I.jpeg)

Select as many charts as you'd like with the **Copy to clipboard** button, or by using the **Ctrl + C** shortcut, then press **Move selected** to choose the target directory.
Alternatively, use **Clear clipboard** to start the selection process over.

![image](https://i.imgur.com/y2gbdvE.png) 

### ⚠️IMPORTANT

The process of moving charts involves the album art and audio file(s) as well, as long as the selected chart **SRTBs** aren't also present in the target directory.

Examples: 
- Copy FinalBoss.srtb to "WIP" folder -> FinalBoss.srtb exists in "WIP"? -> Cancel operation.
- Copy FinalBoss.srtb to "Showcase" folder -> FinalBoss.srtb does **NOT** exit in "Showcase"? -> SRTB and related custom files will be moved here.

## TODOs

- Switching directories from within the track list menu through UI
- "Separation of concerns" mode: assign custom directories exclusively for SRTBs, images and audio files.
- Better UI (SpinCore integration)
- Fix custom UI occasionally not appearing when opening the side panel for the first time
- Config file reloading without closing the game

## Dependencies:
- [ChartHelper](https://github.com/SRXDModdingGroup/SRXDChartStatistics/releases/tag/ChartHelper-1.0.1.0)
- Newtonsoft.Json (included)
