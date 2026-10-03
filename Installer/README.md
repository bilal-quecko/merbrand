# Windows installer

The installer configuration is ready. No Unity build or installer has been generated as part of this setup.

## Included settings

- Application: **Mera Brand Pakistan Simulation**, version **1.0**.
- Publisher: **BnM Technologies**.
- Windows x64 application, installed for the current Windows user without administrator elevation.
- Installation folder: `%LOCALAPPDATA%\Programs\MeraBrandPakistanSimulation`.
- Start Menu shortcut and a desktop shortcut selected by default.
- Uninstall support and a stable application ID for future upgrades.
- The complete Windows build is included, excluding debug symbols and Unity folders marked not to ship.
- Unity saved data and remembered login credentials are preserved. Use the application's logout action to forget a saved login.

## When you authorize packaging

1. Make a fresh Windows desktop build using **Mera Brand > Desktop > Build Windows Desktop** in Unity. The existing build predates recent project changes; packaging it would ship those older files.
2. Check that `Builds/Windows` contains `MeraBrandPakistan.exe`, `MeraBrandPakistan_Data`, `UnityPlayer.dll`, and all other folders and files Unity generated. Launch the application and check login, booking, logo upload, and navigation.
3. Install **Inno Setup 6.3 or newer** from https://jrsoftware.org/isdl.php. The compiler was not found in the usual installation folders during preparation.
4. Open `Installer/MeraBrandPakistan.iss` in Inno Setup and click **Compile**.
5. The installer will be written to `Builds/Installer/MeraBrandPakistan-Setup-1.0.exe`.
6. Test installation, shortcuts, launch, upgrade, and uninstall on a separate Windows account or test computer before distribution.

## Future releases

Update `AppVersion`, `VersionInfoVersion`, and the Unity application version together. Keep `AppId` and the installation folder unchanged. Rebuild the app before compiling each installer.

The installer uses the application's existing icon. A custom installer icon, license agreement, and certificate for signing can be added later. Signing is not configured in this script.

Do not place administrator passwords, Supabase service-role keys, or other private credentials in the build folder: that entire folder is distributed to users.
