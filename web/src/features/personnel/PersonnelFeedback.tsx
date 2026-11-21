import { Alert, Button } from "antd";
import { useQueryClient } from "@tanstack/react-query";
import { parseErrorMessage } from "../../api/client";
import { ApiClientError } from "../../api/types";
import { AUTH_QUERY_KEY } from "../../context/AuthContext";
import { ErrorAlert } from "../../components/common/ErrorAlert";

export const PERSONNEL_ERROR_MESSAGES: Record<string, string> = {
  "authorization.scope_forbidden": "Bạn không có quyền quản lý nhân viên này (nằm ngoài phạm vi phân công của bạn).",
  "authorization.forbidden": "Bạn không có quyền thực hiện thao tác này.",
  "employees.validation_error": "Thông tin nhân viên không hợp lệ hoặc thiếu trường bắt buộc.",
  "employees.invalid_dates": "Ngày sinh và ngày vào làm không hợp lệ (ngày sinh phải trước ngày vào làm và không ở tương lai).",
  "employees.department_not_found": "Phòng ban đã chọn không tồn tại trong hệ thống.",
  "employees.position_not_found": "Chức vụ đã chọn không tồn tại trong hệ thống.",
  "identity.duplicate_username": "Tên đăng nhập đã tồn tại trong hệ thống. Vui lòng chọn tên khác.",
  "identity.duplicate_email": "Email đã được sử dụng bởi tài khoản khác trong hệ thống.",
  "employees.duplicate_citizen_id": "Số CCCD đã tồn tại trong hệ thống.",
  "employees.duplicate_code": "Mã nhân viên đã tồn tại trong hệ thống.",
  "account.creation_failed": "Không thể tạo tài khoản người dùng trên hệ thống.",
  "account.role_assignment_failed": "Không thể gán quyền cho tài khoản nhân viên.",
  "concurrency.token_required": "Thiếu phiên bản dữ liệu. Vui lòng tải lại hồ sơ trước khi thao tác.",
  "concurrency.invalid_token": "Phiên bản dữ liệu không hợp lệ. Vui lòng tải lại hồ sơ trước khi thao tác.",
  "resource.conflict": "Dữ liệu hồ sơ đã được thay đổi trên máy chủ bởi phiên khác. Vui lòng tải bản mới để đối chiếu.",
  "employees.already_terminated": "Nhân viên đã nghỉ việc, không thể chỉnh sửa hồ sơ.",
  "employees.already_offboarded": "Nhân viên này đã được ghi nhận nghỉ việc trước đó.",
  "employees.required_field_null": "Trường thông tin bắt buộc không được để trống.",
  "employees.invalid_termination_date": "Ngày nghỉ việc không hợp lệ (phải từ ngày vào làm và không quá ngày hiện tại).",
  "account.offboard_failed": "Không thể thu hồi phiên đăng nhập khi cho nghỉ việc.",
  "employees.bulk_limit_exceeded": "Số lượng hồ sơ yêu cầu vượt quá giới hạn cho phép.",
  "employee.not_found": "Không tìm thấy hồ sơ nhân viên trên máy chủ.",
};

export function PersonnelFeedback({ error, retry }: { error: unknown; retry?: () => void }) {
  const client = useQueryClient();
  if (!error) return null;

  const parsed = parseErrorMessage(error);

  if (parsed.code && PERSONNEL_ERROR_MESSAGES[parsed.code]) {
    parsed.message = PERSONNEL_ERROR_MESSAGES[parsed.code]!;
  }

  if (error instanceof ApiClientError) {
    if (error.status === 403) {
      parsed.message = parsed.code && PERSONNEL_ERROR_MESSAGES[parsed.code]
        ? PERSONNEL_ERROR_MESSAGES[parsed.code]!
        : "Bạn không có quyền quản lý nhân viên này hoặc xem thông tin này (nằm ngoài phạm vi phân công).";
      // 403 permissions cannot be resolved by immediate client-side retry
      return <ErrorAlert {...parsed} onRetry={undefined} />;
    }
    if (error.status === 401) {
      return (
        <Alert
          type="warning"
          showIcon
          message="Phiên đăng nhập đã hết hạn"
          description={
            <Button onClick={() => void client.invalidateQueries({ queryKey: AUTH_QUERY_KEY })}>
              Đăng nhập lại
            </Button>
          }
        />
      );
    }
  }

  return <ErrorAlert {...parsed} onRetry={retry} />;
}
