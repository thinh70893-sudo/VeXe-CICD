
import { useState } from "react";
import {
    ArrowLeft,
    Camera,
    CameraOff,
    CheckCircle2,
    QrCode,
    ShieldCheck
} from "lucide-react";

import { Link } from "react-router-dom";
import { Api } from "../api/client.js";
import {
    BaoLoi,
    NhanTrangThai,
    TieuDe
} from "../components/GiaoDien.jsx";

import MayQuetQr from "../components/MayQuetQr.jsx";
import { useAuth } from "../context/AuthContext.jsx";
import { useToast } from "../context/ToastContext.jsx";
import { ngayVaGioVN } from "../utils/hienThi.js";

export default function SoatVe() {
    const { token } = useAuth();
    const toast = useToast();

    const [maQr, datMaQr] = useState("");
    const [ketQua, datKetQua] = useState(null);
    const [loi, datLoi] = useState("");
    const [dangXuLy, datDangXuLy] = useState(false);
    const [quet, datQuet] = useState(false);

    // Xác minh mã QR với Backend
    async function kiemTra(e) {
        e?.preventDefault();

        if (!maQr.trim() || dangXuLy) return;

        datDangXuLy(true);
        datLoi("");
        datKetQua(null);

        try {
            const ketQuaMoi = await Api.xacMinhVe(
                token,
                maQr.trim()
            );

            datKetQua(ketQuaMoi);
        } catch (err) {
            datLoi(err.message);
        } finally {
            datDangXuLy(false);
        }
    }

    // Xác nhận hành khách lên xe
    async function soat() {
        if (!ketQua || dangXuLy) return;

        const xacNhan = window.confirm(
            `Xác nhận soát vé cho ${ketQua.hoTen}?`
        );

        if (!xacNhan) return;

        datDangXuLy(true);
        datLoi("");

        try {
            await Api.soatVe(token, maQr.trim());

            toast("Đã soát vé thành công.", "success");

            const ketQuaMoi = await Api.xacMinhVe(
                token,
                maQr.trim()
            );

            datKetQua(ketQuaMoi);
        } catch (err) {
            datLoi(err.message);
        } finally {
            datDangXuLy(false);
        }
    }

    // Bật hoặc tắt camera
    function batTatCamera() {
        datLoi("");
        datQuet(trangThai => !trangThai);
    }

    // Nhận dữ liệu QR từ camera
    function khiQuetThanhCong(noiDungQr) {
        datMaQr(noiDungQr);
        datKetQua(null);
        datLoi("");
        datQuet(false);

        toast("Đã quét mã QR thành công.", "success");
    }

    const veHopLe =
        ketQua?.trangThaiVe === "ISSUED" &&
        ketQua?.trangThaiDon === "PAID";

    return (
        <div className="page-section">
            <div className="container">
                <Link className="back-link" to="/quan-tri">
                    <ArrowLeft size={17} />
                    Bảng điều khiển
                </Link>

                <TieuDe
                    nhan="NHÂN VIÊN / ADMIN"
                    tieuDe="Soát vé QR"
                    moTa="Quét mã QR trên vé điện tử hoặc nhập mã QR để xác minh và check-in."
                />

                <div className="scanner-layout">
                    <form className="card" onSubmit={kiemTra}>
                        <div className="scanner-icon">
                            <QrCode size={42} />
                        </div>

                        <h2>Xác minh vé</h2>

                        <p>
                            Nhập nội dung mã QR hoặc sử dụng
                            camera để quét trực tiếp vé xe.
                        </p>

                        <label className="field">
                            <span>Mã vé QR *</span>
                            <input
                                required
                                value={maQr}
                                onChange={(e) => {
                                    datMaQr(e.target.value);
                                    datKetQua(null);
                                    datLoi("");
                                }}
                                placeholder="BTICKET:xxxxxxxx-xxxx-..."
                                autoFocus
                            />
                        </label>

                        <div className="action-row">
                            <button
                                type="button"
                                className="btn btn-soft"
                                onClick={batTatCamera}
                            >
                                {quet ? (
                                    <CameraOff size={18} />
                                ) : (
                                    <Camera size={18} />
                                )}

                                {quet ? "Tắt camera" : "Quét camera"}
                            </button>

                            <button
                                type="submit"
                                className="btn btn-primary"
                                disabled={dangXuLy || !maQr.trim()}
                            >
                                {dangXuLy
                                    ? "Đang kiểm tra..."
                                    : "Xác minh vé"}
                            </button>
                        </div>

                        {quet && (
                            <div className="scanner-video">
                                <MayQuetQr
                                    khiQuetThanhCong={khiQuetThanhCong}
                                />
                            </div>
                        )}

                        <BaoLoi message={loi} />
                    </form>

                    <div className="card verify-result">
                        <h3>Kết quả xác minh</h3>

                        {ketQua ? (
                            <>
                                <div className="verify-detail">
                                    <span>Trạng thái vé</span>
                                    <NhanTrangThai
                                        giaTri={ketQua.trangThaiVe}
                                    />
                                </div>

                                <div className="verify-detail">
                                    <span>Hành khách</span>
                                    <strong>{ketQua.hoTen}</strong>
                                </div>

                                <div className="verify-detail">
                                    <span>Ghế</span>
                                    <strong>{ketQua.maGhe}</strong>
                                </div>

                                <div className="verify-detail">
                                    <span>Chuyến xe</span>
                                    <strong>{ketQua.maChuyen}</strong>
                                </div>

                                <div className="verify-detail">
                                    <span>Khởi hành</span>
                                    <strong>
                                        {ngayVaGioVN(
                                            ketQua.gioKhoiHanhUtc
                                        )}
                                    </strong>
                                </div>

                                {veHopLe ? (
                                    <button
                                        type="button"
                                        className="btn btn-primary btn-block"
                                        disabled={dangXuLy}
                                        onClick={soat}
                                    >
                                        <CheckCircle2 size={18} />
                                        Xác nhận khách lên xe
                                    </button>
                                ) : (
                                    <div className="error-banner">
                                        Vé không trong trạng thái hợp lệ
                                        để check-in.
                                    </div>
                                )}
                            </>
                        ) : (
                            <div className="verify-empty">
                                <ShieldCheck size={46} />
                                <p>
                                    Kết quả xác minh sẽ hiển thị
                                    tại đây. Chỉ tài khoản STAFF
                                    hoặc ADMIN mới được soát vé.
                                </p>
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}
