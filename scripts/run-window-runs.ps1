# The SceneOptimizationAuditor window in the visible test editor (tests/AuditTests/Editor/AuditTestWindowRuns.cs):
# runs of every scope on heavy scenes while the window repaints, all groups and help cards open, by category, narrow,
# and styles made again as after a script reload. Any exception logged while it draws fails the run.
#
#   powershell -File scripts\run-window-runs.ps1
param(
    [string]$Reports = "E:\dev_personal\unity-tools-scratch\reports\audit-tests"
)

. (Join-Path $PSScriptRoot "audit-server.ps1")
Start-AuditServer -Gui
$code = Invoke-AuditCommand "AuditTestsEditor.AuditTestWindowRuns.Run" @() 20
Get-Content (Join-Path $Reports "window-runs.txt") | Select-Object -First 40
Write-Host "window-runs exit=$code"
exit $code
