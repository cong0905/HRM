import React from "react";
import { Spin } from "antd";

interface LoadingSpinnerProps {
  tip?: string;
  fullPage?: boolean;
}

export const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({
  tip = "Đang tải dữ liệu...",
  fullPage = false,
}) => {
  const containerStyle: React.CSSProperties = fullPage
    ? {
        minHeight: "100vh",
        display: "flex",
        flexDirection: "column",
        justifyContent: "center",
        alignItems: "center",
        padding: "24px",
      }
    : {
        padding: "48px 24px",
        display: "flex",
        flexDirection: "column",
        justifyContent: "center",
        alignItems: "center",
      };

  return (
    <div
      role="status"
      aria-live="polite"
      aria-busy="true"
      style={containerStyle}
      data-testid="loading-spinner"
    >
      <Spin size="large" />
      <span style={{ marginTop: 16, color: "#595959", fontSize: 14 }}>{tip}</span>
    </div>
  );
};
