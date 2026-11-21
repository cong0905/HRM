import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import React from "react";
import { render, screen, waitFor, fireEvent, act } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { AuthProvider, AUTH_QUERY_KEY } from "../src/context/AuthContext";
import {
  PersonnelPage,
  EmployeeEditor,
  Profile,
  statusName,
  getTodayVn,
} from "../src/features/personnel/PersonnelPage";
import { personnelApi, employeeChanges, type Employee } from "../src/features/shared/api";
import { PERSONNEL_ERROR_MESSAGES } from "../src/features/personnel/PersonnelFeedback";
import { ApiClientError } from "../src/api/types";

let activeQueryClient: QueryClient | null = null;

const mockHrUser = {
  id: "44444444-4444-4444-4444-444444444444",
  username: "hr_reviewer",
  roles: ["HR"],
  permissions: ["employees.read", "employees.manage", "departments.read"],
};

const mockReadOnlyUser = {
  id: "55555555-5555-5555-5555-555555555555",
  username: "hr_reader",
  roles: ["HR"],
  permissions: ["employees.read"],
};

const mockEmployeeUser = {
  id: "66666666-6666-6666-6666-666666666666",
  username: "employee_user",
  roles: ["Employee"],
  permissions: ["calendar.read", "attendance.read_own", "leaves.create_own", "leaves.read_own"],
};

const mockEmployeeActive: Employee = {
  id: "11111111-1111-1111-1111-111111111111",
  userId: "22222222-2222-2222-2222-222222222222",
  employeeCode: "EMP-2026-001",
  fullName: "Lê Văn Hùng",
  citizenId: "001090012345",
  email: "hung.le@company.vn",
  phone: "0912345678",
  address: "Số 10 Tràng Tiền, Hoàn Kiếm, Hà Nội",
  dateOfBirth: "1995-05-15",
  gender: "Nam",
  maritalStatus: "Độc thân",
  departmentId: "33333333-3333-3333-3333-333333333333",
  positionId: null,
  hireDate: "2024-01-10",
  status: "DangLamViec",
  terminationDate: null,
  rowVersion: "AQIDBAUGBwg=",
};

const mockEmployeeOffboarded: Employee = {
  ...mockEmployeeActive,
  id: "11111111-1111-1111-1111-111111111112",
  fullName: "Trần Thị Mai",
  employeeCode: "EMP-2026-002",
  status: "DaNghiViec",
  terminationDate: "2026-06-30",
  rowVersion: "CQgHBgUEAwI=",
};

function renderWithProviders(ui: React.ReactElement, userProfile = mockHrUser) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
      mutations: { retry: false },
    },
  });
  activeQueryClient = queryClient;

  // Pre-seed auth query data so AuthProvider does not make uncontrolled network calls
  queryClient.setQueryData(AUTH_QUERY_KEY, userProfile);

  // Setup mock fetch for auth, csrf and organization endpoints to avoid uncaught network requests
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
    const url = input instanceof Request ? input.url : input.toString();
    if (url.includes("/api/v1/access/me")) {
      return Promise.resolve(
        new Response(JSON.stringify(userProfile), { status: 200, headers: { "Content-Type": "application/json" } })
      );
    }
    if (url.includes("/api/v1/auth/csrf")) {
      return Promise.resolve(
        new Response(JSON.stringify({ requestToken: "mock-csrf" }), { status: 200, headers: { "Content-Type": "application/json" } })
      );
    }
    if (url.includes("/api/v1/departments")) {
      return Promise.resolve(
        new Response(
          JSON.stringify({
            items: [{ id: "33333333-3333-3333-3333-333333333333", name: "Phòng Kỹ Thuật" }],
            totalCount: 1,
            page: 1,
            pageSize: 20,
          }),
          { status: 200, headers: { "Content-Type": "application/json" } }
        )
      );
    }
    if (url.includes("/api/v1/positions")) {
      return Promise.resolve(
        new Response(
          JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
          { status: 200, headers: { "Content-Type": "application/json" } }
        )
      );
    }
    if (url.includes("/api/v1/employees")) {
      return Promise.resolve(
        new Response(
          JSON.stringify({ items: [mockEmployeeActive], totalCount: 1, page: 1, pageSize: 20 }),
          { status: 200, headers: { "Content-Type": "application/json" } }
        )
      );
    }
    return Promise.reject(new Error(`Unhandled URL in test: ${url}`));
  });
  if (!vi.isMockFunction(globalThis.fetch)) vi.stubGlobal("fetch", fetchMock);

  return {
    ...render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AuthProvider>{ui}</AuthProvider>
        </MemoryRouter>
      </QueryClientProvider>
    ),
    queryClient,
    fetchMock,
  };
}

describe("Personnel 2026-11-16 Contract Validation, Concurrency & HR Scope Tests", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  afterEach(async () => {
    if (activeQueryClient) {
      await activeQueryClient.cancelQueries();
      activeQueryClient.clear();
      activeQueryClient = null;
    }
    vi.unstubAllGlobals();
  });

  describe("1. Contract validation & employeeChanges diff helper", () => {
    it("employeeChanges only sends changed fields and preserves rowVersion and trimmed reason", () => {
      const before = { ...mockEmployeeActive };
      const values = {
        fullName: "Lê Văn Hùng (Cập nhật)",
        phone: "0987654321",
        address: mockEmployeeActive.address, // unchanged
        reason: "   Điều chỉnh số điện thoại chính thức   ",
      };

      const patch = employeeChanges(before, values, values.reason);

      expect(patch.rowVersion).toBe(before.rowVersion);
      expect(patch.reason).toBe("Điều chỉnh số điện thoại chính thức");
      expect(patch.fullName).toBe("Lê Văn Hùng (Cập nhật)");
      expect(patch.phone).toBe("0987654321");
      expect(patch.address).toBeUndefined(); // unchanged field excluded
      expect(patch).not.toHaveProperty("citizenId"); // citizenId cannot be modified via update
      expect(patch).not.toHaveProperty("email"); // email cannot be modified via update
    });

    it("clears nullable fields like maritalStatus to null when set to empty string", () => {
      const before = { ...mockEmployeeActive, maritalStatus: "Độc thân" };
      const values = { maritalStatus: "", reason: "Xóa thông tin hôn nhân" };
      const patch = employeeChanges(before, values, values.reason);

      expect(patch.maritalStatus).toBeNull();
    });

    it("statusName maps DangLamViec and DaNghiViec correctly", () => {
      expect(statusName("DangLamViec")).toBe("Đang làm việc");
      expect(statusName("DaNghiViec")).toBe("Đã nghỉ việc");
      expect(statusName(undefined)).toBe("Đang làm việc");
    });

    it("getTodayVn returns valid YYYY-MM-DD in Vietnam timezone", () => {
      const today = getTodayVn();
      expect(today).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    });
  });

  describe("2. Form submission, double-submit protection & error state retention", () => {
    it("disables save button and prevents duplicate submissions while mutation is pending", async () => {
      type CreateResult = Awaited<ReturnType<typeof personnelApi.create>>;
      let resolveCreation: (value: CreateResult) => void;
      const pendingPromise = new Promise<CreateResult>(resolve => {
        resolveCreation = resolve;
      });

      vi.spyOn(personnelApi, "create").mockImplementation(() => pendingPromise);

      renderWithProviders(<EmployeeEditor onDone={vi.fn()} />);

      // Fill in required fields matching accessibility labels
      fireEvent.change(screen.getByLabelText("Họ và tên"), { target: { value: "Nguyễn Văn Test" } });
      fireEvent.change(screen.getByLabelText("CCCD"), { target: { value: "001234567890" } });
      fireEvent.change(screen.getByLabelText("Email"), { target: { value: "test@company.vn" } });
      fireEvent.change(screen.getByLabelText("Số điện thoại"), { target: { value: "0901234567" } });
      fireEvent.change(screen.getByLabelText("Địa chỉ"), { target: { value: "Hà Nội" } });
      fireEvent.change(screen.getByLabelText("Ngày sinh"), { target: { value: "1990-01-01" } });
      fireEvent.change(screen.getByLabelText("Giới tính"), { target: { value: "Nam" } });
      fireEvent.change(screen.getByLabelText("Ngày vào làm"), { target: { value: "2023-01-01" } });
      fireEvent.change(screen.getByLabelText("Lý do thay đổi"), { target: { value: "Tuyển dụng mới Q4" } });

      const submitButton = screen.getByRole("button", { name: "Tạo nhân viên" });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(personnelApi.create).toHaveBeenCalledTimes(1);
      });

      // Submit button is disabled while pending
      expect(submitButton).toBeDisabled();

      // Double-click attempt while pending
      fireEvent.click(submitButton);
      expect(personnelApi.create).toHaveBeenCalledTimes(1);

      // Resolve mutation
      await act(async () => {
        resolveCreation({
          profile: mockEmployeeActive,
          deliveryStatus: "TestDelivered",
          provisioningStatus: "InactivePendingActivation",
        });
      });
    });

    it("preserves user form input values when server returns an error so the user can fix and retry", async () => {
      vi.spyOn(personnelApi, "create").mockRejectedValue(
        new ApiClientError({
          status: 400,
          code: "employees.invalid_dates",
          message: "Ngày sinh và ngày vào làm không hợp lệ.",
        })
      );

      renderWithProviders(<EmployeeEditor onDone={vi.fn()} />);

      fireEvent.change(screen.getByLabelText("Họ và tên"), { target: { value: "Trần Bảo Lưu" } });
      fireEvent.change(screen.getByLabelText("CCCD"), { target: { value: "001099887766" } });
      fireEvent.change(screen.getByLabelText("Email"), { target: { value: "baoluu@company.vn" } });
      fireEvent.change(screen.getByLabelText("Số điện thoại"), { target: { value: "0933333333" } });
      fireEvent.change(screen.getByLabelText("Địa chỉ"), { target: { value: "Đà Nẵng" } });
      fireEvent.change(screen.getByLabelText("Ngày sinh"), { target: { value: "1990-01-01" } });
      fireEvent.change(screen.getByLabelText("Giới tính"), { target: { value: "Nam" } });
      fireEvent.change(screen.getByLabelText("Ngày vào làm"), { target: { value: "2024-01-01" } });
      fireEvent.change(screen.getByLabelText("Lý do thay đổi"), { target: { value: "Hồ sơ lỗi ngày" } });

      const submitButton = screen.getByRole("button", { name: "Tạo nhân viên" });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(PERSONNEL_ERROR_MESSAGES["employees.invalid_dates"]!)).toBeInTheDocument();
      });

      // Assert form input values were NOT erased
      expect(screen.getByLabelText("Họ và tên")).toHaveValue("Trần Bảo Lưu");
      expect(screen.getByLabelText("CCCD")).toHaveValue("001099887766");
      expect(screen.getByLabelText("Email")).toHaveValue("baoluu@company.vn");
      expect(screen.getByLabelText("Lý do thay đổi")).toHaveValue("Hồ sơ lỗi ngày");
    });

    it("handles create 409 duplicate email: preserves form, allows editing email and resubmits successfully without calling get()", async () => {
      const getSpy = vi.spyOn(personnelApi, "get");
      const onDone = vi.fn();

      const createSpy = vi.spyOn(personnelApi, "create")
        .mockRejectedValueOnce(
          new ApiClientError({
            status: 409,
            code: "identity.duplicate_email",
            message: "Email đã tồn tại.",
          })
        )
        .mockResolvedValueOnce({
          profile: { ...mockEmployeeActive, email: "unique.email@company.vn" },
          deliveryStatus: "TestDelivered",
          provisioningStatus: "InactivePendingActivation",
        });

      renderWithProviders(<EmployeeEditor onDone={onDone} />);

      fireEvent.change(screen.getByLabelText("Họ và tên"), { target: { value: "Lê Trùng Email" } });
      fireEvent.change(screen.getByLabelText("CCCD"), { target: { value: "001099999999" } });
      fireEvent.change(screen.getByLabelText("Email"), { target: { value: "duplicate@company.vn" } });
      fireEvent.change(screen.getByLabelText("Số điện thoại"), { target: { value: "0911222333" } });
      fireEvent.change(screen.getByLabelText("Địa chỉ"), { target: { value: "Hà Nội" } });
      fireEvent.change(screen.getByLabelText("Ngày sinh"), { target: { value: "1992-02-02" } });
      fireEvent.change(screen.getByLabelText("Giới tính"), { target: { value: "Nam" } });
      fireEvent.change(screen.getByLabelText("Ngày vào làm"), { target: { value: "2024-02-02" } });
      fireEvent.change(screen.getByLabelText("Lý do thay đổi"), { target: { value: "Tạo tài khoản" } });

      const submitButton = screen.getByRole("button", { name: "Tạo nhân viên" });
      fireEvent.click(submitButton);

      // Verify duplicate error is displayed
      await waitFor(() => {
        expect(screen.getByText(PERSONNEL_ERROR_MESSAGES["identity.duplicate_email"]!)).toBeInTheDocument();
      });

      // Verify ConflictNotice ("Tải bản mới để so sánh") is NOT shown for create
      expect(screen.queryByRole("button", { name: "Tải bản mới để so sánh" })).not.toBeInTheDocument();

      // Submit button is NOT locked for compare; user can change email and submit again
      expect(submitButton).not.toBeDisabled();

      // User fixes the email
      fireEvent.change(screen.getByLabelText("Email"), { target: { value: "unique.email@company.vn" } });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(createSpy).toHaveBeenCalledTimes(2);
        expect(onDone).toHaveBeenCalledTimes(1);
      });

      // Absolutely verify personnelApi.get was NEVER called
      expect(getSpy).not.toHaveBeenCalled();
    });
  });

  describe("3. 409 Concurrency & rowVersion conflict handling", () => {
    it("handles 409 conflict on edit: disables overwrite, prompts deliberate reload, and updates baseline when accepted", { timeout: 20000 }, async () => {
      vi.spyOn(personnelApi, "update").mockRejectedValue(
        new ApiClientError({
          status: 409,
          code: "resource.conflict",
          message: "Dữ liệu hồ sơ đã được thay đổi trên máy chủ.",
        })
      );

      const updatedServerRecord: Employee = {
        ...mockEmployeeActive,
        phone: "0999999999",
        rowVersion: "TkVXV1JPS1ZFUlNJT04=", // Updated version
      };
      vi.spyOn(personnelApi, "get").mockResolvedValue(updatedServerRecord);

      renderWithProviders(<EmployeeEditor initial={mockEmployeeActive} onDone={vi.fn()} />);

      fireEvent.change(screen.getByLabelText("Lý do thay đổi"), { target: { value: "Đổi số điện thoại" } });
      fireEvent.change(screen.getByLabelText("Số điện thoại"), { target: { value: "0911223344" } });

      const saveButton = screen.getByRole("button", { name: "Lưu thay đổi" });
      fireEvent.click(saveButton);

      // 409 Conflict triggers notice
      await waitFor(() => {
        expect(screen.getByText(/Dữ liệu đã thay đổi/i)).toBeInTheDocument();
      });

      // Submit button is disabled to prevent overwriting
      expect(saveButton).toBeDisabled();

      // Click deliberate comparison button
      const compareButton = screen.getByRole("button", { name: "Tải bản mới để so sánh" });
      fireEvent.click(compareButton);

      await waitFor(() => {
        expect(personnelApi.get).toHaveBeenCalledWith(mockEmployeeActive.id);
        expect(screen.getByRole("region", { name: "So sánh hồ sơ" })).toBeInTheDocument();
      });

      // Accept new server version
      const acceptButton = screen.getByRole("button", { name: "Dùng bản mới và nhập lại thay đổi" });
      fireEvent.click(acceptButton);

      // Verify save button is re-enabled with new baseline
      await waitFor(() => {
        expect(screen.queryByRole("region", { name: "So sánh hồ sơ" })).not.toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Lưu thay đổi" })).not.toBeDisabled();
      });
    });

    it("blocks editing if employee was offboarded on server during conflict comparison", { timeout: 20000 }, async () => {
      vi.spyOn(personnelApi, "update").mockRejectedValue(
        new ApiClientError({
          status: 409,
          code: "resource.conflict",
          message: "Conflict",
        })
      );

      const offboardedOnServer: Employee = {
        ...mockEmployeeOffboarded,
        rowVersion: "TkVXV1ZFUlNJT04y",
      };
      vi.spyOn(personnelApi, "get").mockResolvedValue(offboardedOnServer);

      renderWithProviders(<EmployeeEditor initial={mockEmployeeActive} onDone={vi.fn()} />);

      fireEvent.change(screen.getByLabelText("Lý do thay đổi"), { target: { value: "Cập nhật địa chỉ" } });
      fireEvent.click(screen.getByRole("button", { name: "Lưu thay đổi" }));

      await waitFor(() => {
        expect(screen.getByText(/Dữ liệu đã thay đổi/i)).toBeInTheDocument();
      });

      fireEvent.click(screen.getByRole("button", { name: "Tải bản mới để so sánh" }));

      await waitFor(() => {
        expect(screen.getByText(/Nhân viên đã nghỉ việc trên máy chủ/i)).toBeInTheDocument();
      });

      // Cannot adopt or edit an offboarded employee
      expect(screen.queryByRole("button", { name: "Dùng bản mới và nhập lại thay đổi" })).not.toBeInTheDocument();
    });
  });

  describe("4. Sensitive stale data purge on 403 Forbidden / HR Scope denial", () => {
    it("purges employees query cache, closes open drawer/modal and displays no stale records when server rejects HR scope with 403", async () => {
      const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
        const url = input instanceof Request ? input.url : input.toString();
        if (url.includes("/api/v1/access/me")) {
          return Promise.resolve(
            new Response(JSON.stringify(mockHrUser), { status: 200, headers: { "Content-Type": "application/json" } })
          );
        }
        if (url.includes("/api/v1/employees")) {
          return Promise.resolve(
            new Response(
              JSON.stringify({
                status: 403,
                errorCode: "authorization.scope_forbidden",
                title: "Forbidden",
                detail: "HR caller not assigned to any employees in this scope.",
              }),
              { status: 403, headers: { "Content-Type": "application/problem+json" } }
            )
          );
        }
        return Promise.reject(new Error(`Unhandled URL: ${url}`));
      });
      vi.stubGlobal("fetch", fetchMock);

      const { queryClient } = renderWithProviders(<PersonnelPage />);

      await waitFor(() => {
        expect(
          screen.getByText(PERSONNEL_ERROR_MESSAGES["authorization.scope_forbidden"]!)
        ).toBeInTheDocument();
      });

      // Table shows empty state, not sensitive records
      expect(screen.getByText("Không có hồ sơ phù hợp trong phạm vi được cấp.")).toBeInTheDocument();
      // Total count displays "—"
      expect(screen.getByText("— hồ sơ trong phạm vi được cấp")).toBeInTheDocument();

      // Query cache for employees was purged
      expect(queryClient.getQueryData(["employees", 1, {}])).toBeNull();
    });

    it("regression test: preloaded cached data is wiped, drawer is closed and DOM has no PII when scope is revoked mid-session", { timeout: 20000 }, async () => {
      vi.spyOn(personnelApi, "list").mockResolvedValue({ items: [mockEmployeeActive], totalCount: 1, page: 1, pageSize: 20 });
      vi.spyOn(personnelApi, "get").mockResolvedValue(mockEmployeeActive);
      const { queryClient } = renderWithProviders(<PersonnelPage />);

      // Pre-seed query cache simulating previously loaded data
      queryClient.setQueryData(["employees", 1, {}], {
        items: [mockEmployeeActive],
        totalCount: 1,
        page: 1,
        pageSize: 20,
      });

      // Employee is visible initially
      await waitFor(() => {
        expect(screen.getByText("Lê Văn Hùng")).toBeInTheDocument();
      });

      // Open drawer
      fireEvent.click(screen.getByRole("button", { name: "Lê Văn Hùng" }));

      // Now scope is revoked on server; trigger a filter change or refetch that returns 403
      vi.spyOn(personnelApi, "list").mockRejectedValue(
        new ApiClientError({
          status: 403,
          code: "authorization.scope_forbidden",
          message: "Scope revoked",
        })
      );

      // Trigger search filter to cause refetch
      await act(async () => { await queryClient.invalidateQueries({ queryKey: ["employees"] }); });

      await waitFor(() => {
        expect(
          screen.getByText(PERSONNEL_ERROR_MESSAGES["authorization.scope_forbidden"]!)
        ).toBeInTheDocument();
      });

      // Verify Drawer containing PII is completely closed/hidden from DOM
      expect(screen.queryByText("Hồ sơ nhân viên")).not.toBeInTheDocument();
      expect(screen.queryByText("EMP-2026-001")).not.toBeInTheDocument();

      // Table records wiped
      expect(screen.queryByText("Lê Văn Hùng")).not.toBeInTheDocument();
      expect(screen.getByText("— hồ sơ trong phạm vi được cấp")).toBeInTheDocument();

      // Cache set to null without refetch loop
      expect(queryClient.getQueryData(["employees", 1, {}])).toBeNull();
    });

    it("hides editor and profile, purges cache and prevents mutation when UPDATE receives 403 scope forbidden", { timeout: 20000 }, async () => {
      vi.spyOn(personnelApi, "update").mockRejectedValue(
        new ApiClientError({
          status: 403,
          code: "authorization.scope_forbidden",
          message: "Phạm vi bị từ chối.",
        })
      );

      const { queryClient } = renderWithProviders(
        <EmployeeEditor initial={mockEmployeeActive} onDone={vi.fn()} />
      );

      // Initial PII is present in fields
      expect(screen.getByLabelText("Số điện thoại")).toHaveValue(mockEmployeeActive.phone);

      fireEvent.change(screen.getByLabelText("Lý do thay đổi"), { target: { value: "Đổi địa chỉ" } });
      fireEvent.click(screen.getByRole("button", { name: "Lưu thay đổi" }));

      await waitFor(() => {
        expect(
          screen.getByText(PERSONNEL_ERROR_MESSAGES["authorization.scope_forbidden"]!)
        ).toBeInTheDocument();
      });

      // Entire editor form is removed from DOM to prevent PII exposure
      expect(screen.queryByLabelText("Số điện thoại")).not.toBeInTheDocument();
      expect(screen.queryByLabelText("Địa chỉ")).not.toBeInTheDocument();
      expect(screen.queryByRole("button", { name: "Lưu thay đổi" })).not.toBeInTheDocument();

      // Cache is cleared
      expect(queryClient.getQueryData(["employee-detail", mockEmployeeActive.id]) ?? null).toBeNull();
    });
  });

  describe("5. Role and permission enforcement", () => {
    it("renders full profile details without querying departments when user lacks departments.read", async () => {
      const fetchSpy = vi.fn();
      vi.stubGlobal("fetch", fetchSpy);

      // Render profile under an Employee user who lacks departments.read
      renderWithProviders(<Profile profile={mockEmployeeActive} />, mockEmployeeUser);

      expect(screen.getByText("Mã nhân viên")).toBeInTheDocument();
      expect(screen.getByText("EMP-2026-001")).toBeInTheDocument();
      expect(screen.getByText("Lê Văn Hùng")).toBeInTheDocument();
      expect(screen.getByText("001090012345")).toBeInTheDocument();
      expect(screen.getByText("hung.le@company.vn")).toBeInTheDocument();
      expect(screen.getByText("0912345678")).toBeInTheDocument();
      expect(screen.getByText("Đang làm việc")).toBeInTheDocument();

      // Since employee lacks departments.read, NO fetch to /api/v1/departments should occur
      expect(fetchSpy).not.toHaveBeenCalled();
    });

    it("read-only HR without employees.manage cannot edit or offboard employees", { timeout: 20000 }, async () => {
      const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
        const url = input instanceof Request ? input.url : input.toString();
        if (url.includes("/api/v1/access/me")) {
          return Promise.resolve(
            new Response(JSON.stringify(mockReadOnlyUser), { status: 200, headers: { "Content-Type": "application/json" } })
          );
        }
        if (url.includes("/api/v1/employees")) {
          return Promise.resolve(
            new Response(
              JSON.stringify({
                items: [mockEmployeeActive],
                totalCount: 1,
                page: 1,
                pageSize: 20,
              }),
              { status: 200, headers: { "Content-Type": "application/json" } }
            )
          );
        }
        return Promise.reject(new Error(`Unhandled URL: ${url}`));
      });
      vi.stubGlobal("fetch", fetchMock);

      renderWithProviders(<PersonnelPage />, mockReadOnlyUser);

      await waitFor(() => {
        expect(screen.getByText("Lê Văn Hùng")).toBeInTheDocument();
      });

      // Read-only HR does not see "Thêm nhân viên" or "Nghỉ việc" buttons
      expect(screen.queryByRole("button", { name: "Thêm nhân viên" })).not.toBeInTheDocument();
      expect(screen.queryByRole("button", { name: "Nghỉ việc" })).not.toBeInTheDocument();
    });

    it("renders offboarded employee as read-only with warning tag and termination date", () => {
      renderWithProviders(<Profile profile={mockEmployeeOffboarded} />);

      expect(screen.getByText("Trần Thị Mai")).toBeInTheDocument();
      expect(screen.getByText("Đã nghỉ việc")).toBeInTheDocument();
      expect(screen.getByText("Ngày nghỉ việc")).toBeInTheDocument();
      expect(screen.getByText("2026-06-30")).toBeInTheDocument();
    });
  });
});

