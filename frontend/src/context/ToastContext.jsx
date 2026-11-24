import { createContext, useCallback, useContext, useState } from 'react';
import { CheckCircle2, XCircle, X } from 'lucide-react';
const NguCanh = createContext(null);
export function ToastProvider({ children }) {
  const [ds, datDs] = useState([]);
  const thongBao = useCallback((noiDung, loai = 'success') => {
    const ma = Math.random().toString(36).slice(2);
    datDs(cu => [...cu, { ma, noiDung, loai }].slice(-4));
    setTimeout(() => datDs(cu => cu.filter(x => x.ma !== ma)), 5000);
  }, []);
  return <NguCanh.Provider value={thongBao}>
    {children}
    <div className="toast-stack" aria-live="polite">
      {ds.map(x => <div className={`toast ${x.loai}`} key={x.ma}>
        {x.loai === 'success' ? <CheckCircle2 size={19} /> : <XCircle size={19} />}
        <span>{x.noiDung}</span>
        <button type="button" className="icon-button" onClick={() => datDs(cu => cu.filter(t => t.ma !== x.ma))} aria-label="Đóng thông báo"><X size={17}/></button>
      </div>)}
    </div>
  </NguCanh.Provider>;
}
export const useToast = () => useContext(NguCanh);
