import { useState, useCallback } from 'react';
import {
  Table,
  Card,
  Input,
  Select,
  DatePicker,
  Row,
  Col,
  Button,
  Drawer,
  Descriptions,
  Timeline,
  Popconfirm,
  InputNumber,
  Space,
  message,
} from 'antd';
import {
  SearchOutlined,
  DownloadOutlined,
  ReloadOutlined,
} from '@ant-design/icons';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { StatusBadge, PageHeader } from '@payment-gateway/ui';
import type {
  TransactionDto,
  TransactionDetailDto,
} from '@payment-gateway/types';
import { transactionsApi } from '../api';

const { RangePicker } = DatePicker;

const statusOptions = [
  { value: '', label: 'All Statuses' },
  { value: 'pending', label: 'Pending' },
  { value: 'completed', label: 'Succeeded' },
  { value: 'failed', label: 'Failed' },
  { value: 'refunded', label: 'Refunded' },
];

export function Transactions() {
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [dateRange, setDateRange] = useState<[dayjs.Dayjs | null, dayjs.Dayjs | null] | null>(null);
  const [amountMin, setAmountMin] = useState<number | null>(null);
  const [amountMax, setAmountMax] = useState<number | null>(null);
  const [selectedTx, setSelectedTx] = useState<string | null>(null);

  const { data, isLoading, isPlaceholderData } = useQuery({
    queryKey: ['transactions', page, pageSize, search, statusFilter, dateRange, amountMin, amountMax],
    queryFn: () =>
      transactionsApi.getTransactions({
        page,
        pageSize,
        search: search || undefined,
        sortBy: 'createdAt',
        sortDirection: 'desc',
      }),
    placeholderData: (prev) => prev,
  });

  const { data: txDetail, isLoading: detailLoading } = useQuery({
    queryKey: ['transaction', selectedTx],
    queryFn: () => transactionsApi.getTransaction(selectedTx!),
    enabled: !!selectedTx,
  });

  const refundMutation = useMutation({
    mutationFn: (id: string) => transactionsApi.refundCharge(id),
    onSuccess: () => {
      message.success('Refund initiated successfully');
      queryClient.invalidateQueries({ queryKey: ['transactions'] });
      queryClient.invalidateQueries({ queryKey: ['transaction', selectedTx] });
    },
    onError: () => {
      message.error('Failed to initiate refund');
    },
  });

  const handleExportCSV = useCallback(() => {
    if (!data?.items?.length) return;
    const headers = ['Transaction ID', 'Amount', 'Currency', 'Status', 'Customer', 'Date'];
    const rows = data.items.map((tx) => [
      tx.externalId,
      tx.amount.toFixed(2),
      tx.currency,
      tx.status,
      tx.customerEmail,
      dayjs(tx.createdAt).format('YYYY-MM-DD HH:mm:ss'),
    ]);
    const csv = [headers, ...rows].map((r) => r.join(',')).join('\n');
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `transactions-${dayjs().format('YYYY-MM-DD')}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }, [data]);

  const columns = [
    {
      title: 'Transaction ID',
      dataIndex: 'externalId',
      key: 'externalId',
      render: (id: string) => (
        <span style={{ fontFamily: 'monospace' }}>{id.slice(0, 16)}...</span>
      ),
    },
    {
      title: 'Amount',
      dataIndex: 'amount',
      key: 'amount',
      render: (amount: number, record: TransactionDto) =>
        `$${amount.toFixed(2)}`,
    },
    {
      title: 'Currency',
      dataIndex: 'currency',
      key: 'currency',
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => <StatusBadge status={status} />,
    },
    {
      title: 'Customer',
      dataIndex: 'customerEmail',
      key: 'customerEmail',
    },
    {
      title: 'Date',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (date: string) => dayjs(date).format('MMM DD, YYYY HH:mm'),
    },
  ];

  return (
    <>
      <PageHeader
        title="Transactions"
        actions={
          <Space>
            <Button icon={<DownloadOutlined />} onClick={handleExportCSV}>
              Export CSV
            </Button>
          </Space>
        }
      />

      <Card style={{ marginBottom: 16 }}>
        <Row gutter={[16, 16]} align="middle">
          <Col xs={24} sm={8}>
            <Input
              placeholder="Search by transaction ID"
              prefix={<SearchOutlined />}
              value={search}
              onChange={(e) => {
                setSearch(e.target.value);
                setPage(1);
              }}
              allowClear
            />
          </Col>
          <Col xs={24} sm={4}>
            <Select
              style={{ width: '100%' }}
              value={statusFilter}
              onChange={(v) => {
                setStatusFilter(v);
                setPage(1);
              }}
              options={statusOptions}
            />
          </Col>
          <Col xs={24} sm={6}>
            <RangePicker
              style={{ width: '100%' }}
              value={dateRange}
              onChange={(dates) => {
                setDateRange(dates as [dayjs.Dayjs | null, dayjs.Dayjs | null] | null);
                setPage(1);
              }}
            />
          </Col>
          <Col xs={12} sm={3}>
            <InputNumber
              style={{ width: '100%' }}
              placeholder="Min $"
              value={amountMin}
              onChange={(v) => {
                setAmountMin(v);
                setPage(1);
              }}
              min={0}
            />
          </Col>
          <Col xs={12} sm={3}>
            <InputNumber
              style={{ width: '100%' }}
              placeholder="Max $"
              value={amountMax}
              onChange={(v) => {
                setAmountMax(v);
                setPage(1);
              }}
              min={0}
            />
          </Col>
        </Row>
      </Card>

      <Table
        dataSource={data?.items ?? []}
        columns={columns}
        rowKey="id"
        loading={isLoading}
        style={{ opacity: isPlaceholderData ? 0.6 : 1 }}
        pagination={{
          current: page,
          pageSize,
          total: data?.totalCount ?? 0,
          showSizeChanger: true,
          showTotal: (t) => `Total ${t} transactions`,
          onChange: (p, ps) => {
            setPage(p);
            setPageSize(ps);
          },
        }}
        onRow={(record) => ({
          style: { cursor: 'pointer' },
          onClick: () => setSelectedTx(record.id),
        })}
      />

      <Drawer
        title="Transaction Details"
        open={!!selectedTx}
        onClose={() => setSelectedTx(null)}
        width={520}
      >
        {txDetail && (
          <>
            <Descriptions column={1} bordered size="small">
              <Descriptions.Item label="Transaction ID">
                <span style={{ fontFamily: 'monospace' }}>{txDetail.externalId}</span>
              </Descriptions.Item>
              <Descriptions.Item label="Amount">
                ${txDetail.amount.toFixed(2)} {txDetail.currency}
              </Descriptions.Item>
              <Descriptions.Item label="Status">
                <StatusBadge status={txDetail.status} />
              </Descriptions.Item>
              <Descriptions.Item label="Type">{txDetail.type}</Descriptions.Item>
              <Descriptions.Item label="Customer">{txDetail.customerEmail}</Descriptions.Item>
              <Descriptions.Item label="Payment Method">{txDetail.paymentMethod}</Descriptions.Item>
              <Descriptions.Item label="Description">{txDetail.description}</Descriptions.Item>
              {txDetail.failureReason && (
                <Descriptions.Item label="Failure Reason">
                  {txDetail.failureReason}
                </Descriptions.Item>
              )}
              <Descriptions.Item label="Date">
                {dayjs(txDetail.createdAt).format('MMM DD, YYYY HH:mm:ss')}
              </Descriptions.Item>
            </Descriptions>

            {txDetail.metadata && Object.keys(txDetail.metadata).length > 0 && (
              <Card title="Metadata" size="small" style={{ marginTop: 16 }}>
                <Descriptions column={1} size="small">
                  {Object.entries(txDetail.metadata).map(([key, value]) => (
                    <Descriptions.Item key={key} label={key}>
                      {value}
                    </Descriptions.Item>
                  ))}
                </Descriptions>
              </Card>
            )}

            {txDetail.timeline && txDetail.timeline.length > 0 && (
              <Card title="Events Timeline" size="small" style={{ marginTop: 16 }}>
                <Timeline
                  items={txDetail.timeline.map((event) => ({
                    color:
                      event.status === 'completed' || event.status === 'succeeded'
                        ? 'green'
                        : event.status === 'failed'
                          ? 'red'
                          : 'blue',
                    children: (
                      <div>
                        <strong>{event.type}</strong>
                        <br />
                        <span>{event.details}</span>
                        <br />
                        <span style={{ color: '#999', fontSize: 12 }}>
                          {dayjs(event.timestamp).format('MMM DD, YYYY HH:mm:ss')}
                        </span>
                      </div>
                    ),
                  }))}
                />
              </Card>
            )}

            {txDetail.status === 'completed' && (
              <div style={{ marginTop: 16 }}>
                <Popconfirm
                  title="Refund Transaction"
                  description="Are you sure you want to refund this transaction?"
                  onConfirm={() => refundMutation.mutate(txDetail.id)}
                  okText="Yes, Refund"
                  cancelText="Cancel"
                >
                  <Button danger loading={refundMutation.isPending}>
                    Refund Transaction
                  </Button>
                </Popconfirm>
              </div>
            )}
          </>
        )}
        {detailLoading && <div style={{ textAlign: 'center', padding: 40 }}>Loading...</div>}
      </Drawer>
    </>
  );
}
