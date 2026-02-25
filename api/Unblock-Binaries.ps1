# Unblock all DLLs and EXEs under api folder (fixes FileLoadException 0x800711C7)
$apiRoot = Join-Path $PSScriptRoot "Bangkok.Api"
$binPath = Join-Path $apiRoot "bin"
if (-not (Test-Path $binPath)) {
    Write-Host "No bin folder found. Build the solution first, then run this script."
    exit 0
}
$count = 0
Get-ChildItem -Path $binPath -Recurse -Include "*.dll","*.exe" -File -ErrorAction SilentlyContinue | ForEach-Object {
    Unblock-File -Path $_.FullName -ErrorAction SilentlyContinue
    $count++
}
Write-Host "Unblocked $count file(s) in $binPath"
