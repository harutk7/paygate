import { useState } from 'react';
import { useNavigate } from 'react-router';
import { Input, Select, Button, Space, Modal, message } from 'antd';
import { SearchOutlined, StopOutlined, CheckCircleOutlined } from '@ant-design/icons';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { PageHeader, StatusBadge, DataTable } from '@payment-gateway/ui';
import type { CustomerDto } from '@payment-gateway/types';
import { adminApi } from '../api';

export function Customers() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');

  const { data, isLoading } = useQuery({
    queryKey: ['admin', 'customers', { page, pageSize, search, status: statusFilter }],
    queryFn: () =>
      adminApi.getCustomers({
        page,
        pageSize,
        search: search || undefined,
        sortBy: statusFilter !== 'all' ? statusFilter : undefined,
      }),
  });

  const statusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: 'active' | 'suspended' }) =>
      adminApi.updateCustomerStatus(id, { status }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'customers'] });
      message.success('Customer status updated');
    },
    onError: () => {
      message.error('Failed to update customer status');
    },
  });

  const handleStatusChange = (customer: CustomerDto, newStatus: 'active' | 'suspended') => {
    Modal.confirm({
      title: `${newStatus === 'suspended' ? 'Suspend' : 'Activate'} Customer`,
      content: `Are you sure you want to ${newStatus === 'suspended' ? 'suspend' : 'activate'} ${customer.organizationName}?`,
      okText: 'Confirm',
      okType: newStatus === 'suspended' ? 'danger' : 'primary',
      onOk: () => statusMutation.mutateAsync({ id: customer.id, status: newStatus }),
    });
  };

  const columns = [
    {
      title: 'Organization Name',
      dataIndex: 'organizationName',
      key: 'organizationName',
      ellipsis: true,
    },
    {
      title: 'Admin Email',
      dataIndex: 'email',
      key: 'email',
      ellipsis: true,
    },
    {
      title: 'Plan',
      dataIndex: 'planName',
      key: 'planName',
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => <StatusBadge status={status} />,
    },
    {
      title: 'Transactions',
      dataIndex: 'totalTransactions',
      key: 'totalTransactions',
      align: 'right' as const,
    },
    {
      title: 'Created',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (date: string) => new Date(date).toLocaleDateString(),
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_: unknown, record: CustomerDto) => (
        <Space>
          {record.status === 'active' ? (
            <Button
              size="small"
              danger
              icon={<StopOutlined />}
              onClick={(e) => {
                e.stopPropagation();
                handleStatusChange(record, 'suspended');
              }}
            >
              Suspend
            </Button>
          ) : (
            <Button
              size="small"
              type="primary"
              icon={<CheckCircleOutlined />}
              onClick={(e) => {
                e.stopPropagation();
                handleStatusChange(record, 'active');
              }}
            >
              Activate
            </Button>
          )}
        </Space>
      ),
    },
  ];

  const filteredItems = statusFilter !== 'all'
    ? (data?.items ?? []).filter((c) => c.status === statusFilter)
    : (data?.items ?? []);

  return (
    <>
      <PageHeader title="Customers" />

      <Space style={{ marginBottom: 16 }} wrap>
        <Input
          placeholder="Search by name or email..."
          prefix={<SearchOutlined />}
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
          style={{ width: 300 }}
          allowClear
        />
        <Select
          value={statusFilter}
          onChange={(value) => {
            setStatusFilter(value);
            setPage(1);
          }}
          style={{ width: 150 }}
          options={[
            { label: 'All Statuses', value: 'all' },
            { label: 'Active', value: 'active' },
            { label: 'Suspended', value: 'suspended' },
          ]}
        />
      </Space>

      <DataTable<CustomerDto>
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
        onRow={(record) => ({
          onClick: () => navigate(`/customers/${record.id}`),
          style: { cursor: 'pointer' },
        })}
      />
    </>
  );
}
