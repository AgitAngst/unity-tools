# Runs the SceneOptimizationAuditor tests (tests/AuditTests) in the Unity 2019.4 test project. All steps go to
# one open editor, the test server (scripts\audit-server.ps1): it is started once and stays open. Reports:
# unity-tools-scratch\reports\audit-tests\<step>.txt, the editor log: unity-tools-scratch\reports\server\editor.log.
#
#   powershell -File scripts\run-audit-tests.ps1                 # everything
#   powershell -File scripts\run-audit-tests.ps1 -Only base,quality
#
# Exit code: 0 - every step passed, 1 - some step failed.
param(
    [string[]]$Only = @(),
    [string]$Reports = "E:\dev_personal\unity-tools-scratch\reports\audit-tests"
)

. (Join-Path $PSScriptRoot "audit-server.ps1")

# -File passes "a,b" as one string: split it here.
$Only = @($Only | ForEach-Object { $_ -split "," } | Where-Object { $_ })
New-Item -ItemType Directory -Force $Reports | Out-Null
Start-AuditServer

$scene = "Assets/AuditGenerated/Scenes/AuditSeed.unity"
$cli = "AuditTestsEditor.AuditTests.RunCli"
# name, method, arguments, expected exit code (0 when left out)
$steps = @(
    @("base", "AuditTestsEditor.AuditTests.Run", @("-auditVariant", "base")),
    @("legacy-multipass", "AuditTestsEditor.AuditTests.Run", @("-auditVariant", "legacy-multipass")),
    @("legacy-spi", "AuditTestsEditor.AuditTests.Run", @("-auditVariant", "legacy-spi")),
    @("legacy-no-openvr", "AuditTestsEditor.AuditTests.Run", @("-auditVariant", "legacy-no-openvr")),
    @("quality", "AuditTestsEditor.AuditTests.Run", @("-auditVariant", "quality")),
    @("project", "AuditTestsEditor.AuditTests.Run", @("-auditVariant", "project")),
    @("fixed-step-90", "AuditTestsEditor.AuditTests.Run", @("-auditVariant", "fixed-step-90")),
    @("budgets-low", "AuditTestsEditor.AuditTests.Run", @("-auditVariant", "budgets-low")),
    @("counts", "AuditTestsEditor.AuditTests.Run", @("-auditVariant", "counts")),
    @("no-listener", "AuditTestsEditor.AuditTests.Run", @("-auditVariant", "no-listener")),
    @("xr-openvr-spi", "AuditTestsEditor.AuditTests.Run", @("-auditVariant", "xr-openvr-spi")),
    @("xr-openvr-multipass", "AuditTestsEditor.AuditTests.Run", @("-auditVariant", "xr-openvr-multipass")),
    @("xr-mock", "AuditTestsEditor.AuditTests.Run", @("-auditVariant", "xr-mock")),
    @("xr-both", "AuditTestsEditor.AuditTests.Run", @("-auditVariant", "xr-both")),
    @("export-unit", "AuditTestsEditor.AuditTests.RunExportUnit", @()),
    @("cli-json", $cli, @("-auditScene", $scene, "-auditReport", "$Reports\cli.json")),
    @("cli-csv", $cli, @("-auditScene", $scene, "-auditReport", "$Reports\cli.csv")),
    @("cli-ru", $cli, @("-auditScene", $scene, "-auditReport", "$Reports\cli-ru.json", "-auditLang", "ru")),
    @("cli-no-args", $cli, @(), 1),
    @("cli-bad-scene", $cli, @("-auditScene", "Assets/Nope.unity", "-auditReport", "$Reports\nope.json"), 1),
    @("fixes", "AuditTestsEditor.AuditTests.RunFixes", @()),
    @("lightmap-report", "AuditTestsEditor.AuditTests.RunLightmapReport", @()),
    @("multi-select", "AuditTestsEditor.AuditTests.RunMultiSelect", @()),
    @("accepted", "AuditTestsEditor.AuditTests.RunAccepted", @()),
    @("history", "AuditTestsEditor.AuditTests.RunHistory", @()),
    @("gpu", "AuditTestsEditor.AuditTestViewpoints.RunGpu", @()),
    @("viewpoints", "AuditTestsEditor.AuditTestViewpoints.RunViewpoints", @()),
    @("viewpoints-tour", "AuditTestsEditor.AuditTestViewpoints.RunTour", @()),
    # Starts a short tour in Play Mode; the next command waits for Play Mode to end and checks what it measured.
    @("viewpoints-tour-start", "AuditTestsEditor.AuditTestViewpoints.RunTourStart", @()),
    @("viewpoints-tour-check", "AuditTestsEditor.AuditTestViewpoints.RunTourCheck", @()),
    @("steamvr-on", "AuditTestsEditor.AuditTests.EnableSteamVrStub", @()),
    @("viewpoints-teleports", "AuditTestsEditor.AuditTestViewpoints.RunTeleports", @()),
    @("steamvr-not-in-scene", "AuditTestsEditor.AuditTests.Run", @("-auditVariant", "steamvr-not-in-scene")),
    @("steamvr-in-scene", "AuditTestsEditor.AuditTests.Run", @("-auditVariant", "steamvr-in-scene")),
    @("steamvr-off", "AuditTestsEditor.AuditTests.DisableSteamVrStub", @())
)

$log = Join-Path $AuditServerDir "editor.log"
$failed = 0
foreach ($step in $steps) {
    $name = $step[0]
    if ($Only.Count -gt 0 -and $Only -notcontains $name) { continue }
    $expected = 0
    if ($step.Count -gt 3) { $expected = $step[3] }
    $logStart = 0
    if (Test-Path $log) { $logStart = (Get-Content $log).Count }
    $started = Get-Date
    $code = Invoke-AuditCommand $step[1] $step[2]
    $seconds = [int]((Get-Date) - $started).TotalSeconds
    $ok = $code -eq $expected
    if (-not $ok) { $failed++ }
    $mark = "FAIL"
    if ($ok) { $mark = "ok" }
    Write-Output ("{0,-22} exit={1} expected={2} {3,4}s {4}" -f $name, $code, $expected, $seconds, $mark)
    if (-not $ok) {
        $report = Join-Path $Reports "$name.txt"
        if (Test-Path $report) { Get-Content $report | Where-Object { $_ -match '^(FAIL|CRASH)' } | Select-Object -First 15 }
        Get-Content $log | Select-Object -Skip $logStart | Select-String -Pattern 'error CS|Exception|\[AuditTests\]|\[SceneAudit\]' |
            Select-Object -First 10 | ForEach-Object { "  log: " + $_.Line }
    }
    if ($code -eq -1) { Write-Output "the test server is gone or did not answer: see $log"; break }
}
Write-Output "failed steps: $failed"
if ($failed -gt 0) { exit 1 }
exit 0
