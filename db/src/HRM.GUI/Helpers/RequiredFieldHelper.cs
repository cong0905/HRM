using System.Drawing;
using System.Windows.Forms;

namespace HRM.GUI.Helpers
{
    /// <summary>
    /// Tiện ích đánh dấu nhãn là trường bắt buộc bằng 1 nhãn sao màu đỏ.
    /// Sử dụng trong constructor của Form/Control sau khi gọi InitializeComponent().
    /// </summary>
    public static class RequiredFieldHelper
    {
        public static void MarkRequired(Label label, int offset = 4)
        {
            if (label == null) return;
            if (UIHelper.IsDesignTime()) return;

            var star = new Label
            {
                Text = "*",
                ForeColor = Color.Red,
                AutoSize = true,
                BackColor = Color.Transparent,
                Font = new Font(label.Font, FontStyle.Bold)
            };

            Control? parent = label.Parent ?? label.FindForm();
            if (parent == null) return;

            parent.Controls.Add(star);
            star.BringToFront();

            // Đặt vị trí ngay sau nhãn gốc
            var x = label.Location.X + label.Width + offset;
            var y = label.Location.Y + Math.Max(0, (label.Height - star.Height) / 2);
            star.Location = new Point(x, y);
        }

        public static void MarkRequired(params Label[] labels)
        {
            if (labels == null) return;
            foreach (var l in labels) MarkRequired(l);
        }
    }
}
