import React, { useState, useEffect } from "react";
import { AuthFrame } from "../components/common/AuthFrame";
import { Form, Input, Button, Card, Typography, Result } from "antd";
import { LockOutlined, CheckCircleOutlined, ArrowLeftOutlined } from "@ant-design/icons";
import { useSearchParams, useNavigate, Link } from "react-router-dom";
import { resetPasswordApi, activateAccountApi } from "../api/auth";
import { parseErrorMessage } from "../api/client";
import { ErrorAlert } from "../components/common/ErrorAlert";

const { Title, Text, Paragraph } = Typography;

interface ResetFormValues {
  newPassword: string;
  confirmPassword: string;
}

interface ResetPasswordPageProps {
  isActivate?: boolean;
}

export const ResetPasswordPage: React.FC<ResetPasswordPageProps> = ({ isActivate = false }) => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [form] = Form.useForm<ResetFormValues>();

  // State to hold sensitive credentials in memory only; never in local/sessionStorage
  const [userId, setUserId] = useState<string>("");
  const [code, setCode] = useState<string>("");
  const [isSuccess, setIsSuccess] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorInfo, setErrorInfo] = useState<{
    message: string;
    traceId?: string;
    code?: string;
  } | null>(null);

  useEffect(() => {
    // Read code and userId from URL query parameters
    const paramUserId = searchParams.get("userId") || "";
    const paramCode = searchParams.get("code") || "";

    if (paramUserId || paramCode) {
      setUserId(paramUserId);
      setCode(paramCode);

      // Contract requirement: Immediately clean code and userId from URL to prevent leakage via browser history or referrer!
      window.history.replaceState({}, document.title, window.location.pathname);
    }
  }, [searchParams]);

  const handleSubmit = async (values: ResetFormValues) => {
    if (!userId || !code) {
      setErrorInfo({
        message: "Thiếu thông tin mã xác thực hoặc định danh người dùng. Vui lòng mở lại liên kết từ email.",
      });
      return;
    }

    setIsSubmitting(true);
    setErrorInfo(null);

    try {
      const payload = {
        userId,
        code,
        newPassword: values.newPassword,
      };

      if (isActivate) {
        await activateAccountApi(payload);
      } else {
        await resetPasswordApi(payload);
      }

      setIsSuccess(true);
    } catch (err) {
      const parsed = parseErrorMessage(
        err,
        isActivate
          ? "Kích hoạt tài khoản thất bại. Vui lòng thử lại."
          : "Đặt lại mật khẩu thất bại. Vui lòng thử lại."
      );
      setErrorInfo({
        message: parsed.message,
        traceId: parsed.traceId,
        code: parsed.code,
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isSuccess) {
    return (
      <AuthFrame>
        <Card className="auth-card">
          <Result
            status="success"
            title={isActivate ? "Kích hoạt tài khoản thành công!" : "Đặt lại mật khẩu thành công!"}
            subTitle="Mật khẩu của bạn đã được cập nhật thành công vào hệ thống. Bạn có thể sử dụng mật khẩu mới này để đăng nhập ngay."
            extra={[
              <Button
                type="primary"
                key="login"
                icon={<ArrowLeftOutlined />}
                onClick={() => navigate("/login")}
                size="large"
                block
              >
                Chuyển đến màn hình Đăng nhập
              </Button>,
            ]}
          />
        </Card>
    </AuthFrame>
    );
  }

  return (
    <AuthFrame>
      <Card className="auth-card">
        <div className="auth-heading">
          <Title level={3} style={{ marginBottom: 4 }}>
            {isActivate ? "Kích hoạt tài khoản" : "Đặt lại mật khẩu"}
          </Title>
          <Text type="secondary">
            {isActivate
              ? "Thiết lập mật khẩu an toàn để kích hoạt tài khoản nhân viên của bạn"
              : "Vui lòng nhập mật khẩu mới đáp ứng tiêu chuẩn an toàn của hệ thống"}
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

        {(!userId || !code) && (
          <div style={{ marginBottom: 20 }}>
            <Paragraph type="warning" style={{ fontSize: 13, background: "#fffbe6", padding: 12, borderRadius: 6, border: "1px solid #ffe58f" }}>
              Cảnh báo: Không phát hiện mã xác thực trong phiên làm việc.
              Nếu bạn mở trang này trực tiếp, hãy dùng liên kết đầy đủ được cung cấp bởi hệ thống.
            </Paragraph>
          </div>
        )}

        <Form<ResetFormValues>
          form={form}
          layout="vertical"
          onFinish={handleSubmit}
          autoComplete="on"
        >
          <Form.Item
            label="Mật khẩu mới"
            name="newPassword"
            htmlFor="reset_new_password"
            rules={[
              { required: true, message: "Vui lòng nhập mật khẩu mới" },
              { min: 12, message: "Mật khẩu phải chứa ít nhất 12 ký tự" },
              {
                pattern: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{12,}$/,
                message: "Mật khẩu phải bao gồm chữ thường, chữ hoa, số và ký tự đặc biệt",
              },
            ]}
            extra="Ít nhất 12 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt."
          >
            <Input.Password
              id="reset_new_password"
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
            htmlFor="reset_confirm_password"
            dependencies={["newPassword"]}
            rules={[
              { required: true, message: "Vui lòng xác nhận lại mật khẩu" },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  if (!value || getFieldValue("newPassword") === value) {
                    return Promise.resolve();
                  }
                  return Promise.reject(new Error("Mật khẩu xác nhận không khớp!"));
                },
              }),
            ]}
          >
            <Input.Password
              id="reset_confirm_password"
              prefix={<LockOutlined style={{ color: "#bfbfbf" }} />}
              placeholder="Nhập lại mật khẩu mới"
              autoComplete="new-password"
              disabled={isSubmitting}
              size="large"
            />
          </Form.Item>

          <Form.Item style={{ marginTop: 24, marginBottom: 12 }}>
            <Button
              type="primary"
              htmlType="submit"
              icon={<CheckCircleOutlined />}
              loading={isSubmitting}
              disabled={isSubmitting || !userId || !code}
              block
              size="large"
            >
              {isSubmitting ? "Đang xử lý..." : isActivate ? "Kích hoạt tài khoản" : "Cập nhật mật khẩu mới"}
            </Button>
          </Form.Item>

          <div style={{ textAlign: "center", marginTop: 16 }}>
            <Link to="/login">
              <ArrowLeftOutlined style={{ marginRight: 6 }} />
              Quay lại Đăng nhập
            </Link>
          </div>
        </Form>
      </Card>
    </AuthFrame>
  );
};
