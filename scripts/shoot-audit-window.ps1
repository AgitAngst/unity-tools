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
    @("lightmap-ru-dark", "ru", "dark", @("-shotTab", "1", "-shotScan")),
    @("lightmap-en-dark", "en", "dark", @("-shotTab", "1", "-shotScan")),
    @("picked-ru-dark", "ru", "dark", @("-shotExpand", "critical", "-shotPick")),
    @("lightmap-picked-ru-dark", "ru", "dark", @("-shotTab", "1", "-shotScan", "-shotPick")),
    @("budgets-ru-dark", "ru", "dark", @("-shotTab", "2")),
    @("profiler-en-dark", "en", "dark", @("-shotTab", "3")),
    @("history-ru-dark", "ru", "dark", @("-shotTab", "4", "-shotHistory")),
    @("history-en-dark", "en", "dark", @("-shotTab", "4", "-shotHistory"))
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
