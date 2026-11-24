import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Activity, ArrowRight, BarChart3, BusFront, CheckCircle2, ClipboardList, Database, Edit3, Mail, Plus, RefreshCw, Search, ShieldCheck, Ticket, Trash2, Users, Wallet, X, AlertCircle, QrCode } from 'lucide-react';
import { Api } from '../api/client.js';
import { BaoLoi, NhanTrangThai, TaiTrang, TieuDe, TrangThongBao } from '../components/GiaoDien.jsx';
import { useAuth } from '../context/AuthContext.jsx';
import { useToast } from '../context/ToastContext.jsx';
import { taiNguyenQuanTri } from '../data/quanTri.js';
import { danhSach, gioVN, lay, ngayVaGioVN, tien, docNgayUtc } from '../utils/hienThi.js';
const MENU = [{ma:'tong-quan',ten:'Tổng quan',bieuTuong:BarChart3},{ma:'don-hang',ten:'Đơn đặt vé',bieuTuong:Ticket},{ma:'chuyen-xe',ten:'Chuyến xe',bieuTuong:BusFront},{ma:'danh-muc',ten:'Danh mục CRUD',bieuTuong:Database},{ma:'nguoi-dung',ten:'Người dùng',bieuTuong:Users},{ma:'email',ten:'Email vé QR',bieuTuong:Mail},{ma:'hoan-tien',ten:'Hoàn tiền',bieuTuong:Wallet}];
function duLieuRong(cauHinh) {return Object.fromEntries(cauHinh.truong.map(([key,, , loai])=>[key,loai==='checkbox'?true:'']));}
function taoGiaTri(cauHinh, banGhi) {return Object.fromEntries(cauHinh.truong.map(([key,tenCot,,loai])=>{let giaTri=lay(banGhi,tenCot);if(loai==='datetime-local'&&giaTri){const d=docNgayUtc(giaTri);giaTri=Number.isNaN(d.getTime())?'':new Date(d.getTime()-d.getTimezoneOffset()*60000).toISOString().slice(0,16);}return [key,loai==='checkbox'?Boolean(giaTri):giaTri??''];}));}
function duLieuGui(cauHinh, data) {const ketQua={};for(const [key,,,loai] of cauHinh.truong) {const val=data[key];if(loai==='checkbox')ketQua[key]=!!val;else if(val!==''&&val!==undefined&&val!==null){ketQua[key]=loai==='number'?Number(val):loai==='datetime-local'?new Date(val).toISOString():val;}}return ketQua;}

const TEN_COT_TIENG_VIET = {
  // Người dùng
  UserId: "Mã người dùng",
  FullName: "Họ và tên",
  Email: "Email",
  Phone: "Số điện thoại",
  Role: "Vai trò",

  // Chuyến xe
  TripId: "Mã chuyến",
  RouteId: "Mã tuyến",
  BusId: "Mã xe",
  DepartureAtUtc: "Giờ khởi hành",
  ArrivalAtUtc: "Giờ đến dự kiến",
  Price: "Giá vé",
  Status: "Trạng thái",

  // Tuyến xe và bến xe
  RouteName: "Tên tuyến",
  StationId: "Mã bến xe",
  StationName: "Tên bến xe",
  CityId: "Mã thành phố",
  CityName: "Tên thành phố",

  // Xe khách và ghế
  LicensePlate: "Biển số xe",
  Capacity: "Sức chứa",
  SeatId: "Mã ghế",
  SeatCode: "Số ghế",

  // Đơn đặt vé
  BookingId: "Mã đơn đặt vé",
  TicketId: "Mã vé",
  TotalAmount: "Tổng tiền",
  CreatedAtUtc: "Ngày tạo"
};

const TEN_COT_BO_SUNG = {
  Id: "Mã định danh",
  FromStationId: "Mã bến đi",
  ToStationId: "Mã bến đến",
  OriginCityId: "Mã thành phố đi",
  DestinationCityId: "Mã thành phố đến",
  DepartureTime: "Giờ khởi hành",
  ArrivalTime: "Giờ đến",
  BasePrice: "Giá vé cơ bản",
  DurationMinutes: "Thời gian di chuyển (phút)",
  DistanceKm: "Khoảng cách (km)",
  IsActive: "Đang hoạt động",
  UpdatedAtUtc: "Ngày cập nhật",
  CreatedBy: "Người tạo"
};


const TEN_COT_DAY_DU = {
  ...TEN_COT_TIENG_VIET,
  ...TEN_COT_BO_SUNG,

  // 1. Thành phố
  CityId: "Mã thành phố",
  CityCode: "Mã viết tắt thành phố",
  CityName: "Tên thành phố",

  // 2. Bến xe
  StationId: "Mã bến xe",
  StationCode: "Mã viết tắt bến xe",
  AddressLine: "Địa chỉ",
  CityId: "Mã thành phố",
  StationName: "Tên bến xe",

  // 3. Tuyến đường
  RouteId: "Mã tuyến",
  RouteCode: "Mã tuyến đường",
  OriginStationId: "Mã bến khởi hành",
  DestinationStationId: "Mã bến đến",
  DistanceKm: "Khoảng cách (km)",
  EstimatedMinutes: "Thời gian dự kiến (phút)",

  // 4. Loại xe
  BusTypeId: "Mã loại xe",
  TypeCode: "Mã loại",
  TypeName: "Tên loại xe",
  DefaultCapacity: "Sức chứa mặc định",

  // 5. Xe khách
  BusId: "Mã xe",
  PlateNumber: "Biển số xe",
  Manufacturer: "Hãng sản xuất",
  Capacity: "Sức chứa",
  Status: "Trạng thái",

  // 6. Ghế
  SeatId: "Mã ghế",
  SeatCode: "Số ghế",
  FloorNumber: "Tầng",

  // 7. Tài xế
  DriverId: "Mã tài xế",
  DriverCode: "Mã nhân viên lái xe",
  FullName: "Họ và tên",
  PhoneNumber: "Số điện thoại",
  LicenseClass: "Hạng giấy phép lái xe",

  // 8. Điểm dừng chuyến
  TripStopId: "Mã điểm dừng",
  TripId: "Mã chuyến",
  StopOrder: "Thứ tự dừng",
  PlannedArrivalAtUtc: "Giờ đến dự kiến",
  PlannedDepartureAtUtc: "Giờ khởi hành dự kiến",
  IsPickup: "Cho phép đón khách",
  IsDropoff: "Cho phép trả khách",

  // 9. Khuyến mãi
  PromotionId: "Mã khuyến mãi",
  PromoCode: "Mã giảm giá",
  Description: "Mô tả",
  DiscountType: "Loại giảm giá",
  DiscountValue: "Giá trị giảm",
  MaxDiscountVnd: "Giảm tối đa (VNĐ)",
  MinOrderVnd: "Đơn hàng tối thiểu (VNĐ)",
  MaxUses: "Lượt sử dụng tối đa",
  UsedCount: "Lượt đã sử dụng",

  // Các trường dùng chung
  Id: "Mã định danh",
  IsActive: "Đang hoạt động",
  CreatedAtUtc: "Ngày tạo",
  UpdatedAtUtc: "Ngày cập nhật",
  CreatedBy: "Người tạo",
  UpdatedBy: "Người cập nhật"
};

// Chuyển tên thuộc tính JSON thành nhãn tiếng Việt
function layTenCotTiengViet(tenCot) {
  const tenChuan =
    tenCot.charAt(0).toUpperCase() + tenCot.slice(1);

  return (
    TEN_COT_DAY_DU[tenCot] ??
    TEN_COT_DAY_DU[tenChuan] ??
    tenCot.replace(/([A-Z])/g, " $1").trim()
  );
}


function BangDuLieu({duLieu,hanhDong,idColumn}){const keys = Object.keys(duLieu?.[0] || {});return <div className="table-scroll"><table className="data-table"><thead><tr>{keys.map(k=><th key={k}>


{layTenCotTiengViet(k)}


</th>)}{hanhDong&&<th>Thao tác</th>}</tr></thead><tbody>{duLieu.map((r,i)=><tr key={String(lay(r,idColumn)||i)}>{keys.map(k=><td key={k}>{typeof r[k]==='boolean'?(r[k]?'Có':'Không'):typeof r[k]==='object'?JSON.stringify(r[k]):String(r[k]??'—').slice(0,110)}</td>)}{hanhDong&&<td><div className="table-actions">{hanhDong(r)}</div></td>}</tr>)}</tbody></table></div>}
function KhungQuanTri({children}) {return <div className="admin-panel">{children}</div>}
function TongQuan({token}){const [data,datData]=useState(null);const [loi,datLoi]=useState('');useEffect(()=>{Promise.all([Api.adminDon(token),Api.adminDoanhThu(token),Api.adminNguoiDung(token),Api.adminEmail(token)]).then(([don,doanhThu,nguoiDung,email])=>datData({don,doanhThu,nguoiDung,email})).catch(e=>datLoi(e.message))},[token]);if(loi)return <BaoLoi message={loi}/>;if(!data)return <TaiTrang/>;const tongThu=data.doanhThu.reduce((s,x)=>s+Number(lay(x,'doanhThu')||0),0);return <><div className="admin-metrics">{[{ten:'Tổng đơn gần đây',val:data.don.length,icon:ClipboardList,phu:'100 đơn mới nhất'}, {ten:'Doanh thu thống kê',val:tien(tongThu),icon:Wallet,phu:'Từ dữ liệu tổng hợp'}, {ten:'Tài khoản',val:data.nguoiDung.length,icon:Users,phu:'Tối đa 200 bản ghi'}, {ten:'Email đã ghi nhận',val:data.email.length,icon:Mail,phu:'Outbox gần đây'}].map(x=><div className="metric-card" key={x.ten}><span className="metric-icon"><x.icon size={22}/></span><span>{x.ten}</span><strong>{x.val}</strong><small>{x.phu}</small></div>)}</div><div className="card"><div className="card-heading"><div><h3>Đơn đặt vé mới nhất</h3><p>Cập nhật theo SQL Server</p></div></div>{data.don.length?<BangDuLieu duLieu={data.don.slice(0,8)} idColumn="maDatVe"/>:<p>Chưa có dữ liệu đơn.</p>}</div></>}
function DonHang({token}){const [ds,datDs]=useState([]);const [loi,datLoi]=useState('');const [dangTai,datDangTai]=useState(true);useEffect(()=>{Api.adminDon(token).then(datDs).catch(e=>datLoi(e.message)).finally(()=>datDangTai(false))},[token]);return <div className="card"><h3>Quản lý đơn đặt vé</h3><p className="muted">Đơn đã thanh toán cần được xử lý theo nghiệp vụ hoàn tiền; không xóa trực tiếp.</p><BaoLoi message={loi}/>{dangTai?<TaiTrang/>:ds.length?<BangDuLieu duLieu={ds} idColumn="maDatVe" hanhDong={r=><Link className="table-icon" to={`/don/${r.maDatVe}`} title="Xem chi tiết"><ArrowRight size={17}/></Link>}/>:<TrangThongBao tieuDe="Chưa có đơn"/>}</div>}
function QuanLyDanhMuc({token}){const toast=useToast();const [taiNguyen,datTaiNguyen]=useState('routes');const [ds,datDs]=useState([]);const [dangTai,datDangTai]=useState(true);const [loi,datLoi]=useState('');const [dangLuu,datDangLuu]=useState(false);const [moForm,datMoForm]=useState(false);const [sua,datSua]=useState(null);const config=taiNguyenQuanTri[taiNguyen];const [form,datForm]=useState(duLieuRong(config));const [loc,datLoc]=useState('');async function nap(){datDangTai(true);datLoi('');try{datDs(await Api.quanTri(token,taiNguyen))}catch(e){datLoi(e.message)}finally{datDangTai(false)}}useEffect(()=>{datMoForm(false);datSua(null);datForm(duLieuRong(taiNguyenQuanTri[taiNguyen]));nap()},[taiNguyen,token]);function moThem(){datForm(duLieuRong(config));datSua(null);datMoForm(true)}function moSua(r){datSua(lay(r,config.cotId));datForm(taoGiaTri(config,r));datMoForm(true)}async function gui(e){e.preventDefault();datDangLuu(true);datLoi('');try{const body=duLieuGui(config,form);if(sua!==null) await Api.suaQuanTri(token,taiNguyen,sua,body);else await Api.taoQuanTri(token,taiNguyen,body);toast(sua!==null?'Đã cập nhật dữ liệu.':'Đã tạo bản ghi mới.');datMoForm(false);nap()}catch(e){datLoi(e.message)}finally{datDangLuu(false)}}async function xoa(r){const id=lay(r,config.cotId);if(!window.confirm(`Xóa hoặc vô hiệu hóa bản ghi #${id}?`))return;try{await Api.xoaQuanTri(token,taiNguyen,id);toast('Đã xử lý yêu cầu xóa.');nap()}catch(e){toast(e.message,'error')}}const hien=ds.filter(r=>JSON.stringify(r).toLowerCase().includes(loc.toLowerCase()));return <><div className="admin-toolbar"><label className="field"><span>Nhóm dữ liệu</span><select value={taiNguyen} onChange={e=>datTaiNguyen(e.target.value)}>{Object.entries(taiNguyenQuanTri).map(([k,c])=><option key={k} value={k}>{c.ten}</option>)}</select></label><label className="field search-mini"><span>Tìm trong bảng</span><input value={loc} onChange={e=>datLoc(e.target.value)} placeholder="Nhập từ khóa"/></label><button type="button" className="btn btn-primary" onClick={moThem}><Plus size={17}/> Thêm {config.ten.toLowerCase()}</button></div><BaoLoi message={loi}/><div className="card"><div className="card-heading"><h3>{config.ten} ({hien.length})</h3><button className="btn btn-soft" onClick={nap}><RefreshCw size={16}/> Làm mới</button></div>{dangTai?<TaiTrang/>:hien.length?<BangDuLieu duLieu={hien} idColumn={config.cotId} hanhDong={r=><><button className="table-icon" title="Sửa" onClick={()=>moSua(r)}><Edit3 size={16}/></button><button className="table-icon danger" title="Xóa / ẩn" onClick={()=>xoa(r)}><Trash2 size={16}/></button></>}/>:<TrangThongBao tieuDe="Chưa có bản ghi"/>}</div>{moForm&&<div className="modal-backdrop" onClick={()=>datMoForm(false)}><div className="modal-card" role="dialog" aria-modal="true" aria-label="Biểu mẫu CRUD" onClick={e=>e.stopPropagation()}><div className="modal-heading"><h2>{sua!==null?'Cập nhật':'Thêm mới'} {config.ten.toLowerCase()}</h2><button className="icon-button" type="button" onClick={()=>datMoForm(false)}><X size={20}/></button></div><form onSubmit={gui}><div className="modal-form">{config.truong.map(([key,,label,loai,batBuoc,chon])=><label key={key} className={`field ${loai==='checkbox'?'checkbox-row':''}`}><span>{label}{batBuoc?' *':''}</span>{loai==='checkbox'?<input type="checkbox" checked={!!form[key]} onChange={e=>datForm(f=>({...f,[key]:e.target.checked}))}/>:loai==='select'?<select required={!!batBuoc} value={form[key]} onChange={e=>datForm(f=>({...f,[key]:e.target.value}))}><option value="">Chọn</option>{(chon||[]).map(x=><option key={x} value={x}>{x}</option>)}</select>:<input required={!!batBuoc} type={loai} step={loai==='number'?'any':undefined} value={form[key]} onChange={e=>datForm(f=>({...f,[key]:e.target.value}))}/>}</label>)}</div><div className="modal-actions"><button type="button" className="btn btn-soft" onClick={()=>datMoForm(false)}>Hủy</button><button className="btn btn-primary" type="submit" disabled={dangLuu}>{dangLuu?'Đang lưu...':'Lưu dữ liệu'}</button></div></form></div></div>}</>}
function QuanLyChuyen({token}){const toast=useToast();const [ds,datDs]=useState([]);const [tuyen,datTuyen]=useState([]);const [xe,datXe]=useState([]);const [taiXe,datTaiXe]=useState([]);const [loi,datLoi]=useState('');const [dangLuu,datDangLuu]=useState(false);const [dangTai,datDangTai]=useState(true);const [hienForm,datHienForm]=useState(false);const [dangSua,datDangSua]=useState(null);const [maChuyenGhe,datMaChuyenGhe]=useState('');const [dsGhe,datDsGhe]=useState([]);const [gheLoi,datGheLoi]=useState('');const [form,datForm]=useState({maChuyen:'',maTuyen:'',maXe:'',maTaiXe:'',gioKhoiHanhUtc:'',gioDenUtc:'',giaVe:''});async function nap(){datDangTai(true);try{const [a,b,c,d]=await Promise.all([Api.chuyen({soDong:100}),Api.tuyen(),Api.quanTri(token,'buses'),Api.quanTri(token,'drivers')]);datDs(a);datTuyen(b);datXe(c);datTaiXe(d)}catch(e){datLoi(e.message)}finally{datDangTai(false)}}useEffect(()=>{nap()},[token]);async function tao(e){e.preventDefault();datDangLuu(true);datLoi('');try{
 const duLieu={maTuyen:Number(form.maTuyen),maXe:Number(form.maXe),maTaiXe:form.maTaiXe?Number(form.maTaiXe):null,gioKhoiHanhUtc:new Date(form.gioKhoiHanhUtc).toISOString(),gioDenUtc:new Date(form.gioDenUtc).toISOString(),giaVe:Number(form.giaVe)};
 if(dangSua!==null) await Api.suaChuyen(token,dangSua,{...duLieu,trangThai:'SCHEDULED'});
 else await Api.taoChuyen(token,{maChuyen:form.maChuyen,...duLieu});
 toast(dangSua!==null?'Đã cập nhật chuyến xe.':'Đã tạo chuyến xe và sơ đồ ghế.');datHienForm(false);datDangSua(null);nap()
 }catch(e){datLoi(e.message)}finally{datDangLuu(false)}}
 async function moSua(r){datLoi('');try{const info=await Api.chiTietChuyen(r.maChuyen);const local=(utc)=>{const d=docNgayUtc(utc);return new Date(d.getTime()-d.getTimezoneOffset()*60000).toISOString().slice(0,16)};datForm({maChuyen:info.tripCode,maTuyen:info.routeId,maXe:info.busId,maTaiXe:'',gioKhoiHanhUtc:local(info.departureAtUtc),gioDenUtc:local(info.arrivalAtUtc),giaVe:info.priceVnd});datDangSua(r.maChuyen);datHienForm(true)}catch(e){datLoi(e.message)}}
 async function napGhe(id){datGheLoi('');try{datDsGhe(await Api.ghe(id));datMaChuyenGhe(String(id))}catch(e){datGheLoi(e.message)}}
 async function doiGhe(id,khoa){try{await Api.khoaGhe(token,Number(maChuyenGhe),id,khoa);toast(khoa?'Đã khóa ghế.':'Đã mở lại ghế.');await napGhe(maChuyenGhe)}catch(e){toast(e.message,'error')}}async function huy(r){if(!window.confirm(`Hủy chuyến ${r.maChuyenIn}? Chuyến có vé sẽ không được phép hủy trực tiếp.`))return;try{await Api.huyChuyen(token,r.maChuyen);toast('Đã hủy chuyến.');nap()}catch(e){toast(e.message,'error')}}return <><div className="admin-toolbar"><p className="muted">Quản lý chuyến đang mở bán. Chuyến đã bán vé cần xử lý riêng.</p><button className="btn btn-primary" onClick={()=>{datDangSua(null);datForm({maChuyen:'',maTuyen:'',maXe:'',maTaiXe:'',gioKhoiHanhUtc:'',gioDenUtc:'',giaVe:''});datHienForm(x=>!x)}}><Plus size={17}/> Tạo chuyến xe</button></div><BaoLoi message={loi}/>{hienForm&&<form className="card" onSubmit={tao}><h3>{dangSua!==null?'Sửa chuyến xe':'Tạo chuyến xe mới'}</h3>{dangSua!==null&&<p className="muted">Backend hiện không trả DriverId trong API chi tiết chuyến. Hãy chọn lại tài xế; để trống sẽ hủy phân công tài xế cũ.</p>}<div className="form-grid"><label className="field"><span>Mã chuyến *</span><input required disabled={dangSua!==null} value={form.maChuyen} onChange={e=>datForm(f=>({...f,maChuyen:e.target.value}))} placeholder="TR-2026-001"/></label><label className="field"><span>Tuyến đường *</span><select required value={form.maTuyen} onChange={e=>datForm(f=>({...f,maTuyen:e.target.value}))}><option value="">Chọn tuyến</option>{tuyen.map(r=><option key={r.maTuyen} value={r.maTuyen}>{r.benDi} → {r.benDen}</option>)}</select></label><label className="field"><span>Xe *</span><select required value={form.maXe} onChange={e=>datForm(f=>({...f,maXe:e.target.value}))}><option value="">Chọn xe</option>{xe.map(r=><option key={r.busId} value={r.busId}>{r.plateNumber} · {r.capacity} chỗ</option>)}</select></label><label className="field"><span>Tài xế</span><select value={form.maTaiXe} onChange={e=>datForm(f=>({...f,maTaiXe:e.target.value}))}><option value="">Chưa phân công</option>{taiXe.map(r=><option key={r.driverId} value={r.driverId}>{r.fullName}</option>)}</select></label><label className="field"><span>Giờ khởi hành (giờ Việt Nam) *</span><input type="datetime-local" required value={form.gioKhoiHanhUtc} onChange={e=>datForm(f=>({...f,gioKhoiHanhUtc:e.target.value}))}/></label><label className="field"><span>Giờ đến dự kiến (giờ Việt Nam) *</span><input type="datetime-local" required value={form.gioDenUtc} onChange={e=>datForm(f=>({...f,gioDenUtc:e.target.value}))}/></label><label className="field"><span>Giá vé VND *</span><input type="number" min="0" required value={form.giaVe} onChange={e=>datForm(f=>({...f,giaVe:e.target.value}))}/></label></div><div className="action-row"><button type="button" className="btn btn-soft" onClick={()=>{datHienForm(false);datDangSua(null)}}>Đóng</button><button type="submit" className="btn btn-primary" disabled={dangLuu}>{dangSua!==null?'Cập nhật chuyến':'Lưu chuyến xe'}</button></div></form>}<div className="card"><h3>Chuyến đang mở bán</h3>{dangTai?<TaiTrang/>:ds.length?<BangDuLieu idColumn="maChuyen" duLieu={ds} hanhDong={r=><><button className="table-icon" title="Sửa chuyến" onClick={()=>moSua(r)}><Edit3 size={17}/></button><button className="table-icon" title="Quản lý ghế chuyến" onClick={()=>napGhe(r.maChuyen)}><BusFront size={17}/></button><button className="table-icon danger" title="Hủy chuyến" onClick={()=>huy(r)}><Trash2 size={17}/></button></>}/>:<TrangThongBao tieuDe="Chưa có chuyến"/>}</div>{!!maChuyenGhe&&<div className="card"><div className="card-heading"><div><h3>Trạng thái ghế – chuyến #{maChuyenGhe}</h3><p>Ghế HELD hoặc SOLD không được khóa/mở trực tiếp.</p></div><button type="button" className="btn btn-soft" onClick={()=>napGhe(maChuyenGhe)}><RefreshCw size={17}/> Làm mới</button></div><BaoLoi message={gheLoi}/><div className="admin-seats">{dsGhe.map(g=><div className="admin-seat" key={g.maGhe}><strong>{g.kyHieuGhe}</strong><NhanTrangThai giaTri={g.trangThai}/>{['AVAILABLE','BLOCKED'].includes(g.trangThai)&&<button className="btn btn-soft btn-small" type="button" onClick={()=>doiGhe(g.maGhe,g.trangThai==='AVAILABLE')}>{g.trangThai==='AVAILABLE'?'Khóa':'Mở'}</button>}</div>)}</div></div>}</>}
function NguoiDung({token}){const toast=useToast();const [ds,datDs]=useState([]);const [loi,datLoi]=useState('');useEffect(()=>{Api.adminNguoiDung(token).then(datDs).catch(e=>datLoi(e.message))},[token]);async function doiTrangThai(u){try{await Api.khoaNguoiDung(token,u.maNguoiDung,!u.dangHoatDong);toast('Đã cập nhật trạng thái.');datDs(await Api.adminNguoiDung(token))}catch(e){toast(e.message,'error')}}async function doiVaiTro(u,v){if(!window.confirm(`Thay đổi quyền của ${u.email} sang ${v}?`))return;try{await Api.doiVaiTro(token,u.maNguoiDung,v);toast('Đã thay đổi vai trò.');datDs(await Api.adminNguoiDung(token))}catch(e){toast(e.message,'error')}}return <div className="card"><h3>Quản lý người dùng</h3><BaoLoi message={loi}/>{ds.length?<div className="table-scroll"><table className="data-table"><thead><tr><th>Tên</th><th>Email</th><th>Điện thoại</th><th>Vai trò</th><th>Trạng thái</th><th>Thao tác</th></tr></thead><tbody>{ds.map(u=><tr key={u.maNguoiDung}><td>{u.hoTen}</td><td>{u.email}</td><td>{u.soDienThoai}</td><td><select value={u.vaiTro} onChange={e=>doiVaiTro(u,e.target.value)}><option>CUSTOMER</option><option>STAFF</option><option>ADMIN</option></select></td><td>{u.dangHoatDong?'Hoạt động':'Đã khóa'}</td><td><button className="btn btn-soft btn-small" onClick={()=>doiTrangThai(u)}>{u.dangHoatDong?'Khóa':'Mở khóa'}</button></td></tr>)}</tbody></table></div>:<p>Không có bản ghi.</p>}</div>}
function Email({token}){const [ds,datDs]=useState([]);const [loi,datLoi]=useState('');useEffect(()=>{Api.adminEmail(token).then(datDs).catch(e=>datLoi(e.message))},[token]);return <div className="card"><h3>Hàng đợi gửi email</h3><p className="muted">EmailOutbox do Backend Worker xử lý. Trạng thái SENT xác nhận SMTP đã chấp nhận email, không đồng nghĩa người nhận đã mở.</p><BaoLoi message={loi}/>{ds.length?<BangDuLieu idColumn="maEmail" duLieu={ds}/>:<TrangThongBao tieuDe="Chưa có email"/>}</div>}
function HoanTien({token,vaiTro}){const toast=useToast();const [ds,datDs]=useState([]);const [loi,datLoi]=useState('');async function nap(){try{datDs(await Api.adminHoan(token))}catch(e){datLoi(e.message)}}useEffect(()=>{nap()},[token]);async function capNhat(r,trangThai){if(!window.confirm(`Chuyển trạng thái yêu cầu #${r.refundId} sang ${trangThai}? Với COMPLETED, bạn xác nhận đã thực hiện hoàn tiền bên ngoài.`))return;try{await Api.duyetHoan(token,r.refundId,trangThai);toast('Đã cập nhật yêu cầu hoàn tiền.');nap()}catch(e){toast(e.message,'error')}}return <div className="card"><h3>Yêu cầu hoàn tiền</h3><p className="muted">COMPLETED chỉ là ghi nhận hoàn tiền thủ công, không gọi ngân hàng.</p><BaoLoi message={loi}/>{ds.length?<BangDuLieu duLieu={ds} idColumn="refundId" hanhDong={vaiTro==='ADMIN'?r=><>{r.status==='REQUESTED'&&<><button className="btn btn-soft btn-small" onClick={()=>capNhat(r,'APPROVED')}>Duyệt</button><button className="btn btn-soft btn-small" onClick={()=>capNhat(r,'REJECTED')}>Từ chối</button></>}{r.status==='APPROVED'&&<button className="btn btn-soft btn-small" onClick={()=>capNhat(r,'COMPLETED')}>Đã hoàn ngoài hệ thống</button>}</>:null}/>:<TrangThongBao tieuDe="Chưa có yêu cầu hoàn tiền"/>}</div>}
export default function QuanTri(){const {token,vaiTro,taiKhoan}=useAuth();const [tab,datTab]=useState('tong-quan');const dsMenu=vaiTro==='ADMIN'?MENU:MENU.filter(x=>!['danh-muc','chuyen-xe'].includes(x.ma));return <div className="page-section admin-page"><div className="container"><TieuDe nhan="KHU VỰC QUẢN TRỊ" tieuDe="Bảng điều khiển" moTa={`Chào ${taiKhoan?.hoTen||'bạn'} · ${vaiTro} · Theo dõi hoạt động BusGo`}/><div className="admin-layout"><aside className="admin-sidebar"><span className="sidebar-label">QUẢN LÝ HỆ THỐNG</span>{dsMenu.map(m=><button type="button" key={m.ma} className={tab===m.ma?'active':''} onClick={()=>datTab(m.ma)}><m.bieuTuong size={19}/>{m.ten}</button>)}<Link to="/soat-ve"><QrCode size={19}/> Soát vé QR <ArrowRight size={16}/></Link></aside><main className="admin-main"><div className="admin-content-head"><div><span className="eyebrow">BUSGO / QUẢN TRỊ</span><h2>{dsMenu.find(x=>x.ma===tab)?.ten||'Tổng quan'}</h2></div><span className="role-pill">{vaiTro}</span></div>{tab==='tong-quan'&&<TongQuan token={token}/>} {tab==='don-hang'&&<DonHang token={token}/>} {tab==='chuyen-xe'&&<QuanLyChuyen token={token}/>} {tab==='danh-muc'&&<QuanLyDanhMuc token={token}/>} {tab==='nguoi-dung'&&<NguoiDung token={token}/>} {tab==='email'&&<Email token={token}/>} {tab==='hoan-tien'&&<HoanTien token={token} vaiTro={vaiTro}/>}</main></div></div></div>}
