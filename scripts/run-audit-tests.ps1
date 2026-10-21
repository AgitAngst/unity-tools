# Runs the SceneOptimizationAuditor tests (tests/AuditTests) in the Unity 2019.4 test project, one Unity
# launch per step. Reports: unity-tools-scratch\reports\audit-tests\<step>.txt, Unity logs next to them.
#
#   powershell -File scripts\run-audit-tests.ps1                 # everything
#   powershell -File scripts\run-audit-tests.ps1 -Only base,quality
#
# Exit code: 0 - every step passed, 1 - some step failed.
param(
    [string[]]$Only = @(),
    [string]$UnityVersion = "2019.4.41f2",
    [string]$Project = "E:\dev_personal\unity-tools-scratch\TestProject2019",
    [string]$Reports = "E:\dev_personal\unity-tools-scratch\reports\audit-tests"
)

# -File passes "a,b" as one string: split it here.
$Only = @($Only | ForEach-Object { $_ -split "," } | Where-Object { $_ })
$unity = "C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe"
$repo = Split-Path $PSScriptRoot
New-Item -ItemType Directory -Force $Reports | Out-Null

# The test project links the utility and the tests from the repository.
foreach ($link in @(@("SceneOptimizationAuditor", "SceneOptimizationAuditor"), @("AuditTests", "tests\AuditTests"))) {
    $path = Join-Path $Project "Assets\$($link[0])"
    if (-not (Test-Path $path)) { New-Item -ItemType Junction -Path $path -Target (Join-Path $repo $link[1]) | Out-Null }
}

$scene = "Assets/AuditGenerated/Scenes/AuditSeed.unity"
$steps = @(
    @("base", "AuditTestsEditor.AuditTests.Run", "-auditVariant base"),
    @("legacy-multipass", "AuditTestsEditor.AuditTests.Run", "-auditVariant legacy-multipass"),
    @("legacy-spi", "AuditTestsEditor.AuditTests.Run", "-auditVariant legacy-spi"),
    @("legacy-no-openvr", "AuditTestsEditor.AuditTests.Run", "-auditVariant legacy-no-openvr"),
    @("quality", "AuditTestsEditor.AuditTests.Run", "-auditVariant quality"),
    @("project", "AuditTestsEditor.AuditTests.Run", "-auditVariant project"),
    @("fixed-step-90", "AuditTestsEditor.AuditTests.Run", "-auditVariant fixed-step-90"),
    @("budgets-low", "AuditTestsEditor.AuditTests.Run", "-auditVariant budgets-low"),
    @("counts", "AuditTestsEditor.AuditTests.Run", "-auditVariant counts"),
    @("no-listener", "AuditTestsEditor.AuditTests.Run", "-auditVariant no-listener"),
    @("xr-openvr-spi", "AuditTestsEditor.AuditTests.Run", "-auditVariant xr-openvr-spi"),
    @("xr-openvr-multipass", "AuditTestsEditor.AuditTests.Run", "-auditVariant xr-openvr-multipass"),
    @("xr-mock", "AuditTestsEditor.AuditTests.Run", "-auditVariant xr-mock"),
    @("xr-both", "AuditTestsEditor.AuditTests.Run", "-auditVariant xr-both"),
    @("export-unit", "AuditTestsEditor.AuditTests.RunExportUnit", ""),
    @("cli-json", "UnityTools.SceneOptimization.ReportExporter.RunFromCommandLine", "-auditScene $scene -auditReport $Reports\cli.json"),
    @("cli-csv", "UnityTools.SceneOptimization.ReportExporter.RunFromCommandLine", "-auditScene $scene -auditReport $Reports\cli.csv"),
    @("cli-ru", "UnityTools.SceneOptimization.ReportExporter.RunFromCommandLine", "-auditScene $scene -auditReport $Reports\cli-ru.json -auditLang ru"),
    @("cli-no-args", "UnityTools.SceneOptimization.ReportExporter.RunFromCommandLine", "", 1),
    @("cli-bad-scene", "UnityTools.SceneOptimization.ReportExporter.RunFromCommandLine", "-auditScene Assets/Nope.unity -auditReport $Reports\nope.json", 1),
    @("fixes", "AuditTestsEditor.AuditTests.RunFixes", ""),
    @("steamvr-on", "AuditTestsEditor.AuditTests.EnableSteamVrStub", ""),
    @("steamvr-not-in-scene", "AuditTestsEditor.AuditTests.Run", "-auditVariant steamvr-not-in-scene"),
    @("steamvr-in-scene", "AuditTestsEditor.AuditTests.Run", "-auditVariant steamvr-in-scene"),
    @("steamvr-off", "AuditTestsEditor.AuditTests.DisableSteamVrStub", "")
)

$failed = 0
foreach ($step in $steps) {
    $name = $step[0]
    if ($Only.Count -gt 0 -and $Only -notcontains $name) { continue }
    $expected = 0
    if ($step.Count -gt 3) { $expected = $step[3] }
    $log = Join-Path $Reports "$name.log"
    $argLine = "-batchmode -nographics -quit -projectPath `"$Project`" -logFile `"$log`" -executeMethod $($step[1]) $($step[2])"
    $started = Get-Date
    $p = Start-Process -FilePath $unity -ArgumentList $argLine -Wait -PassThru -NoNewWindow
    $seconds = [int]((Get-Date) - $started).TotalSeconds
    $ok = $p.ExitCode -eq $expected
    if (-not $ok) { $failed++ }
    $mark = "FAIL"
    if ($ok) { $mark = "ok" }
    Write-Output ("{0,-22} exit={1} expected={2} {3,4}s {4}" -f $name, $p.ExitCode, $expected, $seconds, $mark)
    if (-not $ok) {
        $report = Join-Path $Reports "$name.txt"
        if (Test-Path $report) { Get-Content $report | Where-Object { $_ -match '^(FAIL|CRASH)' } | Select-Object -First 15 }
        Select-String -Path $log -Pattern 'error CS|Exception|\[AuditTests\]|\[SceneAudit\]' | Select-Object -First 10 | ForEach-Object { "  log: " + $_.Line }
    }
}
Write-Output "failed steps: $failed"
if ($failed -gt 0) { exit 1 }
exit 0
