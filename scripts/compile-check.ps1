# Compiles a utility's Editor sources against the DLLs of an installed Unity editor without opening Unity.
# Catches C# version and API mistakes fast; it is not a replacement for the import in Unity itself.
#
#   powershell -File scripts\compile-check.ps1 -Source SceneOptimizationAuditor\Editor
#   powershell -File scripts\compile-check.ps1 -Source X\Editor -UnityVersion 2019.4.41f2 -Log E:\logs\x.log
#
# Exit code: 0 - compiled without errors, 1 - errors (see the log), 2 - setup problem.
param(
    [Parameter(Mandatory = $true)][string]$Source,
    [string]$UnityVersion = "2019.4.41f2",
    [string]$UnityRoot = "C:\Program Files\Unity\Hub\Editor",
    [string]$LangVersion = "7.3",
    [string]$Log = "E:\dev_personal\unity-tools-scratch\reports\compile-check.log",
    [string]$Out = "E:\dev_personal\unity-tools-scratch\build"
)

$data = Join-Path $UnityRoot "$UnityVersion\Editor\Data"
$csc = Join-Path $data "Tools\Roslyn\csc.exe"
$api = Join-Path $data "MonoBleedingEdge\lib\mono\4.7.1-api"
if (-not (Test-Path $csc)) { Write-Output "csc not found: $csc"; exit 2 }
if (-not (Test-Path $Source)) { Write-Output "source not found: $Source"; exit 2 }

$refs = @()
$refs += Get-ChildItem (Join-Path $data "Managed\UnityEngine") -Filter *.dll | ForEach-Object { $_.FullName }
$refs += Join-Path $data "Managed\UnityEditor.dll"
foreach ($name in "mscorlib.dll", "System.dll", "System.Core.dll", "Facades\netstandard.dll") { $refs += Join-Path $api $name }

New-Item -ItemType Directory -Force $Out | Out-Null
New-Item -ItemType Directory -Force (Split-Path $Log) | Out-Null
$rsp = Join-Path $Out "compile-check.rsp"
$lines = @("-nologo", "-nostdlib+", "-target:library", "-langversion:$LangVersion", "-warn:4",
    "-define:UNITY_EDITOR;UNITY_2019_4", "-out:`"$(Join-Path $Out 'check.dll')`"")
foreach ($r in $refs) { $lines += "-r:`"$r`"" }
foreach ($f in Get-ChildItem $Source -Recurse -Filter *.cs) { $lines += "`"$($f.FullName)`"" }
Set-Content -Path $rsp -Value $lines -Encoding ASCII

& $csc -noconfig "@$rsp" *> $Log
$code = $LASTEXITCODE
$errors = (Select-String -Path $Log -Pattern ': error ' -SimpleMatch | Measure-Object).Count
$warnings = (Select-String -Path $Log -Pattern ': warning ' -SimpleMatch | Measure-Object).Count
Write-Output "csc exit=$code errors=$errors warnings=$warnings log=$Log"
Get-Content $Log -Tail 25
if ($code -ne 0) { exit 1 }
exit 0
