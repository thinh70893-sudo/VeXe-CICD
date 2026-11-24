export function lay(duLieu, ...cacTen) {
  if (!duLieu || typeof duLieu !== 'object') return undefined;
  for (const ten of cacTen) {
    if (duLieu[ten] !== undefined) return duLieu[ten];
    const tim = Object.keys(duLieu).find(k => k.toLowerCase() === String(ten).toLowerCase());
    if (tim) return duLieu[tim];
  }
  return undefined;
}
export function tien(soTien) { return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(Number(soTien || 0)); }
export function docNgayUtc(ngay) {
  if (!ngay) return new Date(NaN);
  if (ngay instanceof Date) return ngay;
  const chuoi = String(ngay).trim();
  // Dapper/SQL Server đôi khi serialize datetime UTC không kèm Z; không để trình duyệt hiểu là giờ địa phương.
  return new Date(/^\d{4}-\d{2}-\d{2}T.*(?:Z|[+-]\d{2}:\d{2})$/i.test(chuoi) || !chuoi.includes('T') ? chuoi : `${chuoi}Z`);
}
export function gioVN(ngay) {
  if (!ngay) return '—';
  const k = docNgayUtc(ngay);
  if (Number.isNaN(k.getTime())) return '—';
  return new Intl.DateTimeFormat('vi-VN', { hour: '2-digit', minute: '2-digit', timeZone: 'Asia/Ho_Chi_Minh' }).format(k);
}
export function ngayVN(ngay) {
  if (!ngay) return '—';
  const k = docNgayUtc(ngay);
  if (Number.isNaN(k.getTime())) return '—';
  return new Intl.DateTimeFormat('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', timeZone: 'Asia/Ho_Chi_Minh' }).format(k);
}
export function ngayVaGioVN(ngay) { return `${gioVN(ngay)}, ${ngayVN(ngay)}`; }
export function ngayMoi() {
  return new Intl.DateTimeFormat('en-CA', { year: 'numeric', month: '2-digit', day: '2-digit', timeZone: 'Asia/Ho_Chi_Minh' }).format(new Date());
}
export function ngayTuongLai(soNgay = 1) {
  const ngay = new Date(Date.now() + soNgay * 86400000);
  return new Intl.DateTimeFormat('en-CA', { year: 'numeric', month: '2-digit', day: '2-digit', timeZone: 'Asia/Ho_Chi_Minh' }).format(ngay);
}
export function nhanTrangThai(trangThai) {
  return ({ PENDING:'Chờ thanh toán', PAID:'Đã thanh toán', EXPIRED:'Hết hạn', CANCELLED:'Đã hủy', REFUNDED:'Đã hoàn tiền', ISSUED:'Đã phát hành', CHECKED_IN:'Đã lên xe', REQUESTED:'Đang xét duyệt', APPROVED:'Đã duyệt', REJECTED:'Từ chối', COMPLETED:'Đã hoàn tất', SENT:'Đã gửi', RETRY:'Đang thử lại', SUCCEEDED:'Thành công', AVAILABLE:'Còn trống', HELD:'Đang giữ', SOLD:'Đã bán', BLOCKED:'Bị khóa' })[trangThai] || trangThai || '—';
}
export function trangThaiMau(trangThai) {
  if (['PAID','ISSUED','COMPLETED','SUCCEEDED','AVAILABLE','SENT'].includes(trangThai)) return 'success';
  if (['CANCELLED','EXPIRED','REJECTED','BLOCKED','SOLD','REFUNDED'].includes(trangThai)) return 'danger';
  return 'warning';
}
export function danhSach(duLieu) { return Array.isArray(duLieu) ? duLieu : []; }
