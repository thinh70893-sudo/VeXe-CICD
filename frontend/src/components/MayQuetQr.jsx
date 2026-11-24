
import { useEffect, useRef, useState } from "react";
import { Html5Qrcode } from "html5-qrcode";

export default function MayQuetQr({
    khiQuetThanhCong
}) {
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
        let dangChay = false;

        const mayQuet = new Html5Qrcode(
            maVungQuet.current,
            false
        );

        async function dungCamera() {
            try {
                if (dangChay && mayQuet.isScanning) {
                    await mayQuet.stop();
                }
            } catch (loi) {
                console.warn("Không dừng được camera:", loi);
            } finally {
                dangChay = false;
            }
        }

        async function khoiDong() {
            try {
                await mayQuet.start(
                    { facingMode: "environment" },
                    {
                        fps: 10,
                        qrbox: { width: 240, height: 240 }
                    },
                    (noiDungQr) => {
                        if (daHuy || daQuet) return;

                        daQuet = true;
                        hamThanhCong.current?.(noiDungQr);
                    },
                    () => { }
                );

                dangChay = true;

                // Nếu component đã bị đóng trong lúc
                // trình duyệt đang mở camera.
                if (daHuy) {
                    await dungCamera();
                }
            } catch (loi) {
                if (!daHuy) {
                    console.error("Lỗi camera:", loi);
                    datLoiCamera(
                        "Không mở được camera. Hãy kiểm tra quyền truy cập."
                    );
                }
            }
        }

        khoiDong();

        return () => {
            daHuy = true;
            void dungCamera();
        };
    }, []);

    return (
        <div className="may-quet-qr">
            <div id={maVungQuet.current} />

            {loiCamera && (
                <div className="error-banner">
                    {loiCamera}
                </div>
            )}

            <p>
                Đưa mã QR trên vé vào giữa khung hình.
            </p>
        </div>
    );
}
