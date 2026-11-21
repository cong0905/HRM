import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Button, Form, Input, Table, Typography } from "antd";
import { hrmClient } from "../../api/client";
import { unwrap, type Schema } from "../shared/api";
import { Feedback } from "../shared/Feedback";

export function AuditPage() {
  const [page, setPage] = useState(1);
  const [action, setAction] = useState("");
  const query = useQuery({ queryKey: ["audit", page, action], queryFn: async ({ signal }) => unwrap(await hrmClient.GET("/api/v1/audit-events", { params: { query: { page, pageSize: 20, action: action || undefined } }, signal })), retry: false });
  return <section className="business-page"><Typography.Title level={2}>Nhật ký hệ thống</Typography.Title>
    <Form layout="inline" className="business-filters" onFinish={v => { setAction(v.action || ""); setPage(1); }}><Form.Item name="action" label="Hành động"><Input allowClear maxLength={100} /></Form.Item><Button htmlType="submit">Lọc nhật ký</Button></Form>
    <Feedback error={query.error} retry={() => void query.refetch()} />
    <div className="business-table" role="region" aria-label="Nhật ký hệ thống" tabIndex={0}><Table<Schema<"AuditEventResponse">> rowKey="id" dataSource={query.isError ? [] : query.data?.items} loading={query.isFetching} scroll={{ x: 850 }} pagination={{ current: page, total: query.data?.totalCount, pageSize: 20, showSizeChanger: false, onChange: setPage }} columns={[
      { title: "Thời điểm", dataIndex: "occurredAtUtc", render: value => new Date(value as string).toLocaleString("vi-VN", { timeZone: "Asia/Ho_Chi_Minh" }) },
      { title: "Người thực hiện", dataIndex: "actorUsername" }, { title: "Hành động", dataIndex: "action" }, { title: "Đối tượng", dataIndex: "entityName" }, { title: "Lý do", dataIndex: "reason" },
    ]} /></div>
  </section>;
}
