namespace HRM.GUI.Forms.Main;

/// <summary>Cấu hình cột DataGridView dùng chung cho màn chấm công / nghỉ phép.</summary>
public partial class frmMain
{
    private static void WireChamCongHistoryTimeCellFormatting(DataGridView dgv)
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

    private static void ApplyChamCongHistoryColumns(DataGridView dgv, bool showGhiChuForAdmin)
    {
        foreach (DataGridViewColumn col in dgv.Columns)
        {
            col.MinimumWidth = 80;
            switch (col.DataPropertyName)
            {
                case "MaChamCong":
                    col.Visible = false;
                    break;
                case "MaNhanVien":
                    col.Visible = false;
                    break;
                case "MaNV":
                    col.HeaderText = "Mã NV";
                    col.DisplayIndex = 0;
                    col.MinimumWidth = 90;
                    break;
                case "TenNhanVien":
                    col.HeaderText = "Họ Tên";
                    col.DisplayIndex = 1;
                    col.MinimumWidth = 150;
                    break;
                case "TenPhongBan":
                    col.HeaderText = "Phòng Ban";
                    col.DisplayIndex = 2;
                    col.MinimumWidth = 120;
                    break;
                case "TenChucVu":
                    col.HeaderText = "Chức Vụ";
                    col.DisplayIndex = 3;
                    col.MinimumWidth = 120;
                    break;
                case "NgayChamCong":
                    col.HeaderText = "Ngày";
                    col.DisplayIndex = 4;
                    col.DefaultCellStyle.Format = "dd/MM/yyyy";
                    break;
                case "GioVao":
                    col.HeaderText = "Giờ Vào";
                    col.DisplayIndex = 5;
                    break;
                case "GioRa":
                    col.HeaderText = "Giờ Ra";
                    col.DisplayIndex = 6;
                    break;
                case "TongGioLam":
                    col.HeaderText = "Tổng Giờ";
                    col.DisplayIndex = 7;
                    col.DefaultCellStyle.Format = "N2";
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    break;
                case "HinhThuc":
                    col.HeaderText = "Hình Thức";
                    col.DisplayIndex = 8;
                    break;
                case "TrangThai":
                    col.HeaderText = "Trạng Thái";
                    col.DisplayIndex = 9;
                    break;
                case "GhiChu":
                    col.HeaderText = "Ghi chú";
                    col.DisplayIndex = 10;
                    col.MinimumWidth = 120;
                    col.Visible = showGhiChuForAdmin;
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    col.FillWeight = 120;
                    break;
            }
        }
    }

    private static void ApplyDonNghiPhepGridColumns(DataGridView dgv)
    {
        foreach (DataGridViewColumn col in dgv.Columns)
        {
            col.MinimumWidth = 72;
            switch (col.DataPropertyName)
            {
                case "MaDonPhep":
                    col.Visible = false;
                    break;
                case "MaNhanVien":
                    col.Visible = false;
                    break;
                case "MaNV":
                    col.HeaderText = "Mã NV";
                    col.DisplayIndex = 0;
                    col.MinimumWidth = 90;
                    break;
                case "TenNhanVien":
                    col.HeaderText = "Họ Tên";
                    col.DisplayIndex = 1;
                    col.MinimumWidth = 150;
                    break;
                case "TenPhongBan":
                    col.HeaderText = "Phòng Ban";
                    col.DisplayIndex = 2;
                    col.MinimumWidth = 120;
                    break;
                case "TenChucVu":
                    col.HeaderText = "Chức Vụ";
                    col.DisplayIndex = 3;
                    col.MinimumWidth = 120;
                    break;
                case "TenLoaiPhep":
                    col.HeaderText = "Loại Phép";
                    col.DisplayIndex = 4;
                    break;
                case "NgayBatDau":
                    col.HeaderText = "Từ Ngày";
                    col.DisplayIndex = 5;
                    col.DefaultCellStyle.Format = "dd/MM/yyyy";
                    break;
                case "NgayKetThuc":
                    col.HeaderText = "Đến Ngày";
                    col.DisplayIndex = 6;
                    col.DefaultCellStyle.Format = "dd/MM/yyyy";
                    break;
                case "SoNgayNghi":
                    col.HeaderText = "Số Ngày";
                    col.DisplayIndex = 7;
                    col.DefaultCellStyle.Format = "F0";
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    break;
                case "LyDo":
                    col.HeaderText = "Lý do";
                    col.DisplayIndex = 8;
                    col.MinimumWidth = 120;
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    col.FillWeight = 140;
                    break;
                case "TrangThai":
                    col.HeaderText = "Trạng Thái";
                    col.DisplayIndex = 9;
                    break;
                case "TenNguoiDuyet":
                    col.HeaderText = "Người Duyệt";
                    col.DisplayIndex = 10;
                    break;
                case "NgayTao":
                    col.HeaderText = "Ngày tạo";
                    col.DisplayIndex = 11;
                    col.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                    break;
            }
        }
    }
}
