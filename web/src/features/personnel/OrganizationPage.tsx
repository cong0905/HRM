import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Alert, Button, Form, Input, Modal, Space, Table, Tabs, Typography } from "antd";
import { organizationApi } from "../shared/api";
import { Feedback } from "../shared/Feedback";
import { useAuth } from "../../context/AuthContext";

type Entry = { id: string; name: string };
export function OrganizationPage() {
  const [kind, setKind] = useState<"departments" | "positions">("departments");
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState<Entry>();
  const [mode, setMode] = useState<"create" | "edit" | "delete">();
  const [notice, setNotice] = useState("");
  const [form] = Form.useForm<{ name: string; reason: string }>();
  const { hasPermission } = useAuth();
  const client = useQueryClient();
  const query = useQuery({ queryKey: ["organization", kind, page], queryFn: ({ signal }) => organizationApi.list(kind, page, signal), retry: false });
  const write = useMutation({ mutationFn: async (values: { name: string; reason: string }) => mode === "delete"
    ? organizationApi.remove(kind, selected!.id, { expectedName: selected!.name, reason: values.reason })
    : organizationApi.save(kind, selected?.id, { name: values.name, reason: values.reason, expectedName: selected?.name }),
    onSuccess: () => { setMode(undefined); setNotice("Đã lưu thay đổi danh mục."); void client.invalidateQueries({ queryKey: ["organization"] }); void client.invalidateQueries({ queryKey: ["organization-options"] }); } });
  const open = (next: typeof mode, item?: Entry) => { setSelected(item); form.resetFields(); form.setFieldsValue({ name: item?.name || "" }); write.reset(); setMode(next); };
  const title = kind === "departments" ? "Phòng ban" : "Chức vụ";
  return <section className="business-page"><div className="business-heading"><Typography.Title level={2}>Phòng ban và chức vụ</Typography.Title>
    {hasPermission("departments.manage") && <Button type="primary" onClick={() => open("create")}>Thêm {title.toLowerCase()}</Button>}</div>
    <Tabs activeKey={kind} onChange={key => { setKind(key as typeof kind); setPage(1); }} items={[{ key: "departments", label: "Phòng ban" }, { key: "positions", label: "Chức vụ" }]} />
    {notice && <Alert type="success" showIcon message={notice} closable onClose={() => setNotice("")} />}<Feedback error={query.error} retry={() => void query.refetch()} />
    <div className="business-table" role="region" aria-label={title} tabIndex={0}><Table rowKey="id" loading={query.isFetching} dataSource={query.isError ? [] : query.data?.items} pagination={{ current: page, pageSize: 20, total: query.data?.totalCount, onChange: setPage, showSizeChanger: false }} columns={[
      { title: "Tên", dataIndex: "name" }, ...(hasPermission("departments.manage") ? [{ title: "Thao tác", render: (_: unknown, row: Entry) => <Space wrap><Button onClick={() => open("edit", row)}>Sửa</Button><Button danger onClick={() => open("delete", row)}>Xóa</Button></Space> }] : []),
    ]} /></div>
    <Modal title={`${mode === "create" ? "Thêm" : mode === "edit" ? "Sửa" : "Xóa"} ${title.toLowerCase()}`} open={!!mode} footer={null} onCancel={() => { if (!write.isPending) setMode(undefined); }} maskClosable={!write.isPending} closable={!write.isPending}>
      {mode === "delete" && <Alert type="warning" showIcon message={`Xóa ${selected?.name}?`} description="Máy chủ sẽ từ chối xóa danh mục đang được hồ sơ sử dụng." />}
      <Feedback error={write.error} />
      <Form form={form} layout="vertical" onFinish={values => write.mutate(values)} disabled={write.isPending}>
        {mode !== "delete" && <Form.Item name="name" label="Tên" rules={[{ required: true, whitespace: true }]}><Input maxLength={100} /></Form.Item>}
        <Form.Item name="reason" label="Lý do thay đổi" rules={[{ required: true, whitespace: true }]}><Input.TextArea maxLength={500} /></Form.Item>
        <Button type="primary" danger={mode === "delete"} htmlType="submit" loading={write.isPending}>{mode === "delete" ? "Xác nhận xóa" : "Lưu thay đổi"}</Button>
      </Form>
    </Modal>
  </section>;
}
