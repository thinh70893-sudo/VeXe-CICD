import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

import {
    ArrowLeft, ArrowRight, AlertCircle,
    Check, CircleCheckBig, Clock3,
    Info, LockKeyhole, RefreshCw,
    ShieldCheck, Ticket, Users, BusFront
} from 'lucide-react';

import { Api } from '../api/client.js';
import { BaoLoi, KhoiBuoc, TaiTrang, TieuDe, TomTatDon } from '../components/GiaoDien.jsx';
import { useAuth } from '../context/AuthContext.jsx';
import { useToast } from '../context/ToastContext.jsx';
import { lay, tien, gioVN, ngayVN, docNgayUtc } from '../utils/hienThi.js';
const KHOA_GIU = 'busticket:giu-ghe';
function taoHanhKhach(dsGhe, taiKhoan) {return dsGhe.map((ghe,i)=>({maGhe:ghe.maGhe,kyHieuGhe:ghe.kyHieuGhe,hoTen:i===0?(taiKhoan?.hoTen||''):'',soDienThoai:i===0?(taiKhoan?.soDienThoai||''):''}));}
export default function ChonGhe() {
  const { maChuyen } = useParams(); const navigate=useNavigate(); const {token,daDangNhap,taiKhoan}=useAuth(); const toast=useToast();
  const [chuyen, datChuyen]=useState(null); const [ghe, datGhe]=useState([]); const [diemDung, datDiemDung]=useState([]);
  const [dangTai,datDangTai]=useState(true); const [dangXuLy,datDangXuLy]=useState(false); const [loi,datLoi]=useState(''); const [chon,datChon]=useState([]);
  const [giu,datGiu]=useState(null); const [thoiGian,datThoiGian]=useState(0); const [hanhKhach,datHanhKhach]=useState([]);
  const [lienHe,datLienHe]=useState({tenLienHe:'',emailLienHe:'',soDienThoaiLienHe:''}); const [maKhuyenMai,datMaKhuyenMai]=useState('');
  const [dongY,datDongY]=useState(false);
  useEffect(()=>{datLienHe({tenLienHe:taiKhoan?.hoTen||'',emailLienHe:taiKhoan?.email||'',soDienThoaiLienHe:taiKhoan?.soDienThoai||''});},[taiKhoan?.hoTen,taiKhoan?.email,taiKhoan?.soDienThoai]);
  const nap=useCallback(async()=>{ datDangTai(true); datLoi(''); try {
    const [a,b,c]=await Promise.all([Api.chiTietChuyen(maChuyen),Api.ghe(maChuyen),Api.diemDung(maChuyen)]);
    datChuyen(a);datGhe(b||[]);datDiemDung(c||[]);
  } catch(e){ datLoi(e.message); } finally{datDangTai(false)};},[maChuyen]);
  useEffect(()=>{nap()},[nap]);
  useEffect(()=>{ if(!giu?.hetHanUtc)return; const capNhat=()=>{const con=Math.max(0,Math.ceil((docNgayUtc(giu.hetHanUtc).getTime()-Date.now())/1000));datThoiGian(con);if(con===0){datGiu(null);sessionStorage.removeItem(KHOA_GIU);toast('Thời gian giữ ghế đã hết. Hãy chọn ghế lại.','error');nap();}};capNhat();const timer=setInterval(capNhat,1000);return()=>clearInterval(timer); },[giu?.hetHanUtc,nap]);
  useEffect(()=>{ if(!token)return; let saved;try{saved=JSON.parse(sessionStorage.getItem(KHOA_GIU)||'null');}catch{}; if(saved && String(saved.maChuyen)===String(maChuyen) && docNgayUtc(saved.hetHanUtc).getTime()>Date.now()) { Api.xemGiuGhe(token,saved.maGiuGhe).then(x=>{datGiu(x);datChon(saved.danhSachMaGhe||[]);}).catch(()=>sessionStorage.removeItem(KHOA_GIU)); } },[token,maChuyen]);
  const gheDaChon=useMemo(()=>ghe.filter(x=>chon.includes(x.maGhe)),[ghe,chon]);
  const gia=useMemo(()=>gheDaChon.reduce((s,x)=>s+Number(x.giaVe||0),0),[gheDaChon]);
  const nhomTang=useMemo(()=>Object.groupBy?Object.groupBy(ghe,x=>x.tang||1):ghe.reduce((o,x)=>{(o[x.tang||1]??=[]).push(x);return o},{}),[ghe]);
  function doiGhe(ma) {if(giu)return;datChon(c=>c.includes(ma)?c.filter(x=>x!==ma):c.length>=5?c:[...c,ma]);}
  async function giuCho() { if(!daDangNhap){ navigate(`/dang-nhap?troVe=${encodeURIComponent(`/chuyen/${maChuyen}/chon-ghe`)}`);return;} if(!chon.length){toast('Vui lòng chọn ít nhất một ghế.','error');return;} datDangXuLy(true);datLoi('');try{const response=await Api.giuGhe(token,{maChuyen:Number(maChuyen),danhSachMaGhe:chon});datGiu(response);sessionStorage.setItem(KHOA_GIU,JSON.stringify(response));datHanhKhach(taoHanhKhach(gheDaChon,taiKhoan));toast('Đã giữ ghế, vui lòng điền thông tin trước khi hết giờ.');}catch(e){datLoi(e.message);nap();}finally{datDangXuLy(false)} }
  async function huyGiu(){datDangXuLy(true);try{await Api.boGiuGhe(token,giu.maGiuGhe);}catch{}finally{sessionStorage.removeItem(KHOA_GIU);datGiu(null);datChon([]);datDangXuLy(false);nap();}}
  async function taoDon(e){e.preventDefault();if(!dongY){toast('Bạn cần đồng ý thông tin đặt vé.','error');return;}datDangXuLy(true);datLoi('');try{const response=await Api.datVe(token,{maGiuGhe:giu.maGiuGhe,tenLienHe:lienHe.tenLienHe,emailLienHe:lienHe.emailLienHe,soDienThoaiLienHe:lienHe.soDienThoaiLienHe,hanhKhach:hanhKhach.map(({maGhe,hoTen,soDienThoai})=>({maGhe,hoTen,soDienThoai})),maKhuyenMai:maKhuyenMai.trim()||null}); sessionStorage.removeItem(KHOA_GIU);navigate(`/thanh-toan/${response.maDatVe}`,{replace:true});}catch(e){datLoi(e.message);if([409,410].includes(e.maTrangThai)){sessionStorage.removeItem(KHOA_GIU);datGiu(null);nap();}}finally{datDangXuLy(false)}}
  const thoiGianChu=`${String(Math.floor(thoiGian/60)).padStart(2,'0')}:${String(thoiGian%60).padStart(2,'0')}`;
  return <div className="page-section"><div className="container"><button className="back-link" onClick={()=>navigate(-1)} type="button"><ArrowLeft size={17}/> Quay lại danh sách chuyến</button><TieuDe nhan="ĐẶT VÉ XE" tieuDe={giu?'Thông tin hành khách':'Chọn chỗ ngồi của bạn'} moTa="Ghế được cập nhật theo thời gian thực; chỉ giữ chỗ khi bạn xác nhận."/><KhoiBuoc buoc={giu?3:2}/><BaoLoi message={loi} retry={loi&&!giu?nap:null}/>
  {dangTai?<TaiTrang/>:!chuyen?null:<div className="booking-layout"><div className="booking-content">
    {!giu?<div className="card seat-card"><div className="card-heading"><div><h2>Sơ đồ ghế</h2><p>Chọn tối đa 5 ghế trong một lần đặt vé</p></div><button type="button" className="btn btn-soft" onClick={nap}><RefreshCw size={16}/> Cập nhật</button></div>
      <div className="seat-legend"><span><i className="legend-empty"/> Còn trống</span><span><i className="legend-selected"/> Đang chọn</span><span><i className="legend-held"/> Đang giữ</span><span><i className="legend-sold"/> Đã bán / Khóa</span></div>
      {Object.entries(nhomTang).sort(([a],[b])=>Number(a)-Number(b)).map(([tang,danhSach])=><div className="bus-floor" key={tang}><div className="bus-floor-heading"><span>Tầng {tang}</span><span>Đầu xe <BusFront size={18}/></span></div><div className="seats-grid">{danhSach.map((g)=>{const khongChon=g.trangThai!=='AVAILABLE', selected=chon.includes(g.maGhe);return <button key={g.maGhe} disabled={khongChon} aria-label={`Ghế ${g.kyHieuGhe}: ${khongChon?g.trangThai:selected?'đã chọn':'còn trống'}`} aria-pressed={selected} title={`${g.kyHieuGhe} · ${tien(g.giaVe)}`} className={`seat ${selected?'selected':''} ${g.trangThai==='HELD'?'held':''} ${khongChon&&g.trangThai!=='HELD'?'unavailable':''}`} onClick={()=>doiGhe(g.maGhe)} type="button">{selected?<Check size={14}/>:null}{g.kyHieuGhe}</button>})}</div></div>)}
      <div className="tip"><Info size={18}/> Ghế đang giữ ở Redis sẽ tự mở sau 5 phút nếu chưa thanh toán.</div></div>
    : <form className="card passengers-form" onSubmit={taoDon}><div className="card-heading"><div><h2>Thông tin liên hệ</h2><p>Vé QR sẽ được gửi về email tài khoản của bạn.</p></div><span className="hold-timer"><Clock3 size={16}/> {thoiGianChu}</span></div>
      <div className="form-grid"><label className="field"><span>Họ tên liên hệ *</span><input required minLength={2} value={lienHe.tenLienHe} onChange={e=>datLienHe(x=>({...x,tenLienHe:e.target.value}))} placeholder="Nguyễn Văn A"/></label><label className="field"><span>Số điện thoại *</span><input type="tel" required value={lienHe.soDienThoaiLienHe} onChange={e=>datLienHe(x=>({...x,soDienThoaiLienHe:e.target.value}))} placeholder="09xxxxxxxx"/></label><label className="field full"><span>Email nhận vé *</span><input type="email" readOnly value={lienHe.emailLienHe} title="Email phải trùng email tài khoản"/><small>Vì lý do bảo mật, email nhận vé phải trùng email đăng nhập.</small></label></div>
      <div className="form-divider"/><h3>Thông tin hành khách</h3>{hanhKhach.map((kh,i)=><div className="passenger-group" key={kh.maGhe}><div className="passenger-heading"><span className="passenger-no">{i+1}</span><strong>Hành khách {i+1}</strong><span className="seat-pill">Ghế {kh.kyHieuGhe}</span></div><div className="form-grid"><label className="field"><span>Họ và tên *</span><input required value={kh.hoTen} onChange={e=>datHanhKhach(a=>a.map((x,j)=>i===j?{...x,hoTen:e.target.value}:x))} placeholder="Tên hành khách"/></label><label className="field"><span>Số điện thoại</span><input type="tel" value={kh.soDienThoai} onChange={e=>datHanhKhach(a=>a.map((x,j)=>i===j?{...x,soDienThoai:e.target.value}:x))} placeholder="Không bắt buộc"/></label></div></div>)}
      <label className="field promo-field"><span>Mã khuyến mãi</span><input value={maKhuyenMai} onChange={e=>datMaKhuyenMai(e.target.value.toUpperCase())} placeholder="Nhập nếu có"/></label>
      <label className="check-field"><input type="checkbox" checked={dongY} onChange={e=>datDongY(e.target.checked)}/><span>Tôi xác nhận thông tin hành khách chính xác và đồng ý quy định đặt vé của nhà xe.</span></label>
      <div className="action-row"><button type="button" className="btn btn-soft" disabled={dangXuLy} onClick={huyGiu}>Chọn lại ghế</button><button type="submit" className="btn btn-primary" disabled={dangXuLy||thoiGian<=0}>{dangXuLy?'Đang xử lý...':'Tiếp tục thanh toán'}<ArrowRight size={18}/></button></div>
    </form>}
  </div>
  <aside className="booking-sidebar"><TomTatDon gioDi={lay(chuyen,'khoiHanhUtc','departureAtUtc')} gioDen={lay(chuyen,'denNoiUtc','arrivalAtUtc')} benDi={lay(chuyen,'benDi','originStation')} benDen={lay(chuyen,'benDen','destinationStation')} ghe={gheDaChon.map(x=>x.kyHieuGhe)} gia={gia}/>{!giu?<button type="button" className="btn btn-primary btn-block" disabled={!chon.length||dangXuLy} onClick={giuCho}>{dangXuLy?'Đang giữ ghế...':`Giữ ${chon.length||''} ghế`}<ArrowRight size={18}/></button>:<div className="secure-info"><LockKeyhole size={17}/> Ghế của bạn đang được giữ tạm thời. Vui lòng hoàn tất trước khi đồng hồ về 00:00.</div>}{!giu&&<div className="sidebar-note"><ShieldCheck size={20}/><div><b>An tâm đặt chỗ</b><p>Chỉ thanh toán sau khi xác nhận thông tin. Trạng thái ghế sẽ được kiểm tra lần nữa.</p></div></div>}</aside>
  </div>}
  </div></div>;
}

