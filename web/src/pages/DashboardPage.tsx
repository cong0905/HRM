import React from "react";
import { Button, Empty } from "antd";
import {
  TeamOutlined,
  ClockCircleOutlined,
  DollarCircleOutlined,
  SettingOutlined,
  KeyOutlined,
  RightOutlined,
} from "@ant-design/icons";
import { Link } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { DashboardMetrics } from "../features/dashboard/DashboardMetrics";

const roleNames: Record<string, string> = {
  SystemAdmin: "Quản trị hệ thống",
  HR: "Nhân sự",
  Hr: "Nhân sự",
  Payroll: "Tiền lương",
  Employee: "Nhân viên",
};

export const DashboardPage: React.FC = () => {
  const { user, hasPermission } = useAuth();
  const modules = [
    {
      path: "/personnel",
      name: "Hồ sơ Nhân sự",
      detail: "Hồ sơ, phòng ban và chức vụ",
      icon: <TeamOutlined />,
      permissions: ["employees.read", "employees.manage"],
    },
    {
      path: "/attendance",
      name: "Chấm công & Điểm danh",
      detail: "Theo dõi công và thời gian làm việc",
      icon: <ClockCircleOutlined />,
      permissions: ["attendance.read_all", "attendance.read_own", "attendance.manage"],
    },
    {
      path: "/payroll",
      name: "Bảng lương",
      detail: "Kỳ lương và phiếu lương",
      icon: <DollarCircleOutlined />,
      permissions: ["payroll.read_all", "payroll.read_own"],
    },
    {
      path: "/admin",
      name: "Quản trị Hệ thống",
      detail: "Tài khoản và quyền truy cập",
      icon: <SettingOutlined />,
      permissions: ["accounts.manage", "accounts.assign_roles"],
    },
  ].filter((module) => module.permissions.some(hasPermission));

  return (
    <div className="page-container">
      <div className="page-heading">
        <div>
          <h1>Tổng quan</h1>
          <p>Chào mừng trở lại, {user?.username || "Người dùng"}!</p>
        </div>
      </div>

      <section className="account-summary" aria-label="Thông tin tài khoản">
        <span className="account-initial account-initial-large" aria-hidden="true">
          {user?.username?.slice(0, 2).toUpperCase() || "NV"}
        </span>
        <div className="account-summary-info">
          <strong>{user?.username || "Người dùng"}</strong>
          <span>
            {user?.roles?.map((role) => roleNames[role] || role).join(", ") || "Chưa được gán vai trò"}
          </span>
        </div>
        <Link to="/change-password">
          <Button icon={<KeyOutlined />}>Đổi mật khẩu tài khoản</Button>
        </Link>
      </section>

      {/* Real Permission-based Metrics Cards */}
      <DashboardMetrics />

      <section className="module-section" aria-labelledby="modules-heading">
        <h2 id="modules-heading">Không gian làm việc</h2>
        {modules.length > 0 ? (
          <div className="module-list">
            {modules.map((module) => (
              <Link to={module.path} className="module-row" key={module.path}>
                <span className="module-icon" aria-hidden="true">
                  {module.icon}
                </span>
                <span className="module-copy">
                  <strong>{module.name}</strong>
                  <span>{module.detail}</span>
                </span>
                <RightOutlined aria-hidden="true" />
              </Link>
            ))}
          </div>
        ) : (
          <Empty
            image={Empty.PRESENTED_IMAGE_SIMPLE}
            description="Tài khoản chưa được cấp quyền truy cập phân hệ. Vui lòng liên hệ quản trị viên."
          />
        )}
      </section>
    </div>
  );
};
