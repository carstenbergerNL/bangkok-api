# Unblocks all .dll and .exe in the given output path (fixes FileLoadException 0x800711C7).
param([Parameter(Mandatory=$true)][string]$OutputPath)
$path = $OutputPath.TrimEnd('\')
if (Test-Path -LiteralPath $path -PathType Container) {
    Get-ChildItem -LiteralPath $path -File -ErrorAction SilentlyContinue
    | Where-Object { $_.Extension -match '\.(dll|exe)$' }
    | ForEach-Object { Unblock-File -LiteralPath $_.FullName -ErrorAction SilentlyContinue }
}
