import React, { createContext, useContext, useCallback, useMemo } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { fetchCurrentAccess, clearCachedCsrfToken } from "../api/client";
import { loginApi, logoutApi, changePasswordApi, type LoginParams, type ChangePasswordParams } from "../api/auth";
import { ApiClientError, type CurrentUserAccessResponse } from "../api/types";

export interface AuthContextType {
  user: CurrentUserAccessResponse | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  isError: boolean;
  error: unknown;
  hasPermission: (permission: string) => boolean;
  hasAnyPermission: (permissions: string[]) => boolean;
  hasRole: (role: string) => boolean;
  login: (params: LoginParams) => Promise<void>;
  logout: () => Promise<void>;
  changePassword: (params: ChangePasswordParams) => Promise<void>;
  retryBootstrap: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AUTH_QUERY_KEY = ["access", "me"] as const;

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const queryClient = useQueryClient();

  const {
    data: user = null,
    isLoading,
    isError,
    error,
    refetch,
  } = useQuery({
    queryKey: AUTH_QUERY_KEY,
    queryFn: async ({ signal }): Promise<CurrentUserAccessResponse | null> => {
      try {
        const access = await fetchCurrentAccess(signal);
        return access ?? null;
      } catch (err) {
        const isUnauthorized =
          typeof err === "object" &&
          err !== null &&
          (("status" in err && err.status === 401) ||
            ("code" in err && err.code === "auth.unauthorized"));
        if (isUnauthorized) {
          // Normal anonymous visitor or expired session:
          // Purge private queries & CSRF token to prevent stale PII retention,
          // while keeping auth query intact to avoid loop.
          clearCachedCsrfToken();
          queryClient.removeQueries({
            predicate: (query) => query.queryKey[0] !== "access",
          });
          return null;
        }
        // Real network errors or 5xx are preserved
        throw err;
      }
    },
    // Never retry on 401 or 403; allow max 2 retries for intermittent network glitches
    retry: (failureCount, queryError) => {
      if (queryError instanceof ApiClientError && (queryError.status === 401 || queryError.status === 403)) {
        return false;
      }
      return failureCount < 2;
    },
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });

  const isAuthenticated = Boolean(user && user.id);

  const hasPermission = useCallback(
    (permission: string): boolean => {
      if (!user) return false;
      return user.permissions.includes(permission);
    },
    [user]
  );

  const hasAnyPermission = useCallback(
    (permissions: string[]): boolean => {
      if (!user) return false;
      return permissions.some((p) => user.permissions.includes(p));
    },
    [user]
  );

  const hasRole = useCallback(
    (role: string): boolean => {
      if (!user) return false;
      return user.roles.includes(role);
    },
    [user]
  );

  const login = useCallback(
    async (params: LoginParams): Promise<void> => {
      // Purge any stale private queries from previous session before loading new identity
      await queryClient.cancelQueries();
      queryClient.removeQueries({
        predicate: (query) => query.queryKey[0] !== "access",
      });
      clearCachedCsrfToken();

      await loginApi(params);
      const profile = await fetchCurrentAccess();
      queryClient.setQueryData(AUTH_QUERY_KEY, profile);
    },
    [queryClient]
  );

  const logout = useCallback(async (): Promise<void> => {
    let logoutError: unknown = null;
    try {
      await logoutApi();
    } catch (e) {
      if (e instanceof ApiClientError && e.status === 401) {
        // 401 on logout is idempotent; session already revoked on server
      } else {
        logoutError = e;
      }
    } finally {
      // Always purge local private cache and CSRF token
      await queryClient.cancelQueries();
      queryClient.removeQueries({
        predicate: (query) => query.queryKey[0] !== "access",
      });
      clearCachedCsrfToken();
    }

    if (logoutError) {
      // Must propagate server logout failure so UI does not pretend logout succeeded when cookie is active
      throw logoutError;
    }

    queryClient.setQueryData(AUTH_QUERY_KEY, null);
  }, [queryClient]);

  const changePassword = useCallback(
    async (params: ChangePasswordParams): Promise<void> => {
      await changePasswordApi(params);
      await queryClient.cancelQueries();
      queryClient.removeQueries({
        predicate: (query) => query.queryKey[0] !== "access",
      });
      clearCachedCsrfToken();
      queryClient.setQueryData(AUTH_QUERY_KEY, null);
    },
    [queryClient]
  );

  const retryBootstrap = useCallback(async (): Promise<void> => {
    await refetch();
  }, [refetch]);

  const value = useMemo(
    () => ({
      user,
      isAuthenticated,
      isLoading,
      isError,
      error,
      hasPermission,
      hasAnyPermission,
      hasRole,
      login,
      logout,
      changePassword,
      retryBootstrap,
    }),
    [
      user,
      isAuthenticated,
      isLoading,
      isError,
      error,
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
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
