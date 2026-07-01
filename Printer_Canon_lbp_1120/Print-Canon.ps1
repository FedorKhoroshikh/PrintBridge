#requires -version 5
# Launcher: starts the Windows XP VM used to print on the Canon LBP-1120.
# The USB printer is passed through AUTOMATICALLY by a VirtualBox USB filter
# (Canon CAPT, VendorId 04a9 / ProductId 262b), so no manual attach is needed.
#
# NOTE: ASCII-only on purpose. PowerShell 5.1 reads .ps1 in the system ANSI
# codepage; non-ASCII without a BOM corrupts the file and breaks parsing.
#
# NOTE: do NOT use ErrorActionPreference='Stop'. In PS 5.1 any VBoxManage
# stderr output would then become a terminating error and kill the script
# before startvm. 'Continue' + 2>$null makes the launch reliable.

$ErrorActionPreference = 'Continue'

$VBox = 'C:\Program Files\Oracle\VirtualBox\VBoxManage.exe'
$VM   = 'Microelectronics'

function Say($t, $c='Gray'){ Write-Host $t -ForegroundColor $c }

if (-not (Test-Path $VBox)) { Say "VBoxManage not found: $VBox" Red; Start-Sleep 3; exit 1 }

# Start the VM if it is not already running.
$running = & $VBox list runningvms 2>$null
if ("$running" -match [regex]::Escape($VM)) {
    Say "VM already running - printer ready." Green
} else {
    Say "Starting VM '$VM'..." Cyan
    & $VBox startvm $VM --type gui 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Say "Done. XP is booting (~30-60s); printer attaches automatically (USB filter)." Green
    } else {
        Say "VBoxManage returned $LASTEXITCODE. Start the VM manually in VirtualBox." Yellow
        Start-Sleep 4
    }
}
Start-Sleep -Seconds 2
