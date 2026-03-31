param(
    [Alias("t")][string]$Title,
    [Alias("m")][string]$Message,
    [Alias("i")][string]$Icon,
    [Alias("f")][string]$From,
    [Alias("h")][switch]$Help,
    [switch]$Init,
    [switch]$Configure
)

$configFile = if ($env:XDG_CONFIG_HOME) {
    Join-Path $env:XDG_CONFIG_HOME "winnotifier/config"
} else {
    Join-Path $HOME ".config/winnotifier/config"
}

if ($Help) {
    Write-Output @"
Usage: winnotify -t <title> -m <message> [-i <icon>] [-f <from>]

Options:
  -t, -Title    Notification title (required)
  -m, -Message  Notification message (required)
  -i, -Icon     Emoji icon (e.g. rocket, :bell:)
  -f, -From     Sender name
  -h, -Help       Show this help
  -Init           Create a sample config file
  -Configure      Open the config file in an editor

Config:
  File: $configFile
  Env:  WINNOTIFIER_ENDPOINT, WINNOTIFIER_TOKEN
"@
    exit 0
}

if ($Configure) {
    if (-not (Test-Path $configFile)) {
        [Console]::Error.WriteLine("Config file not found: $configFile")
        [Console]::Error.WriteLine("Run 'winnotify --init' to create one.")
        exit 1
    }
    Start-Process $configFile
    exit 0
}

if ($Init) {
    if (Test-Path $configFile) {
        [Console]::Error.WriteLine("Config file already exists: $configFile")
        exit 1
    }
    New-Item -ItemType Directory -Force -Path (Split-Path $configFile) | Out-Null
    Set-Content $configFile @"
# WinNotifier config
endpoint=http://localhost:8080
token=
"@
    Write-Output "Created: $configFile"
    exit 0
}

if (-not $Title -or -not $Message) {
    [Console]::Error.WriteLine("Usage: winnotify -t <title> -m <message> [-i <icon>] [-f <from>]")
    [Console]::Error.WriteLine("Try 'winnotify -Help' for more information.")
    [Console]::Error.WriteLine("Run 'winnotify -Init' to create a config file.")
    exit 1
}

# Load config (file first, then env vars override)
$endpoint = ""
$token = ""

if (Test-Path $configFile) {
    Get-Content $configFile | ForEach-Object {
        $line = $_ -replace '#.*', ''
        if ($line -match '^\s*(\w+)\s*=\s*(.+?)\s*$') {
            switch ($Matches[1]) {
                "endpoint" { $endpoint = $Matches[2] }
                "token"    { $token = $Matches[2] }
            }
        }
    }
}

if ($env:WINNOTIFIER_ENDPOINT) { $endpoint = $env:WINNOTIFIER_ENDPOINT }
if ($env:WINNOTIFIER_TOKEN)    { $token = $env:WINNOTIFIER_TOKEN }

if (-not $endpoint) {
    $msg = @"
Error: endpoint not configured.

Option 1: Create a config file
  New-Item -ItemType Directory -Force -Path "$(Split-Path $configFile)"
  Set-Content "$configFile" @'
  endpoint=http://<windows-ip>:8080
  token=<your-token>
  '@

Option 2: Set environment variables
  `$env:WINNOTIFIER_ENDPOINT = "http://<windows-ip>:8080"
  `$env:WINNOTIFIER_TOKEN = "<your-token>"
"@
    [Console]::Error.WriteLine($msg)
    exit 1
}

# Build payload
$payload = @{ title = $Title; message = $Message }
if ($Icon)  { $payload.icon = $Icon }
if ($From)  { $payload.from = $From }

$json = $payload | ConvertTo-Json -Compress
$headers = @{ "Content-Type" = "application/json" }

if ($token) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes(":$token")
    $encoded = [System.Convert]::ToBase64String($bytes)
    $headers["Authorization"] = "Basic $encoded"
}

try {
    Invoke-RestMethod -Uri "$endpoint/notify" -Method Post -Headers $headers -Body $json | Out-Null
} catch {
    [Console]::Error.WriteLine("Error: $($_.Exception.Message)")
    exit 1
}
