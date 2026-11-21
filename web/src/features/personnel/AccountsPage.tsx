import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Alert, Button, Form, Input, Modal, Select, Space, Table, Tag, Typography } from "antd";
import { hrmClient } from "../../api/client";
import { useAuth } from "../../context/AuthContext";
import { unwrap, type Schema } from "../shared/api";
import { Feedback } from "../shared/Feedback";

type Account = Schema<"AccountResponse">;
export function AccountsPage() {
  const [page, setPage] = useState(1);
  const [username, setUsername] = useState("");
  const [selected, setSelected] = useState<Account>();
  const [mode, setMode] = useState<"roles" | "disable" | "scopes">();
  const [notice, setNotice] = useState("");
  const [form] = Form.useForm<{ roles: string[]; reason: string; employeeIds?: string }>();
  const { user, hasPermission } = useAuth();
  const client = useQueryClient();
  const query = useQuery({ queryKey: ["accounts", page, username], queryFn: async ({ signal }) => unwrap(await hrmClient.GET("/api/v1/accounts", { params: { query: { page, pageSize: 20, username } }, signal })), retry: false });
  const scopes = useQuery({ queryKey: ["account-scopes", selected?.id], enabled: mode === "scopes" && !!selected,
    queryFn: async ({ signal }) => unwrap(await hrmClient.GET("/api/v1/accounts/{id}/scopes", { params: { path: { id: selected!.id } }, signal })), retry: false });
  const write = useMutation({ mutationFn: async (values: { roles: string[]; reason: string; employeeIds?: string }) => mode === "scopes"
    ? unwrap(await hrmClient.POST("/api/v1/accounts/{id}/scopes", { params: { path: { id: selected!.id } }, body: { reason: values.reason, employeeIds: values.employeeIds!.split(/[\s,]+/).filter(Boolean) } }))
    : mode === "roles"
    ? unwrap(await hrmClient.PUT("/api/v1/accounts/{id}/roles", { params: { path: { id: selected!.id } }, body: values }))
    : unwrap(await hrmClient.POST("/api/v1/accounts/{id}/disable", { params: { path: { id: selected!.id } }, body: { reason: values.reason } })),
    onSuccess: () => { setNotice(mode === "scopes" ? "Đã cấp phạm vi hồ sơ cho HR." : "Đã cập nhật tài khoản và thu hồi các phiên cũ."); setMode(undefined); void client.invalidateQueries({ queryKey: ["accounts"] }); void client.invalidateQueries({ queryKey: ["account-scopes"] }); } });
  const open = (next: typeof mode, account: Account) => { setSelected(account); form.resetFields(); form.setFieldsValue({ roles: account.roles }); write.reset(); setMode(next); };
  return <section className="business-page"><Typography.Title level={2}>Tài khoản và vai trò</Typography.Title>
    <Form layout="inline" className="business-filters" onFinish={v => { setUsername(v.username || ""); setPage(1); }}><Form.Item name="username" label="Tên đăng nhập"><Input allowClear maxLength={100} /></Form.Item><Button htmlType="submit">Tìm kiếm</Button></Form>
    {notice && <Alert type="success" message={notice} showIcon closable onClose={() => setNotice("")} />}<Feedback error={query.error} retry={() => void query.refetch()} />
    <div className="business-table" role="region" aria-label="Danh sách tài khoản" tabIndex={0}><Table<Account> rowKey="id" dataSource={query.isError ? [] : query.data?.items} loading={query.isFetching} scroll={{ x: 650 }} pagination={{ current: page, total: query.data?.totalCount, pageSize: 20, showSizeChanger: false, onChange: setPage }} columns={[
      { title: "Tên đăng nhập", dataIndex: "username" }, { title: "Trạng thái", render: (_, a) => a.isActive ? "Hoạt động" : "Chưa hoạt động / đã khóa" },
      { title: "Vai trò", render: (_, a) => a.roles.map(role => <Tag key={role}>{role}</Tag>) },
      { title: "Thao tác", render: (_, a) => <Space wrap>{hasPermission("accounts.assign_roles") && <Button disabled={a.id === user?.id} onClick={() => open("roles", a)}>Đổi vai trò</Button>}
        {hasPermission("hr.scope_manage") && a.roles.includes("HR") && <Button disabled={a.id === user?.id} onClick={() => open("scopes", a)}>Cấp phạm vi</Button>}
        <Button danger disabled={!a.isActive || a.id === user?.id} onClick={() => open("disable", a)}>Khóa tài khoản</Button></Space> },
    ]} /></div>
    <Modal title={`${mode === "roles" ? "Đổi vai trò" : mode === "scopes" ? "Cấp phạm vi hồ sơ" : "Khóa tài khoản"} · ${selected?.username || ""}`} open={!!mode} onCancel={() => { if (!write.isPending) setMode(undefined); }} footer={null} closable={!write.isPending} maskClosable={!write.isPending}>
      {mode === "scopes" ? <><Alert type="info" showIcon message="Cấp quyền theo từng hồ sơ" description="Dùng mã hồ sơ từ kết quả tạo nhân viên. Chỉ cấp những hồ sơ HR được phép quản lý; phòng ban không tự cấp quyền." />
        <Feedback error={scopes.error} retry={() => void scopes.refetch()} /><p>Hồ sơ đã được cấp: {scopes.data?.map(s => s.employeeId).join(", ") || "Chưa có"}</p></>
        : <Alert type="warning" showIcon message="Các phiên đăng nhập cũ sẽ bị thu hồi" description="Người dùng cần đăng nhập lại với quyền mới. Không thể thay đổi vai trò hoặc khóa chính mình." />}
      <Feedback error={write.error} />
      <Form form={form} layout="vertical" onFinish={values => write.mutate(values)} disabled={write.isPending}>
        {mode === "roles" && <Form.Item name="roles" label="Vai trò"><Select mode="multiple" options={["Employee", "HR", "Payroll", "SystemAdmin"].map(value => ({ value, label: value }))} /></Form.Item>}
        {mode === "scopes" && <Form.Item name="employeeIds" label="Mã hồ sơ cần cấp" extra="Mỗi UUID một dòng, lấy từ kết quả tạo nhân viên." rules={[{ required: true, whitespace: true }, { validator: (_, value: string) => !value || value.split(/[\s,]+/).filter(Boolean).every(id => /^[\da-f]{8}-[\da-f]{4}-[\da-f]{4}-[\da-f]{4}-[\da-f]{12}$/i.test(id)) ? Promise.resolve() : Promise.reject(new Error("Mã hồ sơ phải có định dạng UUID.")) }]}><Input.TextArea rows={3} /></Form.Item>}
        <Form.Item name="reason" label="Lý do thay đổi" rules={[{ required: true, whitespace: true }]}><Input.TextArea maxLength={500} /></Form.Item>
        <Button danger={mode === "disable"} type="primary" htmlType="submit" loading={write.isPending}>Xác nhận thay đổi</Button>
      </Form>
    </Modal>
  </section>;
}
