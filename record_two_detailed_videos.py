import os
import time
from playwright.sync_api import sync_playwright

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
VIDEO_DIR = os.path.join(BASE_DIR, "videos")
os.makedirs(VIDEO_DIR, exist_ok=True)

V1_FINAL_VIDEO = os.path.join(VIDEO_DIR, "VIDEO_01_WEB_V1_VULNERABLE_BEFORE.webm")
V2_FINAL_VIDEO = os.path.join(VIDEO_DIR, "VIDEO_02_WEB_V2_SECURE_AFTER.webm")

def record_v1():
    print("\n==========================================================================")
    print(" BẮT ĐẦU QUAY VIDEO 1: WEB V1_VULNERABLE (BASELINE - CHƯA BẢO MẬT)")
    print(" Kịch bản: Tương tác trực tiếp từng bước thực tế (Form, Search, IDOR, Admin)")
    print("==========================================================================")

    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome", headless=True)
        context = browser.new_context(
            viewport={"width": 1920, "height": 1080},
            device_scale_factor=1,
            record_video_dir=VIDEO_DIR,
            record_video_size={"width": 1920, "height": 1080}
        )
        page = context.new_page()

        # 1. Truy cập V1
        print(">>> [1/6] Mở Web V1 (http://localhost:5001)...")
        page.goto("http://localhost:5001", wait_until="networkidle")
        page.wait_for_selector("#ordersTableBody tr td")
        time.sleep(3)

        # 2. Bước 1: Chưa đăng nhập (Anonymous) mà vẫn thấy hết đơn hàng (KB1 Broken Auth)
        print(">>> [2/6] Bước 1 (KB1): Chưa đăng nhập xem đơn hàng...")
        page.evaluate("quickLogin('anonymous')")
        time.sleep(1.5)
        # Cuộn xuống bảng đơn hàng
        page.evaluate("window.scrollTo({top: 480, behavior: 'smooth'})")
        time.sleep(3)
        # Cuộn tiếp xuống Live HTTP Inspector để thấy 200 OK
        page.evaluate("window.scrollTo({top: 850, behavior: 'smooth'})")
        time.sleep(3)

        # 3. Bước 2: Tấn công SQL Injection vào ô tìm kiếm thực tế (KB4 SQLi)
        print(">>> [3/6] Bước 2 (KB4): Tấn công SQLi vào ô tìm kiếm...")
        page.evaluate("window.scrollTo({top: 420, behavior: 'smooth'})")
        time.sleep(1)
        # Nhấp chuột vào ô tìm kiếm và gõ payload từng ký tự
        page.click("#searchKeyword")
        page.fill("#searchKeyword", "")
        time.sleep(0.5)
        page.type("#searchKeyword", "' OR 1=1 --", delay=100)
        time.sleep(1.5)
        # Nhấp nút Tìm
        page.evaluate("searchOrders()")
        time.sleep(3)
        # Cuộn xem toàn bộ đơn bị dump ra
        page.evaluate("window.scrollTo({top: 560, behavior: 'smooth'})")
        time.sleep(3)

        # 4. Bước 3: Đăng nhập userA và khai thác IDOR xem đơn người khác (KB3 IDOR)
        print(">>> [4/6] Bước 3 (KB3): userA khai thác IDOR xem đơn #3 của userB...")
        page.evaluate("window.scrollTo({top: 200, behavior: 'smooth'})")
        time.sleep(1)
        page.evaluate("quickLogin('userA')")
        time.sleep(2)
        page.evaluate("window.scrollTo({top: 420, behavior: 'smooth'})")
        time.sleep(1)
        # Gõ ID đơn hàng #3 vào ô input
        page.click("#orderIdInput")
        page.fill("#orderIdInput", "")
        page.type("#orderIdInput", "3", delay=150)
        time.sleep(1)
        page.evaluate("quickViewOrder(3)")
        time.sleep(3.5)

        # 5. Bước 4: userA gọi API Quản trị lộ mật khẩu Plaintext (KB2 Broken RBAC)
        print(">>> [5/6] Bước 4 (KB2): userA truy cập Quản trị người dùng...")
        page.evaluate("switchTab('users')")
        time.sleep(1.5)
        page.evaluate("loadAdminUsers()")
        time.sleep(2)
        page.evaluate("window.scrollTo({top: 600, behavior: 'smooth'})")
        time.sleep(4)

        # 6. Bước 5: Thử nghiệm Lỗi Logic Tiền Âm (KB6 Business Logic)
        print(">>> [6/6] Bước 5 (KB6): Thử nghiệm tạo đơn với số tiền âm...")
        page.evaluate("switchTab('create-order')")
        time.sleep(1.5)
        page.evaluate("window.scrollTo({top: 480, behavior: 'smooth'})")
        time.sleep(1)
        page.fill("#newProductName", "Lỗ Hổng Tiền Âm Thực Nghiệm")
        page.fill("#newTotalAmount", "-999999")
        time.sleep(1.5)
        page.evaluate("submitCreateOrder()")
        time.sleep(3.5)

        # Kết thúc Video 1
        page.evaluate("window.scrollTo({top: 0, behavior: 'smooth'})")
        time.sleep(2)

        video_handle = page.video
        context.close()
        browser.close()

        if video_handle:
            saved_path = video_handle.path()
            if os.path.exists(V1_FINAL_VIDEO):
                os.remove(V1_FINAL_VIDEO)
            os.rename(saved_path, V1_FINAL_VIDEO)
            print(f"=== THÀNH CÔNG: ĐÃ LƯU VIDEO 1 TẠI: {V1_FINAL_VIDEO} ===")


def record_v2():
    print("\n==========================================================================")
    print(" BẮT ĐẦU QUAY VIDEO 2: WEB V2_SECURE (HARDENED - ĐÃ BẢO MẬT ĐA LỚP)")
    print(" Kịch bản: Tương tác từng bước chứng minh cơ chế phòng thủ hoạt động")
    print("==========================================================================")

    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome", headless=True)
        context = browser.new_context(
            viewport={"width": 1920, "height": 1080},
            device_scale_factor=1,
            record_video_dir=VIDEO_DIR,
            record_video_size={"width": 1920, "height": 1080}
        )
        page = context.new_page()

        # 1. Truy cập V2
        print(">>> [1/7] Mở Web V2 (http://localhost:5002)...")
        page.goto("http://localhost:5002", wait_until="networkidle")
        page.wait_for_selector("#ordersTableBody tr td")
        time.sleep(3)

        # 2. Bước 1: Chưa đăng nhập bị chặn 401 Unauthorized (KB1 Authentication)
        print(">>> [2/7] Bước 1 (KB1): Chưa đăng nhập gọi API -> Chặn 401...")
        page.evaluate("quickLogin('anonymous')")
        time.sleep(1.5)
        page.evaluate("window.scrollTo({top: 480, behavior: 'smooth'})")
        time.sleep(2.5)
        # Cuộn xem Live HTTP Inspector hiện 401 Unauthorized
        page.evaluate("window.scrollTo({top: 850, behavior: 'smooth'})")
        time.sleep(3)

        # 3. Bước 2: Đăng nhập userA nhận JWT JTI, giải mã Claims và Data Isolation
        print(">>> [3/7] Bước 2: Đăng nhập userA -> Nhận JWT Bearer, giải mã Claims...")
        page.evaluate("window.scrollTo({top: 220, behavior: 'smooth'})")
        time.sleep(1)
        page.evaluate("quickLogin('userA')")
        time.sleep(2.5)
        # Cuộn xem bảng đơn hàng: chỉ thấy 3 đơn của userA (#1, #2, #5)
        page.evaluate("window.scrollTo({top: 520, behavior: 'smooth'})")
        time.sleep(3)

        # 4. Bước 3: Tấn công SQL Injection vào ô tìm kiếm -> Bị chặn 400 Bad Request (KB4)
        print(">>> [4/7] Bước 3 (KB4): Thử SQLi vào ô tìm kiếm -> Chặn 400 Bad Request...")
        page.evaluate("window.scrollTo({top: 420, behavior: 'smooth'})")
        time.sleep(1)
        page.click("#searchKeyword")
        page.fill("#searchKeyword", "")
        time.sleep(0.5)
        page.type("#searchKeyword", "' OR 1=1 --", delay=100)
        time.sleep(1.5)
        page.evaluate("searchOrders()")
        time.sleep(3)
        # Thử lại với từ khóa hợp lệ Dell
        page.fill("#searchKeyword", "")
        page.type("#searchKeyword", "Dell", delay=100)
        time.sleep(1)
        page.evaluate("searchOrders()")
        time.sleep(3)

        # 5. Bước 4: userA thử xem đơn #3 của userB -> Bị chặn 403 Forbidden (KB3 IDOR)
        print(">>> [5/7] Bước 4 (KB3): userA xem đơn người khác -> Chặn 403 Forbidden...")
        page.click("#orderIdInput")
        page.fill("#orderIdInput", "")
        page.type("#orderIdInput", "3", delay=150)
        time.sleep(1)
        page.evaluate("quickViewOrder(3)")
        time.sleep(3)
        # Thử lại đơn chính chủ #1
        page.fill("#orderIdInput", "")
        page.type("#orderIdInput", "1", delay=150)
        time.sleep(1)
        page.evaluate("quickViewOrder(1)")
        time.sleep(3)

        # 6. Bước 5: Phân quyền RBAC & Mật khẩu băm BCrypt (KB2)
        print(">>> [6/7] Bước 5 (KB2): userA bị chặn Admin 403, Admin đăng nhập an toàn...")
        page.evaluate("switchTab('users')")
        time.sleep(1.5)
        page.evaluate("loadAdminUsers()")
        time.sleep(2.5)
        # Đổi sang đăng nhập admin
        page.evaluate("window.scrollTo({top: 200, behavior: 'smooth'})")
        time.sleep(1)
        page.evaluate("quickLogin('admin')")
        time.sleep(1.5)
        page.evaluate("switchTab('users')")
        time.sleep(1)
        page.evaluate("loadAdminUsers()")
        time.sleep(2)
        page.evaluate("window.scrollTo({top: 600, behavior: 'smooth'})")
        time.sleep(3.5)

        # 7. Bước 6: Kiểm tra OWASP Security Headers & Nhật ký Audit Logs
        print(">>> [7/7] Bước 6: Kiểm tra 5 OWASP Headers & Bảng Audit Logs...")
        page.evaluate("switchTab('security-headers')")
        time.sleep(1)
        page.evaluate("checkSecurityHeadersLive()")
        time.sleep(2.5)
        page.evaluate("document.getElementById('tab-security-headers').scrollIntoView({block: 'center', behavior: 'smooth'})")
        time.sleep(3)

        # Cuộn xem bảng Audit Logs
        page.evaluate("loadAuditLogs()")
        time.sleep(1.5)
        page.evaluate("document.getElementById('auditLogsBody').scrollIntoView({block: 'center', behavior: 'smooth'})")
        time.sleep(4)

        # Kết thúc Video 2
        page.evaluate("window.scrollTo({top: 0, behavior: 'smooth'})")
        time.sleep(2)

        video_handle = page.video
        context.close()
        browser.close()

        if video_handle:
            saved_path = video_handle.path()
            if os.path.exists(V2_FINAL_VIDEO):
                os.remove(V2_FINAL_VIDEO)
            os.rename(saved_path, V2_FINAL_VIDEO)
            print(f"=== THÀNH CÔNG: ĐÃ LƯU VIDEO 2 TẠI: {V2_FINAL_VIDEO} ===")


if __name__ == "__main__":
    record_v1()
    record_v2()
    print("\n==========================================================================")
    print(" ĐÃ QUAY THÀNH CÔNG 2 VIDEO DEMO THỰC TẾ RIÊNG BIỆT:")
    print(f" 1. Video V1 (Lỗ hổng): {V1_FINAL_VIDEO}")
    print(f" 2. Video V2 (Phòng thủ): {V2_FINAL_VIDEO}")
    print("==========================================================================")
