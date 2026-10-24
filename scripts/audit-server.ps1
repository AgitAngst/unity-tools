# The test server: one open Unity editor with the test project that runs test commands (AuditTestServer in
# tests\AuditTests\Editor), so tests and window shots do not start Unity for every step. Dot-source it:
#
#   . "$PSScriptRoot\audit-server.ps1"
#   Start-AuditServer                                   # batch editor for tests; an open one is reused
#   Start-AuditServer -Gui                              # visible editor for window shots
#   $code = Invoke-AuditCommand "AuditTestsEditor.AuditTests.Run" @("-auditVariant", "base")
#
# Tests need the batch editor: a visible one adds the OpenVR package and stops on modal dialogs when a variant
# turns on built-in VR. Asking for the other mode closes the open editor and starts one in that mode. The
# editor stays open after the run; Stop-AuditServer closes it.

$AuditServerDir = "E:\dev_personal\unity-tools-scratch\reports\server"
$AuditProject = "E:\dev_personal\unity-tools-scratch\TestProject2019"
$AuditUnity = "C:\Program Files\Unity\Hub\Editor\2019.4.41f2\Editor\Unity.exe"

# True while the editor that wrote <dir>\alive runs. Not the tick time: a step that changes scripting defines
# or VR settings recompiles scripts, and the editor does not tick for a while.
function Test-AuditServer {
    $alive = Join-Path $AuditServerDir "alive"
    if (-not (Test-Path $alive)) { return $false }
    $lines = @(Get-Content $alive -ErrorAction SilentlyContinue)
    if ($lines.Count -lt 1 -or $lines[0] -notmatch '^\d+$') { return $false }
    $process = Get-Process -Id ([int]$lines[0]) -ErrorAction SilentlyContinue
    return $null -ne $process -and $process.ProcessName -eq "Unity"
}

# Mode of the open editor: batch, gui or nothing.
function Get-AuditServerMode {
    if (-not (Test-AuditServer)) { return $null }
    $lines = @(Get-Content (Join-Path $AuditServerDir "alive") -ErrorAction SilentlyContinue)
    if ($lines.Count -lt 2) { return $null }
    return $lines[1]
}

function Stop-AuditServer {
    if (-not (Test-AuditServer)) { return }
    $id = [int]@(Get-Content (Join-Path $AuditServerDir "alive"))[0]
    Send-AuditCommand "AuditTestsEditor.AuditTestServer.Quit" | Out-Null
    $process = Get-Process -Id $id -ErrorAction SilentlyContinue
    if ($process -and -not $process.WaitForExit(120000)) { throw "The test server did not close in 2 minutes." }
    Get-ChildItem $AuditServerDir -Include *.cmd, *.result, alive -File -Recurse | Remove-Item -Force
}

function Start-AuditServer([switch]$Gui) {
    $mode = "batch"
    if ($Gui) { $mode = "gui" }
    $open = Get-AuditServerMode
    if ($open -eq $mode) { return }
    if ($open) { Stop-AuditServer }
    $repo = Split-Path $PSScriptRoot
    # The test project links the utility and the tests from the repository.
    foreach ($link in @(@("SceneOptimizationAuditor", "SceneOptimizationAuditor"), @("AuditTests", "tests\AuditTests"))) {
        $path = Join-Path $AuditProject "Assets\$($link[0])"
        if (-not (Test-Path $path)) { New-Item -ItemType Junction -Path $path -Target (Join-Path $repo $link[1]) | Out-Null }
    }
    $lock = Join-Path $AuditProject "Temp\UnityLockfile"
    if (Test-Path $lock) {
        try { [IO.File]::Open($lock, 'Open', 'ReadWrite', 'None').Close() }
        catch { throw "The test project is open in an editor without -auditServer: close it and run again." }
    }
    New-Item -ItemType Directory -Force $AuditServerDir | Out-Null
    Get-ChildItem $AuditServerDir -Include *.cmd, *.result, alive -File -Recurse | Remove-Item -Force
    $argLine = "-projectPath `"$AuditProject`" -logFile `"$AuditServerDir\editor.log`" -auditServer `"$AuditServerDir`""
    # No -nographics: an editor that stays open runs its frame loop, and offscreen cameras of the test scene
    # crash Unity without a graphics device.
    if (-not $Gui) { $argLine = "-batchmode " + $argLine }
    $process = Start-Process -FilePath $AuditUnity -ArgumentList $argLine -PassThru
    $deadline = (Get-Date).AddMinutes(10)
    while ((Get-AuditServerMode) -ne $mode) {
        if ($process.HasExited -or (Get-Date) -gt $deadline) { throw "The test server did not start: see $AuditServerDir\editor.log" }
        Start-Sleep -Seconds 2
    }
}

# Queues a command and returns its name; the editor runs commands one by one in name order.
function Send-AuditCommand([string]$Method, [string[]]$Arguments = @()) {
    $name = (Get-Date).ToString("yyyyMMdd-HHmmss-fff")
    $tmp = Join-Path $AuditServerDir "$name.tmp"
    Set-Content -Path $tmp -Value (@($Method) + $Arguments) -Encoding UTF8
    Rename-Item $tmp "$name.cmd"
    return $name
}

# Waits for the answer of a command: its exit code, or -1 when the editor is gone or the time is out.
function Wait-AuditResult([string]$Name, [int]$TimeoutMinutes = 10) {
    $result = Join-Path $AuditServerDir "$Name.result"
    $deadline = (Get-Date).AddMinutes($TimeoutMinutes)
    while (-not (Test-Path $result)) {
        if ((Get-Date) -gt $deadline -or -not (Test-AuditServer)) { return -1 }
        Start-Sleep -Milliseconds 300
    }
    $code = [int](Get-Content $result -Raw).Trim()
    Remove-Item $result -Force
    return $code
}

function Invoke-AuditCommand([string]$Method, [string[]]$Arguments = @(), [int]$TimeoutMinutes = 10) {
    return Wait-AuditResult (Send-AuditCommand $Method $Arguments) $TimeoutMinutes
}
