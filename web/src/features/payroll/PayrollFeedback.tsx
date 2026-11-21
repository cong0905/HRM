import { Alert } from "antd";
import { parseErrorMessage } from "../../api/client";
import { Feedback } from "../shared/Feedback";

const payrollMessages: Record<string, string> = {
  "payroll.cannot_self_approve": "Người thực hiện tính toán nháp không được tự phê duyệt chính kỳ lương này (Quy tắc phân tách quyền hạn).",
  "payroll.calculator_cannot_publish": "Người tính toán không được thực hiện công bố bảng lương.",
  "payroll.period_immutable": "Kỳ lương đã công bố hoặc đã khóa. Dữ liệu mang tính bất biến và không thể sửa đổi.",
  "payroll.period_already_approved": "Kỳ lương đã được phê duyệt trước đó.",
  "payroll.period_not_calculated": "Chỉ có thể phê duyệt kỳ lương đã có bản tính nháp hoàn chỉnh.",
  "payroll.run_not_approved": "Bản chạy hiện tại phải được phê duyệt trước khi công bố.",
  "payroll.run_items_mismatch": "Dữ liệu phiếu lương không khớp với bản tính đã duyệt. Tải lại trạng thái mới nhất.",
  "payroll.snapshot_missing": "Chưa có bản chụp tính toán (Snapshot) cho kỳ lương này.",
  "payroll.version_conflict": "Phiên bản kỳ lương hoặc bản chạy đã thay đổi bởi người khác. Tải lại trạng thái để đối chiếu.",
  "payroll.salary_closed_period": "Ngày hiệu lực làm thay đổi kỳ lương đã công bố hoặc khóa. Vui lòng chọn ngày hiệu lực mới hơn.",
  "payroll.salary_effective_order": "Ngày hiệu lực mức lương mới phải lớn hơn ngày hiệu lực liền trước. Không cho phép chồng lấn.",
  "payroll.period_exists": "Kỳ lương cho tháng này đã tồn tại trong hệ thống.",
  "payroll.money_precision": "Số tiền hỗ trợ tối đa 2 chữ số thập phân; không tự ý làm tròn.",
  "payroll.reason_required": "Bắt buộc nhập lý do từ 10 đến 500 ký tự.",
  "payroll.invalid_status_filter": "Bộ lọc trạng thái kỳ lương không hợp lệ.",
  "payroll.adjustment_conflict": "Khoản điều chỉnh đã bị thay đổi hoặc đã hủy bởi người khác.",
  "payroll.employee_outside_period": "Nhân viên không có khoảng thời gian làm việc giao với kỳ lương đã chọn.",
  "payroll.invalid_salary": "Mức lương hoặc ngày hiệu lực không hợp lệ.",
  "payroll.invalid_adjustment": "Khoản điều chỉnh không hợp lệ (yêu cầu số tiền dương và thuộc danh mục cho phép).",
  "payroll.inputs_changed": "Dữ liệu công, phép hoặc mức lương đã thay đổi. Cần tính toán nháp lại, đối chiếu bản chạy mới và phê duyệt lại trước khi công bố.",
  "payroll.calculation_blocked": "Bản tính có lương âm hoặc thiếu giờ ra. Cần xử lý dữ liệu công và tính lại trước khi duyệt/công bố.",
  "payroll.negative_net": "Lương thực nhận âm. Kiểm tra các khoản giảm trừ trước khi duyệt/công bố.",
  "payroll.salary_missing": "Chưa có mức lương hiệu lực cho nhân viên trong kỳ. Thiết lập lịch sử lương rồi tính lại.",
  "payroll.not_publishable": "Bảng lương có lỗi chặn (lương âm hoặc thiếu dữ liệu) nên chưa thể công bố.",
};

export function PayrollFeedback({ error, retry }: { error: unknown; retry?: () => void }) {
  if (!error) return null;
  const parsed = parseErrorMessage(error);
  if (parsed.code && payrollMessages[parsed.code]) {
    return <Alert type="error" showIcon message={payrollMessages[parsed.code]} description={parsed.traceId ? `Mã truy vết: ${parsed.traceId}` : undefined} />;
  }
  return <Feedback error={error} retry={retry} />;
}
