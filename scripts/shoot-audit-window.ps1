# Screenshots of the SceneOptimizationAuditor window in the test project. Every shot goes to the open test
# server (scripts\audit-server.ps1), which opens the window; window-shot.ps1 captures it (PrintWindow), so
# nothing else on the screen gets in. The editor stays open after the run.
#
#   powershell -File scripts\shoot-audit-window.ps1
#   powershell -File scripts\shoot-audit-window.ps1 -Only problems-ru-dark
param(
    [string[]]$Only = @(),
    [string]$Out = "E:\dev_personal\unity-tools-scratch\reports\shots"
)

. (Join-Path $PSScriptRoot "audit-server.ps1")

$Only = @($Only | ForEach-Object { $_ -split "," } | Where-Object { $_ })
New-Item -ItemType Directory -Force $Out | Out-Null
Start-AuditServer -Gui

# name, language, skin, extra arguments
$shots = @(
    @("empty-ru-dark", "ru", "dark", @("-shotEmpty")),
    @("problems-ru-dark", "ru", "dark", @("-shotExpand", "critical")),
    @("problems-en-dark", "en", "dark", @("-shotExpand", "critical")),
    # Light skin: Unity 2019.4 has no API to switch skins from code, check it by hand.
    @("viewpoints-empty-ru-dark", "ru", "dark", @("-shotTab", "1", "-shotViewpointsEmpty")),
    @("viewpoints-ru-dark", "ru", "dark", @("-shotTab", "1", "-shotViewpoints")),
    @("viewpoints-en-dark", "en", "dark", @("-shotTab", "1", "-shotViewpoints")),
    @("viewpoints-narrow-ru-dark", "ru", "dark", @("-shotTab", "1", "-shotViewpoints", "-shotWidth", "560")),
    # The tour on made-up numbers: with and without the headset, wide and narrow.
    @("tour-ru-dark", "ru", "dark", @("-shotTab", "1", "-shotViewpoints", "-shotTour", "headset")),
    @("tour-en-dark", "en", "dark", @("-shotTab", "1", "-shotViewpoints", "-shotTour", "headset")),
    @("tour-noheadset-ru-dark", "ru", "dark", @("-shotTab", "1", "-shotViewpoints", "-shotTour", "noheadset")),
    @("tour-narrow-ru-dark", "ru", "dark", @("-shotTab", "1", "-shotViewpoints", "-shotTour", "headset", "-shotWidth", "560")),
    @("lightmap-ru-dark", "ru", "dark", @("-shotTab", "2", "-shotScan")),
    @("lightmap-en-dark", "en", "dark", @("-shotTab", "2", "-shotScan")),
    @("picked-ru-dark", "ru", "dark", @("-shotExpand", "critical", "-shotPick")),
    @("lightmap-picked-ru-dark", "ru", "dark", @("-shotTab", "2", "-shotScan", "-shotPick")),
    # The Lighting tab on made-up lightmaps: every segment, narrow, and before the first check.
    @("light-empty-ru-dark", "ru", "dark", @("-shotTab", "3")),
    @("light-atlases-ru-dark", "ru", "dark", @("-shotTab", "3", "-shotLight", "atlases")),
    @("light-objects-ru-dark", "ru", "dark", @("-shotTab", "3", "-shotLight", "objects")),
    @("light-lights-en-dark", "en", "dark", @("-shotTab", "3", "-shotLight", "lights")),
    @("light-probes-ru-dark", "ru", "dark", @("-shotTab", "3", "-shotLight", "probes")),
    @("light-narrow-ru-dark", "ru", "dark", @("-shotTab", "3", "-shotLight", "objects", "-shotWidth", "560")),
    @("light-refl-ru-dark", "ru", "dark", @("-shotTab", "3", "-shotLight", "reflections")),
    @("light-refl-en-dark", "en", "dark", @("-shotTab", "3", "-shotLight", "reflections")),
    @("light-refl-narrow-ru-dark", "ru", "dark", @("-shotTab", "3", "-shotLight", "reflections", "-shotWidth", "560")),
    # Copies of a prefab as one row, and the Heaviest tab on forty houses.
    @("copies-ru-dark", "ru", "dark", @("-shotCopies")),
    @("copies-en-dark", "en", "dark", @("-shotCopies")),
    @("heavy-empty-ru-dark", "ru", "dark", @("-shotTab", "4")),
    @("heavy-ru-dark", "ru", "dark", @("-shotTab", "4", "-shotHeavy", "tris")),
    @("heavy-en-dark", "en", "dark", @("-shotTab", "4", "-shotHeavy", "dc")),
    @("heavy-narrow-ru-dark", "ru", "dark", @("-shotTab", "4", "-shotHeavy", "tex", "-shotWidth", "560")),
    # Scopes: the build scenes one by one, a folder of assets, a selection.
    @("scope-build-ru-dark", "ru", "dark", @("-shotScope", "build", "-shotExpand", "all")),
    @("scope-build-en-dark", "en", "dark", @("-shotScope", "build", "-shotExpand", "all")),
    @("scope-folder-ru-dark", "ru", "dark", @("-shotScope", "folder", "-shotExpand", "all")),
    @("scope-selection-ru-dark", "ru", "dark", @("-shotScope", "selection", "-shotExpand", "all")),
    @("scope-narrow-ru-dark", "ru", "dark", @("-shotScope", "build", "-shotWidth", "560")),
    @("budgets-ru-dark", "ru", "dark", @("-shotTab", "5")),
    @("budgets-en-dark", "en", "dark", @("-shotTab", "5")),
    @("profiler-en-dark", "en", "dark", @("-shotTab", "6")),
    @("history-ru-dark", "ru", "dark", @("-shotTab", "7", "-shotHistory")),
    @("history-en-dark", "en", "dark", @("-shotTab", "7", "-shotHistory"))
)

foreach ($shot in $shots) {
    $name = $shot[0]
    if ($Only.Count -gt 0 -and $Only -notcontains $name) { continue }
    $png = Join-Path $Out "$name.png"
    Remove-Item "$png.ready", "$png.done" -ErrorAction SilentlyContinue
    $command = Send-AuditCommand "AuditTestsEditor.AuditTestShots.Shot" (@("-shotOut", $png, "-shotLang", $shot[1], "-shotSkin", $shot[2]) + $shot[3])
    $deadline = (Get-Date).AddMinutes(5)
    while (-not (Test-Path "$png.ready") -and (Test-AuditServer) -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 500 }
    if (Test-Path "$png.ready") {
        $title = Get-Content "$png.ready" -Raw -Encoding UTF8
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "window-shot.ps1") -Title $title.Trim() -Out $png
    }
    else { Write-Output "$name : window did not open" }
    Set-Content -Path "$png.done" -Value "done"
    $code = Wait-AuditResult $command 3
    if ($code -ne 0) { Write-Output "$name : shot command answered $code" }
    Remove-Item "$png.ready", "$png.done" -ErrorAction SilentlyContinue
}
