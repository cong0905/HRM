import { useState } from "react";
import { Button, Input, Select, Space } from "antd";
import { useQuery } from "@tanstack/react-query";
import { personnelApi } from "./api";
import { Feedback } from "./Feedback";

export function EmployeePicker({ id, value, onChange }: { id?: string; value?: string; onChange?: (id?: string) => void }) {
  const [page, setPage] = useState(1);
  const [name, setName] = useState("");
  const query = useQuery({ queryKey: ["employee-picker", page, name], queryFn: ({ signal }) => personnelApi.list({ page, pageSize: 20, name: name || undefined }, signal), retry: false });
  return <Space direction="vertical" className="full-width"><Select id={id} allowClear className="full-width" placeholder="Chọn nhân viên trong phạm vi được cấp" value={value} onChange={onChange} loading={query.isFetching}
    options={query.data?.items.map(p => ({ value: p.id, label: `${p.fullName} · ${p.employeeCode}` }))}
    dropdownRender={menu => <><Input.Search aria-label="Tìm nhân viên trong danh mục" placeholder="Tìm họ tên" onSearch={value => { setPage(1); setName(value); }} />{menu}<Space>
      <Button size="small" disabled={page === 1} onClick={() => setPage(p => p - 1)}>Trang trước</Button><span>{page}</span>
      <Button size="small" disabled={!query.data || page * 20 >= query.data.totalCount} onClick={() => setPage(p => p + 1)}>Trang sau</Button>
    </Space></>} /><Feedback error={query.error} retry={() => void query.refetch()} /></Space>;
}
