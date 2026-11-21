import React from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { LoadingSpinner } from "../common/LoadingSpinner";
import { ErrorAlert } from "../common/ErrorAlert";
import { ForbiddenPage } from "../../pages/ForbiddenPage";
import { parseErrorMessage } from "../../api/client";

interface ProtectedRouteProps {
  children?: React.ReactNode;
  requiredPermission?: string;
  requiredAnyPermission?: string[];
  requiredRole?: string;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  children,
  requiredPermission,
  requiredAnyPermission,
  requiredRole,
}) => {
  const { isAuthenticated, isLoading, isError, error, hasPermission, hasAnyPermission, hasRole, retryBootstrap } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return <LoadingSpinner fullPage tip="Đang xác thực phiên làm việc..." />;
  }

  if (isError) {
    const parsed = parseErrorMessage(error, "Không thể kết nối với máy chủ để xác thực phiên.");
    return (
      <div style={{ maxWidth: 600, margin: "80px auto", padding: "0 16px" }}>
        <ErrorAlert
          message={parsed.message}
          traceId={parsed.traceId}
          code={parsed.code}
          onRetry={retryBootstrap}
        />
      </div>
    );
  }

  if (!isAuthenticated) {
    // Prevent open redirect: only pass relative paths starting with /
    const safeFrom = location.pathname.startsWith("/") && !location.pathname.startsWith("//")
      ? `${location.pathname}${location.search}`
      : "/";

    return <Navigate to="/login" state={{ from: safeFrom }} replace />;
  }

  if (requiredPermission && !hasPermission(requiredPermission)) {
    return <ForbiddenPage />;
  }
  if (requiredAnyPermission && !hasAnyPermission(requiredAnyPermission)) return <ForbiddenPage />;

  if (requiredRole && !hasRole(requiredRole)) {
    return <ForbiddenPage />;
  }

  return <>{children}</>;
};
