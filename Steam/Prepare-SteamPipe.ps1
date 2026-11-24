param(
  [Parameter(Mandatory=$true)][ValidatePattern('^[1-9][0-9]*$')][string]$AppId,
  [Parameter(Mandatory=$true)][ValidatePattern('^[1-9][0-9]*$')][string]$DepotId,
  [switch]$ForUpload
)
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$player = Join-Path $project 'Builds/Dawnwager-Windows'
foreach ($item in @('Dawnwager.exe','UnityPlayer.dll','Dawnwager_Data')) {
  if (!(Test-Path (Join-Path $player $item))) {
    throw "Build the Windows player first. Missing $item in Builds/Dawnwager-Windows."
  }
}
$target = Join-Path $PSScriptRoot 'Generated'
$null = New-Item -ItemType Directory -Force $target
$preview = if ($ForUpload) { '0' } else { '1' }

@"
"AppBuild"
{
  "AppID" "$AppId"
  "Desc" "Dawnwager Unity 0.6 Windows"
  "ContentRoot" "../../Builds/Dawnwager-Windows"
  "BuildOutput" "../BuildOutput"
  "Preview" "$preview"
  "Depots"
  {
    "$DepotId" "depot_build_$DepotId.vdf"
  }
}
"@ | Set-Content -Encoding Ascii (Join-Path $target "app_build_$AppId.vdf")

@"
"DepotBuild"
{
  "DepotID" "$DepotId"
  "FileMapping"
  {
    "LocalPath" "*"
    "DepotPath" "."
    "Recursive" "1"
  }
  "FileExclusion" "*.pdb"
  "FileExclusion" "*_BurstDebugInformation_DoNotShip*"
}
"@ | Set-Content -Encoding Ascii (Join-Path $target "depot_build_$DepotId.vdf")

Write-Host "SteamPipe configuration saved to $target (Preview=$preview)."
Write-Host "Set the Windows launch executable in Steamworks to Dawnwager.exe."
