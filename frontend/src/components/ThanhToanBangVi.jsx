import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Api } from '../api/client.js';

const vnd = x => `${Number(x || 0).toLocaleString('vi-VN')} đ`;

export default function ThanhToanBangVi({ token, maDatVe, tongTien }) {
  const navigate = useNavigate();
  const [soDu, datSoDu] = useState(null);
  const [dangGui, datDangGui] = useState(false);
  const [loi, datLoi] = useState('');
  const maLanYeuCau = useRef(crypto.randomUUID());

  useEffect(() => {
    Api.soDuVi(token).then(x => datSoDu(Number(x.soDu)))
      .catch(e => datLoi(e.message));
  }, [token]);

  async function thanhToan() {
    if (dangGui || soDu === null || soDu < Number(tongTien)) return;
    if (!window.confirm(`Dùng ${vnd(tongTien)} trong ví BusGo để thanh toán đơn này?`)) return;
    datDangGui(true);
    datLoi('');
    try {
      await Api.thanhToanBangVi(token, maDatVe, maLanYeuCau.current);
      navigate(`/don/${maDatVe}?success=1`, { replace: true });
    } catch (e) {
      // Khong doi MaLanYeuCau khi ket noi loi, de backend chong tru tien 2 lan.
      datLoi(e.message);
    } finally { datDangGui(false); }
  }

  return <div className="card">
    <h3>Thanh toán bằng số dư ví BusGo</h3>
    <p>Số dư khả dụng: <b>{soDu === null ? 'Đang tải...' : vnd(soDu)}</b></p>
    <p>Số tiền đơn hàng: <b>{vnd(tongTien)}</b></p>
    {loi && <div className="error-banner">{loi}</div>}
    <button type="button" className="btn btn-primary btn-block"
      disabled={dangGui || soDu === null || soDu < Number(tongTien)} onClick={thanhToan}>
      {dangGui ? 'Đang thanh toán...' : 'Thanh toán bằng ví BusGo'}
    </button>
    {soDu !== null && soDu < Number(tongTien) && <small>
      Số dư chưa đủ. Bạn có thể dùng cách thanh toán Sandbox hiện có.
    </small>}
  </div>;
}
