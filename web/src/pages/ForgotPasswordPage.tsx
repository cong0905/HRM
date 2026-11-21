import React, { useState } from "react";
import { AuthFrame } from "../components/common/AuthFrame";
import { Form, Input, Button, Card, Typography, Alert, Space } from "antd";
import { MailOutlined, ArrowLeftOutlined, SendOutlined } from "@ant-design/icons";
import { Link } from "react-router-dom";
import { forgotPasswordApi } from "../api/auth";
import { parseErrorMessage } from "../api/client";
import { ErrorAlert } from "../components/common/ErrorAlert";
import type { DeliveryResponse } from "../api/types";

const { Title, Text, Paragraph } = Typography;

interface ForgotFormValues {
  email: string;
}

export const ForgotPasswordPage: React.FC = () => {
  const [form] = Form.useForm<ForgotFormValues>();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [deliveryResult, setDeliveryResult] = useState<DeliveryResponse | null>(null);
  const [errorInfo, setErrorInfo] = useState<{
    message: string;
    traceId?: string;
    code?: string;
  } | null>(null);

  const handleSubmit = async (values: ForgotFormValues) => {
    setIsSubmitting(true);
    setErrorInfo(null);
    setDeliveryResult(null);

    try {
      const response = await forgotPasswordApi({
        email: values.email.trim(),
      });
      setDeliveryResult(response);
    } catch (err) {
      const parsed = parseErrorMessage(err, "Không thể gửi yêu cầu quên mật khẩu. Vui lòng thử lại.");
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
          <Title level={3} style={{ marginBottom: 4 }}>
            Quên mật khẩu
          </Title>
          <Text type="secondary">
            Nhập địa chỉ email đăng ký để nhận hướng dẫn đặt lại mật khẩu
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

        {deliveryResult ? (
          <div style={{ marginTop: 16 }}>
            <Alert
              message="Kết quả gửi yêu cầu"
              description={
                <Space direction="vertical" size={6} style={{ width: "100%", marginTop: 8 }}>
                  <div>{deliveryResult.message}</div>
                  <div style={{ fontSize: 13, marginTop: 4 }}>
                    <Text strong>Chế độ phân phát máy chủ: </Text>
                    <Text code>{deliveryResult.deliveryMode}</Text>
                  </div>
                  {deliveryResult.deliveryMode === "not-configured" && (
                    <Paragraph type="warning" style={{ fontSize: 12, margin: "8px 0 0 0" }}>
                      Lưu ý môi trường phát triển: Máy chủ thông báo kênh gửi email chưa được cấu hình.
                      Vui lòng liên hệ quản trị viên để lấy mã đặt lại mật khẩu trực tiếp.
                    </Paragraph>
                  )}
                </Space>
              }
              type={deliveryResult.deliveryMode === "not-configured" ? "warning" : "info"}
              showIcon
            />
            <div style={{ marginTop: 24, textAlign: "center" }}>
              <Link to="/login">
                <Button type="primary" icon={<ArrowLeftOutlined />}>
                  Quay lại Đăng nhập
                </Button>
              </Link>
            </div>
          </div>
        ) : (
          <Form<ForgotFormValues>
            form={form}
            layout="vertical"
            onFinish={handleSubmit}
            autoComplete="on"
          >
            <Form.Item
              label="Địa chỉ email đã đăng ký"
              name="email"
              htmlFor="forgot_email"
              rules={[
                { required: true, message: "Vui lòng nhập email" },
                { type: "email", message: "Địa chỉ email không đúng định dạng" },
                { max: 256, message: "Email không được vượt quá 256 ký tự" },
              ]}
            >
              <Input
                id="forgot_email"
                prefix={<MailOutlined style={{ color: "#bfbfbf" }} />}
                placeholder="example@hrm.com"
                autoComplete="email"
                disabled={isSubmitting}
                size="large"
              />
            </Form.Item>

            <Form.Item style={{ marginTop: 24, marginBottom: 12 }}>
              <Button
                type="primary"
                htmlType="submit"
                icon={<SendOutlined />}
                loading={isSubmitting}
                disabled={isSubmitting}
                block
                size="large"
              >
                {isSubmitting ? "Đang gửi yêu cầu..." : "Gửi yêu cầu đặt lại mật khẩu"}
              </Button>
            </Form.Item>

            <div style={{ textAlign: "center", marginTop: 16 }}>
              <Link to="/login">
                <ArrowLeftOutlined style={{ marginRight: 6 }} />
                Quay lại màn hình đăng nhập
              </Link>
            </div>
          </Form>
        )}
      </Card>
    </AuthFrame>
  );
};
