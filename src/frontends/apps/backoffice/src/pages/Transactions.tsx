import { useState } from 'react';
import { Select, DatePicker, Space, Drawer, Timeline, Typography, Descriptions } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { PageHeader, StatusBadge, DataTable } from '@payment-gateway/ui';
import type { TransactionDto } from '@payment-gateway/types';
import { adminApi } from '../api';

const { RangePicker } = DatePicker;

export function Transactions() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [selectedTx, setSelectedTx] = useState<TransactionDto | null>(null);
  const [drawerOpen, setDrawerOpen] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ['admin', 'transactions', { page, pageSize, status: statusFilter }],
    queryFn: () =>
      adminApi.getTransactions({
        page,
        pageSize,
        sortBy: statusFilter !== 'all' ? statusFilter : undefined,
      }),
  });

  const openDetail = (tx: TransactionDto) => {
    setSelectedTx(tx);
    setDrawerOpen(true);
  };

  const columns = [
    {
      title: 'Transaction ID',
      dataIndex: 'externalId',
      key: 'externalId',
      ellipsis: true,
      width: 200,
    },
    {
      title: 'Organization',
      dataIndex: 'customerEmail',
      key: 'customerEmail',
      ellipsis: true,
    },
    {
      title: 'Amount',
      dataIndex: 'amount',
      key: 'amount',
      render: (v: number, r: TransactionDto) => (
        <span style={{ fontWeight: 500 }}>
          ${v?.toFixed(2)} <Typography.Text type="secondary">{r.currency}</Typography.Text>
        </span>
      ),
      align: 'right' as const,
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (s: string) => <StatusBadge status={s} />,
    },
    {
      title: 'Type',
      dataIndex: 'type',
      key: 'type',
      render: (t: string) => t?.toUpperCase(),
    },
    {
      title: 'Date',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (d: string) => new Date(d).toLocaleString(),
      width: 180,
    },
  ];

  const filteredItems = statusFilter !== 'all'
    ? (data?.items ?? []).filter((t) => t.status === statusFilter)
    : (data?.items ?? []);

  return (
    <>
      <PageHeader title="All Transactions" />

      <Space style={{ marginBottom: 16 }} wrap>
        <RangePicker style={{ width: 280 }} />
        <Select
          value={statusFilter}
          onChange={(v) => {
            setStatusFilter(v);
            setPage(1);
          }}
          style={{ width: 180 }}
          options={[
            { label: 'All Statuses', value: 'all' },
            { label: 'Pending', value: 'pending' },
            { label: 'Completed', value: 'completed' },
            { label: 'Failed', value: 'failed' },
            { label: 'Refunded', value: 'refunded' },
          ]}
        />
      </Space>

      <DataTable<TransactionDto>
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
          onClick: () => openDetail(record),
          style: { cursor: 'pointer' },
        })}
      />

      <Drawer
        title="Transaction Detail"
        placement="right"
        width={480}
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
      >
        {selectedTx && (
          <>
            <Descriptions column={1} size="small" style={{ marginBottom: 24 }}>
              <Descriptions.Item label="Transaction ID">{selectedTx.externalId}</Descriptions.Item>
              <Descriptions.Item label="Amount">
                ${selectedTx.amount?.toFixed(2)} {selectedTx.currency}
              </Descriptions.Item>
              <Descriptions.Item label="Status">
                <StatusBadge status={selectedTx.status} />
              </Descriptions.Item>
              <Descriptions.Item label="Type">{selectedTx.type?.toUpperCase()}</Descriptions.Item>
              <Descriptions.Item label="Customer">{selectedTx.customerEmail}</Descriptions.Item>
              <Descriptions.Item label="Description">{selectedTx.description || '-'}</Descriptions.Item>
              <Descriptions.Item label="Date">
                {new Date(selectedTx.createdAt).toLocaleString()}
              </Descriptions.Item>
            </Descriptions>

            <Typography.Title level={5}>Events Timeline</Typography.Title>
            <Timeline
              items={[
                {
                  color: 'blue',
                  children: (
                    <>
                      <Typography.Text strong>Created</Typography.Text>
                      <br />
                      <Typography.Text type="secondary">
                        {new Date(selectedTx.createdAt).toLocaleString()}
                      </Typography.Text>
                    </>
                  ),
                },
                {
                  color: selectedTx.status === 'completed' ? 'green' : selectedTx.status === 'failed' ? 'red' : 'gray',
                  children: (
                    <>
                      <Typography.Text strong>
                        {selectedTx.status?.replace(/_/g, ' ').toUpperCase()}
                      </Typography.Text>
                      <br />
                      <Typography.Text type="secondary">
                        {new Date(selectedTx.createdAt).toLocaleString()}
                      </Typography.Text>
                    </>
                  ),
                },
              ]}
            />
          </>
        )}
      </Drawer>
    </>
  );
}
