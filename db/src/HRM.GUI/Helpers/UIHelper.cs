namespace HRM.GUI.Helpers
{
    public static class UIHelper
    {
        public static DataGridView CreateStyledDataGridView(string name)
        {
            var dgv = new DataGridView
            {
                Name = name,
                // Đã bỏ Location và Size để các UserControl tự do thiết lập
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
    }
}
