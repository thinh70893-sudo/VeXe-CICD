import { useEffect, useState } from 'react';
export function useApi(ham, phuThuoc = []) {
  const [duLieu, datDuLieu] = useState(null);
  const [dangTai, datDangTai] = useState(true);
  const [loi, datLoi] = useState('');
  async function taiLai() {
    datDangTai(true); datLoi('');
    try { datDuLieu(await ham()); } catch (e) { datLoi(e.message || 'Có lỗi xảy ra'); }
    finally { datDangTai(false); }
  }
  useEffect(() => { let boQua = false; datDangTai(true); datLoi('');
    Promise.resolve().then(ham).then(x => { if (!boQua) datDuLieu(x); }).catch(e => { if (!boQua) datLoi(e.message); }).finally(() => { if (!boQua) datDangTai(false); });
    return () => { boQua = true; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, phuThuoc);
  return { duLieu, dangTai, loi, taiLai };
}
