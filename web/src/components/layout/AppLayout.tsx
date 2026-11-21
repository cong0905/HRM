import React, { useState, useMemo } from "react";
import { Layout, Menu, Button, Typography, Tag, Dropdown, Drawer, Modal } from "antd";
import type { MenuProps } from "antd";
import {
  DashboardOutlined,
  TeamOutlined,
  ClockCircleOutlined,
  DollarCircleOutlined,
  SettingOutlined,
  DownOutlined,
  LogoutOutlined,
  KeyOutlined,
  MenuOutlined,
} from "@ant-design/icons";
import { useNavigate, useLocation, Outlet, Link } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { parseErrorMessage } from "../../api/client";

const { Header, Content, Sider } = Layout;
const { Text } = Typography;

export const AppLayout: React.FC = () => {
  const { user, logout, hasPermission } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [mobileDrawerOpen, setMobileDrawerOpen] = useState(false);
  const [isLoggingOut, setIsLoggingOut] = useState(false);

  // Generate menu items strictly based on REAL backend permission keys from HrmPermissions.cs:
  // - employees.read / employees.manage for Personnel
  // - attendance.read_all / attendance.read_own / attendance.manage for Attendance
  // - payroll.read_all / payroll.read_own for Payroll
  // - accounts.manage / accounts.assign_roles for System Administration
  // SystemAdmin DOES NOT automatically get HR or Payroll permissions unless explicitly assigned.
  const menuItems = useMemo<MenuProps["items"]>(() => {
    const items: NonNullable<MenuProps["items"]> = [
      {
        key: "/",
        icon: <DashboardOutlined />,
        label: <Link to="/">Tổng quan</Link>,
      },
    ];

    items.push({ key: "/my-profile", icon: <TeamOutlined />, label: <Link to="/my-profile">Hồ sơ của tôi</Link> });
    if (hasPermission("calendar.read")) items.push({ key: "/calendar", icon: <ClockCircleOutlined />, label: <Link to="/calendar">Lịch làm việc</Link> });
    if (hasPermission("departments.read")) items.push({ key: "/organization", icon: <TeamOutlined />, label: <Link to="/organization">Phòng ban và chức vụ</Link> });
    if (hasPermission("audit.read")) items.push({ key: "/audit", icon: <SettingOutlined />, label: <Link to="/audit">Nhật ký hệ thống</Link> });
    if (hasPermission("leaves.read_own") || hasPermission("leaves.read_all") || hasPermission("leaves.approve")) items.push({ key: "/leave", icon: <ClockCircleOutlined />, label: <Link to="/leave">Nghỉ phép</Link> });

    if (hasPermission("employees.read") || hasPermission("employees.manage")) {
      items.push({
        key: "/personnel",
        icon: <TeamOutlined />,
        label: <Link to="/personnel">Nhân sự</Link>,
      });
    }

    if (
      hasPermission("attendance.read_all") ||
      hasPermission("attendance.read_own") ||
      hasPermission("attendance.manage")
    ) {
      items.push({
        key: "/attendance",
        icon: <ClockCircleOutlined />,
        label: <Link to="/attendance">Chấm công</Link>,
      });
    }

    if (hasPermission("payroll.read_all") || hasPermission("payroll.read_own")) {
      items.push({
        key: "/payroll",
        icon: <DollarCircleOutlined />,
        label: <Link to="/payroll">Bảng lương</Link>,
      });
    }

    if (hasPermission("accounts.manage") || hasPermission("accounts.assign_roles")) {
      items.push({
        key: "/admin",
        icon: <SettingOutlined />,
        label: <Link to="/admin">Quản trị</Link>,
      });
    }

    return items;
  }, [hasPermission]);

  const handleLogout = async () => {
    setIsLoggingOut(true);
    try {
      await logout();
      navigate("/login", { replace: true });
    } catch (err) {
      const parsed = parseErrorMessage(err, "Máy chủ không thể hoàn tất việc hủy phiên đăng nhập.");
      Modal.error({
        title: "Đăng xuất chưa hoàn tất",
        content: `Máy chủ gặp sự cố khi hủy phiên làm việc (${parsed.message}). Cookie xác thực có thể vẫn còn hiệu lực. Vui lòng bấm thử lại để đảm bảo an toàn.`,
        okText: "Thử lại",
        cancelText: "Đóng",
        okCancel: true,
        onOk: handleLogout,
      });
    } finally {
      setIsLoggingOut(false);
    }
  };

  const userDropdownItems: MenuProps["items"] = [
    {
      key: "user-info",
      disabled: true,
      label: (
        <div style={{ padding: "4px 0" }}>
          <Text strong>{user?.username || "Người dùng"}</Text>
          <div style={{ marginTop: 4 }}>
            {user?.roles?.map((role) => (
              <Tag color="geekblue" key={role} style={{ fontSize: 11 }}>
                {role}
              </Tag>
            ))}
          </div>
        </div>
      ),
    },
    { type: "divider" },
    {
      key: "change-password",
      icon: <KeyOutlined />,
      label: "Đổi mật khẩu",
      onClick: () => navigate("/change-password"),
    },
    {
      key: "logout",
      icon: <LogoutOutlined />,
      danger: true,
      label: "Đăng xuất",
      onClick: handleLogout,
    },
  ];

  const currentKey = location.pathname;

  const pageName = ({ "/": "Tổng quan", "/personnel": "Nhân viên", "/attendance": "Chấm công", "/leave": "Nghỉ phép", "/payroll": "Bảng lương", "/admin": "Quản trị", "/change-password": "Đổi mật khẩu", "/my-profile": "Hồ sơ của tôi", "/calendar": "Lịch làm việc", "/organization": "Phòng ban và chức vụ", "/audit": "Nhật ký hệ thống" } as Record<string, string>)[currentKey] || "HRM Next";

  return (
    <Layout className="hrm-app">
      <a href="#main-content" className="skip-link">Đến nội dung chính</a>
      <Sider breakpoint="lg" collapsedWidth="0" trigger={null} width={216} className="desktop-sider">
        <Link to="/" className="app-brand" aria-label="HRM Next - Trang chủ">
          <strong>HRM Next</strong><span>Quản lý nhân sự</span>
        </Link>
        <nav aria-label="Điều hướng chính">
          <Menu mode="inline" selectedKeys={[currentKey]} items={menuItems} />
        </nav>
        <div className="sidebar-account">
          <span className="account-initial" aria-hidden="true">{user?.username?.slice(0, 2).toUpperCase() || "NV"}</span>
          <div><strong title={user?.username || undefined}>{user?.username || "Người dùng"}</strong><span>Tài khoản làm việc</span></div>
        </div>
      </Sider>
      <Drawer title="Menu HRM Next" placement="left" onClose={() => setMobileDrawerOpen(false)} open={mobileDrawerOpen} styles={{ body: { padding: "12px 0" } }} width={260}>
        <nav aria-label="Điều hướng trên điện thoại">
          <Menu mode="inline" selectedKeys={[currentKey]} items={menuItems} onClick={() => setMobileDrawerOpen(false)} />
        </nav>
      </Drawer>
      <Layout className="workspace">
        <Header className="app-header">
          <div className="header-location">
            <Button type="text" className="mobile-menu-toggle" icon={<MenuOutlined />} onClick={() => setMobileDrawerOpen(true)} aria-label="Mở menu điều hướng" />
            <span className="header-product">HRM Next</span><span className="breadcrumb-divider" aria-hidden="true">/</span><span className="header-page">{pageName}</span>
          </div>
          <Dropdown menu={{ items: userDropdownItems }} trigger={["click"]} placement="bottomRight">
            <Button type="text" loading={isLoggingOut} className="account-button" aria-label="Tài khoản cá nhân">
              <span className="account-initial" aria-hidden="true">{user?.username?.slice(0, 2).toUpperCase() || "NV"}</span>
              <span className="account-name">{user?.username || "Tài khoản"}</span><DownOutlined />
            </Button>
          </Dropdown>
        </Header>
        <Content id="main-content" tabIndex={-1} className="app-content"><Outlet /></Content>
      </Layout>
    </Layout>
  );
};
