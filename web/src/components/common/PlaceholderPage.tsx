import React from "react";
import { Button, Empty } from "antd";
import { Link } from "react-router-dom";

interface PlaceholderPageProps { moduleName: string; requiredPermission?: string; }

export const PlaceholderPage: React.FC<PlaceholderPageProps> = ({ moduleName }) => (
  <div className="page-container">
    <div className="page-heading"><div><h1>{moduleName}</h1><p>Không gian quản lý {moduleName.toLocaleLowerCase("vi-VN")}.</p></div></div>
    <section className="module-empty" aria-label="Trạng thái phân hệ">
      <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={<><strong>Phân hệ đang được hoàn thiện</strong><p>Dữ liệu và thao tác nghiệp vụ chưa được kết nối trên giao diện này.</p></>}>
        <Link to="/"><Button>Về tổng quan</Button></Link>
      </Empty>
    </section>
  </div>
);
