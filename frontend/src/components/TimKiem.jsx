import { useEffect, useState } from 'react';
import { ArrowLeftRight, CalendarDays, MapPin, Search } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { Api } from '../api/client.js';
import { ngayMoi } from '../utils/hienThi.js';
import { BaoLoi } from './GiaoDien.jsx';
export default function TimKiem({ banDau = {}, trongHero = false }) {
  const navigate = useNavigate();
  const [diaDiem, datDiaDiem] = useState([]);
  const [loi, datLoi] = useState('');
  const [boLoc, datBoLoc] = useState({ diemDiId: banDau.diemDiId || '', diemDenId: banDau.diemDenId || '', ngay: banDau.ngay || ngayMoi() });
  useEffect(() => { let ok=true; Api.thanhPho().then(x=>{if(ok)datDiaDiem(x)}).catch(e=>{if(ok)datLoi(e.message)}); return ()=>{ok=false}; },[]);
  useEffect(()=>{ datBoLoc({ diemDiId: banDau.diemDiId || '', diemDenId: banDau.diemDenId || '', ngay: banDau.ngay || ngayMoi() }); },[banDau.diemDiId,banDau.diemDenId,banDau.ngay]);
  function tim(e) { e.preventDefault(); if (boLoc.diemDiId && boLoc.diemDenId && boLoc.diemDiId===boLoc.diemDenId) {datLoi('Điểm đi và điểm đến phải khác nhau.'); return;} datLoi(''); const params=new URLSearchParams(Object.entries(boLoc).filter(([,v])=>v!=='')); navigate(`/tim-chuyen?${params.toString()}`); }
  const chon = (field, label, bieuTuong) => <label className="search-field"><span className="search-field-icon">{bieuTuong}</span><span className="search-field-content"><span className="field-caption">{label}</span><select aria-label={label} value={boLoc[field]} onChange={e=>datBoLoc(o=>({...o,[field]:e.target.value}))}><option value="">Tất cả điểm {field==='diemDiId'?'đi':'đến'}</option>{diaDiem.map(d=><option key={d.maThanhPho} value={d.maThanhPho}>{d.tenThanhPho}</option>)}</select></span></label>;
  return <div className={`search-box ${trongHero?'on-hero':''}`}>
    <form onSubmit={tim} className="search-form">
      <div className="city-search-pair">
        {chon('diemDiId','Điểm khởi hành', <MapPin size={22}/>)}
        <button type="button" className="swap-btn" title="Đổi chiều tuyến" aria-label="Đổi điểm đi và đến" onClick={()=>datBoLoc(o=>({...o,diemDiId:o.diemDenId,diemDenId:o.diemDiId}))}><ArrowLeftRight size={18}/></button>
        {chon('diemDenId','Điểm đến', <MapPin size={22}/>)}
      </div>
      <label className="search-field date-field"><span className="search-field-icon"><CalendarDays size={22}/></span><span className="search-field-content"><span className="field-caption">Ngày khởi hành</span><input type="date" min={ngayMoi()} value={boLoc.ngay} onChange={e=>datBoLoc(o=>({...o,ngay:e.target.value}))} required/></span></label>
      <button className="btn btn-primary search-submit" type="submit"><Search size={19}/> Tìm chuyến</button>
    </form>
    {loi && <div className="search-error"><BaoLoi message={loi}/></div>}
  </div>;
}
