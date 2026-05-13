# 📋 BÁO CÁO PHÂN TÍCH TRÙNG LẶP CODE — DỰ ÁN HRM

> **Ngày:** 11/05/2026 · **Phạm vi:** GUI (WinForms) · BLL (Services) · DAL (Repositories)  
> **File chính bị ảnh hưởng:** `frmMain.cs` (3201 dòng), 7 UserControl, 4 Repository, 3 Service  
> **Ước tính code thừa:** ~800+ dòng có thể loại bỏ

---

## MỤC LỤC

1. [Vấn đề nghiêm trọng nhất (BUG)](#1-vấn-đề-nghiêm-trọng-nhất-bug)
2. [Trùng lặp Giao diện (UI)](#2-trùng-lặp-giao-diện-ui)
3. [Trùng lặp Logic xử lý](#3-trùng-lặp-logic-xử-lý)
4. [Trùng lặp Data Access](#4-trùng-lặp-data-access)
5. [Anti-pattern: Service Locator](#5-anti-pattern-service-locator)
6. [frmMain.cs — "God Class"](#6-frmMaincs--god-class)
7. [Bảng tổng hợp](#7-bảng-tổng-hợp)
8. [Hướng dẫn sửa](#8-hướng-dẫn-sửa-cho-đội-nhóm)

---

## 1. Vấn đề nghiêm trọng nhất (BUG)

> [!CAUTION]
> **Kiểm tra quyền Admin viết 4 cách khác nhau → gây lỗi phân quyền thực tế.**

Trong toàn bộ dự án có **4 cách** kiểm tra quyền Admin, dẫn đến logic phân quyền **không nhất quán**:

| Cách viết | Có role "HR"? | Dùng ở |
|-----------|:---:|--------|
| `UIHelper.IsAdmin(_session)` | ❌ | ucNhanVien, ucPhongBan, ucChamCong, ucNghiPhep, ucBangLuong, ucThuongPhat |
| `_session?.VaiTro == "Admin" \|\| == "Quản trị viên"` (inline) | ❌ | frmMain (dòng 372, 385, 398, 2248, 2262, 2275), ucTinTuyenDung |
| `IsAdminSession()` (hàm riêng frmMain) | ❌ | frmMain (dòng 567, 580, 593, 706, 920, 1096, 1813) |
| `== "Admin" \|\| == "Quản trị viên" \|\| == "HR"` | ✅ | ucUngVien (dòng 57), ucPhongVan (dòng 62, 66) |

**Hậu quả:** Người có role `"HR"` có quyền Thêm/Sửa/Xóa ở module Ứng viên và Phỏng vấn, nhưng **không có quyền** ở Tin tuyển dụng — dù cả 3 đều thuộc nhóm Tuyển dụng.

**Cách sửa:** Thống nhất dùng `UIHelper.IsAdmin()`, bổ sung thêm hàm `UIHelper.IsHROrAdmin()` nếu cần.

---

## 2. Trùng lặp Giao diện (UI)

### 2.1 `CreateStyledDataGridView` — Viết 2 bản, phải dùng 1

Hàm tạo DataGridView (~50 dòng) tồn tại ở 2 nơi:

| Định nghĩa | File | Dòng |
|-------------|------|------|
| ✅ Bản chính (nên dùng) | [UIHelper.cs](file:///d:/dotnet%20project/db/src/HRM.GUI/Helpers/UIHelper.cs#L83-L132) | 83–132 |
| ❌ Bản copy (cần xóa) | [frmMain.cs](file:///d:/dotnet%20project/db/src/HRM.GUI/Forms/Main/frmMain.cs#L226-L277) | 226–277 |

`frmMain` dùng bản copy của mình ở 7 chỗ (dòng 402, 597, 768, 969, 2086, 2279, 2443). Các UserControl mới (ucNhanVien, ucPhongBan...) đã dùng đúng `UIHelper` — chỉ cần xóa bản trong frmMain.

### 2.2 `CreateChamCongHistoryGrid` — Tương tự

| Định nghĩa | File | Dòng |
|-------------|------|------|
| ✅ Bản chính | [UIHelper.cs](file:///d:/dotnet%20project/db/src/HRM.GUI/Helpers/UIHelper.cs#L138-L188) | 138–188 |
| ❌ Bản copy | [frmMain.cs](file:///d:/dotnet%20project/db/src/HRM.GUI/Forms/Main/frmMain.cs#L280-L330) | 280–330 |

### 2.3 Cấu hình cột (switch-case) bị viết 2 lần cho cùng entity

Mỗi entity có 2 bộ switch-case cấu hình cột giống nhau — 1 trong `frmMain`, 1 trong UserControl:

| Entity | Trong frmMain | Trong UserControl |
|--------|---------------|-------------------|
| NhânViên | frmMain.cs:459–487 | ucNhanVien.cs:281–316 |
| PhòngBan | frmMain.cs:601–616 | ucPhongBan.cs:105–127 |
| ChamCong | frmMain.GridBindings.cs:20–98 | UIHelper.cs:206–266 |
| NghỉPhép | frmMain.GridBindings.cs:100–175 | UIHelper.cs:269–326 |

### 2.4 Label tiêu đề — Copy-paste ở mọi module

Đoạn code tạo Label tiêu đề giống nhau xuất hiện ở **9+ nơi**, mặc dù `UIHelper` đã có `CreateModuleTitleLabel()` sẵn mà **không ai dùng**:

```csharp
// Đoạn này lặp lại ở ucNhanVien:38, ucUngVien:35, ucPhongBan:31, 
// ucTinTuyenDung:31, ucPhongVan:24, frmMain:334, frmMain:529, frmMain:709...
var lblTitle = new Label {
    Text = "...",
    Font = new Font("Segoe UI", 16, FontStyle.Bold),
    ForeColor = Color.FromArgb(30, 60, 120),
    AutoSize = true,
    Location = new Point(20, 15)
};
```

### 2.5 Bộ nút Thêm/Sửa/Xóa — Copy-paste ở mọi module

3 nút hành động (cùng màu, cùng style, cùng tọa độ) được viết lại thủ công ở **7 nơi**:

- ucUngVien:59–66, ucTinTuyenDung:49–56, ucPhongVan:53–92
- ucPhongBan:62–99, ucNhanVien:136–165
- frmMain:363–400 (NhanVien), frmMain:558–595 (PhongBan)

> [!NOTE]
> `UIHelper.CreateActionButton()` đã tồn tại nhưng chỉ có ucNhanVien và ucPhongBan dùng. Các module khác tự viết tay Button.

---

## 3. Trùng lặp Logic xử lý

### 3.1 Logic nút Xóa — Lặp 7+ lần

Pattern hoàn toàn giống nhau: Kiểm tra chọn dòng → Xác nhận → Gọi Service → Thông báo → Reload.

| File | Dòng | Entity |
|------|------|--------|
| ucUngVien.cs | 210–251 | UngVien |
| ucTinTuyenDung.cs | 146–185 | TinTuyenDung |
| ucPhongVan.cs | 182–220 | PhongVan |
| ucPhongBan.cs | 175–199 | PhongBan |
| ucNhanVien.cs | 166–188 | NhanVien |
| frmMain.cs | 433–457 | NhanVien |
| frmMain.cs | 664–689 | PhongBan |

### 3.2 Logic nút Thêm — Lặp 7+ lần

```csharp
// Pattern lặp ở 7+ nơi, chỉ khác tên Form:
var frm = Program.ServiceProvider.GetRequiredService<frmThemXxx>();
if (frm.ShowDialog() == DialogResult.OK) {
    dgv.DataSource = await service.GetAllAsync();
}
```

| File | Dòng |
|------|------|
| ucUngVien.cs | 162–181 |
| ucTinTuyenDung.cs | 100–118 |
| ucPhongVan.cs | 137–155 |
| ucPhongBan.cs | 154–161 |
| ucNhanVien.cs | 138–143 |
| frmMain.cs | 406–413, 642–649 |

### 3.3 Logic nút Sửa — Lặp 7+ lần

```csharp
// Pattern lặp:
int id = Convert.ToInt32(dgv.CurrentRow.Cells["MaXxx"].Value);
var frm = Program.ServiceProvider.GetRequiredService<frmSuaXxx>();
frm.MaXxxCachSua = id;
if (frm.ShowDialog() == DialogResult.OK) {
    dgv.DataSource = await service.GetAllAsync();
}
```

### 3.4 Logic Tìm kiếm — Lặp 7+ lần

```csharp
// Pattern lặp ở mỗi module:
var keyword = txtSearch.Text.Trim();
dgv.DataSource = string.IsNullOrWhiteSpace(keyword)
    ? await service.GetAllAsync()
    : await service.SearchAsync(keyword);
```

### 3.5 Logic Load dữ liệu ban đầu — Lặp 7+ lần

```csharp
// Pattern lặp ở cuối mỗi hàm LoadXxxView():
try {
    var data = await service.GetAllAsync();
    dgv.DataSource = data;
} catch (Exception ex) {
    MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", ...);
}
```

### 3.6 Logic Lưu form (btnLuu_Click) — Lặp giữa Thêm và Sửa

frmThemNhanVien:46–98 và frmSuaNhanVien:74–115 có cùng cấu trúc:
```
Validate → btnLuu.Enabled = false → Tạo DTO → Gọi Service → 
MessageBox → DialogResult.OK → Close → catch → finally: btnLuu.Enabled = true
```

### 3.7 Load ComboBox — Lặp giữa Thêm và Sửa

frmThemNhanVien:22–44 và frmSuaNhanVien:29–71 đều load PhongBan + ChucVu vào ComboBox bằng code giống nhau.

---

## 4. Trùng lặp Data Access

### 4.1 Repository Search — Cùng pattern, khác entity

4 Repository có hàm Search với cấu trúc giống nhau:

| Repository | Hàm | Dòng |
|------------|-----|------|
| NhanVienRepository | SearchAsync | [40–57](file:///d:/dotnet%20project/db/src/HRM.DAL/Repositories/NhanVienRepository.cs#L40-L57) |
| UngVienRepository | SearchAsync | [23–38](file:///d:/dotnet%20project/db/src/HRM.DAL/Repositories/UngVienRepository.cs#L23-L38) |
| PhongVanRepository | SearchWithDetailsAsync | [22–42](file:///d:/dotnet%20project/db/src/HRM.DAL/Repositories/PhongVanRepository.cs#L22-L42) |
| TinTuyenDungRepository | SearchWithDetailsAsync | [22–38](file:///d:/dotnet%20project/db/src/HRM.DAL/Repositories/TinTuyenDungRepository.cs#L22-L38) |

Pattern chung: `Trim → Include → Where(Contains) → OrderBy → ToListAsync`

### 4.2 Eager Loading lặp trong cùng 1 file

**ChamCongRepository** lặp `.Include(cc => cc.NhanVien).ThenInclude(n => n.PhongBan).Include(cc => cc.NhanVien).ThenInclude(n => n.ChucVu)` **5 lần** (dòng 29–32, 60–61, 74–75, 86–87).

**DonNghiPhepRepository** lặp `.Include(d => d.NhanVien).ThenInclude(...).Include(d => d.LoaiNghiPhep)` **4 lần** (dòng 27–30, 38–41, 59–64).

### 4.3 Service pass-through (chỉ gọi lại Repo)

```csharp
// PhongVanService — nhiều hàm chỉ gọi repo và return true:
public async Task<bool> AddPhongVanAsync(PhongVan pv) {
    await _phongVanRepo.AddAsync(pv);
    return true;  // Luôn true, không validate gì
}
public async Task<bool> UpdatePhongVanAsync(PhongVan pv) {
    await _phongVanRepo.UpdateAsync(pv);
    return true;  // Luôn true
}
```

Tương tự ở TinTuyenDungService (dòng 55–65).

---

## 5. Anti-pattern: Service Locator

> [!WARNING]
> **11+ file** dùng `Program.ServiceProvider.GetRequiredService<>()` trực tiếp thay vì Constructor Injection. Đây là Service Locator anti-pattern.

**File vi phạm (lấy Service trong Constructor):**

| File | Dòng |
|------|------|
| ucUngVien.cs | 19, 28 |
| ucPhongVan.cs | 17 |
| ucTinTuyenDung.cs | 25 |
| ucTaiKhoan.cs | 26 |
| ucPhongBan.cs | 25 |
| ucChamCong.cs | 25 |
| ucNghiPhep.cs | 26 |
| ucHieuSuat.cs | 28–29 |
| ucBangLuong.cs | 27 |
| ucThuongPhat.cs | 26 |
| frmThemUngVien.cs | 12–13 |
| frmSuaUngVien.cs | 15–16 |

**So sánh:** frmThemNhanVien, frmSuaNhanVien, frmChatBot dùng **Constructor Injection** (đúng chuẩn).

---

## 6. frmMain.cs — "God Class"

> [!IMPORTANT]
> `frmMain.cs` có **3201 dòng** code, chứa toàn bộ logic CRUD cho mọi entity (NhanVien, PhongBan, ChamCong, NghiPhep, TaiKhoan, PhongVan, HieuSuat, BangLuong, ThuongPhat). Đây là bản cũ chưa được tách ra UserControl.

Hiện tại, mỗi module đã có UserControl riêng (`ucNhanVien`, `ucPhongBan`...) và `frmMain` đã chuyển sang gọi UserControl (dòng 189–198). **Tuy nhiên**, toàn bộ code cũ trong frmMain (LoadNhanVienView, LoadPhongBanView, LoadBangLuongView...) vẫn còn nguyên → **~2500 dòng code chết**.

---

## 7. Bảng tổng hợp

| # | Vấn đề | Mức độ | Số lần lặp | Dòng thừa |
|---|--------|--------|:----------:|:---------:|
| 1 | Kiểm tra quyền Admin không nhất quán | 🔴 BUG | 4 cách | ~30 |
| 2 | CreateStyledDataGridView trùng | 🔴 Cao | 2 bản | ~50 |
| 3 | CreateChamCongHistoryGrid trùng | 🔴 Cao | 2 bản | ~50 |
| 4 | Cấu hình cột switch-case trùng | 🟡 TB | 4 cặp | ~200 |
| 5 | Label tiêu đề copy-paste | 🟡 TB | 9+ | ~50 |
| 6 | Bộ nút Thêm/Sửa/Xóa copy-paste | 🔴 Cao | 7+ | ~140 |
| 7 | Logic nút Xóa lặp | 🔴 Cao | 7+ | ~150 |
| 8 | Logic nút Thêm lặp | 🟡 TB | 7+ | ~50 |
| 9 | Logic nút Sửa lặp | 🟡 TB | 7+ | ~60 |
| 10 | Logic Tìm kiếm lặp | 🟡 TB | 7+ | ~40 |
| 11 | Service Locator anti-pattern | 🔴 Kiến trúc | 12 file | — |
| 12 | Repository Search lặp pattern | 🟡 TB | 4 | ~60 |
| 13 | Eager Loading lặp trong cùng file | 🟡 TB | 9+ | ~30 |
| 14 | Service pass-through | 🟢 Thấp | 4 hàm | ~20 |
| 15 | frmMain code chết | 🔴 Cao | 1 file | ~2500 |

---

## 8. Hướng dẫn sửa cho đội nhóm

### Ưu tiên 1 — Sửa BUG phân quyền
- Tạo `UIHelper.IsHROrAdmin(session)` bao gồm cả role "HR"
- Replace tất cả inline check bằng `UIHelper.IsAdmin()` hoặc `UIHelper.IsHROrAdmin()`

### Ưu tiên 2 — Xóa code chết trong frmMain
- Xóa `CreateStyledDataGridView()` (dòng 226–277) và `CreateChamCongHistoryGrid()` (dòng 280–330) trong frmMain
- Xóa toàn bộ các hàm `LoadXxxView()` cũ (từ dòng 332 đến ~2500) vì đã có UserControl thay thế
- Xóa `frmMain.GridBindings.cs` vì `UIHelper` đã có bản tương đương

### Ưu tiên 3 — Gom logic CRUD dùng chung
- Tạo helper method trong UIHelper:
  ```csharp
  public static async Task OpenAddForm<TForm>(DataGridView dgv, Func<Task<object>> reloadData)
  public static async Task ConfirmAndDelete(string entityName, Func<Task<bool>> deleteAction, ...)
  ```

### Ưu tiên 4 — Thống nhất DI
- Chuyển các UserControl sang nhận Service qua constructor thay vì gọi `Program.ServiceProvider`

### Ưu tiên 5 — Gom Eager Loading
- Tạo extension method: `IQueryable<ChamCong> WithNhanVienDetails(this IQueryable<ChamCong> q)`
