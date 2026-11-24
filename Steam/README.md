# Dawnwager for Steam

## Windows player

Open the Unity project, run the Campaign smoke from the title screen in Play Mode, then choose **Dawnwager → Build Windows Player** with Windows Build Support installed. Launch `Builds/Dawnwager-Windows/Dawnwager.exe` and play the first campaign battle on an actual Windows device. Ship the whole build directory, including `Dawnwager_Data`, `UnityPlayer.dll` and the credits.

## SteamPipe

Create or open the app in Steamworks and assign a Windows depot. Set the Windows executable in General Installation Settings to `Dawnwager.exe`. From the project folder run:

```powershell
.\Steam\Prepare-SteamPipe.ps1 -AppId YOUR_APP_ID -DepotId YOUR_DEPOT_ID
```

Replace the two placeholder values with your app and depot IDs. The script writes a preview configuration in `Steam/Generated`. In SteamCMD, sign in with your Steam build account and run `run_app_build` with the full path to the generated app VDF. Use `-ForUpload` to generate the upload configuration after checking the preview manifest. The script neither signs in nor uploads nor sets a build live.

## Store

The `Capsules` folder contains store and library artwork and icons. Capture store screenshots from the verified Windows player at 1920×1080 or higher. Complete the Steam store and build review in your Steamworks account. See the [SteamPipe guide](https://partner.steamgames.com/doc/sdk/uploading) and [graphical asset sizes](https://partner.steamgames.com/doc/store/assets).

## Early Access

See [EA-ROADMAP.md](EA-ROADMAP.md) for Day-0 vs Milestone 1 (live matchmaking) vs Milestone 2.

Suggested Steam achievements (wire to `profile.unlockedAchievements` IDs):

| API name | Unlock id |
|---|---|
| TUTORIAL_CLEAR | `tutorial_clear` |
| FIRST_SEAL | `first_seal` |
| FIRST_CHAPTER | `first_chapter` |
| ALL_HEROES | `all_heroes` |
| DAILY_PRACTICE | `daily_win_practice` |
| COSMETIC_BACK | `cosmetic_back` |

Rich presence strings used by the client roadmap: `In queue · Ranked`, `Beacon Ladder · {rank}`, `In battle · Round {n}`.
