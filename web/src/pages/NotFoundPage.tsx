import React from "react";
import { Result, Button } from "antd";
import { useNavigate } from "react-router-dom";

export const NotFoundPage: React.FC = () => {
  const navigate = useNavigate();

  return (
    <div style={{ padding: "48px 16px", minHeight: "60vh", display: "flex", alignItems: "center", justifyContent: "center" }}>
      <Result
        status="404"
        title="404 - Không tìm thấy trang"
        subTitle="Đường dẫn bạn yêu cầu không tồn tại hoặc đã được di chuyển."
        extra={
          <Button type="primary" onClick={() => navigate("/")}>
            Quay về Trang chủ
          </Button>
        }
      />
    </div>
  );
};
