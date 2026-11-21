import React from "react";
import { Result, Button } from "antd";
import { useNavigate } from "react-router-dom";

export const ForbiddenPage: React.FC = () => {
  const navigate = useNavigate();

  return (
    <div style={{ padding: "48px 16px", minHeight: "60vh", display: "flex", alignItems: "center", justifyContent: "center" }}>
      <Result
        status="403"
        title="403 - Không có quyền truy cập"
        subTitle="Tài khoản hiện tại của bạn không sở hữu quyền hạn (permission) cần thiết để truy cập phân hệ này."
        extra={
          <Button type="primary" onClick={() => navigate("/")}>
            Quay về Trang chủ
          </Button>
        }
      />
    </div>
  );
};
