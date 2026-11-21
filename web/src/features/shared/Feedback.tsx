import { Alert, Button, Space } from "antd";
import { useQueryClient } from "@tanstack/react-query";
import { parseErrorMessage } from "../../api/client";
import { ApiClientError } from "../../api/types";
import { AUTH_QUERY_KEY } from "../../context/AuthContext";
import { ErrorAlert } from "../../components/common/ErrorAlert";

export function Feedback({ error, retry }: { error: unknown; retry?: () => void }) {
  const client = useQueryClient();
  if (!error) return null;
  const parsed = parseErrorMessage(error);
  const messages: Record<string, string> = {
    "organization.in_use": "Danh mục đang được hồ sơ nhân viên sử dụng. Chuyển hồ sơ sang danh mục khác trước khi xóa.",
    "organization.duplicate_name": "Tên đã tồn tại. Nhập tên khác.",
    "resource.conflict": "Dữ liệu đã thay đổi. Tải lại bản mới trước khi tiếp tục.",
    "calendar.closed_period": "Ngày áp dụng ảnh hưởng kỳ lương đã công bố hoặc khóa. Chọn ngày sau kỳ đã khóa.",
    "calendar.duplicate_effective_date": "Đã có phiên bản bắt đầu vào ngày này. Chọn ngày áp dụng khác.",
    "calendar.not_configured": "Chưa có lịch áp dụng cho thời gian này. Người có quyền quản lý lịch cần tạo phiên bản phù hợp.",
    "leave.insufficient_balance": "Quỹ phép không đủ cho khoảng ngày đã chọn. Kiểm tra quỹ mới nhất hoặc chọn lại ngày nghỉ.",
    "leave.date_overlap": "Khoảng nghỉ trùng đơn đang chờ duyệt hoặc đã được duyệt. Xem lại đơn trước khi gửi.",
    "leave.cross_year_unsupported": "Đơn qua năm chưa được hỗ trợ. Tạo đơn riêng cho từng năm.",
    "leave.no_working_days": "Khoảng nghỉ không có ngày làm việc theo lịch được áp dụng.",
    "leave.invalid_date_range": "Ngày kết thúc phải từ ngày bắt đầu trở đi.",
    "leave.stale_version": "Đơn đã được phiên khác thay đổi. Tải trạng thái mới nhất để đối chiếu.",
    "leave.invalid_status": "Đơn đã được xử lý. Tải trạng thái mới nhất; không gửi lại quyết định cũ.",
    "leave.self_approval_forbidden": "Không được tự duyệt hoặc từ chối đơn nghỉ phép của mình.",
    "leave.period_closed": "Đơn thuộc kỳ lương đã công bố hoặc khóa; chỉ được xem.",
    "leave.cancel_approved_unsupported": "Không thể hủy đơn đã duyệt trong phiên bản hiện tại.",
    "attendance.period_closed": "Ngày công thuộc kỳ lương đã công bố hoặc khóa; chỉ được xem.",
    "attendance.self_adjustment_forbidden": "Không được tự điều chỉnh công hoặc xác nhận OT của mình.",
    "attendance.already_checked_in": "Đã có lần vào ca. Tải lại trạng thái công trước khi tiếp tục.",
    "attendance.already_checked_out": "Ngày công đã có giờ ra. Không thể chấm ra thêm lần nữa.",
    "attendance.not_checked_in": "Chưa có lần vào ca để ghi nhận ra ca.",
    "attendance.invalid_interval": "Giờ ra phải sau giờ vào và thuộc ngày công được điều chỉnh.",
    "attendance.future_instant": "Không thể nhập giờ công trong tương lai so với máy chủ.",
    "attendance.invalid_overtime_minutes": "Số phút OT phải lớn hơn 0 và không quá 1440 phút.",
    "attendance.not_completed": "Cần hoàn tất giờ vào/ra trước khi xác nhận OT.",
    "attendance.overtime_already_confirmed": "Ngày công này đã được xác nhận OT. Tải lại để xem kết quả.",
    "concurrency.invalid_token": "Phiên bản dữ liệu không hợp lệ. Tải lại bản ghi trước khi thao tác.",
    "concurrency.token_required": "Thiếu phiên bản dữ liệu. Tải lại bản ghi trước khi thao tác.",
    "idempotency.payload_mismatch": "Nội dung thao tác đã thay đổi. Tải lại dữ liệu và xác nhận quyết định mới.",
  };
  if (parsed.code && messages[parsed.code]) parsed.message = messages[parsed.code]!;
  if (error instanceof ApiClientError && error.status === 403) parsed.message = "Bạn không có quyền thực hiện thao tác hoặc xem bản ghi này.";
  if (error instanceof ApiClientError && error.status === 401) return <Alert type="warning" showIcon message="Phiên đăng nhập đã hết hạn" description={<Button onClick={() => void client.invalidateQueries({ queryKey: AUTH_QUERY_KEY })}>Đăng nhập lại</Button>} />;
  return <ErrorAlert {...parsed} onRetry={retry} />;
}

export function ConflictNotice({ onCompare, pending }: { onCompare: () => void; pending?: boolean }) {
  return <Alert type="warning" showIcon message="Dữ liệu đã thay đổi" description={<Space direction="vertical">
    <span>Bản nhập của bạn được giữ lại. Tải bản mới để so sánh trước khi sửa và lưu lại.</span>
    <Button onClick={onCompare} loading={pending}>Tải bản mới để so sánh</Button>
  </Space>} />;
}
