import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  useMemo,
  useRef,
} from "react";
import { AppState, AppStateStatus } from "react-native";
import { SessionManager } from "../api/session";
import { DataCacheManager } from "../storage/cache";
import {
  mobileLogin,
  logout as apiLogout,
  changePassword as apiChangePassword,
  fetchCurrentAccess,
} from "../api/auth";
import { ApiClientError } from "../api/errors";
import type {
  CurrentUserAccessResponse,
  LoginRequest,
  ChangePasswordRequest,
} from "../api/types";

export type AuthStatus =
  | "bootstrapping"    // Đang khôi phục phiên từ SecureStore và xác thực server
  | "authenticated"    // Đã xác thực thành công với máy chủ
  | "unauthenticated"  // Chưa đăng nhập hoặc phiên đã bị hủy/hết hạn
  | "offline_session"; // Có phiên hợp lệ nhưng mất kết nối mạng khi mở app

export interface AuthContextType {
  status: AuthStatus;
  user: CurrentUserAccessResponse | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  isError: boolean;
  error: unknown;
  isAppBackgrounded: boolean;
  serverRevocationNotice: string | null;
  clearServerRevocationNotice: () => void;
  hasPermission: (permission: string) => boolean;
  hasAnyPermission: (permissions: string[]) => boolean;
  hasRole: (role: string) => boolean;
  login: (params: LoginRequest) => Promise<void>;
  logout: () => Promise<{ serverConfirmed: boolean }>;
  changePassword: (params: ChangePasswordRequest) => Promise<void>;
  retryBootstrap: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [status, setStatus] = useState<AuthStatus>("bootstrapping");
  const [user, setUser] = useState<CurrentUserAccessResponse | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<unknown>(null);
  const [isAppBackgrounded, setIsAppBackgrounded] = useState<boolean>(false);
  const [serverRevocationNotice, setServerRevocationNotice] = useState<string | null>(null);

  const statusRef = useRef(status);
  statusRef.current = status;

  /**
   * Khôi phục phiên an toàn từ SecureStore khi ứng dụng khởi chạy (Session Restoration).
   * Chống flash dữ liệu cũ hoặc flash màn hình đăng nhập trước khi hoàn tất kiểm tra.
   */
  const bootstrapSession = useCallback(async (): Promise<void> => {
    setStatus("bootstrapping");
    setError(null);
    setUser(null); // Che dữ liệu người dùng khi đang bootstrap

    const bootstrapEpoch = SessionManager.getEpoch();

    try {
      const stored = await SessionManager.initialize();
      if (SessionManager.getEpoch() !== bootstrapEpoch) {
        return;
      }

      if (!stored) {
        setUser(null);
        setStatus("unauthenticated");
        return;
      }

      // Đã có token trên thiết bị: gọi server xác minh danh tính và quyền hạn
      try {
        const access = await fetchCurrentAccess();
        if (SessionManager.getEpoch() !== bootstrapEpoch) {
          return;
        }
        setUser(access);
        DataCacheManager.setActiveUser(access.id, SessionManager.getEpoch());
        setStatus("authenticated");
      } catch (err) {
        if (SessionManager.getEpoch() !== bootstrapEpoch) {
          return;
        }

        if (err instanceof ApiClientError) {
          if (err.status === 401) {
            // Phiên đã bị server thu hồi
            await SessionManager.invalidateSession();
            setUser(null);
            setStatus("unauthenticated");
            return;
          }
          if (err.isNetworkError) {
            // Lỗi mạng tạm thời: giữ nguyên token trong SecureStore, chuyển sang offline_session
            setUser(null);
            setStatus("offline_session");
            setError(err);
            return;
          }
        }
        setError(err);
        setUser(null);
        setStatus("offline_session");
      }
    } catch (err) {
      if (SessionManager.getEpoch() === bootstrapEpoch) {
        setError(err);
        setUser(null);
        setStatus("unauthenticated");
      }
    }
  }, []);

  useEffect(() => {
    void bootstrapSession();

    // Lắng nghe sự kiện phiên bị hủy từ SessionManager (refresh thất bại hoặc 401 retry lần 2)
    const unsubscribe = SessionManager.subscribeSessionExpired(() => {
      setUser(null);
      setStatus("unauthenticated");
    });

    return () => {
      unsubscribe();
    };
  }, [bootstrapSession]);

  /**
   * Xử lý vòng đời AppState (Background / Resume)
   * - Khi chạy ngầm (background): che giấu nội dung nhạy cảm.
   * - Khi quay lại (resume): xác minh lại phiên với server để phát hiện thu hồi từ web.
   */
  useEffect(() => {
    const handleAppStateChange = (nextAppState: AppStateStatus) => {
      if (nextAppState === "background" || nextAppState === "inactive") {
        setIsAppBackgrounded(true);
      } else if (nextAppState === "active") {
        setIsAppBackgrounded(false);
        if (statusRef.current === "authenticated" || statusRef.current === "offline_session") {
          void bootstrapSession();
        }
      }
    };

    const sub = AppState.addEventListener("change", handleAppStateChange);
    return () => {
      sub.remove();
    };
  }, [bootstrapSession]);

  /**
   * Kiểm tra quyền hạn: Chỉ trả về true khi đang ở trạng thái 'authenticated'.
   */
  const hasPermission = useCallback(
    (permission: string): boolean => {
      if (status !== "authenticated" || !user?.permissions) return false;
      return user.permissions.includes(permission);
    },
    [status, user]
  );

  const hasAnyPermission = useCallback(
    (permissions: string[]): boolean => {
      if (status !== "authenticated" || !user?.permissions) return false;
      return permissions.some((p) => user.permissions.includes(p));
    },
    [status, user]
  );

  const hasRole = useCallback(
    (role: string): boolean => {
      if (status !== "authenticated" || !user?.roles) return false;
      return user.roles.includes(role);
    },
    [status, user]
  );

  /**
   * Đăng nhập phiên mới.
   * Cách ly cache: xóa sạch dữ liệu cũ và áp dụng Epoch guards.
   */
  const login = useCallback(
    async (params: LoginRequest): Promise<void> => {
      setIsLoading(true);
      setError(null);
      setUser(null); // Che dữ liệu ngay

      const loginTask = mobileLogin(params);
      const loginEpoch = SessionManager.getEpoch();

      try {
        await loginTask;
        if (SessionManager.getEpoch() !== loginEpoch) {
          return;
        }

        const access = await fetchCurrentAccess();
        if (SessionManager.getEpoch() !== loginEpoch) {
          return;
        }

        setUser(access);
        DataCacheManager.setActiveUser(access.id, SessionManager.getEpoch());
        setStatus("authenticated");
        setServerRevocationNotice(null);
      } catch (err) {
        if (SessionManager.getEpoch() === loginEpoch) {
          setError(err);
          setUser(null);
          setStatus("unauthenticated");
        }
        throw err;
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  /**
   * Đăng xuất an toàn:
   * Che dữ liệu, dọn sạch SecureStore cục bộ ngay và lưu thông báo nếu server chưa xác nhận thu hồi.
   */
  const logout = useCallback(async (): Promise<{ serverConfirmed: boolean }> => {
    setIsLoading(true);
    // Che dữ liệu ngay tại chỗ
    setUser(null);
    DataCacheManager.clearAll();
    setStatus("unauthenticated");

    try {
      const result = await apiLogout();
      if (!result.localCleared) {
        setServerRevocationNotice('Ứng dụng đã khóa phiên. Chưa thể xóa dữ liệu đăng nhập trên thiết bị; vui lòng thử lại sau.');
      } else if (!result.serverConfirmed) {
        setServerRevocationNotice(
          "Đã xóa phiên trên thiết bị. Máy chủ chưa thể xác nhận thu hồi do lỗi kết nối mạng."
        );
      } else {
        setServerRevocationNotice(null);
      }
      return result;
    } finally {
      setIsLoading(false);
    }
  }, []);

  /**
   * Đổi mật khẩu.
   * Server thu hồi toàn bộ phiên -> chuyển unauthenticated.
   */
  const changePassword = useCallback(
    async (params: ChangePasswordRequest): Promise<void> => {
      setIsLoading(true);
      setError(null);

      try {
        await apiChangePassword(params);
        setUser(null);
        setStatus("unauthenticated");
        setServerRevocationNotice(
          "Mật khẩu đã được thay đổi thành công. Mọi phiên làm việc cũ đã được thu hồi, vui lòng đăng nhập lại."
        );
      } catch (err) {
        setError(err);
        throw err;
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  const retryBootstrap = useCallback(async (): Promise<void> => {
    await bootstrapSession();
  }, [bootstrapSession]);

  const clearServerRevocationNotice = useCallback(() => {
    setServerRevocationNotice(null);
  }, []);

  const value = useMemo<AuthContextType>(
    () => ({
      status,
      user,
      isAuthenticated: status === "authenticated" && Boolean(user?.id),
      isLoading,
      isError: Boolean(error),
      error,
      isAppBackgrounded,
      serverRevocationNotice,
      clearServerRevocationNotice,
      hasPermission,
      hasAnyPermission,
      hasRole,
      login,
      logout,
      changePassword,
      retryBootstrap,
    }),
    [
      status,
      user,
      isLoading,
      error,
      isAppBackgrounded,
      serverRevocationNotice,
      clearServerRevocationNotice,
      hasPermission,
      hasAnyPermission,
      hasRole,
      login,
      logout,
      changePassword,
      retryBootstrap,
    ]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth phải được sử dụng bên trong AuthProvider");
  }
  return context;
}
