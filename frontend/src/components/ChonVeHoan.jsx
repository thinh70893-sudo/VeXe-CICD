import { useEffect, useMemo, useRef, useState } from 'react';
import { Api } from '../api/client.js';

const vnd = (soTien) => `${Number(soTien || 0).toLocaleString('vi-VN')} đ`;

export default function ChonVeHoan({ token, maDatVe, onHoanXong }) {
  const [danhSach, datDanhSach] = useState([]);
  const [daChon, datDaChon] = useState([]);
  const [lyDo, datLyDo] = useState('');
  const [dangTai, datDangTai] = useState(true);
  const [dangGui, datDangGui] = useState(false);
  const [loi, datLoi] = useState('');
  const [ketQua, datKetQua] = useState(null);
  const maLanYeuCau = useRef(crypto.randomUUID());

  const nap = async () => {
    datDangTai(true);
    datLoi('');
    try { datDanhSach(await Api.duKienHoanVe(token, maDatVe)); }
    catch (e) { datLoi(e.message); }
    finally { datDangTai(false); }
  };

  useEffect(() => {
    datDaChon([]);
    maLanYeuCau.current = crypto.randomUUID();
    nap();
  }, [token, maDatVe]);

  const tienHoan = useMemo(() => danhSach
    .filter(ve => daChon.includes(ve.maVe))
    .reduce((tong, ve) => tong + Number(ve.soTienHoan || 0), 0), [danhSach, daChon]);

  function chonVe(maVe, duocChon) {
    datDaChon(ds => duocChon ? [...ds, maVe] : ds.filter(x => x !== maVe));
    maLanYeuCau.current = crypto.randomUUID();
  }

  async function hoan(e) {
    e.preventDefault();
    if (dangGui || daChon.length === 0 || lyDo.trim().length < 3) return;
    if (!window.confirm(`Xác nhận hoàn ${daChon.length} vé, dự kiến ${vnd(tienHoan)} vào ví BusGo?`)) return;
    datDangGui(true);
    datLoi('');
    try {
      const kq = await Api.hoanNhieuVe(token, maDatVe, {
        danhSachMaVe: daChon,
        lyDo: lyDo.trim(),
        maLanYeuCau: maLanYeuCau.current
      });
      datKetQua(kq);
      datDaChon([]);
      datLyDo('');
      maLanYeuCau.current = crypto.randomUUID();
      await nap();
      onHoanXong?.();
    } catch (e) {
      // Neu mang chập chờn, GIU NGUYEN maLanYeuCau de retry an toan.
      datLoi(e.message || 'Không xử lý được yêu cầu hoàn vé.');
    } finally { datDangGui(false); }
  }

  return (
    <form className="card refund-form" onSubmit={hoan}>
      <h3>Chọn vé cần hoàn</h3>
      <p>Chỉ hoàn khi còn hơn 60 phút trước giờ khởi hành. Tiền hoàn được cộng vào ví BusGo, không rút ra ngân hàng.</p>
      {dangTai && <p>Đang kiểm tra các vé và số tiền hoàn...</p>}
      {loi && <div className="error-banner">{loi}</div>}
      {ketQua && <div className="success-banner">
        Đã hoàn {ketQua.soVeDaHoan} vé, cộng {vnd(ketQua.soTienHoan)} vào ví. Số dư hiện tại: {vnd(ketQua.soDuVi)}.
      </div>}
      {danhSach.map(ve => (
        <label key={ve.maVe} className="field" style={{ display: 'flex', flexDirection: 'row', alignItems: 'center', gap: 12, marginBottom: 8 }}>
          <input type="checkbox" style={{ width: 18, height: 18, flexShrink: 0 }}
            disabled={!ve.duDieuKien || dangGui}
            checked={daChon.includes(ve.maVe)}
            onChange={e => chonVe(ve.maVe, e.target.checked)} />
          <span style={{ flex: 1 }}>
            <strong>{ve.hoTen} – Ghế {ve.maGhe}</strong>
            <small style={{ display: 'block' }}>
              {ve.duDieuKien ? 'Đủ điều kiện hoàn' : `Không thể hoàn – ${ve.trangThaiVe}`}
            </small>
          </span>
          <strong>{vnd(ve.soTienHoan)}</strong>
        </label>
      ))}
      <div className="price-total"><span>Tổng tiền hoàn ({daChon.length} vé)</span><strong>{vnd(tienHoan)}</strong></div>
      <label className="field"><span>Lý do hoàn vé *</span>
        <textarea required minLength={3} maxLength={300} rows={3}
          value={lyDo} onChange={e => datLyDo(e.target.value)}
          placeholder="Ví dụ: Thay đổi kế hoạch di chuyển" />
      </label>
      <button type="submit" className="btn btn-primary btn-block"
        disabled={dangGui || dangTai || daChon.length === 0 || lyDo.trim().length < 3}>
        {dangGui ? 'Đang hoàn vé...' : `Xác nhận hoàn ${daChon.length} vé vào ví`}
      </button>
    </form>
  );
}
