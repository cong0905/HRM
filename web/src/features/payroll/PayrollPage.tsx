import { useState } from "react";
import { Alert, Tabs } from "antd";
import { useAuth } from "../../context/AuthContext";
import { PayrollWorkflowWizard } from "./PayrollWorkflowWizard";
import { PayrollAdjustments } from "./PayrollAdjustments";
import { MyPayslips } from "./MyPayslips";

export function PayrollPage() {
  const { hasPermission } = useAuth();
  const [notice, setNotice] = useState<string | null>(null);

  const canReadAll = hasPermission("payroll.read_all");
  const canReadOwn = hasPermission("payroll.read_own");

  // Pure employee branch: only own published payslips
  if (!canReadAll && canReadOwn) {
    return (
      <div className="page-container">
        <div className="page-heading">
          <div>
            <h1>Phiếu lương cá nhân</h1>
            <p>Tra cứu chi tiết phiếu lương đã được công bố của bạn</p>
          </div>
        </div>

        {notice && (
          <Alert
            type="success"
            showIcon
            closable
            message={notice}
            onClose={() => setNotice(null)}
            style={{ marginBottom: 16 }}
          />
        )}

        <MyPayslips />
      </div>
    );
  }

  // Admin / Payroll Officer branch: full workspace with tabs
  const tabItems = [
    {
      key: "wizard",
      label: "Quản lý kỳ lương",
      children: <PayrollWorkflowWizard onNotice={setNotice} />,
    },
    {
      key: "adjustments",
      label: "Điều chỉnh & Lương cơ bản",
      children: <PayrollAdjustments onNotice={setNotice} />,
    },
  ];

  if (canReadOwn) {
    tabItems.push({
      key: "my-payslips",
      label: "Phiếu lương của tôi",
      children: <MyPayslips />,
    });
  }

  return (
    <div className="page-container">
      <div className="page-heading">
        <div>
          <h1>Quản lý Bảng lương</h1>
          <p>Khởi tạo kỳ, tính toán nháp, phê duyệt và công bố bảng lương</p>
        </div>
      </div>

      {notice && (
        <Alert
          type="success"
          showIcon
          closable
          message={notice}
          onClose={() => setNotice(null)}
          style={{ marginBottom: 16 }}
        />
      )}

      <Tabs defaultActiveKey="wizard" items={tabItems} />
    </div>
  );
}
