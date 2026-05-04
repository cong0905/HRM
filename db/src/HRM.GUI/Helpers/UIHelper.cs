using System.ComponentModel;
using HRM.Common.DTOs;

namespace HRM.GUI.Helpers
{
    /// <summary>
    /// Tập hợp các hàm tiện ích vẽ giao diện và xử lý chung
    /// dùng cho mọi UserControl trong module HRM.GUI.
    /// </summary>
    public static class UIHelper
    {
        /// <summary>
        /// True khi code chạy trong WinForms Designer (Visual Studio) — cần bỏ qua DI, DB, v.v.
        /// </summary>
        public static bool IsDesignTime() => LicenseManager.UsageMode == LicenseUsageMode.Designtime;

        public static Label CreateModuleTitleLabel(string text, Point location)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 60, 120),
                AutoSize = true,
                Location = location
            };
        }

        public static TextBox CreateSearchTextBox(Point location, Size size, string placeholder)
        {
            return new TextBox
            {
                Location = location,
                Size = size,
                Font = new Font("Segoe UI", 10),
                PlaceholderText = placeholder
            };
        }

        public static Label CreateFilterLabel(string text, Point location)
        {
            return new Label
            {
                Text = text,
                Location = location,
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };
        }

        public static ComboBox CreateFilterComboBox(Point location, Size size)
        {
            return new ComboBox
            {
                Location = location,
                Size = size,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
        }

        public static Button CreateActionButton(string text, Point location, Size size, Color backColor)
        {
            var btn = new Button
            {
                Text = text,
                Location = location,
                Size = size,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        /// <summary>
        /// Lưới dữ liệu dạng "danh sách" — dùng cho hầu hết các module
        /// (Nhân viên, Phòng ban, Tài khoản, Phỏng vấn, Tin tuyển dụng...).
        /// UserControl gọi xong tự đặt Location/Size theo bố cục riêng.
        /// </summary>
        public static DataGridView CreateStyledDataGridView(string name)
        {
            var dgv = new DataGridView
            {
                Name = name,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                ColumnHeadersHeight = 45,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                RowTemplate = new DataGridViewRow { Height = 40 },
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(230, 230, 230),
                EnableHeadersVisualStyles = false
            };

            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                SelectionBackColor = Color.FromArgb(41, 128, 185)
            };

            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 60, 60),
                SelectionBackColor = Color.FromArgb(226, 239, 252),
                SelectionForeColor = Color.FromArgb(30, 30, 30),
                Font = new Font("Segoe UI", 10),
                Padding = new Padding(5, 0, 5, 0),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };

            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(250, 252, 255)
            };

            return dgv;
        }

        /// <summary>
        /// Lưới dữ liệu dạng "lịch sử" (Dock=Fill) — dùng cho các module
        /// chấm công và nghỉ phép, đặt bên trong panel có Dock=Fill.
        /// </summary>
        public static DataGridView CreateChamCongHistoryGrid(string name)
        {
            var dgv = new DataGridView
            {
                Name = name,
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                ColumnHeadersHeight = 45,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                RowTemplate = new DataGridViewRow { Height = 40 },
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(230, 230, 230),
                EnableHeadersVisualStyles = false
            };

            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                SelectionBackColor = Color.FromArgb(41, 128, 185),
                SelectionForeColor = Color.White
            };

            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 60, 60),
                SelectionBackColor = Color.FromArgb(226, 239, 252),
                SelectionForeColor = Color.FromArgb(30, 30, 30),
                Font = new Font("Segoe UI", 10),
                Padding = new Padding(5, 0, 5, 0),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };

            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(250, 252, 255)
            };

            return dgv;
        }

        /// <summary>Định dạng cột giờ (TimeSpan) sang dạng hh:mm cho lưới chấm công.</summary>
        public static void WireChamCongHistoryTimeCellFormatting(DataGridView dgv)
        {
            dgv.CellFormatting += (_, e) =>
            {
                if (e.RowIndex < 0 || e.Value == null) return;
                var name = dgv.Columns[e.ColumnIndex].DataPropertyName;
                if (name is "GioVao" or "GioRa" && e.Value is TimeSpan t)
                {
                    e.Value = t.ToString(@"hh\:mm");
                    e.FormattingApplied = true;
                }
            };
        }

        /// <summary>Cấu hình cột cho lưới Lịch sử chấm công.</summary>
        public static void ApplyChamCongHistoryColumns(DataGridView dgv, bool showGhiChuForAdmin)
        {
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.MinimumWidth = 80;
                switch (col.DataPropertyName)
                {
                    case "MaChamCong":
                    case "MaNhanVien":
                        col.Visible = false;
                        break;
                    case "MaNV":
                        col.HeaderText = "Mã NV"; col.DisplayIndex = 0; col.MinimumWidth = 90;
                        break;
                    case "TenNhanVien":
                        col.HeaderText = "Họ Tên"; col.DisplayIndex = 1; col.MinimumWidth = 150;
                        break;
                    case "TenPhongBan":
                        col.HeaderText = "Phòng Ban"; col.DisplayIndex = 2; col.MinimumWidth = 120;
                        break;
                    case "TenChucVu":
                        col.HeaderText = "Chức Vụ"; col.DisplayIndex = 3; col.MinimumWidth = 120;
                        break;
                    case "NgayChamCong":
                        col.HeaderText = "Ngày"; col.DisplayIndex = 4;
                        col.DefaultCellStyle.Format = "dd/MM/yyyy";
                        break;
                    case "GioVao":
                        col.HeaderText = "Giờ Vào"; col.DisplayIndex = 5;
                        break;
                    case "GioRa":
                        col.HeaderText = "Giờ Ra"; col.DisplayIndex = 6;
                        break;
                    case "TongGioLam":
                        col.HeaderText = "Tổng Giờ"; col.DisplayIndex = 7;
                        col.DefaultCellStyle.Format = "N2";
                        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                        break;
                    case "HinhThuc":
                        col.HeaderText = "Hình Thức"; col.DisplayIndex = 8;
                        break;
                    case "TrangThai":
                        col.HeaderText = "Trạng Thái"; col.DisplayIndex = 9;
                        break;
                    case "GhiChu":
                        col.HeaderText = "Ghi chú"; col.DisplayIndex = 10;
                        col.MinimumWidth = 120;
                        col.Visible = showGhiChuForAdmin;
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                        col.FillWeight = 120;
                        break;
                }
            }
        }

        /// <summary>Cấu hình cột cho lưới Đơn nghỉ phép.</summary>
        public static void ApplyDonNghiPhepGridColumns(DataGridView dgv)
        {
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.MinimumWidth = 72;
                switch (col.DataPropertyName)
                {
                    case "MaDonPhep":
                    case "MaNhanVien":
                        col.Visible = false;
                        break;
                    case "MaNV":
                        col.HeaderText = "Mã NV"; col.DisplayIndex = 0; col.MinimumWidth = 90;
                        break;
                    case "TenNhanVien":
                        col.HeaderText = "Họ Tên"; col.DisplayIndex = 1; col.MinimumWidth = 150;
                        break;
                    case "TenPhongBan":
                        col.HeaderText = "Phòng Ban"; col.DisplayIndex = 2; col.MinimumWidth = 120;
                        break;
                    case "TenChucVu":
                        col.HeaderText = "Chức Vụ"; col.DisplayIndex = 3; col.MinimumWidth = 120;
                        break;
                    case "TenLoaiPhep":
                        col.HeaderText = "Loại Phép"; col.DisplayIndex = 4;
                        break;
                    case "NgayBatDau":
                        col.HeaderText = "Từ Ngày"; col.DisplayIndex = 5;
                        col.DefaultCellStyle.Format = "dd/MM/yyyy";
                        break;
                    case "NgayKetThuc":
                        col.HeaderText = "Đến Ngày"; col.DisplayIndex = 6;
                        col.DefaultCellStyle.Format = "dd/MM/yyyy";
                        break;
                    case "SoNgayNghi":
                        col.HeaderText = "Số Ngày"; col.DisplayIndex = 7;
                        col.DefaultCellStyle.Format = "F0";
                        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                        break;
                    case "LyDo":
                        col.HeaderText = "Lý do"; col.DisplayIndex = 8;
                        col.MinimumWidth = 120;
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                        col.FillWeight = 140;
                        break;
                    case "TrangThai":
                        col.HeaderText = "Trạng Thái"; col.DisplayIndex = 9;
                        break;
                    case "TenNguoiDuyet":
                        col.HeaderText = "Người Duyệt"; col.DisplayIndex = 10;
                        break;
                    case "NgayTao":
                        col.HeaderText = "Ngày tạo"; col.DisplayIndex = 11;
                        col.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                        break;
                }
            }
        }

        /// <summary>
        /// Hộp thoại nhập 1 dòng văn bản.
        /// Trả về null nếu người dùng đóng/hủy; chuỗi (có thể rỗng) nếu bấm OK.
        /// </summary>
        public static string? PromptSingleLine(IWin32Window owner, string title, string caption)
        {
            using var f = new Form
            {
                Text = title,
                Width = 440,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            var lbl = new Label { Text = caption, Left = 14, Top = 14, Width = 400, AutoSize = false, Height = 36 };
            var txt = new TextBox { Left = 14, Top = 50, Width = 392 };
            var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 230, Top = 86, Width = 80 };
            var btnCancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Left = 318, Top = 86, Width = 80 };
            f.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
            f.AcceptButton = btnOk;
            f.CancelButton = btnCancel;
            return f.ShowDialog(owner) == DialogResult.OK ? txt.Text : null;
        }

        /// <summary>True nếu phiên đăng nhập có vai trò Admin / Quản trị viên.</summary>
        public static bool IsAdmin(UserSessionDTO? session)
        {
            var v = session?.VaiTro ?? string.Empty;
            return v.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                || v.Equals("Quản trị viên", StringComparison.OrdinalIgnoreCase);
        }
    }
}
