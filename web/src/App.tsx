import React from "react";
import { Routes, Route } from "react-router-dom";
import { LoginPage } from "./pages/LoginPage";
import { ForgotPasswordPage } from "./pages/ForgotPasswordPage";
import { ResetPasswordPage } from "./pages/ResetPasswordPage";
import { ChangePasswordPage } from "./pages/ChangePasswordPage";
import { DashboardPage } from "./pages/DashboardPage";
import { ForbiddenPage } from "./pages/ForbiddenPage";
import { NotFoundPage } from "./pages/NotFoundPage";
import { AppLayout } from "./components/layout/AppLayout";
import { ProtectedRoute } from "./components/guard/ProtectedRoute";
import { PersonnelPage, MyProfilePage } from "./features/personnel/PersonnelPage";
import { OrganizationPage } from "./features/personnel/OrganizationPage";
import { CalendarPage } from "./features/calendar/CalendarPage";
import { AccountsPage } from "./features/personnel/AccountsPage";
import { AuditPage } from "./features/personnel/AuditPage";
import { AttendancePage } from "./features/attendance/AttendancePage";
import { LeavePage } from "./features/leave/LeavePage";
import { PayrollPage } from "./features/payroll/PayrollPage";

export const App: React.FC = () => {
  return (
    <Routes>
      {/* Public Authentication Routes */}
      <Route path="/login" element={<LoginPage />} />
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/reset-password" element={<ResetPasswordPage />} />
      <Route path="/activate" element={<ResetPasswordPage isActivate />} />
      <Route path="/403" element={<ForbiddenPage />} />

      {/* Protected Private Routes wrapped inside AppLayout */}
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<DashboardPage />} />
        <Route path="change-password" element={<ChangePasswordPage />} />
        <Route path="my-profile" element={<MyProfilePage />} />
        <Route path="organization" element={<ProtectedRoute requiredPermission="departments.read"><OrganizationPage /></ProtectedRoute>} />
        <Route path="calendar" element={<ProtectedRoute requiredPermission="calendar.read"><CalendarPage /></ProtectedRoute>} />
        <Route path="audit" element={<ProtectedRoute requiredPermission="audit.read"><AuditPage /></ProtectedRoute>} />
        <Route path="leave" element={<ProtectedRoute requiredAnyPermission={["leaves.read_own", "leaves.read_all", "leaves.approve"]}><LeavePage /></ProtectedRoute>} />

        {/* Business modules guarded by real backend permission policies */}
        <Route
          path="personnel"
          element={
            <ProtectedRoute requiredPermission="employees.read">
              <PersonnelPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="attendance"
          element={
            <ProtectedRoute requiredAnyPermission={["attendance.read_own", "attendance.read_all"]}>
              <AttendancePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="payroll"
          element={
            <ProtectedRoute requiredAnyPermission={["payroll.read_all", "payroll.read_own"]}>
              <PayrollPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="admin"
          element={
            <ProtectedRoute requiredPermission="accounts.manage">
              <AccountsPage />
            </ProtectedRoute>
          }
        />
      </Route>

      {/* 404 Catch-All */}
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
};
