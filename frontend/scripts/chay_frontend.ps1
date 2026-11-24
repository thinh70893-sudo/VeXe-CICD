# Chay bang PowerShell tai thu muc goc frontend
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
Write-Host '=== BUSGO FRONTEND ===' -ForegroundColor Cyan
if (-not (Get-Command node -ErrorAction SilentlyContinue)) { throw 'Chua cai Node.js >=22.12: https://nodejs.org' }
if (-not (Test-Path 'node_modules')) {
  Write-Host 'Dang cai goi npm (can ket noi Internet)...' -ForegroundColor Yellow
  npm install
  if ($LASTEXITCODE -ne 0) { throw 'npm install loi. Kiem tra ket noi npm registry.' }
}
Write-Host 'Mo http://localhost:5173, Backend du kien http://localhost:5080' -ForegroundColor Green
npm run dev
