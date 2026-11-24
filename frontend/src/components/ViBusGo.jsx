import { useEffect, useState } from 'react';
import { Api } from '../api/client.js';

const vnd = (x) => `${Number(x || 0).toLocaleString('vi-VN')} đ`;

export default function ViBusGo({ token }) {
  const [soDu, datSoDu] = useState(null);
  const [lichSu, datLichSu] = useState([]);
  const [loi, datLoi] = useState('');

  async function nap() {
    try {
      const [vi, ds] = await Promise.all([Api.soDuVi(token), Api.lichSuVi(token)]);
      datSoDu(Number(vi.soDu));
      datLichSu(ds);
      datLoi('');
    } catch (e) { datLoi(e.message); }
  }
  useEffect(() => { nap(); }, [token]);

  return <section className="card">
    <h2>Ví BusGo</h2>
    <p>Số dư chỉ dùng để thanh toán vé xe; không hỗ trợ rút tiền.</p>
    {loi && <div className="error-banner">{loi}</div>}
    <div className="price-total"><span>Số dư khả dụng</span>
      <strong>{soDu === null ? 'Đang tải...' : vnd(soDu)}</strong>
    </div>
    <button type="button" onClick={nap} className="btn btn-soft">Làm mới số dư</button>
    <h3 style={{ marginTop: 20 }}>Lịch sử giao dịch</h3>
    {lichSu.length === 0 && <p>Chưa có giao dịch ví.</p>}
    {lichSu.map(gd => <div key={gd.maGiaoDich} className="price-detail">
      <span>{gd.loaiGiaoDich === 'HOAN_VE' ? 'Hoàn tiền vé' : 'Thanh toán vé'} · {new Date(gd.ngayTaoUtc + (gd.ngayTaoUtc.endsWith('Z') ? '' : 'Z')).toLocaleString('vi-VN')}</span>
      <b style={{ color: gd.loaiGiaoDich === 'HOAN_VE' ? '#168153' : '#be5035' }}>
        {gd.loaiGiaoDich === 'HOAN_VE' ? '+' : '-'}{vnd(gd.soTien)}
      </b>
    </div>)}
  </section>;
}
