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

        const mayQuet = new Html5Qrcode(maVungQuet.current, false);

        function thongBaoLoiCamera(loi) {
            const tenLoi = loi?.name || "";
            const thongDiep = String(loi?.message || loi || "").toLowerCase();

            if (
                tenLoi === "NotAllowedError" ||
                tenLoi === "PermissionDeniedError" ||
                thongDiep.includes("permission")
            ) {
                return "Chrome/Safari chưa được cấp quyền camera cho website. Hãy bật quyền Camera trong cài đặt trình duyệt.";
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
                return "Không chọn được camera sau. Hãy thử tải lại trang hoặc chọn lại quyền camera.";
            }

            return `Không mở được camera${tenLoi ? ` (${tenLoi})` : ""}. Hãy kiểm tra quyền camera và kết nối HTTPS của website.`;
        }

        async function dungCamera() {
            try {
                if (mayQuet.isScanning) {
                    await mayQuet.stop();
                }
            } catch (loi) {
                console.warn("Không dừng được camera:", loi);
            }

            try {
                mayQuet.clear();
            } catch (loi) {
                // Scanner có thể chưa khởi tạo xong.
            }
        }

        function xuLyQuetThanhCong(noiDungQr) {
            if (daHuy || daQuet) return;

            daQuet = true;
            hamThanhCong.current?.(noiDungQr);
        }

        function capNhatVideoChoIOS() {
            const vungQuet = document.getElementById(maVungQuet.current);
            const video = vungQuet?.querySelector("video");

            if (video) {
                video.setAttribute("playsinline", "true");
                video.setAttribute("webkit-playsinline", "true");
                video.autoplay = true;
                video.muted = true;
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
                        "Trình duyệt hiện tại không hỗ trợ truy cập camera. Hãy mở website bằng Safari hoặc Chrome phiên bản mới."
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

                try {
                    await mayQuet.start(
                        { facingMode: { ideal: "environment" } },
                        cauHinh,
                        xuLyQuetThanhCong,
                        () => {}
                    );
                } catch (loiMoCameraSau) {
                    if (daHuy) return;

                    console.warn(
                        "Không mở được camera bằng facingMode, thử chọn camera theo ID:",
                        loiMoCameraSau
                    );

                    const danhSachCamera = await Html5Qrcode.getCameras();

                    if (!danhSachCamera || danhSachCamera.length === 0) {
                        throw loiMoCameraSau;
                    }

                    const cameraSau = danhSachCamera.find((camera) =>
                        /back|rear|environment|world|sau/i.test(camera.label || "")
                    );

                    const cameraDuocChon =
                        cameraSau || danhSachCamera[danhSachCamera.length - 1];

                    await mayQuet.start(
                        cameraDuocChon.id,
                        cauHinh,
                        xuLyQuetThanhCong,
                        () => {}
                    );
                }

                if (daHuy) {
                    await dungCamera();
                    return;
                }

                capNhatVideoChoIOS();
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
            void dungCamera();
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
