<#
.SYNOPSIS
  Installs and starts the EvoSim model servers (JEV on vLLM in Docker, the Ollama mutator) so that other
  machines on the local network can use them; or, with -Client, points this machine's localhost ports at them.

.DESCRIPTION
  Server (default), run from a clone of the repository:
    1. checks the prerequisites (see PREREQUISITES below) and stops with the fix for each missing one;
    2. downloads the JEV weights at the pinned revision into %USERPROFILE%\.cache\huggingface (what the container mounts);
    3. pulls the pinned vLLM image by digest, as compose.yaml names it (Docker Hub prunes nightly tags);
    4. starts the jev service of prototype\docker\compose.yaml and waits until it is healthy (5-10 min);
    5. starts Ollama listening on all interfaces and pulls the mutator model;
    6. opens the firewall for both ports, local subnet only (-OpenFirewall, needs an administrator PowerShell);
    7. checks both servers on localhost and on the LAN address, and prints the line to run on each client.

  Client (-Client -Server <address>), administrator PowerShell: the EvoSim scenes call http://localhost:8000 and
  http://localhost:11434 (the host is saved in 33 components), so this forwards those two localhost ports to the
  server with netsh portproxy instead of editing scenes. -Remove deletes the forwards.

  PREREQUISITES (server)
    - Windows 10 22H2 / 11 with WSL 2:    wsl --install          (administrator, then reboot)
    - NVIDIA driver, recent (Windows side only; nothing inside WSL). nvidia-smi must work.
    - NVIDIA GPU with about 16 GB. Less: start with -Quant bitsandbytes (4-bit JEV, less accurate).
    - Docker Desktop, WSL 2 backend:      winget install -e --id Docker.DockerDesktop   (start it once, accept terms)
    - Python 3.10+ (python.org build):    winget install -e --id Python.Python.3.12
      (the "python" command of a fresh Windows is a Microsoft Store stub; the script detects that)
    - Ollama:                             winget install -e --id Ollama.Ollama
    - Git, to clone the repository:       winget install -e --id Git.Git
    - Disk: ~19 GB for the JEV weights, ~33 GB for the vLLM image, ~18 GB for gemma4:26b.
    - RAM: ~20 GB free for gemma4:26b on the CPU (use -MutatorModel qwen3.5:0.8b on a small machine).
  -InstallPrereqs installs the winget ones above; WSL and the reboot stay manual.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File Tools\Install-ModelServers.ps1 -CheckOnly
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File Tools\Install-ModelServers.ps1 -OpenFirewall -Persist
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File Tools\Install-ModelServers.ps1 -Client -Server 192.168.1.31
#>
[CmdletBinding()]
param(
    [switch]$CheckOnly,            # only check prerequisites and the current state; change nothing
    [switch]$InstallPrereqs,       # install the winget prerequisites that are missing
    [switch]$SkipJev,
    [switch]$SkipOllama,
    [switch]$OpenFirewall,         # add inbound rules for both ports, local subnet only (administrator)
    [switch]$Persist,              # make OLLAMA_HOST etc. user variables, so the Ollama tray app also listens on the LAN
    [int]$JevPort = 8000,
    [int]$OllamaPort = 11434,
    [string]$MutatorModel = 'gemma4:26b',
    [ValidateSet('fp8', 'bitsandbytes')][string]$Quant = 'fp8',
    [string]$JevKv = '768M',
    [int]$HealthMinutes = 15,
    [switch]$Client,
    [string]$Server,
    [switch]$Remove
)

$ErrorActionPreference = 'Stop'
$Repo      = 'autotrust/JEV-9B'
$Revision  = 'b63f651ce8ed64481d3f5e73ecdb05f740042f01'
$Patterns  = @('*.json', '*.jinja', 'model-*.safetensors', 'head.safetensors', 'adapter/*', 'adapter_vllm/*')
$Image     = 'vllm/vllm-openai@sha256:a83c7f976879e0c6101f734fd5b84a098d12d6d8d12baa41e31eb323b84b0802'   # = compose.yaml (build nightly-81198e97)
$Container = 'promptevo-jev'
$HfHome    = Join-Path $env:USERPROFILE '.cache\huggingface'   # compose mounts ${USERPROFILE}/.cache/huggingface
$State     = Join-Path $env:LOCALAPPDATA 'EvoSimModelServers'

function Step($m) { Write-Host ''; Write-Host "== $m" -ForegroundColor Cyan }
function Ok($m)   { Write-Host "  ok    $m" -ForegroundColor Green }
function Warn($m) { Write-Host "  warn  $m" -ForegroundColor Yellow; $script:Warnings += $m }
function Bad($m, $fix) {
    Write-Host "  FAIL  $m" -ForegroundColor Red
    if ($fix) { Write-Host "        fix: $fix" -ForegroundColor Red }
    $script:Failures += $m
}
$script:Warnings = @(); $script:Failures = @()

function Test-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    return (New-Object Security.Principal.WindowsPrincipal $id).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}
function Has($cmd) { return [bool](Get-Command $cmd -ErrorAction SilentlyContinue) }
function Get-Http($url, $timeout = 5) {
    try { return Invoke-RestMethod -UseBasicParsing -TimeoutSec $timeout -Uri $url } catch { return $null }
}
function Get-LanIp {
    $a = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue | Where-Object {
        $_.PrefixOrigin -in 'Dhcp', 'Manual' -and $_.IPAddress -notlike '169.254.*' -and
        $_.InterfaceAlias -notmatch 'vEthernet|WSL|Loopback|Docker' } | Select-Object -First 1
    if ($a) { return $a.IPAddress } else { return $null }
}
function Winget-Install($id) {
    if (-not (Has winget)) { Bad "winget is missing" 'install "App Installer" from the Microsoft Store'; return }
    Write-Host "  installing $id ..."
    & winget install -e --id $id --accept-package-agreements --accept-source-agreements | Out-Host
}

# A real Python 3: py -3, then python/python3 (the WindowsApps stub fails the test), then common install paths.
function Find-Python {
    $cands = @(@('py', '-3'), @('python'), @('python3'))
    foreach ($p in @("$env:LOCALAPPDATA\Programs\Python\Python3*\python.exe", "$env:ProgramFiles\Python3*\python.exe",
                     "$env:ProgramData\miniconda3\python.exe", "$env:USERPROFILE\miniconda3\python.exe")) {
        Get-Item $p -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | ForEach-Object { $cands += ,@($_.FullName) }
    }
    foreach ($c in $cands) {
        $exe = $c[0]; $rest = @(); if ($c.Count -gt 1) { $rest = $c[1..($c.Count - 1)] }
        if (-not (Get-Command $exe -ErrorAction SilentlyContinue)) { continue }
        try {
            $v = & $exe @rest -c "import sys; print('%d.%d' % sys.version_info[:2])" 2>$null
            if ($LASTEXITCODE -eq 0 -and $v -match '^3\.(\d+)$' -and [int]$Matches[1] -ge 10) { return ,$c }
        } catch { }
    }
    return $null
}

# ---------------------------------------------------------------------------------------------- client
if ($Client) {
    if (-not (Test-Admin)) { Write-Host 'Client mode needs an administrator PowerShell (netsh portproxy).' -ForegroundColor Red; exit 1 }
    foreach ($port in @($JevPort, $OllamaPort)) {
        & netsh interface portproxy delete v4tov4 listenaddress=127.0.0.1 listenport=$port 2>$null | Out-Null
        & netsh interface portproxy delete v6tov4 listenaddress=::1 listenport=$port 2>$null | Out-Null
    }
    if ($Remove) { Write-Host 'Forwards removed.'; exit 0 }
    if (-not $Server) { Write-Host 'Give -Server <address of the model server>.' -ForegroundColor Red; exit 1 }
    Step "Forwarding localhost:$JevPort and localhost:$OllamaPort to $Server"
    foreach ($port in @($JevPort, $OllamaPort)) {
        $busy = Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue
        if ($busy) {
            $pn = (Get-Process -Id $busy[0].OwningProcess -ErrorAction SilentlyContinue).ProcessName
            Bad "port $port is already used here by '$pn'" 'quit it (a local Ollama tray app or JEV container), then run again'
            continue
        }
        & netsh interface portproxy add v4tov4 listenaddress=127.0.0.1 listenport=$port connectaddress=$Server connectport=$port | Out-Null
        & netsh interface portproxy add v6tov4 listenaddress=::1 listenport=$port connectaddress=$Server connectport=$port | Out-Null
    }
    Start-Sleep 1
    $m = Get-Http "http://localhost:$JevPort/v1/models" 10
    if ($m -and ($m.data.id -contains 'jev-decision')) { Ok "JEV answers on localhost:$JevPort" } else { Bad "JEV does not answer through the forward" "check the server and its firewall: curl.exe http://${Server}:$JevPort/v1/models" }
    $t = Get-Http "http://localhost:$OllamaPort/api/tags" 10
    if ($t) { Ok "Ollama answers on localhost:$OllamaPort ($(@($t.models.name) -join ', '))" } else { Bad "Ollama does not answer through the forward" "check the server: curl.exe http://${Server}:$OllamaPort/api/tags" }
    Write-Host ''; Write-Host 'In Unity: Window > EvoSim > Brain Health > Test connection. Undo with -Client -Remove.'
    if ($script:Failures.Count) { exit 1 } else { exit 0 }
}

# ---------------------------------------------------------------------------------------------- server
$RepoRoot = Split-Path -Parent $PSScriptRoot
$Compose  = Join-Path $RepoRoot 'prototype\docker\compose.yaml'

Step 'Prerequisites'
if ($InstallPrereqs -and -not $CheckOnly) {
    if (-not (Has docker)) { Winget-Install 'Docker.DockerDesktop' }
    if (-not (Find-Python)) { Winget-Install 'Python.Python.3.12' }
    if (-not $SkipOllama -and -not (Has ollama) -and -not (Test-Path "$env:LOCALAPPDATA\Programs\Ollama\ollama.exe")) { Winget-Install 'Ollama.Ollama' }
    $env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User')
}
if (-not (Has ollama) -and (Test-Path "$env:LOCALAPPDATA\Programs\Ollama\ollama.exe")) { $env:Path += ";$env:LOCALAPPDATA\Programs\Ollama" }

if (-not (Test-Path $Compose)) { Bad "compose file not found: $Compose" 'run this script from a clone of the repository (branch llm-evolution)' }
else { Ok "repository: $RepoRoot" }

$wsl = & wsl.exe --status 2>$null
if ($LASTEXITCODE -ne 0) { Bad 'WSL 2 is not installed' 'administrator PowerShell: wsl --install, then reboot' } else { Ok 'WSL is installed' }

if (Has nvidia-smi) {
    $g = (& nvidia-smi --query-gpu=name,memory.total,driver_version --format=csv,noheader,nounits | Select-Object -First 1) -split ',\s*'
    if ([int]$g[1] -lt 15000 -and $Quant -eq 'fp8') { Warn "GPU $($g[0]) has $($g[1]) MiB: FP8 JEV needs ~14.4 GB; rerun with -Quant bitsandbytes" }
    else { Ok "GPU $($g[0]), $($g[1]) MiB, driver $($g[2])" }
} else { Bad 'nvidia-smi not found' 'install the current NVIDIA driver for Windows (https://www.nvidia.com/drivers)' }

$drive = (Get-Item $env:USERPROFILE).PSDrive
$freeGb = [math]::Round($drive.Free / 1GB)
$needGb = 0; if (-not $SkipJev) { $needGb += 55 }; if (-not $SkipOllama) { $needGb += 20 }
if ($freeGb -lt $needGb) { Warn "drive ${drive}: has $freeGb GB free; a fresh install needs ~$needGb GB (weights, image, model)" } else { Ok "drive ${drive}: $freeGb GB free" }

$ramGb = [math]::Round((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory / 1MB)
if (-not $SkipOllama -and $MutatorModel -eq 'gemma4:26b' -and $ramGb -lt 20) { Warn "$ramGb GB RAM free; gemma4:26b on the CPU wants ~20 GB (try -MutatorModel qwen3.5:0.8b)" } else { Ok "$ramGb GB RAM free" }

$py = $null
if (-not $SkipJev) {
    $py = Find-Python
    if ($py) { Ok "Python: $($py -join ' ')" } else { Bad 'no Python 3.10+ found ("python" may be the Microsoft Store stub)' 'winget install -e --id Python.Python.3.12, then open a new PowerShell' }
    if (-not (Has docker)) { Bad 'docker not found' 'winget install -e --id Docker.DockerDesktop, start Docker Desktop once (WSL 2 backend)' }
    else {
        & docker info --format '{{.ServerVersion}}' 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) { Bad 'the Docker engine is not running' 'start Docker Desktop and wait for "Engine running"' } else { Ok 'Docker engine running' }
    }
}
if (-not $SkipOllama) {
    if (Has ollama) { Ok "Ollama: $(& ollama --version 2>$null | Select-Object -Last 1)" } else { Bad 'ollama not found' 'winget install -e --id Ollama.Ollama' }
}
if ($env:HF_HOME -and ((Resolve-Path $env:HF_HOME -ErrorAction SilentlyContinue).Path -ne $HfHome)) {
    Warn "HF_HOME is set to $env:HF_HOME, but the container mounts $HfHome; this script downloads to the latter"
}
$lan = Get-LanIp
if ($lan) { Ok "LAN address: $lan" } else { Warn 'no LAN IPv4 address found' }
$cat = (Get-NetConnectionProfile -ErrorAction SilentlyContinue | Select-Object -First 1).NetworkCategory
if ($cat -eq 'Public') { Warn 'the network profile is Public: other machines may be blocked unless the firewall rules include Public (see -OpenFirewall)' }

if ($script:Failures.Count) {
    Write-Host ''; Write-Host "Stopped: $($script:Failures.Count) prerequisite(s) missing (fix lines above, or -InstallPrereqs)." -ForegroundColor Red
    exit 1
}

# ---------------------------------------------------------------------------------------------- JEV
if (-not $SkipJev) {
    $snap = Join-Path $HfHome "hub\models--autotrust--JEV-9B\snapshots\$Revision"
    $need = @('adapter_vllm\adapter_model.safetensors', 'adapter_vllm\decision_head.json', 'head.safetensors', 'calibration.json', 'config.json')
    $missing = @($need | Where-Object { -not (Test-Path (Join-Path $snap $_)) })
    $shards = @(Get-ChildItem $snap -Filter 'model-*.safetensors' -ErrorAction SilentlyContinue).Count

    Step "JEV weights ($Repo @ $($Revision.Substring(0,12)))"
    if ($missing.Count -eq 0 -and $shards -gt 0) { Ok "present in $snap ($shards shards)" }
    elseif ($CheckOnly) { Warn "not downloaded yet (missing: $($missing -join ', '))" }
    else {
        $venv = Join-Path $State 'venv'
        $vpy = Join-Path $venv 'Scripts\python.exe'
        if (-not (Test-Path $vpy)) {
            $rest = @(); if ($py.Count -gt 1) { $rest = $py[1..($py.Count - 1)] }
            & $py[0] @rest -m venv $venv
            if ($LASTEXITCODE -ne 0) { throw 'python -m venv failed' }
        }
        & $vpy -m pip install --quiet --upgrade pip huggingface_hub
        if ($LASTEXITCODE -ne 0) { throw 'pip install huggingface_hub failed (proxy? offline?)' }
        Write-Host '  downloading ~19 GB, resumable: rerun the script if it stops ...'
        $env:HF_HOME = $HfHome
        $pat = ($Patterns | ForEach-Object { "'$_'" }) -join ','
        & $vpy -c "from huggingface_hub import snapshot_download as d; print(d('$Repo', revision='$Revision', allow_patterns=[$pat]))"
        if ($LASTEXITCODE -ne 0) { throw 'download failed; a 401/403 means the model now needs a token: set $env:HF_TOKEN and rerun' }
        $missing = @($need | Where-Object { -not (Test-Path (Join-Path $snap $_)) })
        if ($missing.Count) { throw "download finished but files are missing: $($missing -join ', ')" }
        Ok "downloaded to $snap"
    }

    Step 'vLLM image (pinned build)'
    & docker image inspect $Image --format '{{.Id}}' 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) { Ok "$Image present" }
    elseif ($CheckOnly) { Warn 'image not pulled yet (~33 GB)' }
    else {
        Write-Host '  pulling (~33 GB) ...'
        & docker pull $Image
        if ($LASTEXITCODE -ne 0) { throw "docker pull $Image failed" }
        Ok "pulled $Image"
    }

    Step "JEV service on port $JevPort"
    $health = & docker inspect -f '{{.State.Health.Status}}' $Container 2>$null
    if ($health -eq 'healthy') { Ok "$Container already healthy" }
    elseif ($CheckOnly) { Warn "$Container is not running ($health)" }
    else {
        & docker run --rm --gpus all --entrypoint nvidia-smi $Image -L 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Docker cannot see the GPU: update the NVIDIA driver, restart Docker Desktop (Settings > Resources > WSL integration)' }
        $env:JEV_PORT = "$JevPort"; $env:JEV_QUANT = $Quant; $env:JEV_KV = $JevKv
        & docker compose -f $Compose up -d jev
        if ($LASTEXITCODE -ne 0) { throw 'docker compose up failed' }
        $deadline = (Get-Date).AddMinutes($HealthMinutes)
        do {
            Start-Sleep 15
            $health = & docker inspect -f '{{.State.Health.Status}}' $Container 2>$null
            $running = & docker inspect -f '{{.State.Running}}' $Container 2>$null
            Write-Host "  $(Get-Date -Format HH:mm:ss) $health"
            if ($running -ne 'true') { break }
        } while ($health -ne 'healthy' -and (Get-Date) -lt $deadline)
        if ($health -ne 'healthy') {
            & docker logs --tail 30 $Container
            throw "JEV did not become healthy. 'out of memory': close GPU apps or lower -JevKv; 'offline'/'not found': weights missing or wrong revision"
        }
        Ok 'healthy'
    }
}

# ---------------------------------------------------------------------------------------------- Ollama
if (-not $SkipOllama) {
    Step "Ollama on 0.0.0.0:$OllamaPort, mutator $MutatorModel"
    $listen = Get-NetTCPConnection -State Listen -LocalPort $OllamaPort -ErrorAction SilentlyContinue
    $onAll = @($listen | Where-Object { $_.LocalAddress -in '0.0.0.0', '::' }).Count -gt 0
    if ($onAll) { Ok "already listening on all interfaces" }
    elseif ($CheckOnly) {
        if ($listen) { Warn "Ollama listens on $(@($listen.LocalAddress) -join ', ') only: other machines can't reach it" } else { Warn 'Ollama is not running' }
    } else {
        # The tray app starts its own server bound to 127.0.0.1; stop it and start one bound to all interfaces.
        Get-Process 'ollama app', 'ollama' -ErrorAction SilentlyContinue | Stop-Process -Force
        Start-Sleep 2
        if ($Persist) {
            [Environment]::SetEnvironmentVariable('OLLAMA_HOST', "0.0.0.0:$OllamaPort", 'User')
            [Environment]::SetEnvironmentVariable('OLLAMA_KEEP_ALIVE', '4h', 'User')
            [Environment]::SetEnvironmentVariable('OLLAMA_MAX_LOADED_MODELS', '1', 'User')
            Ok 'user variables OLLAMA_HOST, OLLAMA_KEEP_ALIVE, OLLAMA_MAX_LOADED_MODELS set (the tray app uses them at next start)'
        }
        $env:OLLAMA_HOST = "0.0.0.0:$OllamaPort"; $env:OLLAMA_KEEP_ALIVE = '4h'; $env:OLLAMA_MAX_LOADED_MODELS = '1'
        New-Item -ItemType Directory -Force $State | Out-Null
        Start-Process ollama -ArgumentList 'serve' -WindowStyle Hidden `
            -RedirectStandardOutput (Join-Path $State 'ollama.out.log') -RedirectStandardError (Join-Path $State 'ollama.log') | Out-Null
        $t0 = Get-Date
        while (-not (Get-Http "http://127.0.0.1:$OllamaPort/api/version") -and ((Get-Date) - $t0).TotalSeconds -lt 30) { Start-Sleep 1 }
        if (Get-Http "http://127.0.0.1:$OllamaPort/api/version") { Ok "ollama serve started (log: $State\ollama.log)" } else { throw "ollama serve did not start; see $State\ollama.log" }
    }
    $tags = Get-Http "http://127.0.0.1:$OllamaPort/api/tags"
    if ($tags -and (@($tags.models.name) -contains $MutatorModel)) { Ok "$MutatorModel present" }
    elseif ($CheckOnly) { Warn "$MutatorModel not pulled" }
    elseif ($tags) {
        $env:OLLAMA_HOST = "127.0.0.1:$OllamaPort"     # the CLI talks to the server; 0.0.0.0 is a listen address
        & ollama pull $MutatorModel
        if ($LASTEXITCODE -ne 0) { throw "ollama pull $MutatorModel failed" }
        Ok "$MutatorModel pulled"
    }
}

# ---------------------------------------------------------------------------------------------- firewall
Step 'Firewall'
$ports = @(); if (-not $SkipJev) { $ports += $JevPort }; if (-not $SkipOllama) { $ports += $OllamaPort }
foreach ($port in $ports) {
    $name = "EvoSim model server $port"
    if (Get-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue) { Ok "rule '$name' exists"; continue }
    if ($OpenFirewall -and -not $CheckOnly) {
        if (-not (Test-Admin)) { Warn "-OpenFirewall needs an administrator PowerShell; rule for $port not added"; continue }
        New-NetFirewallRule -DisplayName $name -Direction Inbound -Protocol TCP -LocalPort $port -Action Allow `
            -RemoteAddress LocalSubnet -Profile Any | Out-Null
        Ok "rule '$name' added (TCP $port, local subnet only)"
    } else {
        Warn "no rule for TCP ${port}: rerun as administrator with -OpenFirewall (or accept Windows' prompt for private networks)"
    }
}

# ---------------------------------------------------------------------------------------------- checks
Step 'Checks'
if (-not $SkipJev) {
    $m = Get-Http "http://localhost:$JevPort/v1/models" 10
    if ($m -and ($m.data.id -contains 'jev-decision')) { Ok "JEV localhost:$JevPort lists $(@($m.data.id) -join ', ')" } else { Bad "JEV does not answer on localhost:$JevPort" $null }
    if ($lan) { if (Get-Http "http://${lan}:$JevPort/v1/models" 10) { Ok "JEV answers on ${lan}:$JevPort" } else { Bad "JEV does not answer on ${lan}:$JevPort" $null } }
}
if (-not $SkipOllama) {
    if (Get-Http "http://127.0.0.1:$OllamaPort/api/tags") { Ok "Ollama 127.0.0.1:$OllamaPort" } else { Bad "Ollama does not answer on 127.0.0.1:$OllamaPort" $null }
    if ($lan) { if (Get-Http "http://${lan}:$OllamaPort/api/tags") { Ok "Ollama answers on ${lan}:$OllamaPort" } else { Bad "Ollama does not answer on ${lan}:$OllamaPort (bound to 127.0.0.1?)" $null } }
}
Write-Host ''
Write-Host 'Note: a check of the LAN address from this machine does not cross the firewall; test from the client.' -ForegroundColor DarkGray
if ($lan) {
    Write-Host "On each client (administrator PowerShell, in its clone of the repository):"
    Write-Host "  powershell -ExecutionPolicy Bypass -File Tools\Install-ModelServers.ps1 -Client -Server $lan"
}
Write-Host ''
Write-Host "Done: $($script:Failures.Count) failure(s), $($script:Warnings.Count) warning(s)."
if ($script:Failures.Count) { exit 1 } else { exit 0 }
