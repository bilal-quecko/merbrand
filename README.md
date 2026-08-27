# Mera Brand Pakistan Family Expo 2026 — Unity Digital Exhibition

Unity 6 project for the interactive 3D version of the Mera Brand Pakistan Family Expo at Tulip Hall Islamabad.

## Current Status

**Phase 1 — Project Foundation**

Implemented:

- Unity 6 project scaffold
- Universal Render Pipeline package
- Unity Input System package
- TextMeshPro package
- Core project folder architecture
- Persistent `AppManager`
- Async `SceneLoader`
- Boot flow
- Central `AppConfig`
- Three-scene architecture: `00_Boot`, `01_MainMenu`, `02_Exhibition`
- Editor setup utility that generates the scenes and Build Settings
- Exhibition scene root hierarchy ready for architecture, stalls, navigation, cameras, systems and UI
- `.gitignore` for Unity

## World Scale Convention

For layout construction:

**1 Unity unit = 1 foot**

This keeps dimensions from the exhibition plan easy to enter and verify.

Normal stall footprints currently defined in `AppConfig`:

- 3 ft × 3 ft
- 3 ft × 6 ft
- 6 ft × 6 ft

The physical stall model/prefab can later be replaced without changing the booking identity or cloud data.

## First-Time Setup

1. Clone/open this repository as a Unity project using **Unity 6**.
2. Allow Unity Package Manager to restore the packages.
3. If Unity prompts to import TMP Essential Resources, import them.
4. In Unity, run:

   `Mera Brand > Phase 1 > Generate Project Foundation`

5. The utility creates the required folders/assets/scenes and configures Build Settings in this order:

   - `00_Boot`
   - `01_MainMenu`
   - `02_Exhibition`

6. Open `Assets/_Project/Scenes/00_Boot.unity` and press Play to verify the boot flow.

## Project Structure

```text
Assets/_Project/
├── Art/
│   ├── Materials/
│   ├── Models/
│   └── Textures/
├── Audio/
├── Config/
├── Editor/
├── Prefabs/
│   ├── Architecture/
│   ├── Stalls/
│   └── UI/
├── Scenes/
├── Scripts/
│   ├── Authentication/
│   ├── Booking/
│   ├── Camera/
│   ├── Core/
│   ├── Network/
│   ├── Platform/
│   ├── Stalls/
│   └── UI/
└── UI/
    ├── Fonts/
    └── Sprites/
```

## Core Rules

- Use TextMeshPro for runtime text and button labels. Do not introduce legacy `UnityEngine.UI.Text`.
- Use the Unity Input System rather than the legacy Input Manager for gameplay/navigation controls.
- Keep the same exhibition scene for Visitor and Admin modes; access/permissions determine available functionality.
- Cloud booking information must remain separate from physical Unity geometry.
- Each stall will receive a permanent internal stall ID when Phase 2/3 stall placement begins.

## Next Phase

Phase 2 will construct the exhibition floor plan from the supplied reference layout using placeholder geometry, with the 3×3, 3×6 and 6×6 stall sizes as the normal stall modules.
