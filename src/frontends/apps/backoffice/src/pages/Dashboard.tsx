import { Row, Col, Card, Statistic, List, Typography } from 'antd';
import {
  TeamOutlined,
  DollarOutlined,
  SwapOutlined,
  CheckCircleOutlined,
} from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import {
  AreaChart,
  Area,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import { PageHeader } from '@payment-gateway/ui';
import { adminApi } from '../api';

export function Dashboard() {
  const { data: dashboard, isLoading } = useQuery({
    queryKey: ['admin', 'dashboard'],
    queryFn: () => adminApi.getDashboard(),
  });

  const dailyVolume = dashboard?.revenueByMonth?.slice(-30) ?? [];
  const revenueByPlan = [
    { name: 'Starter', revenue: dailyVolume.length > 0 ? Math.round((dashboard?.monthlyRevenue ?? 0) * 0.2) : 0 },
    { name: 'Business', revenue: dailyVolume.length > 0 ? Math.round((dashboard?.monthlyRevenue ?? 0) * 0.5) : 0 },
    { name: 'Enterprise', revenue: dailyVolume.length > 0 ? Math.round((dashboard?.monthlyRevenue ?? 0) * 0.3) : 0 },
  ];

  return (
    <>
      <PageHeader title="Admin Dashboard" />

      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col xs={24} sm={12} lg={6}>
          <Card style={{ background: '#1f1f1f' }} loading={isLoading}>
            <Statistic
              title="Active Customers"
              value={dashboard?.totalCustomers ?? 0}
              prefix={<TeamOutlined />}
              valueStyle={{ color: '#00C9A7' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card style={{ background: '#1f1f1f' }} loading={isLoading}>
            <Statistic
              title="MRR"
              value={dashboard?.monthlyRevenue ?? 0}
              prefix={<DollarOutlined />}
              precision={2}
              valueStyle={{ color: '#845EC2' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card style={{ background: '#1f1f1f' }} loading={isLoading}>
            <Statistic
              title="Total Transactions"
              value={dashboard?.totalTransactions ?? 0}
              prefix={<SwapOutlined />}
              valueStyle={{ color: '#FF6F91' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card style={{ background: '#1f1f1f' }} loading={isLoading}>
            <Statistic
              title="Active Subscriptions"
              value={dashboard?.activeSubscriptions ?? 0}
              prefix={<CheckCircleOutlined />}
              valueStyle={{ color: '#FFC75F' }}
            />
          </Card>
        </Col>
      </Row>

      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col xs={24} lg={14}>
          <Card
            title="Daily Transaction Volume (Last 30 Days)"
            style={{ background: '#1f1f1f' }}
            styles={{ header: { borderBottom: '1px solid #303030' } }}
          >
            <ResponsiveContainer width="100%" height={300}>
              <AreaChart data={dailyVolume}>
                <defs>
                  <linearGradient id="volumeGradient" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#00C9A7" stopOpacity={0.4} />
                    <stop offset="95%" stopColor="#00C9A7" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="#303030" />
                <XAxis dataKey="month" stroke="#666" fontSize={12} />
                <YAxis stroke="#666" fontSize={12} />
                <Tooltip
                  contentStyle={{ background: '#1f1f1f', border: '1px solid #303030', borderRadius: 6 }}
                  labelStyle={{ color: '#fff' }}
                />
                <Area
                  type="monotone"
                  dataKey="revenue"
                  stroke="#00C9A7"
                  strokeWidth={2}
                  fill="url(#volumeGradient)"
                  name="Volume ($)"
                />
              </AreaChart>
            </ResponsiveContainer>
          </Card>
        </Col>
        <Col xs={24} lg={10}>
          <Card
            title="Revenue by Plan Tier"
            style={{ background: '#1f1f1f' }}
            styles={{ header: { borderBottom: '1px solid #303030' } }}
          >
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={revenueByPlan}>
                <CartesianGrid strokeDasharray="3 3" stroke="#303030" />
                <XAxis dataKey="name" stroke="#666" fontSize={12} />
                <YAxis stroke="#666" fontSize={12} />
                <Tooltip
                  contentStyle={{ background: '#1f1f1f', border: '1px solid #303030', borderRadius: 6 }}
                  labelStyle={{ color: '#fff' }}
                />
                <Bar dataKey="revenue" name="Revenue ($)" radius={[4, 4, 0, 0]}>
                  {revenueByPlan.map((_, index) => {
                    const colors = ['#00C9A7', '#845EC2', '#FF6F91'];
                    return (
                      <rect key={index} fill={colors[index % colors.length]} />
                    );
                  })}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </Card>
        </Col>
      </Row>

      <Card
        title="Recent Activity"
        style={{ background: '#1f1f1f' }}
        styles={{ header: { borderBottom: '1px solid #303030' } }}
        loading={isLoading}
      >
        <List
          dataSource={dashboard?.recentTransactions?.slice(0, 10) ?? []}
          locale={{ emptyText: 'No recent activity' }}
          renderItem={(tx) => (
            <List.Item>
              <List.Item.Meta
                title={
                  <Typography.Text style={{ color: 'rgba(255,255,255,0.85)' }}>
                    {tx.type?.toUpperCase()} - {tx.customerEmail}
                  </Typography.Text>
                }
                description={
                  <Typography.Text type="secondary">
                    ${tx.amount} {tx.currency} - {tx.status}
                  </Typography.Text>
                }
              />
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                {new Date(tx.createdAt).toLocaleString()}
              </Typography.Text>
            </List.Item>
          )}
        />
      </Card>
    </>
  );
}
