import { useState } from 'react';
import { Input, Select, DatePicker, Space } from 'antd';
import { SearchOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import { PageHeader, DataTable } from '@payment-gateway/ui';
import type { AuditLogEntryDto } from '@payment-gateway/types';
import { adminApi } from '../api';

const { RangePicker } = DatePicker;

export function AuditLog() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [actionFilter, setActionFilter] = useState<string>('all');

  const { data, isLoading } = useQuery({
    queryKey: ['admin', 'audit-log', { page, pageSize, search, action: actionFilter }],
    queryFn: () =>
      adminApi.getAuditLog({
        page,
        pageSize,
        search: search || undefined,
      }),
  });

  const filteredItems = actionFilter !== 'all'
    ? (data?.items ?? []).filter((entry) => entry.action === actionFilter)
    : (data?.items ?? []);

  const columns = [
    {
      title: 'Timestamp',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 180,
      render: (d: string) => new Date(d).toLocaleString(),
    },
    {
      title: 'User Email',
      dataIndex: 'userEmail',
      key: 'userEmail',
      ellipsis: true,
    },
    {
      title: 'Action',
      dataIndex: 'action',
      key: 'action',
      width: 150,
    },
    {
      title: 'Entity Type',
      dataIndex: 'resource',
      key: 'resource',
      width: 140,
    },
    {
      title: 'Entity ID',
      dataIndex: 'resourceId',
      key: 'resourceId',
      ellipsis: true,
      width: 200,
    },
    {
      title: 'Details',
      dataIndex: 'details',
      key: 'details',
      ellipsis: true,
    },
  ];

  return (
    <>
      <PageHeader title="Audit Log" />

      <Space style={{ marginBottom: 16 }} wrap>
        <Input
          placeholder="Search by user email..."
          prefix={<SearchOutlined />}
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
          style={{ width: 280 }}
          allowClear
        />
        <Select
          value={actionFilter}
          onChange={(v) => {
            setActionFilter(v);
            setPage(1);
          }}
          style={{ width: 200 }}
          options={[
            { label: 'All Actions', value: 'all' },
            { label: 'Login', value: 'login' },
            { label: 'Create', value: 'create' },
            { label: 'Update', value: 'update' },
            { label: 'Delete', value: 'delete' },
            { label: 'Suspend', value: 'suspend' },
            { label: 'Activate', value: 'activate' },
          ]}
        />
        <RangePicker style={{ width: 280 }} />
      </Space>

      <DataTable<AuditLogEntryDto>
        dataSource={filteredItems}
        columns={columns}
        rowKey="id"
        loading={isLoading}
        total={data?.totalCount}
        currentPage={page}
        pageSize={pageSize}
        onPageChange={(p, ps) => {
          setPage(p);
          setPageSize(ps);
        }}
      />
    </>
  );
}
