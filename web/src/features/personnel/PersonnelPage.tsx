import { useState, useEffect, useCallback } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Alert, Button, Descriptions, Drawer, Empty, Form, Input, Modal, Select, Space, Spin, Table, Tabs, Tag, Typography } from "antd";
import { PlusOutlined, ReloadOutlined } from "@ant-design/icons";
import { useAuth } from "../../context/AuthContext";
import { personnelApi, organizationApi, employeeChanges, type Employee, type Schema } from "../shared/api";
import { ConflictNotice } from "../shared/Feedback";
import { ApiClientError } from "../../api/types";
import { PersonnelFeedback } from "./PersonnelFeedback";

const { Title, Text } = Typography;

type AccessDeniedHandler = (error: ApiClientError) => void;
const privatePersonnelKeys = new Set(["employees", "employee-detail", "employee-labels", "employee-picker"]);
function clearPersonnelCache(client: ReturnType<typeof useQueryClient>) {
  const filters = { predicate: (query: { queryKey: readonly unknown[] }) => privatePersonnelKeys.has(String(query.queryKey[0])) };
  void client.cancelQueries(filters);
  client.setQueriesData(filters, null);
}

export const statusName = (status?: string) => status === "DaNghiViec" ? "Đã nghỉ việc" : "Đang làm việc";

export const getTodayVn = () =>
  new Intl.DateTimeFormat("en-CA", {
    timeZone: "Asia/Ho_Chi_Minh",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(new Date());

export function Profile({ profile }: { profile: Employee }) {
  const { hasPermission } = useAuth();
  const canReadDepartments = hasPermission("departments.read");

  const deptQuery = useQuery({
    queryKey: ["organization-options", "departments", 1],
    queryFn: ({ signal }) => organizationApi.list("departments", 1, signal),
    enabled: Boolean(profile.departmentId && canReadDepartments),
    retry: false,
    staleTime: 60000,
  });

  const posQuery = useQuery({
    queryKey: ["organization-options", "positions", 1],
    queryFn: ({ signal }) => organizationApi.list("positions", 1, signal),
    enabled: Boolean(profile.positionId && canReadDepartments),
    retry: false,
    staleTime: 60000,
  });

  const deptName =
    (!deptQuery.isError && canReadDepartments
      ? deptQuery.data?.items.find(d => d.id === profile.departmentId)?.name
      : undefined) ||
    profile.departmentId ||
    "—";

  const posName =
    (!posQuery.isError && canReadDepartments
      ? posQuery.data?.items.find(p => p.id === profile.positionId)?.name
      : undefined) ||
    profile.positionId ||
    "—";

  return (
    <Descriptions
      bordered
      column={1}
      size="small"
      items={[
        { key: "code", label: "Mã nhân viên", children: profile.employeeCode || "—" },
        { key: "name", label: "Họ và tên", children: profile.fullName },
        { key: "email", label: "Email", children: profile.email || "—" },
        { key: "citizen", label: "CCCD", children: profile.citizenId || "—" },
        { key: "phone", label: "Số điện thoại", children: profile.phone || "—" },
        { key: "address", label: "Địa chỉ", children: profile.address || "—" },
        { key: "dateOfBirth", label: "Ngày sinh", children: profile.dateOfBirth || "—" },
        { key: "gender", label: "Giới tính", children: profile.gender || "—" },
        { key: "maritalStatus", label: "Tình trạng hôn nhân", children: profile.maritalStatus || "—" },
        { key: "department", label: "Phòng ban", children: deptName },
        { key: "position", label: "Chức vụ", children: posName },
        { key: "hireDate", label: "Ngày vào làm", children: profile.hireDate || "—" },
        {
          key: "status",
          label: "Trạng thái",
          children: (
            <Tag color={profile.status === "DaNghiViec" ? "default" : "blue"}>
              {statusName(profile.status)}
            </Tag>
          ),
        },
        ...(profile.status === "DaNghiViec" && profile.terminationDate
          ? [{ key: "terminationDate", label: "Ngày nghỉ việc", children: profile.terminationDate }]
          : []),
      ]}
    />
  );
}

export function OrganizationSelect({
  kind,
  value,
  onChange,
  id,
}: {
  kind: "departments" | "positions";
  value?: string | null;
  onChange?: (value: string | null) => void;
  id?: string;
}) {
  const [page, setPage] = useState(1);
  const query = useQuery({
    queryKey: ["organization-options", kind, page],
    queryFn: ({ signal }) => organizationApi.list(kind, page, signal),
    retry: false,
  });

  return (
    <Space direction="vertical" className="full-width">
      <Select
        id={id}
        allowClear
        value={value}
        onChange={v => onChange?.(v ?? null)}
        loading={query.isFetching}
        className="full-width"
        options={query.data?.items.map(item => ({ value: item.id, label: item.name }))}
        placeholder="Chưa chọn"
        notFoundContent={query.isError ? "Không tải được danh mục" : "Không có mục nào"}
        dropdownRender={menu => (
          <>
            {menu}
            <Space style={{ padding: "8px 12px" }}>
              <Button size="small" disabled={page === 1} onClick={() => setPage(p => p - 1)}>
                Trang trước
              </Button>
              <Text type="secondary">Trang {page}</Text>
              <Button
                size="small"
                disabled={!query.data || page * 20 >= query.data.totalCount}
                onClick={() => setPage(p => p + 1)}
              >
                Trang sau
              </Button>
            </Space>
          </>
        )}
      />
      <PersonnelFeedback error={query.error} retry={() => void query.refetch()} />
    </Space>
  );
}

type EditorValues = Partial<Employee> & { reason: string; username?: string };

export function EmployeeEditor({
  initial,
  onDone,
  onCancel,
  onAccessDenied,
}: {
  initial?: Employee;
  onDone: (message: string) => void;
  onCancel?: () => void;
  onAccessDenied?: AccessDeniedHandler;
}) {
  const [form] = Form.useForm<EditorValues>();
  const [baseline, setBaseline] = useState(initial);
  const [latest, setLatest] = useState<Employee>();
  const client = useQueryClient();

  const isOffboarded = baseline?.status === "DaNghiViec";

  const save = useMutation({
    mutationFn: async (values: EditorValues) => {
      if (baseline) {
        if (!baseline.rowVersion) {
          throw new Error("Thiếu phiên bản hồ sơ. Đóng và mở lại hồ sơ trước khi sửa.");
        }
        await personnelApi.update(baseline.id, employeeChanges(baseline, values, values.reason));
        return "Đã lưu hồ sơ nhân viên.";
      }
      const created = await personnelApi.create({
        fullName: values.fullName!.trim(),
        citizenId: values.citizenId!.trim(),
        email: values.email!.trim(),
        phone: values.phone!.trim(),
        address: values.address!.trim(),
        dateOfBirth: values.dateOfBirth!,
        gender: values.gender!.trim(),
        hireDate: values.hireDate!,
        maritalStatus: values.maritalStatus ? values.maritalStatus.trim() : null,
        departmentId: values.departmentId || null,
        positionId: values.positionId || null,
        reason: values.reason.trim(),
        username: values.username ? values.username.trim() : null,
      });
      const delivery =
        created.deliveryStatus === "TestDelivered"
          ? "Kênh kiểm thử đã nhận thư; không gửi email thật."
          : created.deliveryStatus === "DeliveryFailedPendingRetry"
          ? "Gửi thư kích hoạt chưa thành công, đang chờ thử lại."
          : "Chưa gửi email kích hoạt; cần cấu hình kênh gửi thư.";
      return `Đã tạo ${created.profile.employeeCode}. Tài khoản chưa hoạt động, đang chờ kích hoạt. ${delivery} Quản trị viên cần cấp phạm vi cho HR trước khi hồ sơ xuất hiện trong danh sách. Mã hồ sơ để cấp phạm vi: ${created.profile.id}`;
    },
    onSuccess: onDone,
  });

  const compare = useMutation({
    mutationFn: () => {
      if (!baseline?.id) throw new Error("Không có hồ sơ để đối chiếu.");
      return personnelApi.get(baseline.id);
    },
    onSuccess: setLatest,
  });

  // Only lock for comparison when UPDATE has a baseline and the error code is resource.conflict
  const conflict = Boolean(
    baseline &&
    save.error instanceof ApiClientError &&
    (save.error.code === "resource.conflict" ||
      (save.error.status === 409 && (!save.error.code || save.error.code === "resource.conflict")))
  );

  const isForbidden =
    (save.error instanceof ApiClientError && (save.error.status === 403 || save.error.status === 401)) ||
    (compare.error instanceof ApiClientError && (compare.error.status === 403 || compare.error.status === 401));

  // Purge sensitive query cache if HR scope was revoked mid-session
  useEffect(() => {
    if (isForbidden) {
      const error = (save.error instanceof ApiClientError && (save.error.status === 403 || save.error.status === 401)) ? save.error : compare.error;
      onAccessDenied?.(error as ApiClientError);
      clearPersonnelCache(client);
      if (baseline?.id) client.setQueryData(["employee-detail", baseline.id], null);
      setLatest(undefined);
    }
  }, [isForbidden, baseline?.id, client, save.error, compare.error, onAccessDenied]);

  // When scope is revoked, hide editor, profile and comparison entirely to prevent PII exposure
  if (isForbidden) {
    return (
      <Space direction="vertical" style={{ width: "100%", padding: "16px 0" }}>
        <PersonnelFeedback error={save.error || compare.error} />
        {onCancel && <Button onClick={onCancel}>Đóng</Button>}
      </Space>
    );
  }

  const handleFinish = (values: EditorValues) => {
    if (save.isPending || conflict || isForbidden || isOffboarded) return;
    save.mutate(values);
  };

  return (
    <Form
      name="employee-editor"
      form={form}
      layout="vertical"
      initialValues={initial || {}}
      onFinish={handleFinish}
      disabled={save.isPending || isOffboarded}
      onFinishFailed={({ errorFields }) => form.scrollToField(errorFields[0]?.name, { focus: true })}
    >
      {initial && (
        <Alert
          type="info"
          showIcon
          message={`${initial.employeeCode} · ${statusName(initial.status)}`}
          description="CCCD, email và quyền tài khoản được quản lý riêng; không thể sửa trực tiếp tại đây."
        />
      )}

      {isOffboarded && (
        <Alert
          type="warning"
          showIcon
          message="Nhân viên đã nghỉ việc"
          description="Hồ sơ nhân viên đã nghỉ việc ở chế độ chỉ đọc. Không thể chỉnh sửa thông tin."
        />
      )}

      <PersonnelFeedback error={save.error} />

      {conflict && (
        <ConflictNotice
          onCompare={() => compare.mutate()}
          pending={compare.isPending}
        />
      )}

      <PersonnelFeedback error={compare.error} retry={() => compare.mutate()} />

      {latest && (
        <section className="conflict-comparison" aria-label="So sánh hồ sơ">
          <Title level={5}>Bản mới trên máy chủ</Title>
          <Profile profile={latest} />
          {latest.status === "DaNghiViec" ? (
            <Alert
              type="error"
              showIcon
              message="Nhân viên đã nghỉ việc trên máy chủ"
              description="Hồ sơ này đã chuyển sang trạng thái Đã nghỉ việc. Bạn không thể tiếp tục chỉnh sửa."
              style={{ marginTop: 12 }}
            />
          ) : (
            <>
              <Button
                type="primary"
                onClick={() => {
                  form.setFieldsValue({ ...latest, reason: "" });
                  setBaseline(latest);
                  setLatest(undefined);
                  save.reset();
                }}
              >
                Dùng bản mới và nhập lại thay đổi
              </Button>
              <p style={{ marginTop: 8, color: "var(--muted)" }}>
                Bản nhập hiện tại vẫn ở bên dưới cho đến khi bạn chọn dùng bản mới.
              </p>
            </>
          )}
        </section>
      )}

      <Form.Item
        name="fullName"
        label="Họ và tên"
        rules={[
          { required: true, whitespace: true, message: "Nhập họ và tên nhân viên." },
          { max: 100, message: "Họ và tên không quá 100 ký tự." },
        ]}
      >
        <Input maxLength={100} placeholder="Nguyễn Văn A" />
      </Form.Item>

      {!initial && (
        <>
          <Form.Item
            name="citizenId"
            label="CCCD"
            rules={[
              { required: true, whitespace: true, message: "Nhập số CCCD." },
              { max: 20, message: "Số CCCD không quá 20 ký tự." },
            ]}
          >
            <Input maxLength={20} placeholder="001234567890" />
          </Form.Item>

          <Form.Item
            name="email"
            label="Email"
            rules={[
              { required: true, message: "Nhập địa chỉ email." },
              { type: "email", message: "Định dạng email không hợp lệ." },
              { max: 256, message: "Email không quá 256 ký tự." },
            ]}
          >
            <Input type="email" maxLength={256} placeholder="employee@congty.vn" />
          </Form.Item>

          <Form.Item
            name="username"
            label="Tên đăng nhập (tùy chọn)"
            rules={[{ max: 100, message: "Tên đăng nhập không quá 100 ký tự." }]}
          >
            <Input maxLength={100} placeholder="Mặc định dùng email nếu để trống" />
          </Form.Item>
        </>
      )}

      <Form.Item
        name="phone"
        label="Số điện thoại"
        rules={[
          { required: true, whitespace: true, message: "Nhập số điện thoại." },
          { max: 20, message: "Số điện thoại không quá 20 ký tự." },
        ]}
      >
        <Input maxLength={20} placeholder="0901234567" />
      </Form.Item>

      <Form.Item
        name="address"
        label="Địa chỉ"
        rules={[
          { required: true, whitespace: true, message: "Nhập địa chỉ." },
          { max: 256, message: "Địa chỉ không quá 256 ký tự." },
        ]}
      >
        <Input maxLength={256} placeholder="Số nhà, đường, phường/xã, quận/huyện, tỉnh/thành" />
      </Form.Item>

      <Form.Item
        name="dateOfBirth"
        label="Ngày sinh"
        dependencies={["hireDate"]}
        rules={[
          { required: !initial, message: "Chọn ngày sinh." },
          {
            validator: async (_, value: string | undefined) => {
              if (!value) return;
              const todayVn = getTodayVn();
              if (value > todayVn) {
                throw new Error("Ngày sinh không thể ở tương lai.");
              }
              const hireDate = form.getFieldValue("hireDate");
              if (hireDate && value >= hireDate) {
                throw new Error("Ngày sinh phải trước ngày vào làm.");
              }
            },
          },
        ]}
      >
        <Input type="date" />
      </Form.Item>

      <Form.Item
        name="gender"
        label="Giới tính"
        rules={[
          { required: true, whitespace: true, message: "Nhập giới tính." },
          { max: 20, message: "Giới tính không quá 20 ký tự." },
        ]}
      >
        <Input maxLength={20} placeholder="Nam, Nữ, hoặc Khác" />
      </Form.Item>

      <Form.Item
        name="maritalStatus"
        label="Tình trạng hôn nhân"
        rules={[{ max: 50, message: "Tình trạng hôn nhân không quá 50 ký tự." }]}
      >
        <Input maxLength={50} placeholder="Độc thân, Đã kết hôn, ..." />
      </Form.Item>

      <Form.Item
        name="hireDate"
        label="Ngày vào làm"
        dependencies={["dateOfBirth"]}
        rules={[
          { required: !initial, message: "Chọn ngày vào làm." },
          {
            validator: async (_, value: string | undefined) => {
              if (!value) return;
              const dateOfBirth = form.getFieldValue("dateOfBirth");
              if (dateOfBirth && dateOfBirth >= value) {
                throw new Error("Ngày vào làm phải sau ngày sinh.");
              }
            },
          },
        ]}
      >
        <Input type="date" />
      </Form.Item>

      <Form.Item name="departmentId" label="Phòng ban">
        <OrganizationSelect kind="departments" />
      </Form.Item>

      <Form.Item name="positionId" label="Chức vụ">
        <OrganizationSelect kind="positions" />
      </Form.Item>

      <Form.Item
        name="reason"
        label="Lý do thay đổi"
        rules={[
          { required: true, whitespace: true, message: "Nhập lý do thay đổi." },
          { max: 500, message: "Lý do không quá 500 ký tự." },
        ]}
      >
        <Input.TextArea maxLength={500} showCount rows={3} placeholder="Ghi rõ lý do thay đổi hoặc tạo hồ sơ" />
      </Form.Item>

      <Space style={{ marginTop: 16 }}>
        <Button
          type="primary"
          htmlType="submit"
          loading={save.isPending}
          disabled={save.isPending || conflict || isOffboarded}
        >
          {initial ? "Lưu thay đổi" : "Tạo nhân viên"}
        </Button>
        {onCancel && (
          <Button onClick={onCancel} disabled={save.isPending}>
            Hủy
          </Button>
        )}
      </Space>
    </Form>
  );
}

export function EmployeeDetails({ id, onDone, onAccessDenied }: { id: string; onDone: (message: string) => void; onAccessDenied?: AccessDeniedHandler }) {
  const queryClient = useQueryClient();
  const { hasPermission } = useAuth();
  // Default to edit tab for HR to maintain existing workflow and browser test expectations
  const [activeTab, setActiveTab] = useState<string>("edit");
  const [accessError, setAccessError] = useState<ApiClientError>();
  const denyAccess = useCallback((error: ApiClientError) => {
    setAccessError(error);
    onAccessDenied?.(error);
  }, [onAccessDenied]);

  const query = useQuery({
    queryKey: ["employee-detail", id],
    queryFn: ({ signal }) => personnelApi.get(id, signal),
    staleTime: 0,
    retry: false,
    enabled: !accessError,
  });

  const detailError = accessError || query.error;
  const isForbidden =
    detailError instanceof ApiClientError && (detailError.status === 403 || detailError.status === 401);

  // Purge sensitive query cache if HR scope revoked without triggering refetch loop
  useEffect(() => {
    if (isForbidden) {
      denyAccess(detailError as ApiClientError);
      clearPersonnelCache(queryClient);
    }
  }, [isForbidden, detailError, queryClient, denyAccess]);

  if (query.isPending && !isForbidden) {
    return (
      <div style={{ textAlign: "center", padding: "40px 0" }}>
        <Spin aria-label="Đang tải hồ sơ" />
      </div>
    );
  }

  if (query.isError || isForbidden) {
    return (
      <PersonnelFeedback
        error={detailError}
        retry={isForbidden ? undefined : () => void query.refetch()}
      />
    );
  }

  if (!query.data) return null;

  const canManage = hasPermission("employees.manage");
  const isOffboarded = query.data.status === "DaNghiViec";

  if (!canManage || isOffboarded) {
    return (
      <Space direction="vertical" style={{ width: "100%" }} size={16}>
        {isOffboarded && (
          <Alert
            type="warning"
            showIcon
            message="Nhân viên đã nghỉ việc"
            description="Hồ sơ nhân viên đã nghỉ việc ở chế độ chỉ đọc. Không thể chỉnh sửa thông tin."
          />
        )}
        <Profile profile={query.data} />
      </Space>
    );
  }

  return (
    <Tabs
      activeKey={activeTab}
      onChange={setActiveTab}
      items={[
        {
          key: "edit",
          label: "Chỉnh sửa hồ sơ",
          children: (
            <EmployeeEditor
              initial={query.data}
              onDone={onDone}
              onCancel={() => setActiveTab("details")}
              onAccessDenied={denyAccess}
            />
          ),
        },
        {
          key: "details",
          label: "Hồ sơ chi tiết",
          children: (
            <Space direction="vertical" style={{ width: "100%" }} size={16}>
              <Profile profile={query.data} />
              <Button type="primary" onClick={() => setActiveTab("edit")}>
                Chuyển sang chỉnh sửa
              </Button>
            </Space>
          ),
        },
      ]}
    />
  );
}

export function MyProfilePage() {
  const query = useQuery({
    queryKey: ["my-profile"],
    queryFn: ({ signal }) => personnelApi.me(signal),
    retry: false,
  });

  return (
    <section className="business-page">
      <Title level={2}>Hồ sơ của tôi</Title>
      <PersonnelFeedback error={query.error} retry={() => void query.refetch()} />
      {query.isPending ? (
        <div style={{ textAlign: "center", padding: "40px 0" }}>
          <Spin />
        </div>
      ) : (
        query.data && !query.isError && <Profile profile={query.data} />
      )}
    </section>
  );
}

export function PersonnelPage() {
  const client = useQueryClient();
  const { hasPermission } = useAuth();
  const [page, setPage] = useState(1);
  const [filters, setFilters] = useState<{ name?: string; departmentId?: string; status?: string }>({});
  const [selection, setSelection] = useState<string>();
  const [notice, setNotice] = useState("");
  const [accessError, setAccessError] = useState<ApiClientError>();
  const [offboard, setOffboard] = useState<Employee>();
  const [filterForm] = Form.useForm();
  const [offboardForm] = Form.useForm<Schema<"OffboardEmployeeRequest">>();
  const denyAccess = useCallback((error: ApiClientError) => {
    setAccessError(error);
    setSelection(undefined);
    setOffboard(undefined);
    setNotice("");
    offboardForm.resetFields();
    clearPersonnelCache(client);
  }, [client, offboardForm]);

  const query = useQuery({
    queryKey: ["employees", page, filters],
    queryFn: ({ signal }) => personnelApi.list({ page, pageSize: 20, ...filters }, signal),
    retry: false,
    enabled: !accessError,
  });

  const listError = accessError || query.error;
  const isListForbidden =
    listError instanceof ApiClientError && (listError.status === 403 || listError.status === 401);

  // Sensitive data guard: immediately purge employees cache and close any open drawer/modal containing PII
  useEffect(() => {
    if (isListForbidden) {
      denyAccess(listError as ApiClientError);
    }
  }, [isListForbidden, listError, denyAccess]);

  const done = (message: string) => {
    setSelection(undefined);
    setNotice(message);
    void client.invalidateQueries({ queryKey: ["employees"] });
    void client.invalidateQueries({ queryKey: ["employee-detail"] });
    void client.invalidateQueries({ queryKey: ["employee-labels"] });
    void client.invalidateQueries({ queryKey: ["employee-picker"] });
  };

  const revoke = useMutation({
    mutationFn: async (values: Schema<"OffboardEmployeeRequest">) => {
      if (!offboard?.rowVersion) {
        throw new Error("Thiếu phiên bản hồ sơ. Tải lại danh sách trước khi nghỉ việc.");
      }
      await personnelApi.offboard(offboard.id, {
        terminationDate: values.terminationDate,
        reason: values.reason.trim(),
        rowVersion: offboard.rowVersion,
      });
    },
    onSuccess: () => {
      setOffboard(undefined);
      offboardForm.resetFields();
      done("Đã ghi nhận nghỉ việc và thu hồi quyền truy cập theo chính sách máy chủ.");
    },
  });

  const revokeConflict = revoke.error instanceof ApiClientError && revoke.error.isConflict;
  const revokeForbidden =
    revoke.error instanceof ApiClientError && (revoke.error.status === 403 || revoke.error.status === 401);

  // If offboard receives 403, purge offboard target and prevent further attempts
  useEffect(() => {
    if (revokeForbidden && offboard?.id) {
      denyAccess(revoke.error as ApiClientError);
    }
  }, [revokeForbidden, offboard?.id, revoke.error, denyAccess]);

  const handleFilterFinish = (values: { name?: string; departmentId?: string; status?: string }) => {
    setPage(1);
    setFilters({
      name: values.name ? values.name.trim() : undefined,
      departmentId: values.departmentId || undefined,
      status: values.status || undefined,
    });
  };

  const handleResetFilters = () => {
    filterForm.resetFields();
    setPage(1);
    setFilters({});
  };

  const handleOffboardFinish = (values: Schema<"OffboardEmployeeRequest">) => {
    if (revoke.isPending || revokeConflict || revokeForbidden) return;
    revoke.mutate(values);
  };

  return (
    <section className="business-page">
      <div className="business-heading">
        <div>
          <Title level={2}>Nhân viên</Title>
          <Text type="secondary">
            {query.isError || isListForbidden ? "—" : (query.data?.totalCount ?? "—")} hồ sơ trong phạm vi được cấp
          </Text>
        </div>
        {hasPermission("employees.manage") && (
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setSelection("new")}>
            Thêm nhân viên
          </Button>
        )}
      </div>

      {notice && <Alert type="success" showIcon message={notice} closable onClose={() => setNotice("")} />}

      <Form
        name="personnel-filters"
        form={filterForm}
        layout="inline"
        className="business-filters"
        onFinish={handleFilterFinish}
      >
        <Form.Item name="name" label="Họ tên">
          <Input allowClear maxLength={100} placeholder="Tìm theo họ tên" />
        </Form.Item>
        {hasPermission("departments.read") && (
          <Form.Item name="departmentId" label="Phòng ban">
            <OrganizationSelect kind="departments" />
          </Form.Item>
        )}
        <Form.Item name="status" label="Trạng thái">
          <Select
            allowClear
            className="status-filter"
            placeholder="Tất cả trạng thái"
            options={[
              { value: "DangLamViec", label: "Đang làm việc" },
              { value: "DaNghiViec", label: "Đã nghỉ việc" },
            ]}
          />
        </Form.Item>
        <Space>
          <Button type="primary" htmlType="submit">
            Tìm kiếm
          </Button>
          <Button icon={<ReloadOutlined />} onClick={handleResetFilters}>
            Đặt lại
          </Button>
        </Space>
      </Form>

      <PersonnelFeedback
        error={listError}
        retry={isListForbidden ? undefined : () => void query.refetch()}
      />

      <div className="business-table" role="region" aria-label="Danh sách nhân viên" tabIndex={0}>
        <Table<Employee>
          rowKey="id"
          dataSource={query.isError || isListForbidden ? [] : query.data?.items}
          loading={query.isFetching}
          scroll={{ x: 780 }}
          locale={{ emptyText: <Empty description="Không có hồ sơ phù hợp trong phạm vi được cấp." /> }}
          pagination={{
            current: page,
            pageSize: 20,
            total: query.isError || isListForbidden ? 0 : query.data?.totalCount,
            showSizeChanger: false,
            onChange: setPage,
          }}
          columns={[
            {
              title: "Nhân viên",
              dataIndex: "fullName",
              render: (_, row) => (
                <>
                  <Button type="link" onClick={() => setSelection(row.id)}>
                    {row.fullName}
                  </Button>
                  <div className="employee-meta">{row.employeeCode}</div>
                </>
              ),
            },
            { title: "Email", dataIndex: "email", render: email => email || "—" },
            { title: "Số điện thoại", dataIndex: "phone", render: phone => phone || "—" },
            {
              title: "Trạng thái",
              render: (_, row) => (
                <Tag color={row.status === "DaNghiViec" ? "default" : "blue"}>
                  {statusName(row.status)}
                </Tag>
              ),
            },
            ...(hasPermission("employees.manage")
              ? [
                  {
                    title: "Thao tác",
                    render: (_: unknown, row: Employee) => (
                      <Button
                        danger
                        disabled={row.status === "DaNghiViec"}
                        onClick={() => {
                          revoke.reset();
                          offboardForm.resetFields();
                          setOffboard(row);
                        }}
                      >
                        Nghỉ việc
                      </Button>
                    ),
                  },
                ]
              : []),
          ]}
        />
      </div>

      {!isListForbidden && <Drawer
        title={selection === "new" ? "Thêm nhân viên" : "Hồ sơ nhân viên"}
        open={Boolean(selection && !isListForbidden)}
        onClose={() => setSelection(undefined)}
        width="min(480px, 100vw)"
        destroyOnClose
      >
        {selection === "new" ? (
          <EmployeeEditor onDone={done} onCancel={() => setSelection(undefined)} onAccessDenied={denyAccess} />
        ) : (
          selection && <EmployeeDetails key={selection} id={selection} onDone={done} onAccessDenied={denyAccess} />
        )}
      </Drawer>}

      {!isListForbidden && <Modal
        title={revokeForbidden ? "Nghỉ việc" : `Nghỉ việc · ${offboard?.fullName || ""}`}
        open={Boolean(offboard && !isListForbidden)}
        onCancel={() => {
          if (!revoke.isPending) setOffboard(undefined);
        }}
        footer={null}
        destroyOnClose
        closable={!revoke.isPending}
        maskClosable={!revoke.isPending}
      >
        <Alert
          type="warning"
          showIcon
          message="Thu hồi quyền truy cập"
          description="Tài khoản và các phiên web/mobile sẽ bị thu hồi. Không thể chấm công mới; lịch sử công, phép và lương được giữ lại."
        />
        <PersonnelFeedback error={revoke.error} />
        {revokeForbidden ? (
          <Button onClick={() => setOffboard(undefined)}>Đóng</Button>
        ) : (
          <>
            {revokeConflict && (
              <Button
                style={{ marginBottom: 16 }}
                onClick={() => {
                  setOffboard(undefined);
                  void query.refetch();
                }}
              >
                Đóng và tải lại danh sách trước khi xác nhận
              </Button>
            )}
            <Form
              name="employee-offboard"
              form={offboardForm}
              layout="vertical"
              onFinish={handleOffboardFinish}
              disabled={revoke.isPending}
            >
              <Form.Item
                name="terminationDate"
                label="Ngày nghỉ việc"
                rules={[{ required: true, message: "Chọn ngày nghỉ việc." }]}
              >
                <Input type="date" />
              </Form.Item>
              <Form.Item
                name="reason"
                label="Lý do nghỉ việc"
                rules={[
                  { required: true, whitespace: true, message: "Nhập lý do thay đổi." },
                  { max: 500, message: "Lý do không quá 500 ký tự." },
                ]}
              >
                <Input.TextArea maxLength={500} showCount rows={3} placeholder="Ghi rõ lý do thôi việc / chấm dứt hợp đồng" />
              </Form.Item>
              <Button
                danger
                type="primary"
                htmlType="submit"
                loading={revoke.isPending}
                disabled={revokeConflict}
              >
                Xác nhận nghỉ việc
              </Button>
            </Form>
          </>
        )}
      </Modal>}
    </section>
  );
}
