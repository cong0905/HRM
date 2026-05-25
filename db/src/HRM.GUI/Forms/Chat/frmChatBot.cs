using System;
using System.Drawing;
using System.Windows.Forms;
using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using System.Linq;

namespace HRM.GUI.Forms.Chat
{
    public partial class frmChatBot : Form
    {
        private readonly IGeminiService _geminiService;
        private readonly IDonNghiPhepService _donNghiPhepService;
        private UserSessionDTO _session;
        private string _systemInstruction = "";

        public frmChatBot(IGeminiService geminiService, IDonNghiPhepService donNghiPhepService)
        {
            InitializeComponent();
            _geminiService = geminiService;
            _donNghiPhepService = donNghiPhepService;
        }

        public async void SetSession(UserSessionDTO session)
        {
            _session = session;

            // Truy vấn dữ liệu thực tế từ Database
            int maNhanVien = _session.MaNhanVien;
            int namHienTai = DateTime.Now.Year;
            var soNgayPhepDTO = await _donNghiPhepService.GetSoNgayPhepAsync(maNhanVien, namHienTai);
            
            int soNgayPhepConLai = soNgayPhepDTO != null ? soNgayPhepDTO.SoNgayConLai : 0;

            // Gắn thông tin vào System Instruction (Ngữ cảnh ẩn)
            _systemInstruction = $"Bạn là trợ lý ảo thân thiện của hệ thống HRM. " +
                                 $"Thông tin người dùng đang chat: Tên là {_session.HoTen}, chức vụ {_session.VaiTro}, phòng ban {_session.TenPhongBan}. " +
                                 $"Dữ liệu nhân sự: Người này còn lại {soNgayPhepConLai} ngày nghỉ phép trong năm nay. " +
                                 $"Quy tắc: Xưng hô lịch sự bằng tên người dùng, nếu hỏi về ngày phép thì lấy dữ liệu trên để trả lời một cách tự nhiên.";

            AppendMessage("Gemini", $"Xin chào {_session.HoTen}! Tôi là trợ lý AI. Tôi đã được cung cấp thông tin cơ bản của bạn. Tôi có thể giúp gì cho bạn hôm nay?", Color.Blue);
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            string userMessage = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(userMessage)) return;

            // Hiển thị tin nhắn của người dùng
            AppendMessage("Bạn", userMessage, Color.Green);
            txtInput.Clear();
            btnSend.Enabled = false;

            // Hiển thị trạng thái đang xử lý
            AppendMessage("Gemini", "Đang suy nghĩ...", Color.Gray);

            // Gọi API Gemini
            string response = await _geminiService.GetResponseAsync(userMessage, _systemInstruction);

            // Xóa dòng "Đang suy nghĩ..." và hiển thị câu trả lời
            RemoveLastLine();
            AppendMessage("Gemini", response, Color.Blue);

            btnSend.Enabled = true;
            txtInput.Focus();
        }

        private void AppendMessage(string sender, string message, Color color)
        {
            rtbChatHistory.SelectionStart = rtbChatHistory.TextLength;
            rtbChatHistory.SelectionLength = 0;

            rtbChatHistory.SelectionFont = new Font(rtbChatHistory.Font, FontStyle.Bold);
            rtbChatHistory.SelectionColor = color;
            rtbChatHistory.AppendText($"[{DateTime.Now:HH:mm}] {sender}: ");

            rtbChatHistory.SelectionFont = new Font(rtbChatHistory.Font, FontStyle.Regular);
            rtbChatHistory.SelectionColor = Color.Black;
            rtbChatHistory.AppendText($"{message}{Environment.NewLine}{Environment.NewLine}");

            rtbChatHistory.ScrollToCaret();
        }

        private void RemoveLastLine()
        {
            string[] lines = rtbChatHistory.Lines;
            if (lines.Length > 2)
            {
                // Xóa dòng cuối cùng (Đang suy nghĩ...)
                int lastLineStart = rtbChatHistory.GetFirstCharIndexFromLine(lines.Length - 3);
                rtbChatHistory.Select(lastLineStart, rtbChatHistory.TextLength - lastLineStart);
                rtbChatHistory.SelectedText = "";
            }
        }

        private void txtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnSend.PerformClick();
                e.SuppressKeyPress = true; // Ngăn tiếng "beep" khi nhấn Enter
            }
        }
    }
}
