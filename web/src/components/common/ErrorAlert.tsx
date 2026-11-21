import React from "react";
import { Alert, Button, Typography, Space } from "antd";
import { ReloadOutlined } from "@ant-design/icons";

const { Text } = Typography;

interface ErrorAlertProps {
  message: string;
  traceId?: string;
  code?: string;
  onRetry?: () => void;
  closable?: boolean;
  style?: React.CSSProperties;
}

export const ErrorAlert: React.FC<ErrorAlertProps> = ({
  message,
  traceId,
  code,
  onRetry,
  closable = false,
  style,
}) => {
  const description = (
    <Space direction="vertical" size={4} style={{ width: "100%", marginTop: 4 }}>
      <div>{message}</div>
      {(traceId || code) && (
        <Space size={12} style={{ fontSize: 12 }}>
          {code && (
            <Text type="secondary">
              Mã lỗi: <Text code>{code}</Text>
            </Text>
          )}
          {traceId && (
            <Text type="secondary">
              Mã tra cứu (Trace ID): <Text code copyable>{traceId}</Text>
            </Text>
          )}
        </Space>
      )}
      {onRetry && (
        <div style={{ marginTop: 8 }}>
          <Button
            size="small"
            type="primary"
            danger
            icon={<ReloadOutlined />}
            onClick={onRetry}
          >
            Thử lại
          </Button>
        </div>
      )}
    </Space>
  );

  return (
    <div role="alert" aria-live="assertive" style={style}>
      <Alert
        message="Đã xảy ra sự cố"
        description={description}
        type="error"
        showIcon
        closable={closable}
      />
    </div>
  );
};
