import React, { useState } from "react";
import { AuthFrame } from "../components/common/AuthFrame";
import { Form, Input, Button, Card, Typography } from "antd";
import { UserOutlined, LockOutlined, LoginOutlined } from "@ant-design/icons";
import { useNavigate, useLocation, Link, Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { parseErrorMessage } from "../api/client";
import { ErrorAlert } from "../components/common/ErrorAlert";

const { Title, Text } = Typography;

interface LoginFormValues {
  username: string;
  password: string;
}

export const LoginPage: React.FC = () => {
  const { login, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [form] = Form.useForm<LoginFormValues>();

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorInfo, setErrorInfo] = useState<{
    message: string;
    traceId?: string;
    code?: string;
  } | null>(null);

  // If already authenticated, redirect to home or destination
  const from = (location.state as { from?: string } | undefined)?.from || "/";
  const safeDestination = from.startsWith("/") && !from.startsWith("//") ? from : "/";

  if (isAuthenticated) {
    return <Navigate to={safeDestination} replace />;
  }

  const handleSubmit = async (values: LoginFormValues) => {
    setIsSubmitting(true);
    setErrorInfo(null);

    try {
      await login({
        username: values.username.trim(),
        password: values.password,
      });
      navigate(safeDestination, { replace: true });
    } catch (err) {
      const parsed = parseErrorMessage(err, "Đăng nhập thất bại. Vui lòng thử lại.");
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
    <AuthFrame>
      <Card className="auth-card">
        <div className="auth-heading">
          <Title level={3}>
            Đăng nhập HRM Next
          </Title>
          <Text type="secondary">Hệ thống Quản trị Nhân sự & Tiền lương</Text>
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

        <Form<LoginFormValues>
          form={form}
          layout="vertical"
          onFinish={handleSubmit}
          requiredMark="optional"
          autoComplete="on"
        >
          <Form.Item
            label="Tên đăng nhập"
            name="username"
            htmlFor="login_username"
            rules={[
              { required: true, message: "Vui lòng nhập tên đăng nhập" },
              { max: 256, message: "Tên đăng nhập không được vượt quá 256 ký tự" },
            ]}
          >
            <Input
              id="login_username"
              prefix={<UserOutlined style={{ color: "#bfbfbf" }} />}
              placeholder="Nhập tên tài khoản"
              autoComplete="username"
              disabled={isSubmitting}
              size="large"
            />
          </Form.Item>

          <Form.Item
            label="Mật khẩu"
            name="password"
            htmlFor="login_password"
            rules={[
              { required: true, message: "Vui lòng nhập mật khẩu" },
              { max: 128, message: "Mật khẩu không được vượt quá 128 ký tự" },
            ]}
          >
            <Input.Password
              id="login_password"
              prefix={<LockOutlined style={{ color: "#bfbfbf" }} />}
              placeholder="Nhập mật khẩu"
              autoComplete="current-password"
              disabled={isSubmitting}
              size="large"
            />
          </Form.Item>

          <div
            style={{
              display: "flex",
              justifyContent: "flex-end",
              marginBottom: 20,
            }}
          >
            <Link to="/forgot-password" style={{ fontSize: 13 }}>
              Quên mật khẩu?
            </Link>
          </div>

          <Form.Item style={{ marginBottom: 8 }}>
            <Button
              type="primary"
              htmlType="submit"
              icon={<LoginOutlined />}
              loading={isSubmitting}
              disabled={isSubmitting}
              block
              size="large"
            >
              {isSubmitting ? "Đang xác thực..." : "Đăng nhập"}
            </Button>
          </Form.Item>
        </Form>
        <p className="auth-support">Liên hệ quản trị viên nếu bạn cần được cấp tài khoản.</p>
      </Card>
    </AuthFrame>
  );
};
