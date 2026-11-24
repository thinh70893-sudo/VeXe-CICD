const gocApi = (import.meta.env.VITE_API_URL || '').replace(/\/$/, '');
export class LoiApi extends Error {
  constructor(thongBao, maTrangThai = 0, duLieu = null) {
    super(thongBao);
    this.name = 'LoiApi';
    this.maTrangThai = maTrangThai;
    this.duLieu = duLieu;
  }
}
function docThongBao(loi) {
  if (!loi || typeof loi !== 'object') return '';
  if (typeof loi.loi === 'string') return loi.loi;
  if (typeof loi.thongBao === 'string') return loi.thongBao;
  if (typeof loi.title === 'string') return loi.title;
  if (typeof loi.message === 'string') return loi.message;
  if (loi.errors && typeof loi.errors === 'object') return Object.values(loi.errors).flat().join('. ');
  return '';
}
export async function yeuCau(duongDan, { method = 'GET', body, token, signal, headers = {}, raw = false } = {}) {
  let traLoi;
  try {
    traLoi = await fetch(`${gocApi}${duongDan}`, {
      method,
      headers: { ...(body !== undefined ? { 'Content-Type': 'application/json' } : {}), ...(token ? { Authorization: `Bearer ${token}` } : {}), ...headers },
      ...(body !== undefined ? { body: JSON.stringify(body) } : {}),
      signal
    });
  } catch (loi) {
    if (loi?.name === 'AbortError') throw loi;
    throw new LoiApi('Không kết nối được tới Backend. Hãy kiểm tra API localhost:5080 và SQL Server/Redis.', 0);
  }
  if (raw && traLoi.ok) return traLoi;
  const kieu = traLoi.headers.get('content-type') || '';
  let duLieu = null;
  if (traLoi.status !== 204) {
    if (kieu.includes('json')) duLieu = await traLoi.json().catch(() => null);
    else duLieu = await traLoi.text().catch(() => '');
  }
  if (!traLoi.ok) {
    const thongBao = docThongBao(duLieu) || (typeof duLieu === 'string' ? duLieu : '') || `Yêu cầu thất bại (HTTP ${traLoi.status})`;
    throw new LoiApi(thongBao, traLoi.status, duLieu);
  }
  return duLieu;
}
export function taoTruyVan(thamSo) {
  const boLoc = Object.entries(thamSo).filter(([, giaTri]) => giaTri !== '' && giaTri !== null && giaTri !== undefined);
  return new URLSearchParams(boLoc.map(([k, v]) => [k, String(v)])).toString();
}
export const Api = {
  soDuVi: (token) => yeuCau('/api/vi-busgo/so-du', { token }),
  lichSuVi: (token) => yeuCau('/api/vi-busgo/lich-su', { token }),
  duKienHoanVe: (token, maDatVe) => yeuCau(`/api/vi-busgo/hoan-ve/du-kien/${maDatVe}`, { token }),
  hoanNhieuVe: (token, maDatVe, body) => yeuCau(`/api/vi-busgo/hoan-ve/theo-don/${maDatVe}`, { token, method: 'POST', body }),
  lichSuHoanVeMoi: (token) => yeuCau('/api/vi-busgo/hoan-ve/lich-su', { token }),
  quanTriHoanVeMoi: (token) => yeuCau('/api/vi-busgo/hoan-ve/quan-tri', { token }),
  thanhToanBangVi: (token, maDatVe, maLanYeuCau) => yeuCau(`/api/vi-busgo/thanh-toan/${maDatVe}`, { token, method: 'POST', body: { maLanYeuCau } }),
  thanhPho: (signal) => yeuCau('/api/catalog/cities', { signal }),
  benXe: (maThanhPho) => yeuCau(`/api/catalog/stations?${taoTruyVan({ maThanhPho })}`),
  tuyen: () => yeuCau('/api/catalog/routes'),
  khuyenMai: () => yeuCau('/api/catalog/promotions'),
  chuyen: (loc, signal) => yeuCau(`/api/trips?${taoTruyVan(loc)}`, { signal }),
  chiTietChuyen: (maChuyen) => yeuCau(`/api/trips/${maChuyen}`),
  ghe: (maChuyen) => yeuCau(`/api/trips/${maChuyen}/seats`),
  diemDung: (maChuyen) => yeuCau(`/api/trips/${maChuyen}/stops`),
  dangNhap: (body) => yeuCau('/api/auth/login', { method: 'POST', body }),
  dangKy: (body) => yeuCau('/api/auth/register', { method: 'POST', body }),
  toi: (token) => yeuCau('/api/auth/me', { token }),
  suaHoSo: (token, body) => yeuCau('/api/auth/profile', { method: 'PUT', token, body }),
  doiMatKhau: (token, body) => yeuCau('/api/auth/password', { method: 'PUT', token, body }),
  giuGhe: (token, body) => yeuCau('/api/seat-holds', { method: 'POST', token, body }),
  xemGiuGhe: (token, maGiuGhe) => yeuCau(`/api/seat-holds/${maGiuGhe}`, { token }),
  boGiuGhe: (token, maGiuGhe) => yeuCau(`/api/seat-holds/${maGiuGhe}`, { method: 'DELETE', token }),
  datVe: (token, body) => yeuCau('/api/bookings', { method: 'POST', token, body }),
  donCuaToi: (token) => yeuCau('/api/bookings/me', { token }),
  don: (token, maDatVe) => yeuCau(`/api/bookings/${maDatVe}`, { token }),
  huyDon: (token, maDatVe) => yeuCau(`/api/bookings/${maDatVe}/cancel`, { token, method: 'POST' }),
  taoThanhToan: (token, maDatVe) => yeuCau('/api/payments', { method: 'POST', token, body: { maDatVe } }),
  xemThanhToan: (token, maDatVe) => yeuCau(`/api/payments/by-booking/${maDatVe}`, { token }),
  thanhToanThu: (token, maThanhToan) => yeuCau(`/api/payments/${maThanhToan}/sandbox-confirm`, { method: 'POST', token }),
  ve: (token, maVe) => yeuCau(`/api/tickets/${maVe}`, { token }),
  anhQr: (token, maVe) => yeuCau(`/api/tickets/${maVe}/qr`, { token, raw: true }),
  guiLaiEmail: (token, maVe) => yeuCau(`/api/tickets/${maVe}/resend-email`, { token, method: 'POST' }),
  hoanTien: (token, maDatVe, lyDo) => yeuCau(`/api/refunds/by-booking/${maDatVe}`, { token, method: 'POST', body: { lyDo } }),
  hoanTienCuaToi: (token) => yeuCau('/api/refunds/me', { token }),
  danhGia: (token, body) => yeuCau('/api/reviews', { token, method: 'POST', body }),
  danhGiaChuyen: (maChuyen) => yeuCau(`/api/reviews/by-trip/${maChuyen}`),
  xacMinhVe: (token, maQr) => yeuCau('/api/tickets/verify', { token, method: 'POST', body: { maQr } }),
  soatVe: (token, maQr) => yeuCau('/api/tickets/check-in', { token, method: 'POST', body: { maQr } }),
  quanTri: (token, taiNguyen) => yeuCau(`/api/admin/crud/${taiNguyen}`, { token }),
  taoQuanTri: (token, taiNguyen, body) => yeuCau(`/api/admin/crud/${taiNguyen}`, { token, body, method: 'POST' }),
  suaQuanTri: (token, taiNguyen, id, body) => yeuCau(`/api/admin/crud/${taiNguyen}/${id}`, { token, body, method: 'PATCH' }),
  xoaQuanTri: (token, taiNguyen, id) => yeuCau(`/api/admin/crud/${taiNguyen}/${id}`, { token, method: 'DELETE' }),
  adminDon: (token) => yeuCau('/api/admin/bookings', { token }),
  adminDoanhThu: (token) => yeuCau('/api/admin/revenue', { token }),
  adminNguoiDung: (token) => yeuCau('/api/admin/users', { token }),
  adminEmail: (token) => yeuCau('/api/admin/email-logs', { token }),
  adminHoan: (token) => yeuCau('/api/refunds', { token }),
  duyetHoan: (token, id, trangThai) => yeuCau(`/api/refunds/${id}/status`, { token, method: 'PATCH', body: { trangThai } }),
  khoaNguoiDung: (token, id, hoatDong) => yeuCau(`/api/admin/users/${id}/active?${taoTruyVan({ hoatDong })}`, { token, method: 'PATCH' }),
  doiVaiTro: (token, id, vaiTro) => yeuCau(`/api/admin/users/${id}/role?${taoTruyVan({ vaiTro })}`, { token, method: 'PATCH' }),
  taoChuyen: (token, body) => yeuCau('/api/trips', { token, method: 'POST', body }),
  suaChuyen: (token, id, body) => yeuCau(`/api/trips/${id}`, { token, method: 'PUT', body }),
  huyChuyen: (token, id) => yeuCau(`/api/trips/${id}`, { token, method: 'DELETE' }),
  khoaGhe: (token, maChuyen, maGhe, khoa) => yeuCau(`/api/trips/${maChuyen}/seats/${maGhe}/block?${taoTruyVan({ khoa })}`, { token, method: 'PATCH' })
};

