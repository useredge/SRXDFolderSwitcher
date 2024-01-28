# Folder Switcher
### Installation
1. Place both SRXDFolderSwitcher.dll and Newtonsoft.Json.dll in **Spin Rhythm/BepInEx/plugins**.
2. Run the game with the mod installed once. This will create a JSON configuration file in **Spin Rhythm/BepInEx/config**.
3. Close the game, edit the configuration file to your liking.
4. Restart.

### Usage instructions
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
##### Note: if you have a custom_path launch argument set up in SRXD's Steam properties, it will be ignored in favor of the paths declared in the config file.
