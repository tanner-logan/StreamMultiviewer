# Stream Multiviewer

A Windows WPF app for viewing 2, 4, or 6 streams at once, with General and Twitch modes.

## Install

Download `StreamMultiviewer-Setup.exe` from the project's GitHub **Releases** page and run it. The installer creates a Start menu shortcut and can optionally create a desktop shortcut. It installs for the current Windows user without requiring administrator access.

The installer also installs the Microsoft Edge WebView2 Runtime if needed. An internet connection is required for that runtime installation. The app is packaged for 64-bit Windows.

To publish a release after pushing the repository to GitHub, create and push a version tag:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

GitHub Actions builds the self-contained app, creates the installer, and attaches it to a GitHub Release for that tag.

## Run from source

Open `StreamMultiviewer.csproj` in Visual Studio, or run:

```powershell
dotnet run
```

## Use

1. Choose **General** for general streams or **Twitch** to sign in first.
2. In Twitch mode, sign in through the in-app Twitch page, then choose **Continue to Twitch setup**.
3. Choose 2, 4, or 6 windows and enter one `http` or `https` URL for each stream.
4. Select **Start Full-Screen View**.
5. Press **Escape** to return to the URL form.

Two windows are centered side by side using 16:9 tiles. Four windows use a 2-by-2 grid, and six use a 3-by-2 grid. General mode searches for `video[data-html5-video]`, then falls back to the first HTML5 `<video>` element. Twitch mode uses a separate layout path that keeps Twitch's player controls with the video.

Twitch mode stores its sign-in session in a persistent WebView2 profile under the current user's local application data. This profile is shared by the Twitch login page and all Twitch stream panes, but it does not reuse Edge or Chrome cookies. To sign out, use Twitch's sign-out controls in the in-app login page.

Playback still depends on the provider supporting its stream page in WebView2. Sites that require unsupported playback features or page controls may not work as expected.
