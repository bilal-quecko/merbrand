# WebGL exhibition loading

The WebGL player contains `00_Boot` and `01_MainMenu`. Its exhibition scene and
scene dependencies are built as remote Addressables content. The Windows/Editor
flow continues to use `Assets/_Project/Scenes/02_Exhibition.unity` directly.

## Build

1. In Unity 6000.2.2f1, wait for the Addressables package to finish importing.
2. Use **Mera Brand > Web > Build Production Web** (or **Build Development Web**).
3. The build command synchronizes a copy of `02_Exhibition` into
   `Assets/_Project/RemoteExhibition`, configures the remote Addressables group,
   builds its bundles, and builds the small WebGL player.
4. Upload the **entire** `Builds/WebGL` directory. Keep
   `RemoteContent/WebGL` beside `index.html` and `Build`:

   ```text
   index.html
   Build/
   StreamingAssets/
   RemoteContent/WebGL/*.bundle
   ```

The generated scene copy and its `.meta` file should be committed after the
first build, along with `Assets/AddressableAssetsData` and the package lock file.
Later Web builds refresh the copy from the original scene automatically.
Do not edit the copy directly.

The Web build uses `Assets/WebGLTemplates/MBP` for its branded page. On a phone
held in portrait, the page asks the visitor to rotate to landscape and waits
to start Unity until the device is sideways. After rebuilding, upload the new
`Builds/WebGL` output or recreate the upload ZIP; an older ZIP still contains
the older page.

The first production build was completed on 30 September 2026. Its initial
player files are 6.54 MiB (`.data.unityweb`), 11.59 MiB (`.wasm.unityweb`),
and 0.09 MiB (`.framework.js.unityweb`). The remote exhibition bundle is
133.15 MiB. The previous `Builds/WebGL` output was preserved as
`Builds/WebGL_PreRemote_20260930` locally. These sizes change as assets and
code change.

## Runtime

The menu starts an asynchronous predownload and shows progress. Entering the
exhibition waits for the same download if necessary, then loads the remote
scene. A failed download stays on the menu; choosing Visitor retries. Cached
bundles can be reused on later visits when the browser retains its cache.

The remote path is derived from the WebGL loader's `streamingAssetsUrl` through
`RemoteExhibitionContentUrl.BaseUrl`. Keep `RemoteContent` beside
`StreamingAssets`. If Unity is embedded in another webpage, set
`streamingAssetsUrl` to the deployed `StreamingAssets` URL; the remote bundle
path follows it. Host the player and bundles over HTTPS; if they use
different origins, configure CORS for the bundle host.

The catalog is embedded in the small initial player. Rebuild and redeploy both
the player and remote bundles together when exhibition content changes. Do not
publish only the new bundles with an old player.

## Verify before publishing

- In browser DevTools, confirm the initial `.data` download is smaller than
  the old 188 MiB file.
- Confirm `.bundle` requests begin after the menu appears.
- Test Enter while downloading, Enter after completion, a failed download,
  and a repeat visit with a warm browser cache.
- Check that all stalls, logos, lighting, and Supabase-backed data appear in
  the remote scene, then compare the latest build report to the old 199.4 MiB
  build. Asset duplication or missing scene dependencies need correction if
  the initial `.data` remains large or the remote scene looks incomplete.

The initial local browser test showed the menu, completed the background
download, and opened the remote visitor scene. Repeat the checks on the
production host, especially HTTPS, response headers, CORS if applicable,
stall data, and a fresh browser cache.
