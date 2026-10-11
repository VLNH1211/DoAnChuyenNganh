import os
import time
from playwright.sync_api import sync_playwright

SCREENSHOT_DIR = os.path.join(os.path.dirname(__file__), "screenshots")
os.makedirs(SCREENSHOT_DIR, exist_ok=True)

def run():
    with sync_playwright() as p:
        # Launch Google Chrome with 1920x1080 viewport
        browser = p.chromium.launch(channel="chrome", headless=True)
        context = browser.new_context(
            viewport={"width": 1920, "height": 1080},
            device_scale_factor=1
        )
        page = context.new_page()

        print("=== BẮT ĐẦU CHỤP 18 ẢNH MINH CHỨNG THỰC NGHIỆM ĐỒ ÁN CHUYÊN NGÀNH ===")

        # -------------------------------------------------------------
        # 1. 01_V1_Dashboard_Overview.png
        # -------------------------------------------------------------
        print("[1/18] Chụp 01_V1_Dashboard_Overview.png...")
        page.goto("http://localhost:5001", wait_until="networkidle")
        page.wait_for_selector("#ordersTableBody tr td")
        time.sleep(1)
        page.screenshot(path=os.path.join(SCREENSHOT_DIR, "01_V1_Dashboard_Overview.png"))

        # -------------------------------------------------------------
        # 2. 02_V2_Dashboard_Overview.png
        # -------------------------------------------------------------
        print("[2/18] Chụp 02_V2_Dashboard_Overview.png...")
        page.goto("http://localhost:5002", wait_until="networkidle")
        page.wait_for_selector("#ordersTableBody tr td")
        time.sleep(1)
        page.screenshot(path=os.path.join(SCREENSHOT_DIR, "02_V2_Dashboard_Overview.png"))

        # -------------------------------------------------------------
        # 3. 03_KB1_Before_V1_Unauthenticated_200.png
        # -------------------------------------------------------------
        print("[3/18] Chụp 03_KB1_Before_V1_Unauthenticated_200.png...")
        page.goto("http://localhost:5001", wait_until="networkidle")
        page.evaluate("quickLogin('anonymous')")
        time.sleep(0.5)
        page.evaluate("runKB1()")
        time.sleep(1.5)
        page.evaluate("window.scrollTo({top: 100, behavior: 'instant'})")
        time.sleep(0.5)
        page.screenshot(path=os.path.join(SCREENSHOT_DIR, "03_KB1_Before_V1_Unauthenticated_200.png"))

        # -------------------------------------------------------------
        # 4. 04_KB1_After_V2_Unauthorized_401.png
        # -------------------------------------------------------------
        print("[4/18] Chụp 04_KB1_After_V2_Unauthorized_401.png...")
        page.goto("http://localhost:5002", wait_until="networkidle")
        page.evaluate("quickLogin('anonymous')")
        time.sleep(0.5)
        page.evaluate("runKB1()")
        time.sleep(1.5)
        page.evaluate("window.scrollTo({top: 100, behavior: 'instant'})")
        time.sleep(0.5)
        page.screenshot(path=os.path.join(SCREENSHOT_DIR, "04_KB1_After_V2_Unauthorized_401.png"))

        # -------------------------------------------------------------
        # 5. 05_KB1_After_V2_ValidToken_UserA_200.png
        # -------------------------------------------------------------
        print("[5/18] Chụp 05_KB1_After_V2_ValidToken_UserA_200.png...")
        page.goto("http://localhost:5002", wait_until="networkidle")
        page.evaluate("quickLogin('userA')")
        time.sleep(1.5)
        page.evaluate("window.scrollTo({top: 520, behavior: 'instant'})")
        time.sleep(0.5)
        page.screenshot(path=os.path.join(SCREENSHOT_DIR, "05_KB1_After_V2_ValidToken_UserA_200.png"))

        # -------------------------------------------------------------
        # 6. 06_KB2_Before_V1_BrokenRBAC_200.png
        # -------------------------------------------------------------
        print("[6/18] Chụp 06_KB2_Before_V1_BrokenRBAC_200.png...")
        page.goto("http://localhost:5001", wait_until="networkidle")
        page.evaluate("quickLogin('userA')")
        time.sleep(0.8)
        page.evaluate("runKB2()")
        time.sleep(1.5)
        page.evaluate("switchTab('users')")
        time.sleep(1)
        page.evaluate("window.scrollTo({top: 620, behavior: 'instant'})")
        time.sleep(0.5)
        page.screenshot(path=os.path.join(SCREENSHOT_DIR, "06_KB2_Before_V1_BrokenRBAC_200.png"))

        # -------------------------------------------------------------
        # 7. 07_KB2_After_V2_RBAC_Blocked_403.png
        # -------------------------------------------------------------
        print("[7/18] Chụp 07_KB2_After_V2_RBAC_Blocked_403.png...")
        page.goto("http://localhost:5002", wait_until="networkidle")
        page.evaluate("quickLogin('userA')")
        time.sleep(0.8)
        page.evaluate("runKB2()")
        time.sleep(1.5)
        page.evaluate("window.scrollTo({top: 100, behavior: 'instant'})")
        time.sleep(0.5)
        page.screenshot(path=os.path.join(SCREENSHOT_DIR, "07_KB2_After_V2_RBAC_Blocked_403.png"))

        # -------------------------------------------------------------
        # 8. 08_KB2_After_V2_Admin_Access_200.png
        # -------------------------------------------------------------
        print("[8/18] Chụp 08_KB2_After_V2_Admin_Access_200.png...")
        page.goto("http://localhost:5002", wait_until="networkidle")
        page.evaluate("quickLogin('admin')")
        time.sleep(1)
        page.evaluate("switchTab('users')")
        time.sleep(1)
        page.evaluate("window.scrollTo({top: 620, behavior: 'instant'})")
        time.sleep(0.5)
        page.screenshot(path=os.path.join(SCREENSHOT_DIR, "08_KB2_After_V2_Admin_Access_200.png"))

        # -------------------------------------------------------------
        # 9. 09_KB3_Before_V1_IDOR_Leaked_200.png
        # -------------------------------------------------------------
        print("[9/18] Chụp 09_KB3_Before_V1_IDOR_Leaked_200.png...")
        page.goto("http://localhost:5001", wait_until="networkidle")
        page.evaluate("quickLogin('userA')")
        time.sleep(0.8)
        page.evaluate("runKB3()")
        time.sleep(1.5)
        page.evaluate("quickViewOrder(3)")
        time.sleep(1)
        page.evaluate("window.scrollTo({top: 600, behavior: 'instant'})")
        time.sleep(0.5)
        page.screenshot(path=os.path.join(SCREENSHOT_DIR, "09_KB3_Before_V1_IDOR_Leaked_200.png"))

        # -------------------------------------------------------------
        # 10. 10_KB3_After_V2_IDOR_Blocked_403.png
        # -------------------------------------------------------------
        print("[10/18] Chụp 10_KB3_After_V2_IDOR_Blocked_403.png...")
        page.goto("http://localhost:5002", wait_until="networkidle")
        page.evaluate("quickLogin('userA')")
        time.sleep(0.8)
        page.evaluate("runKB3()")
        time.sleep(1.5)
        page.evaluate("quickViewOrder(3)")
        time.sleep(1)
        page.evaluate("window.scrollTo({top: 600, behavior: 'instant'})")
        time.sleep(0.5)
        page.screenshot(path=os.path.join(SCREENSHOT_DIR, "10_KB3_After_V2_IDOR_Blocked_403.png"))

        # -------------------------------------------------------------
        # 11. 11_KB4_Before_V1_SQLi_Dumped_200.png
        # -------------------------------------------------------------
        print("[11/18] Chụp 11_KB4_Before_V1_SQLi_Dumped_200.png...")
        page.goto("http://localhost:5001", wait_until="networkidle")
        page.evaluate("runKB4()")
        time.sleep(1.5)
        page.evaluate("window.scrollTo({top: 100, behavior: 'instant'})")
        time.sleep(0.5)
        page.screenshot(path=os.path.join(SCREENSHOT_DIR, "11_KB4_Before_V1_SQLi_Dumped_200.png"))

        # -------------------------------------------------------------
        # 12. 12_KB4_After_V2_SQLi_Blocked_400.png
        # -------------------------------------------------------------
        print("[12/18] Chụp 12_KB4_After_V2_SQLi_Blocked_400.png...")
        page.goto("http://localhost:5002", wait_until="networkidle")
        page.evaluate("quickLogin('userA')")
        time.sleep(0.8)
        page.evaluate("runKB4()")
        time.sleep(1.5)
        page.evaluate("window.scrollTo({top: 100, behavior: 'instant'})")
        time.sleep(0.5)
        page.screenshot(path=os.path.join(SCREENSHOT_DIR, "12_KB4_After_V2_SQLi_Blocked_400.png"))

        # -------------------------------------------------------------
        # 13a. 13a_V1_RunAll.png (V1 All 4 Core KB1-KB4 Vulnerable)
        # -------------------------------------------------------------
        print("[13/18] Chụp 13a_V1_RunAll.png...")
        page.goto("http://localhost:5001", wait_until="networkidle")
        page.evaluate("runAllKB()")
        time.sleep(3.5)
        page.evaluate("window.scrollTo({top: 80, behavior: 'instant'})")
        time.sleep(0.5)
        page.screenshot(path=os.path.join(SCREENSHOT_DIR, "13a_V1_RunAll.png"))

        # -------------------------------------------------------------
        # 13b. 13b_V2_RunAll.png (V2 All 4 Core KB1-KB4 Safe)
        # -------------------------------------------------------------
        print("[14/18] Chụp 13b_V2_RunAll.png...")
        page.goto("http://localhost:5002", wait_until="networkidle")
        page.evaluate("runAllKB()")
        time.sleep(3.5)
        page.evaluate("window.scrollTo({top: 80, behavior: 'instant'})")
        time.sleep(0.5)
        page.screenshot(path=os.path.join(SCREENSHOT_DIR, "13b_V2_RunAll.png"))

        # -------------------------------------------------------------
        # 15a. 15a_V1_RunAll_Extended_KB5_KB8.png (V1 All 4 Extended KB5-KB8 Vulnerable)
        # -------------------------------------------------------------
        print("[15/18] Chụp 15a_V1_RunAll_Extended_KB5_KB8.png...")
        page.goto("http://localhost:5001", wait_until="networkidle")
        page.evaluate("runAllExtKB()")
        time.sleep(4.5)
        page.evaluate("window.scrollTo({top: 320, behavior: 'instant'})")
        time.sleep(0.5)
        page.screenshot(path=os.path.join(SCREENSHOT_DIR, "15a_V1_RunAll_Extended_KB5_KB8.png"))

        # -------------------------------------------------------------
        # 15b. 15b_V2_RunAll_Extended_KB5_KB8.png (V2 All 4 Extended KB5-KB8 Safe)
        # -------------------------------------------------------------
        print("[16/18] Chụp 15b_V2_RunAll_Extended_KB5_KB8.png...")
        page.goto("http://localhost:5002", wait_until="networkidle")
        page.evaluate("runAllExtKB()")
        time.sleep(4.5)
        page.evaluate("window.scrollTo({top: 320, behavior: 'instant'})")
        time.sleep(0.5)
        page.screenshot(path=os.path.join(SCREENSHOT_DIR, "15b_V2_RunAll_Extended_KB5_KB8.png"))

        # -------------------------------------------------------------
        # 16. 16_V2_OWASP_Security_Headers_LiveCheck.png
        # -------------------------------------------------------------
        print("[17/18] Chụp 16_V2_OWASP_Security_Headers_LiveCheck.png...")
        page.goto("http://localhost:5002", wait_until="networkidle")
        page.evaluate("switchTab('security-headers')")
        time.sleep(0.5)
        page.evaluate("checkSecurityHeadersLive()")
        time.sleep(1.5)
        page.evaluate("document.getElementById('tab-security-headers').scrollIntoView({block: 'center', behavior: 'instant'})")
        time.sleep(0.5)
        page.screenshot(path=os.path.join(SCREENSHOT_DIR, "16_V2_OWASP_Security_Headers_LiveCheck.png"))

        # -------------------------------------------------------------
        # 14. 14_V2_Security_Audit_Logs_Table.png
        # -------------------------------------------------------------
        print("[18/18] Chụp 14_V2_Security_Audit_Logs_Table.png...")
        page.goto("http://localhost:5002", wait_until="networkidle")
        page.evaluate("quickLogin('admin')")
        time.sleep(0.8)
        page.evaluate("loadAuditLogs()")
        time.sleep(1.5)
        page.evaluate("document.getElementById('auditLogsBody').scrollIntoView({block: 'center', behavior: 'instant'})")
        time.sleep(1)
        page.screenshot(path=os.path.join(SCREENSHOT_DIR, "14_V2_Security_Audit_Logs_Table.png"))

        context.close()
        browser.close()
        print("=== HOÀN TẤT CHỤP TẤT CẢ 18 ẢNH VÀO THƯ MỤC SCREENSHOTS ===")

if __name__ == "__main__":
    run()
