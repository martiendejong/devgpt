$ErrorActionPreference = 'Stop'

param(
  [string]$ExePath
)

if (-not $ExePath) {
  $ExePath = Join-Path $PSScriptRoot 'bin\Debug\net8.0-windows\DevGPT.ExplorerIntegration.exe'
}

Write-Host "Registering Explorer context menu for: $ExePath"

# Current user only (no admin): Directory context menu
$base = 'HKCU:\Software\Classes\Directory\shell'

$embedKey = Join-Path $base 'DevGPT.Embed'
New-Item -Path $embedKey -Force | Out-Null
New-ItemProperty -Path $embedKey -Name 'MUIVerb' -Value 'Embed files' -PropertyType String -Force | Out-Null
New-Item -Path (Join-Path $embedKey 'command') -Force | Out-Null
New-ItemProperty -Path (Join-Path $embedKey 'command') -Name '(default)' -Value '"' + $ExePath + '" "%1" --embed' -PropertyType String -Force | Out-Null

$chatKey = Join-Path $base 'DevGPT.StartChat'
New-Item -Path $chatKey -Force | Out-Null
New-ItemProperty -Path $chatKey -Name 'MUIVerb' -Value 'Start Chat' -PropertyType String -Force | Out-Null
New-Item -Path (Join-Path $chatKey 'command') -Force | Out-Null
New-ItemProperty -Path (Join-Path $chatKey 'command') -Name '(default)' -Value '"' + $ExePath + '" "%1" --chat' -PropertyType String -Force | Out-Null

Write-Host 'Done. Right-click on a folder to see the menu items.'

