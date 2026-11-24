# PowerShell 7+/Windows PowerShell 5.1 - chay sau khi DB, Redis, SMTP va API da san sang.
$ErrorActionPreference = 'Stop'
$diaChiApi = 'http://localhost:5080'
function GuiJson($duongDan, $noiDung) {
    return Invoke-RestMethod -Method POST -Uri "$diaChiApi$duongDan" -Headers $script:tieuDeAuth `
        -ContentType 'application/json' -Body ($noiDung | ConvertTo-Json -Depth 8)
}
Write-Host '[1] Dang nhap khach mau ...'
$login = Invoke-RestMethod -Method POST -Uri "$diaChiApi/api/auth/login" -ContentType 'application/json' `
    -Body '{"email":"khach1@example.test","matKhau":"Demo@123456"}'
$tieuDeAuth = @{ Authorization = "Bearer $($login.accessToken)" }
Write-Host '[2] Tim chuyen, tim ghe AVAILABLE ...'
$dsChuyen = @(Invoke-RestMethod -Uri "$diaChiApi/api/trips?soDong=100")
$maChuyen = $null
$maGhe = $null
foreach ($chuyen in $dsChuyen) {
    $dsGhe = @(Invoke-RestMethod -Uri "$diaChiApi/api/trips/$($chuyen.maChuyen)/seats")
    $ghe = $dsGhe | Where-Object { $_.trangThai -eq 'AVAILABLE' } | Select-Object -First 1
    if ($ghe) { $maChuyen = $chuyen.maChuyen; $maGhe = $ghe.maGhe; break }
}
if (-not $maGhe) { throw 'Khong co ghe trong, thu tao them chuyen moi.' }
Write-Host "Chuyen=$maChuyen Ghe=$maGhe"
Write-Host '[3] Redis giu ghe ...'
$giuGhe = GuiJson '/api/seat-holds' @{maChuyen=$maChuyen;danhSachMaGhe=@($maGhe)}
Write-Host "Ma giu ghe=$($giuGhe.maGiuGhe), het han=$($giuGhe.hetHanUtc)"
Write-Host '[4] Tao don dat ve ...'
$datVe = GuiJson '/api/bookings' @{
  maGiuGhe=$giuGhe.maGiuGhe; tenLienHe='Nguyen Minh Anh';emailLienHe='khach1@example.test';
  soDienThoaiLienHe='0900000003';hanhKhach=@(@{hoTen='Nguyen Minh Anh';maGhe=$maGhe;soDienThoai='0900000003'});
  maKhuyenMai=$null
}
Write-Host "Don=$($datVe.maDatVe), Tong tien=$($datVe.tongTien) VND"
Write-Host '[5] Tao giao dich thanh toan ...'
$thanhToan = GuiJson '/api/payments' @{maDatVe=$datVe.maDatVe}
Write-Host '[6] Xac nhan thanh toan SANDBOX (Development only) ...'
$daThanhToan = Invoke-RestMethod -Method POST -Uri "$diaChiApi/api/payments/$($thanhToan.maThanhToan)/sandbox-confirm" -Headers $tieuDeAuth
Write-Host "Ket qua=$($daThanhToan.trangThai)"
Write-Host '[7] Lay ma ve va tai anh QR ...'
$don = Invoke-RestMethod -Uri "$diaChiApi/api/bookings/$($datVe.maDatVe)" -Headers $tieuDeAuth
$maVe = $don.hanhKhach[0].maVe
$tepQr = Join-Path $PSScriptRoot 've-qr-demo.png'
Invoke-WebRequest -Uri "$diaChiApi/api/tickets/$maVe/qr" -Headers $tieuDeAuth -OutFile $tepQr
Write-Host "Ve=$maVe, file QR=$tepQr"
Write-Host "[8] Kiem tra thu dien tu sau vai giay tai http://localhost:8025"
Write-Host 'HOAN THANH: Du lieu da luu trong SQL Server, gui email qua Mailpit.'
