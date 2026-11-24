import { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { Api } from '../api/client.js';
const NguCanh = createContext(null);
const KHOA = 'busticket:session';
function docPhien() { try { return JSON.parse(sessionStorage.getItem(KHOA) || 'null'); } catch { return null; } }
export function AuthProvider({ children }) {
  const [phien, datPhien] = useState(docPhien);
  const [taiKhoan, datTaiKhoan] = useState(null);
  const [dangTai, datDangTai] = useState(!!docPhien());
  function dangXuat() { sessionStorage.removeItem(KHOA); datPhien(null); datTaiKhoan(null); }
  function capNhatPhien(duLieu) {
    sessionStorage.setItem(KHOA, JSON.stringify(duLieu));
    datPhien(duLieu);
    datTaiKhoan(duLieu);
  }
  async function dangNhap(email, matKhau) {
    const duLieu = await Api.dangNhap({ email, matKhau });
    capNhatPhien(duLieu);
    return duLieu;
  }
  useEffect(() => {
    let hoatDong = true;
    if (!phien?.accessToken) { datDangTai(false); return; }
    datDangTai(true);
    Api.toi(phien.accessToken).then(x => { if (hoatDong) datTaiKhoan(x); })
      .catch(l => { if (hoatDong && [401,403].includes(l.maTrangThai)) dangXuat(); })
      .finally(() => { if (hoatDong) datDangTai(false); });
    return () => { hoatDong = false; };
  }, [phien?.accessToken]);
  const giaTri = useMemo(() => ({ phien, token: phien?.accessToken, taiKhoan: taiKhoan || phien,
    vaiTro: phien?.vaiTro, daDangNhap: !!phien?.accessToken, dangTai, dangNhap, dangXuat,
    lamMoiHoSo: async () => { if (phien?.accessToken) datTaiKhoan(await Api.toi(phien.accessToken)); }
  }), [phien, taiKhoan, dangTai]);
  return <NguCanh.Provider value={giaTri}>{children}</NguCanh.Provider>;
}
export function useAuth() { const giaTri = useContext(NguCanh); if (!giaTri) throw new Error('Thiếu AuthProvider'); return giaTri; }
