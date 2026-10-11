# Checks the inline documentation of the EvoSim code (Docs/Site/extending/documenting-your-code.md).
#
#   powershell -File Docs\Site\tools\audit.ps1                                   # every assembly
#   powershell -File Docs\Site\tools\audit.ps1 --only Runtime/Senses,Runtime/Actions --max 100
#   powershell -File Docs\Site\tools\audit.ps1 --same-code HEAD                  # only comments changed since HEAD?
#
# Needs the .NET 8 SDK: a user-local one under %LOCALAPPDATA%\EvoSimDocs\dotnet (Docs/Site/README.md), or any on the PATH.
# Exit code 0: nothing to report; 1: issues; 2: the code does not compile.
$ErrorActionPreference = "Stop"
$repo = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
$local = Join-Path $env:LOCALAPPDATA "EvoSimDocs\dotnet"
if (Test-Path (Join-Path $local "dotnet.exe")) { $env:DOTNET_ROOT = $local; $dotnet = Join-Path $local "dotnet.exe" } else { $dotnet = "dotnet" }
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"; $env:DOTNET_NOLOGO = "1"
$project = Join-Path $PSScriptRoot "DocAudit"
$dll = Join-Path $project "bin\out\DocAudit.dll"
$sources = Get-ChildItem $project -File | Where-Object { $_.Extension -in ".cs", ".csproj" }
if (-not (Test-Path $dll) -or ($sources | Where-Object { $_.LastWriteTime -gt (Get-Item $dll).LastWriteTime })) {
  & $dotnet build $project -c Release -o (Join-Path $project "bin\out") --nologo -v quiet | Out-Null
  if ($LASTEXITCODE -ne 0) { Write-Output "could not build DocAudit"; exit 2 }
}
& $dotnet $dll --root $repo @args
exit $LASTEXITCODE
