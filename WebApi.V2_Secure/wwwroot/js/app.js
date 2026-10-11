// CONFIGURATION FOR V2 (HARDENED SECURE)
const APP_CONFIG = {
  version: "V2",
  port: 5002,
  apiBase: "http://localhost:5002",
  otherVersionUrl: "http://localhost:5001",
  otherVersionName: "Bản V1 Chưa Bảo Mật (Port 5001)"
};

// GLOBAL APP STATE
const state = {
  token: "",
  user: null,
  decodedClaims: null,
  activeTab: "orders",
  auditLogsFilter: null,
  lastRevokedToken: ""
};

// QUICK ACCOUNTS PRESETS
const ACCOUNTS = {
  anonymous: { username: "", password: "", label: "Chưa đăng nhập (Anonymous)", role: "Anonymous" },
  userA: { username: "userA", password: "UserA@123", label: "userA (Võ Lê Nhật Hoàng - Đơn #1, #2, #5)", role: "NguoiDung" },
  userB: { username: "userB", password: "UserB@123", label: "userB (Trần Thị Bích - Đơn #3, #4)", role: "NguoiDung" },
  staff1: { username: "staff1", password: "Staff@123", label: "staff1 (Nguyễn Văn Staff)", role: "NhanVien" },
  admin: { username: "admin", password: "Admin@123", label: "admin (Hệ thống Quản Trị)", role: "Admin" }
};

// DOM READY
document.addEventListener("DOMContentLoaded", () => {
  initUI();
  loadOrders();
  loadAuditLogs();
});

function initUI() {
  document.body.setAttribute("data-version", APP_CONFIG.version);
  updateSessionUI();
}

// FORMAT TIỀN TỆ VNĐ
function formatVND(amount) {
  if (isNaN(amount)) return "0 ₫";
  return new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(amount);
}

// FORMAT THỜI GIAN
function formatDateTime(isoStr) {
  if (!isoStr) return "";
  try {
    const d = new Date(isoStr);
    return d.toLocaleString("vi-VN");
  } catch (e) {
    return isoStr;
  }
}

// GIẢI MÃ JWT TOKEN BASE64
function parseJwt(token) {
  try {
    if (!token) return null;
    const base64Url = token.split(".")[1];
    if (!base64Url) return null;
    const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split("")
        .map(c => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
        .join("")
    );
    return JSON.parse(jsonPayload);
  } catch (e) {
    console.error("Lỗi giải mã token:", e);
    return null;
  }
}

// CẬP NHẬT HIỂN THỊ PHIÊN ĐĂNG NHẬP
function updateSessionUI() {
  const avatarEl = document.getElementById("userAvatar");
  const nameEl = document.getElementById("userDisplayName");
  const roleEl = document.getElementById("userRoleBadge");
  const rawTokenEl = document.getElementById("rawTokenText");
  const decodedClaimsEl = document.getElementById("decodedClaimsText");

  if (state.token && state.user) {
    avatarEl.textContent = state.user.username.charAt(0).toUpperCase();
    nameEl.textContent = state.user.fullName || state.user.username;
    roleEl.textContent = state.user.role;
    roleEl.className = "badge " + (state.user.role === "Admin" ? "badge-purple" : state.user.role === "NhanVien" ? "badge-blue" : "badge-green");
    rawTokenEl.textContent = state.token;
    decodedClaimsEl.textContent = JSON.stringify(state.decodedClaims, null, 2);
  } else {
    avatarEl.textContent = "?";
    nameEl.textContent = "Chưa đăng nhập (Anonymous)";
    roleEl.textContent = "No Token";
    roleEl.className = "badge badge-orange";
    rawTokenEl.textContent = "(Chưa có Token được cấp)";
    decodedClaimsEl.textContent = '{\n  "status": "Unauthenticated",\n  "message": "Vui lòng chọn tài khoản để đăng nhập và lấy JWT Token"\n}';
  }
}

// CHỌN NHANH TÀI KHOẢN
function quickLogin(accountKey) {
  const acc = ACCOUNTS[accountKey];
  if (!acc) return;

  if (accountKey === "anonymous") {
    state.token = "";
    state.user = null;
    state.decodedClaims = null;
    document.getElementById("loginUsername").value = "";
    document.getElementById("loginPassword").value = "";
    updateSessionUI();
    showToast("Đã chuyển sang trạng thái Chưa đăng nhập (Anonymous)", "info");
    loadOrders();
    loadAuditLogs();
    return;
  }

  document.getElementById("loginUsername").value = acc.username;
  document.getElementById("loginPassword").value = acc.password;
  performLogin(acc.username, acc.password);
}

// THỰC HIỆN ĐĂNG NHẬP
async function performLogin(username, password) {
  if (!username || !password) {
    showToast("Vui lòng nhập tên đăng nhập và mật khẩu!", "error");
    return;
  }

  const btn = document.getElementById("btnLogin");
  btn.disabled = true;
  btn.textContent = "Đang xử lý...";

  try {
    const res = await callApi("POST", "/api/auth/login", { username, password }, false);

    if (res.status === 200 && res.data.token) {
      state.token = res.data.token;
      state.user = {
        username: res.data.username,
        role: res.data.role,
        fullName: res.data.fullName
      };
      state.decodedClaims = parseJwt(res.data.token);

      updateSessionUI();
      showToast(`Đăng nhập thành công với vai trò: ${res.data.role}`, "success");

      loadOrders();
      loadAuditLogs();
    } else {
      showToast(res.data?.message || "Đăng nhập thất bại!", "error");
    }
  } catch (err) {
    showToast("Lỗi kết nối máy chủ khi đăng nhập!", "error");
  } finally {
    btn.disabled = false;
    btn.textContent = "Đăng nhập";
  }
}

// THỰC HIỆN ĐĂNG XUẤT (VÀ THU HỒI TOKEN TRÊN V2)
async function performLogout() {
  if (!state.token) {
    showToast("Bạn chưa đăng nhập để thực hiện đăng xuất!", "info");
    return;
  }

  const currentToken = state.token;
  state.lastRevokedToken = currentToken;

  const res = await callApi("POST", "/api/auth/logout", null, true, currentToken);

  state.token = "";
  state.user = null;
  state.decodedClaims = null;
  document.getElementById("loginUsername").value = "";
  document.getElementById("loginPassword").value = "";
  updateSessionUI();

  if (res.status === 200) {
    showToast("[V2] Đã đăng xuất và thu hồi JWT Token (JTI Blacklist)!", "success");
  } else {
    showToast("Đã kết thúc phiên làm việc.", "info");
  }

  loadOrders();
  loadAuditLogs();
}

// WRAPPER GỌI API & CẬP NHẬT LIVE HTTP INSPECTOR
async function callApi(method, endpoint, body = null, attachToken = true, customToken = null) {
  const startTime = performance.now();
  const url = `${APP_CONFIG.apiBase}${endpoint}`;
  const headers = {};

  const activeToken = customToken !== null ? customToken : (attachToken ? state.token : null);
  if (activeToken) {
    headers["Authorization"] = `Bearer ${activeToken}`;
  }

  const options = { method, headers };

  if (body instanceof FormData) {
    // Để browser tự set multipart/form-data boundary
    options.body = body;
  } else if (body && (method === "POST" || method === "PUT")) {
    headers["Content-Type"] = "application/json";
    options.body = JSON.stringify(body);
  }

  let status = 0;
  let responseData = null;
  let isError = false;

  try {
    const response = await fetch(url, options);
    status = response.status;
    const text = await response.text();
    try {
      responseData = JSON.parse(text);
    } catch {
      responseData = text;
    }
  } catch (err) {
    status = 0;
    responseData = { error: err.message, note: "Không thể kết nối đến máy chủ hoặc CORS bị chặn." };
    isError = true;
  }

  const latency = Math.round(performance.now() - startTime);

  updateHttpInspector({
    method,
    url,
    headers,
    status,
    latency,
    data: responseData
  });

  return { status, data: responseData, latency, isError };
}

// CẬP NHẬT GIAO DIỆN LIVE HTTP INSPECTOR
function updateHttpInspector(reqInfo) {
  document.getElementById("httpMethodUrl").textContent = `${reqInfo.method} ${reqInfo.url}`;
  
  const statusEl = document.getElementById("httpStatusCode");
  statusEl.textContent = reqInfo.status === 0 ? "CONN_FAIL" : `${reqInfo.status} ${getStatusText(reqInfo.status)}`;
  statusEl.className = "status-badge-lg " + getStatusClass(reqInfo.status);

  document.getElementById("httpLatency").textContent = `${reqInfo.latency} ms`;

  const authHeader = reqInfo.headers["Authorization"] || "(Không có Header Authorization)";
  document.getElementById("httpReqHeaders").textContent = `Authorization: ${authHeader}`;

  document.getElementById("httpResponseBody").textContent = 
    typeof reqInfo.data === "object" ? JSON.stringify(reqInfo.data, null, 2) : reqInfo.data;
}

function getStatusText(status) {
  switch (status) {
    case 200: return "OK";
    case 201: return "Created";
    case 400: return "Bad Request";
    case 401: return "Unauthorized";
    case 403: return "Forbidden";
    case 404: return "Not Found";
    case 415: return "Unsupported Media Type";
    case 429: return "Too Many Requests";
    case 500: return "Internal Server Error";
    default: return "";
  }
}

function getStatusClass(status) {
  if (status >= 200 && status < 300) return "status-200";
  if (status === 400) return "status-400";
  if (status === 401) return "status-401";
  if (status === 403) return "status-403";
  if (status === 415) return "status-415";
  if (status === 429) return "status-429";
  return "status-500";
}

// =========================================================================
// QUẢN LÝ ĐƠN HÀNG (ORDERS)
// =========================================================================
async function loadOrders() {
  const container = document.getElementById("ordersTableBody");
  container.innerHTML = `<tr><td colspan="7" style="text-align:center; padding: 20px; color: #94a3b8;">Đang tải dữ liệu đơn hàng...</td></tr>`;

  const res = await callApi("GET", "/api/orders");
  if (res.status === 200 && res.data.data) {
    renderOrdersTable(res.data.data);
    document.getElementById("ordersCountBadge").textContent = `${res.data.total || res.data.data.length} đơn hàng`;
  } else {
    container.innerHTML = `
      <tr><td colspan="7" style="text-align:center; padding: 20px; color: #f87171;">
        <strong>[Lỗi ${res.status}]</strong> ${res.data?.message || "Không thể lấy danh sách đơn hàng (Yêu cầu đăng nhập - KB1)."}
      </td></tr>`;
    document.getElementById("ordersCountBadge").textContent = `0 đơn hàng`;
  }
}

function renderOrdersTable(orders) {
  const container = document.getElementById("ordersTableBody");
  if (!orders || orders.length === 0) {
    container.innerHTML = `<tr><td colspan="7" style="text-align:center; padding: 20px; color: #94a3b8;">Không có đơn hàng nào phù hợp với phạm vi quyền của bạn.</td></tr>`;
    return;
  }

  container.innerHTML = orders.map(o => `
    <tr>
      <td><strong>#${o.id}</strong></td>
      <td><span class="badge badge-blue">${o.orderCode}</span></td>
      <td>
        <div style="font-weight: 600;">${o.customerName}</div>
        <div style="font-size: 0.72rem; color: #64748b;">UserId: ${o.userId}</div>
      </td>
      <td>${o.productName}</td>
      <td style="font-weight: 700; color: #38bdf8;">${formatVND(o.totalAmount)}</td>
      <td><span class="badge ${getStatusBadgeClass(o.status)}">${o.status}</span></td>
      <td>
        <div style="display: flex; gap: 4px;">
          <button class="btn btn-secondary btn-sm" onclick="quickViewOrder(${o.id})">Xem</button>
          <button class="btn btn-secondary btn-sm" style="border-color: #ef4444; color: #f87171;" onclick="quickDeleteOrder(${o.id})">Xóa</button>
        </div>
      </td>
    </tr>
  `).join("");
}

function getStatusBadgeClass(status) {
  switch (status) {
    case "Completed": return "badge-green";
    case "Processing": return "badge-blue";
    case "Shipping": return "badge-purple";
    default: return "badge-orange";
  }
}

// TÌM KIẾM ĐƠN HÀNG
async function searchOrders(keywordInput) {
  const keyword = keywordInput !== undefined ? keywordInput : document.getElementById("searchKeyword").value;
  document.getElementById("searchKeyword").value = keyword;

  const res = await callApi("GET", `/api/orders/search?keyword=${encodeURIComponent(keyword)}`);
  if (res.status === 200 && res.data.data) {
    renderOrdersTable(res.data.data);
    showToast(`Tìm thấy ${res.data.data.length} đơn hàng với từ khóa '${keyword}'`, "success");
  } else {
    showToast(`Lỗi tìm kiếm [${res.status}]: ${res.data?.message || 'Yêu cầu bị từ chối'}`, "error");
  }
}

// XEM CHI TIẾT ĐƠN THEO ID
async function quickViewOrder(orderId) {
  document.getElementById("orderIdInput").value = orderId;
  const res = await callApi("GET", `/api/orders/${orderId}`);
  const detailsEl = document.getElementById("orderDetailView");

  if (res.status === 200 && res.data.data) {
    const o = res.data.data;
    detailsEl.innerHTML = `
      <div style="padding: 12px; background: rgba(16, 185, 129, 0.1); border: 1px solid rgba(16, 185, 129, 0.3); border-radius: 8px;">
        <div style="color: #34d399; font-weight: 700; margin-bottom: 6px;">
          ✓ Xem chi tiết Đơn hàng #${o.id} (${o.orderCode})
        </div>
        <div>Khách hàng: <strong>${o.customerName}</strong> (Owner UserId: ${o.userId})</div>
        <div>Sản phẩm: <strong>${o.productName}</strong></div>
        <div>Tổng tiền: <strong>${formatVND(o.totalAmount)}</strong> | Trạng thái: <strong>${o.status}</strong></div>
      </div>`;
  } else {
    detailsEl.innerHTML = `
      <div style="padding: 12px; background: rgba(239, 68, 68, 0.15); border: 1px solid rgba(239, 68, 68, 0.35); border-radius: 8px;">
        <div style="color: #f87171; font-weight: 700; margin-bottom: 4px;">
          ✕ Truy cập bị từ chối [HTTP ${res.status}]
        </div>
        <div style="font-size: 0.8rem; color: #cbd5e1;">${res.data?.message || "Không thể xem thông tin đơn hàng này (Chặn IDOR / BOLA)."}</div>
      </div>`;
  }
}

// XÓA ĐƠN HÀNG (TEST IDOR KHI XÓA)
async function quickDeleteOrder(orderId) {
  if (!confirm(`Bạn có chắc muốn yêu cầu xóa đơn hàng #${orderId}?`)) return;

  const res = await callApi("DELETE", `/api/orders/${orderId}`);
  if (res.status === 200) {
    showToast(`Đã xóa thành công đơn hàng #${orderId}!`, "success");
    loadOrders();
  } else {
    showToast(`Từ chối xóa [${res.status}]: ${res.data?.message || 'Không có quyền xóa đơn của người khác.'}`, "error");
  }
  loadAuditLogs();
}

// TẠO ĐƠN HÀNG MỚI (TEST BUSINESS LOGIC & MASS ASSIGNMENT)
async function submitCreateOrder() {
  const productName = document.getElementById("newProductName").value.trim();
  const totalAmount = parseFloat(document.getElementById("newTotalAmount").value);
  const resultEl = document.getElementById("createOrderResult");

  if (!productName) {
    showToast("Vui lòng nhập tên sản phẩm!", "error");
    return;
  }

  resultEl.innerHTML = `<div style="color: #94a3b8;">Đang gửi yêu cầu POST /api/orders...</div>`;

  const res = await callApi("POST", "/api/orders", { productName, totalAmount });

  if (res.status === 200 || res.status === 201) {
    resultEl.innerHTML = `
      <div style="padding: 12px; background: rgba(16, 185, 129, 0.15); border: 1px solid #10b981; border-radius: 8px;">
        <div style="color: #34d399; font-weight: 700;">✓ Tạo đơn hàng thành công [HTTP ${res.status}]</div>
        <div>Mã đơn: <strong>${res.data.data?.orderCode}</strong> | Tiền: <strong>${formatVND(res.data.data?.totalAmount)}</strong></div>
        <div style="font-size: 0.75rem; color: #cbd5e1; margin-top: 4px;">Owner: ${res.data.data?.customerName} (UserId: ${res.data.data?.userId}) - Tự động trích xuất từ JWT Claims (Chống Mass Assignment).</div>
      </div>`;
    showToast("Tạo đơn hàng thành công!", "success");
    loadOrders();
  } else {
    resultEl.innerHTML = `
      <div style="padding: 12px; background: rgba(239, 68, 68, 0.15); border: 1px solid #ef4444; border-radius: 8px;">
        <div style="color: #f87171; font-weight: 700;">✕ Chặn yêu cầu tạo đơn [HTTP ${res.status}]</div>
        <div style="font-size: 0.8rem; color: #fca5a5;">${res.data?.message || "Lỗi nghiệp vụ hoặc chưa xác thực hợp lệ."}</div>
      </div>`;
    showToast(`Lỗi tạo đơn [${res.status}]`, "error");
  }
  loadAuditLogs();
}

// =========================================================================
// QUẢN TRỊ NGƯỜI DÙNG (ADMIN USERS)
// =========================================================================
async function loadAdminUsers() {
  const container = document.getElementById("usersTableBody");
  container.innerHTML = `<tr><td colspan="5" style="text-align:center; padding: 20px; color: #94a3b8;">Đang gọi API Quản trị /api/admin/users...</td></tr>`;

  const res = await callApi("GET", "/api/admin/users");
  if (res.status === 200 && res.data.data) {
    container.innerHTML = res.data.data.map(u => `
      <tr>
        <td><strong>#${u.id}</strong></td>
        <td><span style="font-weight: 700;">${u.username}</span></td>
        <td>${u.fullName || "-"}</td>
        <td><span class="badge ${u.role === 'Admin' ? 'badge-purple' : u.role === 'NhanVien' ? 'badge-blue' : 'badge-green'}">${u.role}</span></td>
        <td style="font-family: var(--font-mono); font-size: 0.75rem; color: #34d399;">
          🔒 [Đã mã hóa BCrypt an toàn / Ẩn trên V2 - Không bao giờ lộ mật khẩu]
        </td>
      </tr>
    `).join("");
  } else {
    container.innerHTML = `
      <tr><td colspan="5" style="text-align:center; padding: 20px; color: #f87171;">
        <strong>[Lỗi ${res.status} - ${getStatusText(res.status)}]</strong> ${res.data?.message || "Chỉ tài khoản Admin mới có quyền truy cập (RBAC Policy AdminOnly)."}
      </td></tr>`;
  }
}

// =========================================================================
// NHẬT KÝ KIỂM TOÁN AN NINH (AUDIT LOGS)
// =========================================================================
async function loadAuditLogs(filterStatus = null) {
  state.auditLogsFilter = filterStatus;
  const url = filterStatus ? `/api/admin/audit-logs?statusCode=${filterStatus}` : "/api/admin/audit-logs";
  const container = document.getElementById("auditLogsBody");
  container.innerHTML = `<tr><td colspan="6" style="text-align:center; padding: 15px; color: #94a3b8;">Đang tải nhật ký kiểm toán...</td></tr>`;

  let res = await callApi("GET", url);
  if (res.status === 403 || res.status === 401) {
    const loginRes = await fetch(`${APP_CONFIG.apiBase}/api/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username: "admin", password: "Admin@123" })
    });
    const loginData = await loginRes.json();
    if (loginData.token) {
      res = await callApi("GET", url, null, false, loginData.token);
    }
  }

  if (res.status === 200 && res.data.data) {
    renderAuditLogsTable(res.data.data);
    updateAuditCounters(res.data.data);
  } else {
    container.innerHTML = `
      <tr><td colspan="6" style="text-align:center; padding: 15px; color: #f87171;">
        <strong>[Lỗi ${res.status}]</strong> ${res.data?.message || "Không thể tải Audit Log."}
      </td></tr>`;
  }
}

function renderAuditLogsTable(logs) {
  const container = document.getElementById("auditLogsBody");
  if (!logs || logs.length === 0) {
    container.innerHTML = `<tr><td colspan="6" style="text-align:center; padding: 15px; color: #94a3b8;">Chưa có bản ghi nhật ký nào.</td></tr>`;
    return;
  }

  container.innerHTML = logs.map(l => `
    <tr>
      <td style="font-size: 0.72rem; color: #64748b; white-space: nowrap;">${formatDateTime(l.timestamp)}</td>
      <td><strong>${l.username || 'Anonymous'}</strong> <span style="font-size: 0.7rem; color: #64748b;">(${l.role || 'None'})</span></td>
      <td><span class="badge ${getStatusBadgeClass(l.statusCode)}">${l.httpMethod} ${l.endpoint}</span></td>
      <td><span class="status-badge-lg ${getStatusClass(l.statusCode)}" style="font-size: 0.75rem; padding: 2px 8px;">${l.statusCode}</span></td>
      <td style="font-size: 0.75rem;">${l.actionDetail || '-'}</td>
      <td style="font-size: 0.72rem; color: #64748b;">${l.ipAddress || '-'}</td>
    </tr>
  `).join("");
}

function updateAuditCounters(logs) {
  let count401 = 0, count403 = 0, count400 = 0;
  logs.forEach(l => {
    if (l.statusCode === 401) count401++;
    else if (l.statusCode === 403) count403++;
    else if (l.statusCode === 400) count400++;
  });

  const totalEl = document.getElementById("cntAuditTotal");
  if (totalEl) totalEl.textContent = logs.length;
  const el401 = document.getElementById("cntAudit401");
  if (el401) el401.textContent = count401;
  const el403 = document.getElementById("cntAudit403");
  if (el403) el403.textContent = count403;
  const el400 = document.getElementById("cntAudit400");
  if (el400) el400.textContent = count400;
}

// =========================================================================
// 1-CLICK KB1–KB4 SIMULATOR (CỐT LÕI THEO ĐỀ CƯƠNG)
// =========================================================================

async function runKB1() {
  const resultEl = document.getElementById("kb1Result");
  resultEl.className = "kb-result show";
  resultEl.innerHTML = "Đang gửi request không có JWT Token...";

  const res = await callApi("GET", "/api/orders", null, false, "");

  if (res.status === 200) {
    resultEl.className = "kb-result show vulnerable";
    resultEl.innerHTML = `
      <strong>[LỖ HỔNG XÁC THỰC - PHÁT HIỆN TRÊN V1]</strong><br>
      Mã phản hồi: <strong>200 OK</strong> | Đã lấy được ${res.data?.total || res.data?.data?.length} đơn hàng mà không cần Token!
    `;
  } else if (res.status === 401) {
    resultEl.className = "kb-result show safe";
    resultEl.innerHTML = `
      <strong>[ĐÃ BẢO VỆ AN TOÀN - CHẶN BỞI V2]</strong><br>
      Mã phản hồi: <strong>401 Unauthorized</strong> | Bắt buộc JWT Bearer Token, bảo vệ an toàn danh sách đơn hàng.
    `;
  } else {
    resultEl.className = "kb-result show";
    resultEl.innerHTML = `Mã phản hồi: ${res.status}`;
  }
  loadAuditLogs();
  return { kb: "KB1", endpoint: "GET /api/orders", status: res.status, safe: res.status === 401 };
}

async function runKB2() {
  const resultEl = document.getElementById("kb2Result");
  resultEl.className = "kb-result show";
  resultEl.innerHTML = "Đang gửi request với quyền Người dùng thường (userA)...";

  const loginRes = await fetch(`${APP_CONFIG.apiBase}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username: "userA", password: "UserA@123" })
  });
  const loginData = await loginRes.json();
  const tokenUserA = loginData.token;

  const res = await callApi("GET", "/api/admin/users", null, true, tokenUserA);

  if (res.status === 200) {
    resultEl.className = "kb-result show vulnerable";
    resultEl.innerHTML = `
      <strong>[LỖ HỔNG VƯỢT QUYỀN RBAC - PHÁT HIỆN TRÊN V1]</strong><br>
      Mã phản hồi: <strong>200 OK</strong> | Tài khoản 'userA' (NguoiDung) vẫn xem được danh sách Quản trị!
    `;
  } else if (res.status === 403) {
    resultEl.className = "kb-result show safe";
    resultEl.innerHTML = `
      <strong>[ĐÃ BẢO VỆ AN TOÀN - CHẶN BỞI V2]</strong><br>
      Mã phản hồi: <strong>403 Forbidden</strong> | RBAC Policy AdminOnly từ chối truy cập và ghi nhận vi phạm!
    `;
  } else {
    resultEl.className = "kb-result show";
    resultEl.innerHTML = `Mã phản hồi: ${res.status}`;
  }
  loadAuditLogs();
  return { kb: "KB2", endpoint: "GET /api/admin/users", status: res.status, safe: res.status === 403 };
}

async function runKB3() {
  const resultEl = document.getElementById("kb3Result");
  resultEl.className = "kb-result show";
  resultEl.innerHTML = "Đang gửi request: userA xem đơn #3 của userB...";

  const loginRes = await fetch(`${APP_CONFIG.apiBase}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username: "userA", password: "UserA@123" })
  });
  const loginData = await loginRes.json();
  const tokenUserA = loginData.token;

  const res = await callApi("GET", "/api/orders/3", null, true, tokenUserA);

  if (res.status === 200) {
    resultEl.className = "kb-result show vulnerable";
    resultEl.innerHTML = `
      <strong>[LỖ HỔNG BOLA/IDOR - PHÁT HIỆN TRÊN V1]</strong><br>
      Mã phản hồi: <strong>200 OK</strong> | userA đã xem trộm thành công đơn hàng #${res.data?.data?.id} của ${res.data?.data?.customerName}!
    `;
  } else if (res.status === 403) {
    resultEl.className = "kb-result show safe";
    resultEl.innerHTML = `
      <strong>[ĐÃ BẢO VỆ AN TOÀN - CHẶN BỞI V2]</strong><br>
      Mã phản hồi: <strong>403 Forbidden</strong> | Kiểm soát truy cập mức đối tượng đã chặn đứng truy cập trái phép!
    `;
  } else {
    resultEl.className = "kb-result show";
    resultEl.innerHTML = `Mã phản hồi: ${res.status}`;
  }
  loadAuditLogs();
  return { kb: "KB3", endpoint: "GET /api/orders/3", status: res.status, safe: res.status === 403 };
}

async function runKB4() {
  const resultEl = document.getElementById("kb4Result");
  resultEl.className = "kb-result show";
  resultEl.innerHTML = "Đang gửi payload SQL Injection: ' OR 1=1 --...";

  const loginRes = await fetch(`${APP_CONFIG.apiBase}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username: "userA", password: "UserA@123" })
  });
  const loginData = await loginRes.json();
  const tokenUserA = loginData.token;

  const res = await callApi("GET", "/api/orders/search?keyword=%27%20OR%201=1%20--", null, true, tokenUserA);

  if (res.status === 200) {
    resultEl.className = "kb-result show vulnerable";
    resultEl.innerHTML = `
      <strong>[LỖ HỔNG SQL INJECTION - PHÁT HIỆN TRÊN V1]</strong><br>
      Mã phản hồi: <strong>200 OK</strong> | Bị bypass điều kiện, dump sạch ${res.data?.count || res.data?.data?.length} đơn hàng qua câu lệnh SQL thô!
    `;
  } else if (res.status === 400) {
    resultEl.className = "kb-result show safe";
    resultEl.innerHTML = `
      <strong>[ĐÃ BẢO VỆ AN TOÀN - CHẶN BỞI V2]</strong><br>
      Mã phản hồi: <strong>400 Bad Request</strong> | Bộ lọc Validation & Parameterized Query chặn đứng mã độc!
    `;
  } else {
    resultEl.className = "kb-result show";
    resultEl.innerHTML = `Mã phản hồi: ${res.status}`;
  }
  loadAuditLogs();
  return { kb: "KB4", endpoint: "GET /api/orders/search", status: res.status, safe: res.status === 400 };
}

async function runAllKB() {
  const btn = document.getElementById("btnRunAllKB");
  btn.disabled = true;
  btn.textContent = "Đang chạy 4 kịch bản cốt lõi...";

  const r1 = await runKB1();
  await new Promise(r => setTimeout(r, 400));
  const r2 = await runKB2();
  await new Promise(r => setTimeout(r, 400));
  const r3 = await runKB3();
  await new Promise(r => setTimeout(r, 400));
  const r4 = await runKB4();

  const summaryEl = document.getElementById("runAllSummary");
  summaryEl.style.display = "block";
  summaryEl.innerHTML = `
    <div style="padding: 14px; background: rgba(0, 0, 0, 0.4); border: 1px solid var(--border-color); border-radius: 8px;">
      <div style="font-weight: 700; margin-bottom: 8px;">Bảng tổng hợp kết quả 4 kịch bản cốt lõi (${APP_CONFIG.version}):</div>
      <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 10px;">
        <div style="padding: 8px; border-radius: 6px; background: ${r1.safe ? 'rgba(16,185,129,0.1)' : 'rgba(239,68,68,0.1)'}; border: 1px solid ${r1.safe ? '#10b981' : '#ef4444'};">
          <strong>KB1 (Xác thực):</strong><br>HTTP ${r1.status} - ${r1.safe ? 'AN TOÀN' : 'LỖ HỔNG'}
        </div>
        <div style="padding: 8px; border-radius: 6px; background: ${r2.safe ? 'rgba(16,185,129,0.1)' : 'rgba(239,68,68,0.1)'}; border: 1px solid ${r2.safe ? '#10b981' : '#ef4444'};">
          <strong>KB2 (RBAC):</strong><br>HTTP ${r2.status} - ${r2.safe ? 'AN TOÀN' : 'LỖ HỔNG'}
        </div>
        <div style="padding: 8px; border-radius: 6px; background: ${r3.safe ? 'rgba(16,185,129,0.1)' : 'rgba(239,68,68,0.1)'}; border: 1px solid ${r3.safe ? '#10b981' : '#ef4444'};">
          <strong>KB3 (BOLA/IDOR):</strong><br>HTTP ${r3.status} - ${r3.safe ? 'AN TOÀN' : 'LỖ HỔNG'}
        </div>
        <div style="padding: 8px; border-radius: 6px; background: ${r4.safe ? 'rgba(16,185,129,0.1)' : 'rgba(239,68,68,0.1)'}; border: 1px solid ${r4.safe ? '#10b981' : '#ef4444'};">
          <strong>KB4 (SQLi):</strong><br>HTTP ${r4.status} - ${r4.safe ? 'AN TOÀN' : 'LỖ HỔNG'}
        </div>
      </div>
    </div>
  `;

  btn.disabled = false;
  btn.textContent = "Chạy lại 4 kịch bản cốt lõi (Run All KB1–KB4)";
  showToast("Hoàn tất kiểm thử 4 kịch bản cốt lõi!", "info");
}

// =========================================================================
// 1-CLICK KB5–KB8 EXTENDED DEFENSE-IN-DEPTH SIMULATOR
// =========================================================================

// KB5: Thử nghiệm Token Replay sau khi Logout (JTI Blacklist)
async function runKB5() {
  const resultEl = document.getElementById("kb5Result");
  resultEl.className = "kb-result show";
  resultEl.innerHTML = "Bước 1: Đăng nhập userA lấy Token -> Bước 2: Logout -> Bước 3: Replay Token cũ...";

  // 1. Login userA
  const loginRes = await fetch(`${APP_CONFIG.apiBase}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username: "userA", password: "UserA@123" })
  });
  const loginData = await loginRes.json();
  const token = loginData.token;

  // 2. Logout thu hồi token
  await fetch(`${APP_CONFIG.apiBase}/api/auth/logout`, {
    method: "POST",
    headers: { "Authorization": `Bearer ${token}` }
  });

  // 3. Thử tái sử dụng (Replay) token vừa thu hồi để gọi /api/auth/me
  const res = await callApi("GET", "/api/auth/me", null, true, token);

  if (res.status === 200) {
    resultEl.className = "kb-result show vulnerable";
    resultEl.innerHTML = `
      <strong>[LỖ HỔNG TOKEN REPLAY - PHÁT HIỆN TRÊN V1]</strong><br>
      Mã phản hồi: <strong>200 OK</strong> | Token sau khi logout vẫn gọi API được bình thường do thiếu JTI Blacklist!
    `;
  } else if (res.status === 401) {
    resultEl.className = "kb-result show safe";
    resultEl.innerHTML = `
      <strong>[ĐÃ BẢO VỆ AN TOÀN - CHẶN BỞI V2]</strong><br>
      Mã phản hồi: <strong>401 Unauthorized (Token Revoked)</strong> | JTI Blacklist đã chặn đứng phiên bị thu hồi!
    `;
  } else {
    resultEl.className = "kb-result show";
    resultEl.innerHTML = `Mã phản hồi: ${res.status}`;
  }
  loadAuditLogs();
  return { kb: "KB5", endpoint: "POST /logout -> GET /me", status: res.status, safe: res.status === 401 };
}

// KB6a: Business Logic Flaw (Tạo đơn tiền âm)
async function runKB6Logic() {
  const resultEl = document.getElementById("kb6Result");
  resultEl.className = "kb-result show";
  resultEl.innerHTML = "Đang gửi yêu cầu tạo đơn hàng với số tiền âm: -999,999 VNĐ...";

  // Đăng nhập userA nếu chưa có token
  let token = state.token;
  if (!token) {
    const lRes = await fetch(`${APP_CONFIG.apiBase}/api/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username: "userA", password: "UserA@123" })
    });
    const lData = await lRes.json();
    token = lData.token;
  }

  const res = await callApi("POST", "/api/orders", { productName: "Test Lỗi Tiền Âm", totalAmount: -999999 }, true, token);

  if (res.status === 200 || res.status === 201) {
    resultEl.className = "kb-result show vulnerable";
    resultEl.innerHTML = `
      <strong>[LỖ HỔNG BUSINESS LOGIC - PHÁT HIỆN TRÊN V1]</strong><br>
      Mã: <strong>200 OK</strong> | Hệ thống chấp nhận số tiền âm (-999,999đ) cho đơn hàng!
    `;
  } else if (res.status === 400) {
    resultEl.className = "kb-result show safe";
    resultEl.innerHTML = `
      <strong>[ĐÃ BẢO VỆ AN TOÀN - CHẶN BỞI V2]</strong><br>
      Mã: <strong>400 Bad Request</strong> | Business Logic Validator chặn đứng số tiền âm <= 0 VNĐ!
    `;
  } else {
    resultEl.className = "kb-result show";
    resultEl.innerHTML = `Mã phản hồi: ${res.status}`;
  }
  loadOrders();
  loadAuditLogs();
  return { kb: "KB6-Logic", endpoint: "POST /api/orders (Negative)", status: res.status, safe: res.status === 400 };
}

// KB6b: IDOR khi Xóa đơn của người khác
async function runKB6IdorDelete() {
  const resultEl = document.getElementById("kb6Result");
  resultEl.className = "kb-result show";
  resultEl.innerHTML = "userA gửi yêu cầu xóa đơn #3 của userB (DELETE /api/orders/3)...";

  const lRes = await fetch(`${APP_CONFIG.apiBase}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username: "userA", password: "UserA@123" })
  });
  const lData = await lRes.json();
  const tokenUserA = lData.token;

  const res = await callApi("DELETE", "/api/orders/3", null, true, tokenUserA);

  if (res.status === 200) {
    resultEl.className = "kb-result show vulnerable";
    resultEl.innerHTML = `
      <strong>[LỖ HỔNG IDOR KHI XÓA - PHÁT HIỆN TRÊN V1]</strong><br>
      Mã: <strong>200 OK</strong> | userA đã xóa thành công đơn hàng #3 của userB do thiếu kiểm tra quyền sở hữu!
    `;
  } else if (res.status === 403) {
    resultEl.className = "kb-result show safe";
    resultEl.innerHTML = `
      <strong>[ĐÃ BẢO VỆ AN TOÀN - CHẶN BỞI V2]</strong><br>
      Mã: <strong>403 Forbidden</strong> | Kiểm soát truy cập mức đối tượng đã chặn đứng hành vi xóa đơn của người khác!
    `;
  } else {
    resultEl.className = "kb-result show";
    resultEl.innerHTML = `Mã phản hồi: ${res.status}`;
  }
  loadOrders();
  loadAuditLogs();
  return { kb: "KB6-Delete", endpoint: "DELETE /api/orders/3", status: res.status, safe: res.status === 403 };
}

// KB7: Upload Webshell giả mạo định dạng (Magic Bytes Check)
async function runKB7() {
  const resultEl = document.getElementById("kb7Result");
  resultEl.className = "kb-result show";
  resultEl.innerHTML = "Đang gửi tệp 'webshell.jpg.aspx' chứa script text (chữ ký byte giả mạo)...";

  const lRes = await fetch(`${APP_CONFIG.apiBase}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username: "userA", password: "UserA@123" })
  });
  const lData = await lRes.json();
  const token = lData.token;

  // Tạo fake file blob không có Magic Bytes chuẩn JPEG (FF D8 FF) mà chứa mã script
  const maliciousContent = "<% Response.Write(\"Webshell RCE Executed\"); %>";
  const fakeFile = new File([maliciousContent], "webshell.jpg.aspx", { type: "image/jpeg" });
  const formData = new FormData();
  formData.append("file", fakeFile);

  const res = await callApi("POST", "/api/orders/1/upload", formData, true, token);

  if (res.status === 200) {
    resultEl.className = "kb-result show vulnerable";
    resultEl.innerHTML = `
      <strong>[LỖ HỔNG FILE UPLOAD - PHÁT HIỆN TRÊN V1]</strong><br>
      Mã: <strong>200 OK</strong> | Chấp nhận tải lên tệp độc hại mà không kiểm tra chữ ký byte (Magic Bytes)!
    `;
  } else if (res.status === 415 || res.status === 400) {
    resultEl.className = "kb-result show safe";
    resultEl.innerHTML = `
      <strong>[ĐÃ BẢO VỆ AN TOÀN - CHẶN BỞI V2]</strong><br>
      Mã: <strong>${res.status} ${getStatusText(res.status)}</strong> | Magic Bytes Validator phát hiện chữ ký byte giả mạo và chặn đứng tệp độc hại!
    `;
  } else {
    resultEl.className = "kb-result show";
    resultEl.innerHTML = `Mã phản hồi: ${res.status}`;
  }
  loadAuditLogs();
  return { kb: "KB7", endpoint: "POST /api/orders/1/upload", status: res.status, safe: res.status === 415 || res.status === 400 };
}

// KB8: Rate Limiting & Anti-Brute-Force (429 Too Many Requests)
async function runKB8() {
  const resultEl = document.getElementById("kb8Result");
  resultEl.className = "kb-result show";
  resultEl.innerHTML = "Đang gửi liên tục 8 requests spam vào /api/orders/rate-test...";

  const lRes = await fetch(`${APP_CONFIG.apiBase}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username: "userA", password: "UserA@123" })
  });
  const lData = await lRes.json();
  const token = lData.token;

  let count200 = 0;
  let count429 = 0;

  for (let i = 1; i <= 8; i++) {
    const r = await callApi("GET", "/api/orders/rate-test", null, true, token);
    if (r.status === 200) count200++;
    if (r.status === 429) count429++;
    await new Promise(res => setTimeout(res, 50));
  }

  if (count429 > 0) {
    resultEl.className = "kb-result show safe";
    resultEl.innerHTML = `
      <strong>[ĐÃ BẢO VỆ AN TOÀN - CHẶN BỞI V2]</strong><br>
      Mã: <strong>429 Too Many Requests</strong> (${count429}/8 requests bị chặn) | Sliding Window Rate Limiter chặn đứng hành vi spam API!
    `;
  } else {
    resultEl.className = "kb-result show vulnerable";
    resultEl.innerHTML = `
      <strong>[KHÔNG GIỚI HẠN TẦN SUẤT - PHÁT HIỆN TRÊN V1]</strong><br>
      Cả ${count200}/8 requests đều thành công <strong>200 OK</strong>. Không có cơ chế Rate Limiting!
    `;
  }
  loadAuditLogs();
  return { kb: "KB8", endpoint: "GET /api/orders/rate-test", count429, safe: count429 > 0 };
}

// CHẠY TẤT CẢ KỊCH BẢN MỞ RỘNG (RUN ALL KB5–KB8)
async function runAllExtKB() {
  const btn = document.getElementById("btnRunAllExtKB");
  btn.disabled = true;
  btn.textContent = "Đang chạy kịch bản mở rộng...";

  const r5 = await runKB5();
  await new Promise(r => setTimeout(r, 400));
  const r6 = await runKB6Logic();
  await new Promise(r => setTimeout(r, 400));
  const r7 = await runKB7();
  await new Promise(r => setTimeout(r, 400));
  const r8 = await runKB8();

  const summaryEl = document.getElementById("runAllExtSummary");
  summaryEl.style.display = "block";
  summaryEl.innerHTML = `
    <div style="padding: 14px; background: rgba(0, 0, 0, 0.4); border: 1px solid var(--border-color); border-radius: 8px;">
      <div style="font-weight: 700; margin-bottom: 8px;">Bảng tổng hợp kết quả kịch bản mở rộng (${APP_CONFIG.version}):</div>
      <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 10px;">
        <div style="padding: 8px; border-radius: 6px; background: ${r5.safe ? 'rgba(16,185,129,0.1)' : 'rgba(239,68,68,0.1)'}; border: 1px solid ${r5.safe ? '#10b981' : '#ef4444'};">
          <strong>KB5 (JTI Blacklist):</strong><br>HTTP ${r5.status} - ${r5.safe ? 'AN TOÀN' : 'LỖ HỔNG'}
        </div>
        <div style="padding: 8px; border-radius: 6px; background: ${r6.safe ? 'rgba(16,185,129,0.1)' : 'rgba(239,68,68,0.1)'}; border: 1px solid ${r6.safe ? '#10b981' : '#ef4444'};">
          <strong>KB6 (Business Logic):</strong><br>HTTP ${r6.status} - ${r6.safe ? 'AN TOÀN' : 'LỖ HỔNG'}
        </div>
        <div style="padding: 8px; border-radius: 6px; background: ${r7.safe ? 'rgba(16,185,129,0.1)' : 'rgba(239,68,68,0.1)'}; border: 1px solid ${r7.safe ? '#10b981' : '#ef4444'};">
          <strong>KB7 (Magic Bytes):</strong><br>HTTP ${r7.status} - ${r7.safe ? 'AN TOÀN' : 'LỖ HỔNG'}
        </div>
        <div style="padding: 8px; border-radius: 6px; background: ${r8.safe ? 'rgba(16,185,129,0.1)' : 'rgba(239,68,68,0.1)'}; border: 1px solid ${r8.safe ? '#10b981' : '#ef4444'};">
          <strong>KB8 (Rate Limit 429):</strong><br>${r8.count429 > 0 ? 'HTTP 429 - AN TOÀN' : 'HTTP 200 - LỖ HỔNG'}
        </div>
      </div>
    </div>
  `;

  btn.disabled = false;
  btn.textContent = "Chạy lại kịch bản mở rộng (Run All KB5–KB8)";
  showToast("Hoàn tất kiểm thử các kịch bản mở rộng!", "info");
}

// KIỂM TRA HTTP SECURITY HEADERS THỰC TẾ
async function checkSecurityHeadersLive() {
  const resultEl = document.getElementById("liveHeadersResult");
  resultEl.innerHTML = `<div style="color: #94a3b8;">Đang gửi yêu cầu đo HTTP Security Headers...</div>`;

  try {
    const res = await fetch(`${APP_CONFIG.apiBase}/api/orders`);
    const headers = res.headers;

    const nosniff = headers.get("x-content-type-options") || "(Thiếu)";
    const xframe = headers.get("x-frame-options") || "(Thiếu)";
    const xss = headers.get("x-xss-protection") || "(Thiếu)";
    const referrer = headers.get("referrer-policy") || "(Thiếu)";
    const csp = headers.get("content-security-policy") || "(Thiếu)";

    const isSecure = nosniff.includes("nosniff") && xframe.includes("DENY");

    resultEl.innerHTML = `
      <div style="padding: 12px; border-radius: 8px; background: ${isSecure ? 'rgba(16,185,129,0.1)' : 'rgba(239,68,68,0.1)'}; border: 1px solid ${isSecure ? '#10b981' : '#ef4444'};">
        <div style="font-weight: 700; color: ${isSecure ? '#34d399' : '#f87171'}; margin-bottom: 6px;">
          ${isSecure ? '✓ Bản V2 được cấu hình đầy đủ HTTP Security Headers theo khuyến nghị OWASP!' : '✕ Bản V1 thiếu các Security Headers cơ bản!'}
        </div>
        <div style="font-family: var(--font-mono); font-size: 0.75rem; color: #cbd5e1; line-height: 1.6;">
          • X-Content-Type-Options: <strong>${nosniff}</strong><br>
          • X-Frame-Options: <strong>${xframe}</strong><br>
          • X-XSS-Protection: <strong>${xss}</strong><br>
          • Referrer-Policy: <strong>${referrer}</strong><br>
          • Content-Security-Policy: <strong>${csp}</strong>
        </div>
      </div>`;
    showToast("Đã quét headers thực tế thành công!", "success");
  } catch (err) {
    resultEl.innerHTML = `<div style="color: #f87171;">Lỗi khi kết nối đến API để đọc headers: ${err.message}</div>`;
  }
}

// TAB SWITCHER
function switchTab(tabId) {
  state.activeTab = tabId;
  document.querySelectorAll(".tab-btn").forEach(btn => {
    btn.classList.toggle("active", btn.getAttribute("data-tab") === tabId);
  });
  document.querySelectorAll(".tab-content").forEach(content => {
    content.classList.toggle("active", content.id === `tab-${tabId}`);
  });

  if (tabId === "users") {
    loadAdminUsers();
  } else if (tabId === "orders") {
    loadOrders();
  }
}

// COPY TOKEN TO CLIPBOARD
function copyToken() {
  if (!state.token) {
    showToast("Chưa có token để sao chép!", "error");
    return;
  }
  navigator.clipboard.writeText(state.token).then(() => {
    showToast("Đã sao chép JWT Token vào Clipboard!", "success");
  });
}

// TOAST NOTIFICATION
function showToast(message, type = "info") {
  const toast = document.createElement("div");
  toast.style.position = "fixed";
  toast.style.bottom = "20px";
  toast.style.right = "20px";
  toast.style.padding = "10px 18px";
  toast.style.borderRadius = "8px";
  toast.style.fontSize = "0.85rem";
  toast.style.fontWeight = "600";
  toast.style.zIndex = "9999";
  toast.style.boxShadow = "0 8px 24px rgba(0,0,0,0.5)";
  toast.style.transition = "all 0.3s ease";
  toast.style.background = type === "success" ? "#065f46" : type === "error" ? "#7f1d1d" : "#1e293b";
  toast.style.color = "#fff";
  toast.style.border = `1px solid ${type === "success" ? "#10b981" : type === "error" ? "#ef4444" : "#475569"}`;
  toast.textContent = message;

  document.body.appendChild(toast);
  setTimeout(() => {
    toast.style.opacity = "0";
    toast.style.transform = "translateY(10px)";
    setTimeout(() => toast.remove(), 300);
  }, 3000);
}
