import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { ArrowRight, Filter, Search, BusFront } from 'lucide-react';
import TimKiem from '../components/TimKiem.jsx';
import { BaoLoi, TaiTrang, TheChuyen, TieuDe, TrangThongBao } from '../components/GiaoDien.jsx';
import { Api } from '../api/client.js';
import { docNgayUtc } from '../utils/hienThi.js';
export default function TimChuyen() {
  const [thamSo] = useSearchParams(); const chuoi = thamSo.toString();
  const [ds, datDs] = useState([]); const [loi, datLoi] = useState(''); const [dangTai, datDangTai] = useState(true);
  const [sapXep, datSapXep] = useState('gio');
  const banDau={diemDiId:thamSo.get('diemDiId')||'',diemDenId:thamSo.get('diemDenId')||'',ngay:thamSo.get('ngay')||''};
  useEffect(()=>{let on=true; const abort=new AbortController(); datDangTai(true);datLoi('');
    Api.chuyen({ ...banDau, soDong:100 },abort.signal).then(data=>{if(on)datDs(data||[])}).catch(e=>{if(on && e.name!=='AbortError')datLoi(e.message)}).finally(()=>{if(on)datDangTai(false)});
    return ()=>{on=false;abort.abort()};
  },[chuoi]);
  const daXep=[...ds].sort((a,b)=>sapXep==='gia'?Number(a.giaVe)-Number(b.giaVe):docNgayUtc(a.khoiHanhUtc)-docNgayUtc(b.khoiHanhUtc));
  return <div className="page-section"><div className="container"><div className="page-breadcrumb">Trang chủ <span>/</span> Tìm chuyến xe</div><TieuDe nhan="ĐẶT VÉ XE KHÁCH" tieuDe="Tìm chuyến xe phù hợp" moTa="Lựa chọn thời gian và chỗ ngồi theo nhu cầu của bạn."/><TimKiem banDau={banDau}/>
  <div className="results-head"><div><strong>{dangTai?'Đang tìm chuyến...':`${daXep.length} chuyến xe phù hợp`}</strong><p>Chọn chuyến để xem sơ đồ ghế</p></div><label className="sort-select"><Filter size={17}/><span>Sắp xếp</span><select value={sapXep} onChange={e=>datSapXep(e.target.value)}><option value="gio">Khởi hành sớm</option><option value="gia">Giá thấp trước</option></select></label></div>
  <BaoLoi message={loi}/>{dangTai?<TaiTrang message="Đang tải lịch trình..."/>:daXep.length===0&&!loi?<TrangThongBao icon={BusFront} tieuDe="Chưa có chuyến phù hợp" moTa="Hãy thử thay đổi ngày đi hoặc chọn tuyến khác. Các chuyến trong database có thể chưa được lập cho ngày này."/>:<div className="results-grid">{daXep.map(x=><TheChuyen key={x.maChuyen} chuyen={x}/>)}</div>}
  </div></div>;
}
