# Project Structure

## AudiologyUnityProject Folder Structure
```text
AudiologyUnityProject/
├── Assets/
│   ├── _Project/
│   │   ├── Features/
│   │   ├── Scenes/
│   │   ├── Settings/
│   │   ├── Shared/
│   │   └── Tests/
│   ├── PastTeam/
│   ├── Plugins/
│   ├── Resources/
│   ├── Samples/
│   ├── TextMesh Pro/
│   ├── ThirdParty/
│   ├── XR/
│   └── XRI/
├── Library/
├── Logs/
├── obj/
├── Packages/
├── ProjectSettings/
└── UserSettings/
```

### Folder Notes
- `Assets/_Project/`: Main folder for current team development.
- `Assets/_Project/Features/`: Contains feature directories. The structure of a
  feature folder is shown below.
- `Assets/_Project/Scenes/`: Contains main scenes used in final build.
- `Assets/_Project/Settings/`: Contains settings files.
- `Assets/_Project/Shared/`: Stores resources shared by multiple features.
- `Assets/_Project/Tests/`: Stores testing-related files.
- `Assets/PastTeam/`: Stores content developed by prior teams.
- `Assets/ThirdParty/`: Stores third-party content.

### Generated Folders
- `Library/`, `Logs/`, `obj/`, `Packages/`, `ProjectSettings/`, and
  `UserSettings/` are Unity generated folders. Avoid modifying them unless
  necessary.
- Inside of the `Assets/` folder, `Plugins/`, `Resources/`, `Samples/`, `TextMesh Pro/`,
  `XR/`, and `XRI` are package generated folders. Avoid modifying them unless
  necessary.

### Rules
- Avoid messing with Unity/package generated folders, unless necessary.
- Test scenes should not go in `Scenes/`. Each feature should have its own
  `TestScenes/` folder where test scenes will be placed. `Scenes/` is only for
  scenes that will be used in the final build.
- The point of a `Features/` folder is to keep assets relevant to the same
  feature close together. Avoid any mass unorganized Script or Scene folders.

## Features Folder Structure
```text
Features/
├── Feature1/
│   ├── Scripts/
│   ├── TestScenes/
│   └── Prefabs/
└── EarwaxSim/
    ├── Scripts/
    ├── TestScenes/
    └── Prefabs/
```

### Folder Notes
- `Features/Feature1/`: This describes the template of a feature folder. All
  content specific to the feature should be placed in here somewhere.
- `Features/Feature1/Scripts/`: Where feature specific scripts are stored.
- `Features/Feature1/TestScenes/`: Where test scenes for the feature are stored.
- `Features/Feature1/Prefabs/`: Where feature specific prefabs are stored.
- `Features/EarwaxSim/`: Directory for the softbody earwax sim feature.

### Rules
- Try to store the proper asset type in the properly named folder.
- You do not need to have all of these folders if you do not have an asset of
  the given type.
  - **Example:** If my feature uses no prefabs, I do not need a `Prefabs/`
    folder.
- You can add more folders to your feature folder as you see fit.
  - **Example:** If
  your feature uses a lot of scriptable objects, add a `ScriptableObjects/`
  folder.
- If your asset will be used by multiple features, add it to `_Project/Shared/`.