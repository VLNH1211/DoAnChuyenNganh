import os
import time
import glob
from playwright.sync_api import sync_playwright

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
VIDEO_DIR = os.path.join(BASE_DIR, "videos")
os.makedirs(VIDEO_DIR, exist_ok=True)

FINAL_VIDEO_NAME = os.path.join(VIDEO_DIR, "DEMO_THUC_NGHIEM_AN_TOAN_THONG_TIN.webm")

def record_demo():
    print("==========================================================================")
    print(" BẮT ĐẦU TỰ ĐỘNG THỰC HIỆN VÀ QUAY VIDEO DEMO HỆ THỐNG AN TOÀN THÔNG TIN")
    print(" Sinh viên: Võ Lê Nhật Hoàng - MSSV: 2387700017 - Lớp: 23DATA1")
    print(" Đề tài: BẢO MẬT ỨNG DỤNG WEB/API TRÊN NỀN TẢNG ASP.NET CORE (HUTECH)")
    print("==========================================================================")
    start_time = time.time()

    with sync_playwright() as p:
        # Launch Google Chrome with 1920x1080 viewport and video recording
        browser = p.chromium.launch(channel="chrome", headless=True)
        context = browser.new_context(
            viewport={"width": 1920, "height": 1080},
            device_scale_factor=1,
            record_video_dir=VIDEO_DIR,
            record_video_size={"width": 1920, "height": 1080}
        )
        page = context.new_page()

        # -------------------------------------------------------------
        # PHẦN 1: DEMO TRÊN BẢN V1_VULNERABLE (CỔNG 5001 - RED TEAM)
        # -------------------------------------------------------------
        print("\n>>> [1/4] MỞ BẢN V1_VULNERABLE (http://localhost:5001) - PHÁT HIỆN LỖ HỔNG...")
        page.goto("http://localhost:5001", wait_until="networkidle")
        page.wait_for_selector("#ordersTableBody tr td")
        time.sleep(2.5)

        # 1. KB1 trên V1 (Chưa đăng nhập gọi API -> 200 OK rò rỉ đơn hàng)
        print("  -> Demo KB1: Chưa đăng nhập truy xuất đơn hàng...")
        page.evaluate("quickLogin('anonymous')")
        time.sleep(1)
        page.evaluate("runKB1()")
        time.sleep(2)
        page.evaluate("window.scrollTo({top: 150, behavior: 'smooth'})")
        time.sleep(2)

        # 2. KB2 trên V1 (userA gọi API Admin -> 200 OK lộ mật khẩu Plaintext)
        print("  -> Demo KB2: userA gọi API Quản trị...")
        page.evaluate("quickLogin('userA')")
        time.sleep(1)
        page.evaluate("runKB2()")
        time.sleep(2)
        page.evaluate("switchTab('users')")
        time.sleep(1.5)
        page.evaluate("window.scrollTo({top: 600, behavior: 'smooth'})")
        time.sleep(2.5)

        # 3. KB3 trên V1 (userA xem trộm đơn #3 của userB -> 200 OK IDOR)
        print("  -> Demo KB3: Khai thác IDOR xem đơn người khác...")
        page.evaluate("switchTab('orders')")
        time.sleep(1)
        page.evaluate("runKB3()")
        time.sleep(1.5)
        page.evaluate("quickViewOrder(3)")
        time.sleep(2)

        # 4. KB4 trên V1 (SQL Injection ' OR 1=1 -- -> 200 OK dump CSDL)
        print("  -> Demo KB4: Tấn công SQL Injection...")
        page.evaluate("runKB4()")
        time.sleep(2)

        # 5. Chạy liên hoàn KB1-KB4 trên V1 (4 thẻ đỏ)
        print("  -> Chạy Run All KB1–KB4 trên V1 (Toàn bộ báo động đỏ)...")
        page.evaluate("window.scrollTo({top: 80, behavior: 'smooth'})")
        time.sleep(1)
        page.evaluate("runAllKB()")
        time.sleep(4)

        # 6. Chạy liên hoàn KB5-KB8 trên V1 (Bị khai thác mở rộng)
        print("  -> Chạy Run All KB5–KB8 mở rộng trên V1...")
        page.evaluate("window.scrollTo({top: 320, behavior: 'smooth'})")
        time.sleep(1)
        page.evaluate("runAllExtKB()")
        time.sleep(5)

        # -------------------------------------------------------------
        # PHẦN 2: CHUYỂN SANG BẢN V2_SECURE (CỔNG 5002 - BLUE TEAM)
        # -------------------------------------------------------------
        print("\n>>> [2/4] CHUYỂN SANG BẢN V2_SECURE (http://localhost:5002) - CƠ CHẾ PHÒNG THỦ...")
        page.goto("http://localhost:5002", wait_until="networkidle")
        page.wait_for_selector("#ordersTableBody tr td")
        time.sleep(2.5)

        # 1. KB1 trên V2 (Chưa đăng nhập -> 401 Unauthorized)
        print("  -> Demo KB1: Chưa đăng nhập gọi API -> Bị chặn 401 Unauthorized...")
        page.evaluate("quickLogin('anonymous')")
        time.sleep(1)
        page.evaluate("runKB1()")
        time.sleep(2)
        page.evaluate("window.scrollTo({top: 150, behavior: 'smooth'})")
        time.sleep(2)

        # 2. Đăng nhập hợp lệ userA trên V2 (Nhận Token JTI, Claims, Data Isolation)
        print("  -> Đăng nhập userA: Cấp JWT Bearer, giải mã Claims và phân tách đơn hàng...")
        page.evaluate("quickLogin('userA')")
        time.sleep(2)
        page.evaluate("window.scrollTo({top: 480, behavior: 'smooth'})")
        time.sleep(2.5)

        # 3. KB2 trên V2 (userA gọi API Admin -> 403 Forbidden)
        print("  -> Demo KB2: userA gọi API Quản trị -> Bị Policy AdminOnly chặn 403...")
        page.evaluate("runKB2()")
        time.sleep(2)

        # 4. Admin đăng nhập hợp lệ trên V2 (Xem danh sách user & mật khẩu băm BCrypt)
        print("  -> Admin đăng nhập hợp lệ: Xem user & mật khẩu băm BCrypt...")
        page.evaluate("quickLogin('admin')")
        time.sleep(1.5)
        page.evaluate("switchTab('users')")
        time.sleep(1.5)
        page.evaluate("window.scrollTo({top: 600, behavior: 'smooth'})")
        time.sleep(2.5)

        # 5. KB3 trên V2 (userA xem đơn #3 của userB -> 403 Forbidden)
        print("  -> Demo KB3: userA xem đơn người khác -> Chặn BOLA/IDOR 403...")
        page.evaluate("quickLogin('userA')")
        time.sleep(1)
        page.evaluate("switchTab('orders')")
        time.sleep(1)
        page.evaluate("runKB3()")
        time.sleep(1.5)
        page.evaluate("quickViewOrder(3)")
        time.sleep(2.5)

        # 6. KB4 trên V2 (SQLi ' OR 1=1 -- -> 400 Bad Request an toàn)
        print("  -> Demo KB4: Gửi SQLi -> Validation & LINQ Parameterized chặn 400...")
        page.evaluate("runKB4()")
        time.sleep(2.5)

        # 7. Chạy liên hoàn KB1-KB4 trên V2 (4 thẻ xanh an toàn)
        print("  -> Chạy Run All KB1–KB4 trên V2 (Toàn bộ xanh an toàn)...")
        page.evaluate("window.scrollTo({top: 80, behavior: 'smooth'})")
        time.sleep(1)
        page.evaluate("runAllKB()")
        time.sleep(4)

        # 8. Chạy liên hoàn KB5-KB8 trên V2 (Chặn cả 4 cuộc tấn công)
        print("  -> Chạy Run All KB5–KB8 mở rộng trên V2 (Chặn 401, 400, 415, 429)...")
        page.evaluate("window.scrollTo({top: 320, behavior: 'smooth'})")
        time.sleep(1)
        page.evaluate("runAllExtKB()")
        time.sleep(5)

        # -------------------------------------------------------------
        # PHẦN 3: KIỂM TRA OWASP HEADERS & AUDIT LOGS TRÊN V2
        # -------------------------------------------------------------
        print("\n>>> [3/4] DEMO OWASP SECURITY HEADERS & NHẬT KÝ KIỂM TOÁN (AUDIT LOGS)...")
        page.evaluate("switchTab('security-headers')")
        time.sleep(1)
        page.evaluate("checkSecurityHeadersLive()")
        time.sleep(2)
        page.evaluate("document.getElementById('tab-security-headers').scrollIntoView({block: 'center', behavior: 'smooth'})")
        time.sleep(3)

        # Cuộn xuống xem Live HTTP Inspector và bảng Audit Logs
        print("  -> Cuộn xem bảng Security Audit Logs theo thời gian thực...")
        page.evaluate("quickLogin('admin')")
        time.sleep(1)
        page.evaluate("loadAuditLogs()")
        time.sleep(1.5)
        page.evaluate("document.getElementById('auditLogsBody').scrollIntoView({block: 'center', behavior: 'smooth'})")
        time.sleep(4)

        # -------------------------------------------------------------
        # KẾT THÚC VÀ ĐÓNG BROWSER ĐỂ LƯU VIDEO
        # -------------------------------------------------------------
        print("\n>>> [4/4] KẾT THÚC DEMO - ĐANG XUẤT VIDEO GHI HÌNH FULL HD...")
        page.evaluate("window.scrollTo({top: 0, behavior: 'smooth'})")
        time.sleep(2)

        # Lấy video handle trước khi đóng context
        video_handle = page.video
        context.close()
        browser.close()
        time.sleep(1.5)

        # Đổi tên file video vừa ghi
        if video_handle:
            saved_path = video_handle.path()
            if os.path.exists(FINAL_VIDEO_NAME):
                try:
                    os.remove(FINAL_VIDEO_NAME)
                except Exception:
                    pass
            for _ in range(5):
                try:
                    os.rename(saved_path, FINAL_VIDEO_NAME)
                    break
                except Exception:
                    time.sleep(1)

            elapsed_sec = int(time.time() - start_time)
            mins = elapsed_sec // 60
            secs = elapsed_sec % 60
            file_size = os.path.getsize(FINAL_VIDEO_NAME)
            size_mb = file_size / (1024 * 1024)

            print(f"\n==========================================================================")
            print(f" THÀNH CÔNG! VIDEO DEMO ĐÃ ĐƯỢC LƯU TẠI:")
            print(f" Đường dẫn: {FINAL_VIDEO_NAME}")
            print(f" Dung lượng: {size_mb:.2f} MB ({file_size:,} bytes)")
            print(f" Thời lượng: {mins:02d}:{secs:02d} ({elapsed_sec} giây)")
            print(f" Độ phân giải: Full HD 1920x1080 (Chuẩn trình chiếu Hội đồng)")
            print(f"==========================================================================")

if __name__ == "__main__":
    record_demo()
