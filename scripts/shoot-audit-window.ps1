# Screenshots of the SceneOptimizationAuditor window in the test project: one Unity launch per shot,
# the window is captured by window-shot.ps1 (PrintWindow), so nothing else on the screen gets in.
#
#   powershell -File scripts\shoot-audit-window.ps1
#   powershell -File scripts\shoot-audit-window.ps1 -Only problems-ru-dark
param(
    [string[]]$Only = @(),
    [string]$UnityVersion = "2019.4.41f2",
    [string]$Project = "E:\dev_personal\unity-tools-scratch\TestProject2019",
    [string]$Out = "E:\dev_personal\unity-tools-scratch\reports\shots"
)

$Only = @($Only | ForEach-Object { $_ -split "," } | Where-Object { $_ })
$unity = "C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe"
New-Item -ItemType Directory -Force $Out | Out-Null

# name, language, skin, extra arguments
$shots = @(
    @("empty-ru-dark", "ru", "dark", "-shotEmpty"),
    @("problems-ru-dark", "ru", "dark", "-shotExpand critical"),
    @("problems-en-dark", "en", "dark", "-shotExpand critical"),
    # Light skin: Unity 2019.4 has no API to switch skins from code, check it by hand.
    @("lightmap-ru-dark", "ru", "dark", "-shotTab 1 -shotScan"),
    @("lightmap-en-dark", "en", "dark", "-shotTab 1 -shotScan"),
    @("picked-ru-dark", "ru", "dark", "-shotExpand critical -shotPick"),
    @("lightmap-picked-ru-dark", "ru", "dark", "-shotTab 1 -shotScan -shotPick"),
    @("budgets-ru-dark", "ru", "dark", "-shotTab 2"),
    @("profiler-en-dark", "en", "dark", "-shotTab 3"),
    @("history-ru-dark", "ru", "dark", "-shotTab 4 -shotHistory"),
    @("history-en-dark", "en", "dark", "-shotTab 4 -shotHistory")
)

foreach ($shot in $shots) {
    $name = $shot[0]
    if ($Only.Count -gt 0 -and $Only -notcontains $name) { continue }
    $png = Join-Path $Out "$name.png"
    Remove-Item "$png.ready", "$png.done" -ErrorAction SilentlyContinue
    $argLine = "-projectPath `"$Project`" -logFile `"$Out\$name.log`" -executeMethod AuditTestsEditor.AuditTestShots.Shot " +
        "-shotOut `"$png`" -shotLang $($shot[1]) -shotSkin $($shot[2]) $($shot[3])"
    $p = Start-Process -FilePath $unity -ArgumentList $argLine -PassThru
    $deadline = (Get-Date).AddMinutes(5)
    while (-not (Test-Path "$png.ready") -and -not $p.HasExited -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 500 }
    if (Test-Path "$png.ready") {
        $title = Get-Content "$png.ready" -Raw -Encoding UTF8
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "window-shot.ps1") -Title $title.Trim() -Out $png
    }
    else { Write-Output "$name : window did not open" }
    Set-Content -Path "$png.done" -Value "done"
    $p.WaitForExit(120000) | Out-Null
    Remove-Item "$png.ready", "$png.done" -ErrorAction SilentlyContinue
}
