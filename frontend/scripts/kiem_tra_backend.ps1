$urls = @('http://localhost:5080/health','http://localhost:5080/api/catalog/cities')
foreach ($url in $urls) {
  try {
    $r = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec 8
    Write-Host "OK: $url" -ForegroundColor Green
  } catch {
    Write-Host "LOI: $url - $($_.Exception.Message)" -ForegroundColor Red
  }
}
