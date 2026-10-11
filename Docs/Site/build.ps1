# Builds the EvoSim documentation site (Docs/Site/README.md) into Docs/Site/_site.
#
#   powershell -File Docs\Site\build.ps1            # build
#   powershell -File Docs\Site\build.ps1 -Serve     # build, then serve on http://localhost:8080
#   powershell -File Docs\Site\build.ps1 -Strict    # fail on any documentation issue or DocFX warning
#
# Needs: the .NET 8 SDK and DocFX (user-local under %LOCALAPPDATA%\EvoSimDocs, or on the PATH), git, and the
# EvoSim.*.csproj files Unity writes at the repository root (open the project in Unity once).
param([switch]$Serve, [switch]$Strict, [int]$Port = 8080)

$ErrorActionPreference = "Stop"
$site = $PSScriptRoot
$repo = (Resolve-Path (Join-Path $site "..\..")).Path
$tools = Join-Path $env:LOCALAPPDATA "EvoSimDocs"
if (Test-Path (Join-Path $tools "dotnet\dotnet.exe")) { $env:DOTNET_ROOT = Join-Path $tools "dotnet"; $env:PATH = "$env:DOTNET_ROOT;$env:PATH" }
$docfx = Join-Path $tools "tools\docfx.exe"
if (-not (Test-Path $docfx)) { $docfx = "docfx" }
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"; $env:DOTNET_NOLOGO = "1"
if (-not (Test-Path (Join-Path $repo "EvoSim.Runtime.csproj"))) { throw "EvoSim.Runtime.csproj is missing: open the project in Unity once (it writes the .csproj files)" }

function Step([string]$what) { Write-Output ""; Write-Output "== $what" }
$audit = Join-Path $site "tools\audit.ps1"

Step "documentation audit"
& powershell -NoProfile -File $audit --max 20
$auditCode = $LASTEXITCODE
if ($auditCode -eq 2) { throw "the code does not compile" }
if ($Strict -and $auditCode -ne 0) { throw "documentation issues (see above)" }

Step "specification, extension points, examples"
& powershell -NoProfile -File $audit --no-report --spec (Join-Path $site "spec") --inventory (Join-Path $site "extending\extension-points.md") `
  --examples "$(Join-Path $site 'guides'),$(Join-Path $site 'extending')"
$examplesCode = $LASTEXITCODE
if ($examplesCode -eq 2) { throw "DocAudit failed" }
if ($Strict -and $examplesCode -ne 0) { throw "an example of the pages does not compile (see above)" }

Step "API metadata (DocFX)"
Get-ChildItem (Join-Path $site "api") -Filter *.yml -ErrorAction SilentlyContinue | Remove-Item
& $docfx metadata (Join-Path $site "docfx.json")
if ($LASTEXITCODE -ne 0) { throw "docfx metadata failed" }
& powershell -NoProfile -File $audit --no-report --api-toc (Join-Path $site "api")
if ($LASTEXITCODE -ne 0) { throw "DocAudit --api-toc failed" }

Step "site (DocFX)"
$buildArgs = @("build", (Join-Path $site "docfx.json"))
if ($Strict) { $buildArgs += "--warningsAsErrors" }
& $docfx @buildArgs
if ($LASTEXITCODE -ne 0) { throw "docfx build failed" }
Write-Output ""
Write-Output "site written to $(Join-Path $site '_site') (open index.html through a web server: -Serve)"

if ($Serve) { & $docfx serve (Join-Path $site "_site") --port $Port }
