import { Route, Routes } from 'react-router-dom';
import { BoCuc, BaoVe, TrangThongBao } from './components/GiaoDien.jsx';
import TrangChu from './pages/TrangChu.jsx';
import TimChuyen from './pages/TimChuyen.jsx';
import ChonGhe from './pages/ChonGhe.jsx';
import ThanhToan from './pages/ThanhToan.jsx';
import DangNhap from './pages/DangNhap.jsx';
import DangKy from './pages/DangKy.jsx';
import TaiKhoan from './pages/TaiKhoan.jsx';
import DonCuaToi from './pages/DonCuaToi.jsx';
import ChiTietDon from './pages/ChiTietDon.jsx';
import ChiTietVe from './pages/ChiTietVe.jsx';
import QuanTri from './pages/QuanTri.jsx';
import SoatVe from './pages/SoatVe.jsx';
export default function App() { return <Routes>
  <Route element={<BoCuc/>}>
    <Route index element={<TrangChu/>}/>
    <Route path="tim-chuyen" element={<TimChuyen/>}/>
    <Route path="dang-nhap" element={<DangNhap/>}/>
    <Route path="dang-ky" element={<DangKy/>}/>
    <Route path="chuyen/:maChuyen/chon-ghe" element={<ChonGhe/>}/>
    <Route element={<BaoVe/>}>
      <Route path="thanh-toan/:maDatVe" element={<ThanhToan/>}/>
      <Route path="don-cua-toi" element={<DonCuaToi/>}/>
      <Route path="don/:maDatVe" element={<ChiTietDon/>}/>
      <Route path="ve/:maVe" element={<ChiTietVe/>}/>
      <Route path="tai-khoan" element={<TaiKhoan/>}/>
    </Route>
    <Route element={<BaoVe vaiTroChoPhep={['ADMIN','STAFF']}/>}>
      <Route path="quan-tri" element={<QuanTri/>}/>
      <Route path="soat-ve" element={<SoatVe/>}/>
    </Route>
    <Route path="*" element={<TrangThongBao tieuDe="Không tìm thấy trang" moTa="Đường dẫn không còn tồn tại." link="/"/>}/>
  </Route>
</Routes>; }
