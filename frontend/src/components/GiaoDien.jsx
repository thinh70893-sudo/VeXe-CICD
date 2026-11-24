import { Link, NavLink, Navigate, Outlet, useLocation } from 'react-router-dom';
import { ArrowRight, BusFront, CalendarDays, CheckCircle2, ChevronRight, ClipboardList, CircleHelp, Headphones, Home, LayoutDashboard, LogIn, LogOut, Menu, Search, ShieldCheck, Ticket, UserRound, X, AlertCircle, LoaderCircle, MapPin, Clock3, CreditCard } from 'lucide-react';
import { useState } from 'react';
import { useAuth } from '../context/AuthContext.jsx';
import { gioVN, ngayVN, nhanTrangThai, tien, trangThaiMau, lay } from '../utils/hienThi.js';

export function Logo() { return <Link to="/" className="logo" aria-label="BusGo - Trang chủ"><span className="logo-mark"><BusFront size={21} strokeWidth={2.5}/></span><span>bus<span className="logo-accent">go</span><span className="logo-dot">.</span></span></Link>; }
const dieuHuong = [{ toi:'/', nhan:'Trang chủ', icon:Home },{toi:'/tim-chuyen', nhan:'Tìm chuyến',icon:Search},{toi:'/don-cua-toi',nhan:'Vé của tôi',icon:Ticket}];
export function ThanhDieuHuong() {
  const { daDangNhap, taiKhoan, vaiTro, dangXuat } = useAuth();
  const [mo, datMo] = useState(false);
  const viTri = useLocation();
  const ten = taiKhoan?.hoTen || 'Tài khoản';
  const menus = [...dieuHuong, ...(vaiTro === 'ADMIN' || vaiTro === 'STAFF' ? [{toi:'/quan-tri',nhan:'Quản lý',icon:LayoutDashboard}] : [])];
  return <>
    <header className="site-header"><div className="container nav-wrap">
      <Logo />
      <nav className="nav-links" aria-label="Điều hướng chính">{menus.map(m => <NavLink key={m.toi} end={m.toi === '/'} to={m.toi}>{m.nhan}</NavLink>)}</nav>
      <div className="nav-actions">
        {daDangNhap ? <><Link className="account-link" to="/tai-khoan"><span className="avatar-small">{ten.slice(0,1).toUpperCase()}</span><span className="hide-sm">{ten.split(' ').slice(-2).join(' ')}</span></Link><button type="button" onClick={dangXuat} className="icon-button hide-sm" aria-label="Đăng xuất" title="Đăng xuất"><LogOut size={19}/></button></>
        : <><Link to="/dang-nhap" className="btn btn-ghost hide-xs">Đăng nhập</Link><Link to="/dang-ky" className="btn btn-primary btn-small">Đăng ký <ArrowRight size={16}/></Link></>}
        <button className="icon-button mobile-menu-toggle" aria-label="Mở menu" aria-expanded={mo} onClick={()=>datMo(x=>!x)}>{mo?<X size={22}/>:<Menu size={22}/>}</button>
      </div>
    </div></header>
    {mo && <div className="mobile-menu"><nav>{menus.map(m => <NavLink end={m.toi === '/'} key={m.toi} to={m.toi} onClick={()=>datMo(false)}><m.icon size={18}/>{m.nhan}</NavLink>)}{daDangNhap && <NavLink to="/tai-khoan" onClick={()=>datMo(false)}><UserRound size={18}/>Tài khoản</NavLink>}{!daDangNhap && <NavLink to="/dang-nhap" onClick={()=>datMo(false)}><LogIn size={18}/>Đăng nhập</NavLink>}</nav></div>}
    <div className="mobile-bottom-nav">{menus.slice(0,4).map(m => <NavLink key={m.toi} to={m.toi} end={m.toi === '/'}><m.icon size={20}/><span>{m.nhan}</span></NavLink>)}</div>
  </>;
}
export function ChanTrang() { return <footer className="footer"><div className="container footer-inner"><div><Logo/><p>Đặt vé xe thật dễ dàng. Mọi hành trình, một nơi.</p><span>Đồ án Lập trình Web · Bản mô phỏng</span></div><div className="footer-links"><Link to="/tim-chuyen">Tìm chuyến xe</Link><Link to="/don-cua-toi">Vé của tôi</Link><Link to="/tai-khoan">Tài khoản</Link></div><div className="footer-support"><div><ShieldCheck size={18}/> Thanh toán sandbox an toàn cho demo</div><div><Headphones size={18}/> Hỗ trợ qua email của dự án</div></div></div></footer>; }
export function BoCuc() { const v = useLocation(); return <div className="app-shell"><ThanhDieuHuong/><main key={v.pathname} className="site-main"><Outlet/></main><ChanTrang/></div>; }
export function BaoVe({ vaiTroChoPhep }) {
  const { daDangNhap, dangTai, vaiTro } = useAuth(); const viTri = useLocation();
  if (dangTai) return <TaiTrang message="Đang kiểm tra tài khoản..."/>;
  if (!daDangNhap) return <Navigate to={`/dang-nhap?troVe=${encodeURIComponent(viTri.pathname + viTri.search)}`} replace/>;
  if (vaiTroChoPhep && !vaiTroChoPhep.includes(vaiTro)) return <TrangThongBao tieuDe="Bạn không có quyền truy cập" moTa="Vui lòng sử dụng tài khoản được phân quyền." icon={ShieldCheck} />;
  return <Outlet/>;
}
export function TaiTrang({ message = 'Đang tải dữ liệu...' }) { return <div className="load-state" role="status"><LoaderCircle className="spin" size={28}/><span>{message}</span></div>; }
export function TrangThongBao({ tieuDe='Chưa có dữ liệu', moTa, icon: BieuTuong=CircleHelp, link, nhanLink='Quay về trang chủ' }) { return <div className="empty-state"><span className="empty-icon"><BieuTuong size={28}/></span><h3>{tieuDe}</h3>{moTa && <p>{moTa}</p>}{link && <Link className="btn btn-primary" to={link}>{nhanLink}<ArrowRight size={17}/></Link>}</div>; }
export function BaoLoi({ message, retry }) { if (!message) return null; return <div className="error-banner" role="alert"><AlertCircle size={19}/><span>{message}</span>{retry && <button type="button" onClick={retry}>Thử lại</button>}</div>; }
export function NhanTrangThai({ giaTri }) { return <span className={`status status-${trangThaiMau(giaTri)}`}><span className="status-dot"/>{nhanTrangThai(giaTri)}</span>; }
export function TieuDe({ nhan, tieuDe, moTa, children }) {return <div className="section-title"><div>{nhan && <div className="eyebrow">{nhan}</div>}<h1>{tieuDe}</h1>{moTa && <p>{moTa}</p>}</div>{children}</div>;}
export function NutTai({ dangTai, children, ...props }) {return <button type="button" className="btn btn-primary" disabled={dangTai || props.disabled} {...props}>{dangTai?<LoaderCircle className="spin" size={18}/>:children}</button>;}
export function TheChuyen({ chuyen, hanhDong, showButton = true }) {
  const ma = lay(chuyen, 'maChuyen', 'tripId');
  const giaVe = lay(chuyen,'giaVe','priceVnd');
  const di = lay(chuyen,'thanhPhoDi','originCity') || lay(chuyen,'benDi','originStation');
  const den = lay(chuyen,'thanhPhoDen','destinationCity') || lay(chuyen,'benDen','destinationStation');
  const gioDi = lay(chuyen,'khoiHanhUtc','departureAtUtc');
  const gioDen = lay(chuyen,'denNoiUtc','arrivalAtUtc');
  const gheTrong = lay(chuyen,'soGheDatabaseTrong','dbAvailableSeats');
  return <article className="trip-card">
    <div className="trip-main"><div className="trip-times"><strong>{gioVN(gioDi)}</strong><span><span className="trip-line"/></span><strong>{gioVN(gioDen)}</strong></div>
    <div className="trip-locations"><div><b>{di}</b><small>{lay(chuyen,'benDi','originStation')}</small></div><div><b>{den}</b><small>{lay(chuyen,'benDen','destinationStation')}</small></div></div></div>
    <div className="trip-meta"><span><CalendarDays size={16}/>{ngayVN(gioDi)}</span><span><BusFront size={16}/>{lay(chuyen,'loaiXe','busType') || 'Xe khách'}</span>{gheTrong !== undefined && <span><CheckCircle2 size={16}/>{gheTrong} ghế trong SQL</span>}</div>
    <div className="trip-footer"><div><small>Giá từ</small><strong>{tien(giaVe)} <em>/ ghế</em></strong></div>{showButton && <Link className="btn btn-primary" to={hanhDong || `/chuyen/${ma}/chon-ghe`}>Chọn ghế <ChevronRight size={17}/></Link>}</div>
  </article>;
}
export function KhoiBuoc({ buoc = 1 }) { const ten = ['Chọn chuyến','Chọn ghế','Điền thông tin','Thanh toán']; return <ol className="steps">{ten.map((x,i)=><li key={x} className={buoc === i+1?'step active':buoc>i+1?'step done':'step'}><span>{buoc>i+1?<CheckCircle2 size={16}/>:i+1}</span><b>{x}</b></li>)}</ol>; }
export function HangThongTin({ icon: BieuTuong, nhan, giaTri }) {return <div className="info-row">{BieuTuong&&<BieuTuong size={18}/>}<span>{nhan}</span><strong>{giaTri}</strong></div>;}
export function TomTatDon({ gioDi, gioDen, benDi, benDen, ghe, gia, maDon }) {return <div className="card summary-card"><div className="summary-title"><Ticket size={19}/>Tóm tắt hành trình</div>{maDon && <HangThongTin nhan="Mã đơn" giaTri={maDon}/>}<div className="summary-route"><div><span className="station-pin"/><strong>{benDi||'Điểm đi'}</strong><small>{gioDi?`${gioVN(gioDi)} · ${ngayVN(gioDi)}`:''}</small></div><div className="summary-route-line"/><div><span className="station-pin end"/><strong>{benDen||'Điểm đến'}</strong><small>{gioDen?`${gioVN(gioDen)} · ${ngayVN(gioDen)}`:''}</small></div></div>{ghe?.length>0&&<HangThongTin nhan="Ghế đã chọn" giaTri={ghe.join(', ')}/ >}{gia!==undefined&&<div className="summary-total"><span>Tổng tiền</span><strong>{tien(gia)}</strong></div>}</div>;}
