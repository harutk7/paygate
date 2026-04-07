import { Row, Col, Card, Table, Button, Space } from 'antd';
import { KeyOutlined, BookOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import dayjs from 'dayjs';
import { StatCard, StatusBadge, PageHeader } from '@payment-gateway/ui';
import type { TransactionDto } from '@payment-gateway/types';
import { transactionsApi, apiKeysApi } from '../api';
import { useNavigate } from 'react-router';

export function Dashboard() {
  const navigate = useNavigate();

  const { data: stats, isLoading: statsLoading } = useQuery({
    queryKey: ['transactionStats'],
    queryFn: () => transactionsApi.getTransactionStats(),
  });

  const { data: recentTx, isLoading: txLoading } = useQuery({
    queryKey: ['recentTransactions'],
    queryFn: () => transactionsApi.getTransactions({ page: 1, pageSize: 10 }),
  });

  const { data: apiKeysData, isLoading: keysLoading } = useQuery({
    queryKey: ['apiKeysCount'],
    queryFn: () => apiKeysApi.getApiKeys({ page: 1, pageSize: 1 }),
  });

  const activeKeyCount = apiKeysData?.totalCount ?? 0;

  const columns = [
    {
      title: 'Transaction ID',
      dataIndex: 'externalId',
      key: 'externalId',
      render: (id: string) => (
        <span style={{ fontFamily: 'monospace' }}>{id.slice(0, 12)}...</span>
      ),
    },
    {
      title: 'Amount',
      dataIndex: 'amount',
      key: 'amount',
      render: (amount: number, record: TransactionDto) =>
        `$${amount.toFixed(2)} ${record.currency}`,
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => <StatusBadge status={status} />,
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
        title="Dashboard"
        actions={
          <Space>
            <Button
              icon={<KeyOutlined />}
              onClick={() => navigate('/api-keys')}
            >
              Create API Key
            </Button>
            <Button
              icon={<BookOutlined />}
              onClick={() => window.open('/docs', '_blank')}
            >
              View Documentation
            </Button>
          </Space>
        }
      />

      <Row gutter={[16, 16]}>
        <Col xs={24} sm={12} lg={6}>
          <StatCard
            title="Total Volume"
            value={stats?.totalVolume ?? 0}
            prefix="$"
            precision={2}
            loading={statsLoading}
          />
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <StatCard
            title="Success Rate"
            value={stats?.successRate ?? 0}
            suffix="%"
            precision={1}
            loading={statsLoading}
          />
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <StatCard
            title="Transaction Count"
            value={stats?.totalCount ?? 0}
            loading={statsLoading}
          />
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <StatCard
            title="Active API Keys"
            value={activeKeyCount}
            loading={keysLoading}
          />
        </Col>
      </Row>

      <Card title="Transaction Volume (Last 30 Days)" style={{ marginTop: 16 }}>
        <ResponsiveContainer width="100%" height={300}>
          <AreaChart data={stats?.volumeByDay ?? []}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis
              dataKey="date"
              tickFormatter={(d) => dayjs(d).format('MMM DD')}
            />
            <YAxis tickFormatter={(v) => `$${v}`} />
            <Tooltip
              formatter={(value) => [`$${Number(value).toFixed(2)}`, 'Volume']}
              labelFormatter={(label) => dayjs(label).format('MMM DD, YYYY')}
            />
            <Area
              type="monotone"
              dataKey="amount"
              stroke="#1677ff"
              fill="#1677ff"
              fillOpacity={0.15}
            />
          </AreaChart>
        </ResponsiveContainer>
      </Card>

      <Card title="Recent Transactions" style={{ marginTop: 16 }}>
        <Table
          dataSource={recentTx?.items ?? []}
          columns={columns}
          rowKey="id"
          pagination={false}
          loading={txLoading}
          size="small"
          onRow={(record) => ({
            style: { cursor: 'pointer' },
            onClick: () => navigate('/transactions'),
          })}
        />
      </Card>
    </>
  );
}
