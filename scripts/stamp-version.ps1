# Writes the version and the build number into SceneOptimizationAuditor/Editor/ToolVersion.cs and the
# "Версия программы" line of docs/README.md. Build = commit count after the next commit, so run it right
# before committing.
#
#   powershell -File scripts\stamp-version.ps1                  # keep X.Y.Z, refresh the build number
#   powershell -File scripts\stamp-version.ps1 -Version 0.7.0   # new X.Y.Z
param([string]$Version = "")

$repo = Split-Path $PSScriptRoot
$file = Join-Path $repo "SceneOptimizationAuditor\Editor\ToolVersion.cs"
$text = [IO.File]::ReadAllText($file)
if ($Version -eq "") { $Version = [regex]::Match($text, 'Version = "([^"]+)"').Groups[1].Value }
$build = [int](git -C $repo rev-list --count HEAD) + 1
$text = [regex]::Replace($text, 'Version = "[^"]+"', "Version = `"$Version`"")
$text = [regex]::Replace($text, 'Build = \d+', "Build = $build")
[IO.File]::WriteAllText($file, $text, (New-Object Text.UTF8Encoding $false))

$readme = Join-Path $repo "docs\README.md"
$doc = [IO.File]::ReadAllText($readme, [Text.Encoding]::UTF8)
$doc = [regex]::Replace($doc, '(?m)^(\S+ \S+: )\d+\.\d+\.\d+$', "`${1}$Version")
[IO.File]::WriteAllText($readme, $doc, (New-Object Text.UTF8Encoding $false))
Write-Output "version $Version.$build"
