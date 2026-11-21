import React, { useState } from "react";
import { Form, Input, Button, Card, Typography, Modal } from "antd";
import { KeyOutlined, LockOutlined, CheckCircleOutlined } from "@ant-design/icons";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { parseErrorMessage } from "../api/client";
import { ErrorAlert } from "../components/common/ErrorAlert";

const { Title, Text } = Typography;

interface ChangePasswordFormValues {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export const ChangePasswordPage: React.FC = () => {
  const { changePassword } = useAuth();
  const navigate = useNavigate();
  const [form] = Form.useForm<ChangePasswordFormValues>();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorInfo, setErrorInfo] = useState<{
    message: string;
    traceId?: string;
    code?: string;
  } | null>(null);

  const handleSubmit = async (values: ChangePasswordFormValues) => {
    setIsSubmitting(true);
    setErrorInfo(null);

    try {
      await changePassword({
        currentPassword: values.currentPassword,
        newPassword: values.newPassword,
      });

      Modal.success({
        title: "Đổi mật khẩu thành công",
        content: "Phiên làm việc hiện tại đã được hủy vì lý do bảo mật. Vui lòng đăng nhập lại với mật khẩu mới.",
        okText: "Đăng nhập lại",
        onOk: () => {
          navigate("/login", { replace: true });
        },
      });
    } catch (err) {
      const parsed = parseErrorMessage(err, "Đổi mật khẩu thất bại. Vui lòng kiểm tra lại mật khẩu hiện tại.");
      setErrorInfo({
        message: parsed.message,
        traceId: parsed.traceId,
        code: parsed.code,
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="password-settings">
      <Card>
        <div style={{ marginBottom: 24 }}>
          <Title level={4} style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 4 }}>
            <KeyOutlined style={{ color: "#3157d5" }} />
            Đổi mật khẩu tài khoản
          </Title>
          <Text type="secondary">
            Để bảo vệ an toàn cho tài khoản, mật khẩu mới phải có độ dài tối thiểu 12 ký tự và bao gồm các loại ký tự bắt buộc.
          </Text>
        </div>

        {errorInfo && (
          <div style={{ marginBottom: 20 }}>
            <ErrorAlert
              message={errorInfo.message}
              traceId={errorInfo.traceId}
              code={errorInfo.code}
            />
          </div>
        )}

        <Form<ChangePasswordFormValues>
          form={form}
          layout="vertical"
          onFinish={handleSubmit}
          autoComplete="on"
        >
          <Form.Item
            label="Mật khẩu hiện tại"
            name="currentPassword"
            htmlFor="change_current_password"
            rules={[{ required: true, message: "Vui lòng nhập mật khẩu hiện tại" }]}
          >
            <Input.Password
              id="change_current_password"
              prefix={<LockOutlined style={{ color: "#bfbfbf" }} />}
              placeholder="Nhập mật khẩu đang sử dụng"
              autoComplete="current-password"
              disabled={isSubmitting}
              size="large"
            />
          </Form.Item>

          <Form.Item
            label="Mật khẩu mới"
            name="newPassword"
            htmlFor="change_new_password"
            rules={[
              { required: true, message: "Vui lòng nhập mật khẩu mới" },
              { min: 12, message: "Mật khẩu mới phải có ít nhất 12 ký tự" },
              {
                pattern: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{12,}$/,
                message: "Mật khẩu phải chứa chữ hoa, chữ thường, chữ số và ký tự đặc biệt",
              },
            ]}
            extra="Tối thiểu 12 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt."
          >
            <Input.Password
              id="change_new_password"
              prefix={<LockOutlined style={{ color: "#bfbfbf" }} />}
              placeholder="Nhập mật khẩu mới"
              autoComplete="new-password"
              disabled={isSubmitting}
              size="large"
            />
          </Form.Item>

          <Form.Item
            label="Xác nhận mật khẩu mới"
            name="confirmPassword"
            htmlFor="change_confirm_password"
            dependencies={["newPassword"]}
            rules={[
              { required: true, message: "Vui lòng xác nhận mật khẩu mới" },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  if (!value || getFieldValue("newPassword") === value) {
                    return Promise.resolve();
                  }
                  return Promise.reject(new Error("Mật khẩu xác nhận không trùng khớp!"));
                },
              }),
            ]}
          >
            <Input.Password
              id="change_confirm_password"
              prefix={<LockOutlined style={{ color: "#bfbfbf" }} />}
              placeholder="Nhập lại mật khẩu mới"
              autoComplete="new-password"
              disabled={isSubmitting}
              size="large"
            />
          </Form.Item>

          <Form.Item style={{ marginTop: 28, marginBottom: 8 }}>
            <Button
              type="primary"
              htmlType="submit"
              icon={<CheckCircleOutlined />}
              loading={isSubmitting}
              disabled={isSubmitting}
              size="large"
              block
            >
              {isSubmitting ? "Đang xử lý..." : "Cập nhật mật khẩu"}
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};
