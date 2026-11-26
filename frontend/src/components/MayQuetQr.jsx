import { useEffect, useRef, useState } from "react";
import { Html5Qrcode } from "html5-qrcode";

export default function MayQuetQr({ khiQuetThanhCong }) {
    const maVungQuet = useRef(
        `may-quet-${Math.random().toString(36).slice(2)}`
    );

    const hamThanhCong = useRef(khiQuetThanhCong);
    const [loiCamera, datLoiCamera] = useState("");

    useEffect(() => {
        hamThanhCong.current = khiQuetThanhCong;
    }, [khiQuetThanhCong]);

    useEffect(() => {
        let daHuy = false;
        let daQuet = false;
        let tienTrinhKhoiDong = null;

        const mayQuet = new Html5Qrcode(maVungQuet.current, false);

        function thongBaoLoiCamera(loi) {
            const tenLoi = loi?.name || "";
            const thongDiep = String(loi?.message || loi || "").toLowerCase();

            if (
                tenLoi === "NotAllowedError" ||
                tenLoi === "PermissionDeniedError" ||
                thongDiep.includes("permission")
            ) {
                return "Trình duyệt chưa được cấp quyền camera cho website. Hãy bật quyền Camera trong cài đặt trình duyệt.";
            }

            if (
                tenLoi === "NotFoundError" ||
                tenLoi === "DevicesNotFoundError"
            ) {
                return "Không tìm thấy camera trên thiết bị.";
            }

            if (
                tenLoi === "NotReadableError" ||
                tenLoi === "TrackStartError"
            ) {
                return "Camera đang được ứng dụng khác sử dụng. Hãy đóng ứng dụng đang dùng camera rồi thử lại.";
            }

            if (
                tenLoi === "OverconstrainedError" ||
                tenLoi === "ConstraintNotSatisfiedError"
            ) {
                return "Không tìm thấy camera sau phù hợp. Hãy kiểm tra camera của thiết bị.";
            }

            if (thongDiep.includes("https") || thongDiep.includes("secure context")) {
                return "Camera cần website được mở bằng HTTPS.";
            }

            return `Không mở được camera${tenLoi ? ` (${tenLoi})` : ""}. Hãy kiểm tra quyền camera của trình duyệt.`;
        }

        async function dungCamera() {
            try {
                if (mayQuet.isScanning) {
                    await mayQuet.stop();
                }
            } catch (loi) {
                console.warn("Không dừng được camera:", loi);
            }
        }

        async function khoiDong() {
            try {
                if (!window.isSecureContext) {
                    throw new Error(
                        "Trang quét vé cần được mở bằng HTTPS để sử dụng camera."
                    );
                }

                if (!navigator.mediaDevices?.getUserMedia) {
                    throw new Error(
                        "Trình duyệt không hỗ trợ truy cập camera. Hãy dùng Chrome hoặc Safari phiên bản mới."
                    );
                }

                const cauHinh = {
                    fps: 10,
                    qrbox: (chieuRong, chieuCao) => {
                        const kichThuoc = Math.floor(
                            Math.min(chieuRong, chieuCao) * 0.72
                        );

                        return {
                            width: kichThuoc,
                            height: kichThuoc
                        };
                    },
                    aspectRatio: 1
                };

                /*
                 * html5-qrcode của dự án yêu cầu facingMode là chuỗi.
                 * Không dùng { ideal: "environment" } và không gọi start()
                 * lần thứ hai khi lần khởi động đầu tiên chưa hoàn tất.
                 */
                tienTrinhKhoiDong = mayQuet.start(
                    { facingMode: "environment" },
                    cauHinh,
                    (noiDungQr) => {
                        if (daHuy || daQuet) return;

                        daQuet = true;
                        hamThanhCong.current?.(noiDungQr);
                    },
                    () => {}
                );

                await tienTrinhKhoiDong;

                // Nếu component đã unmount, cleanup sẽ dừng camera sau khi start() hoàn tất.
                if (daHuy) return;

                // Giữ video chạy bên trong trang trên iPhone/iPad.
                const vungQuet = document.getElementById(maVungQuet.current);
                const video = vungQuet?.querySelector("video");

                if (video) {
                    video.setAttribute("playsinline", "true");
                    video.setAttribute("webkit-playsinline", "true");
                    video.autoplay = true;
                    video.muted = true;

                    if (video.paused) {
                        try {
                            await video.play();
                        } catch (loiPhatVideo) {
                            console.warn(
                                "Trình duyệt chưa cho phát video camera tự động:",
                                loiPhatVideo
                            );
                        }
                    }
                }
            } catch (loi) {
                if (!daHuy) {
                    console.error("Lỗi khởi động camera:", loi);
                    datLoiCamera(thongBaoLoiCamera(loi));
                }
            }
        }

        void khoiDong();

        return () => {
            daHuy = true;

            /*
             * Chờ start() hoàn tất rồi mới stop().
             * Tránh gọi stop/clear trong lúc html5-qrcode đang chuyển trạng thái,
             * đặc biệt khi React StrictMode chạy lại effect.
             */
            if (tienTrinhKhoiDong) {
                void tienTrinhKhoiDong
                    .then(() => dungCamera())
                    .catch(() => {});
            }
        };
    }, []);

    return (
        <div className="may-quet-qr">
            <div id={maVungQuet.current} />

            {loiCamera && (
                <div className="error-banner" role="alert">
                    {loiCamera}
                </div>
            )}

            <p>Đưa mã QR trên vé vào giữa khung hình.</p>
        </div>
    );
}
