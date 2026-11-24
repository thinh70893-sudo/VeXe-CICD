import { useEffect, useState } from 'react';
import { Api } from '../api/client.js';

export default function DanhSachHoanVeMoi({ token }) {
  const [ds, datDs] = useState([]);
  const [loi, datLoi] = useState('');
  async function nap() {
    try { datDs(await Api.quanTriHoanVeMoi(token)); datLoi(''); }
    catch(e) { datLoi(e.message); }
  }
  useEffect(() => { nap(); }, [token]);
  return <div className="card">
    <div className="card-heading"><h3>Hoàn vé vào ví BusGo</h3>
      <button className="btn btn-soft" type="button" onClick={nap}>Làm mới</button>
    </div>
    <p className="muted">Danh sách hoàn tự động theo từng vé. Chỉ để theo dõi; không thực hiện hoàn tiền lần hai.</p>
    {loi && <p className="error-banner">{loi}</p>}
    <div className="table-scroll"><table className="data-table"><thead><tr>
      <th>Mã đợt</th><th>Mã đơn</th><th>Mã người dùng</th><th>Các vé đã hoàn</th><th>Số vé</th><th>Số tiền hoàn</th><th>Trạng thái</th>
    </tr></thead><tbody>{ds.map(x=><tr key={x.maDotHoan}>
      <td>{x.maDotHoan}</td><td>{x.maDatVe}</td><td>{x.maNguoiDung}</td>
      <td>{x.danhSachVe}</td><td>{x.soVe}</td>
      <td>{Number(x.soTienHoan).toLocaleString('vi-VN')}đ</td><td>Đã hoàn vào ví</td>
    </tr>)}</tbody></table></div>
  </div>;
}
