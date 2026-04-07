import { Row, Col, Card, Statistic } from 'antd';
import { DollarOutlined, RiseOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import {
  PieChart,
  Pie,
  Cell,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Area,
  AreaChart,
} from 'recharts';
import { PageHeader } from '@payment-gateway/ui';
import { adminApi } from '../api';

const CHART_COLORS = ['#00C9A7', '#845EC2', '#FF6F91', '#FFC75F', '#4B8BBE'];

export function Revenue() {
  const { data: report, isLoading } = useQuery({
    queryKey: ['admin', 'revenue'],
    queryFn: () => adminApi.getRevenueReport(),
  });

  const revenueByMonth = report?.revenueByMonth ?? [];
  const revenueByPlan = report?.revenueByPlan ?? [];

  return (
    <>
      <PageHeader title="Revenue Report" />

      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col xs={24} sm={12} lg={6}>
          <Card style={{ background: '#1f1f1f' }} loading={isLoading}>
            <Statistic
              title="Current MRR"
              value={report?.mrr ?? 0}
              prefix={<DollarOutlined />}
              precision={2}
              valueStyle={{ color: '#00C9A7' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card style={{ background: '#1f1f1f' }} loading={isLoading}>
            <Statistic
              title="Total Revenue"
              value={report?.totalRevenue ?? 0}
              prefix={<DollarOutlined />}
              precision={2}
              valueStyle={{ color: '#845EC2' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card style={{ background: '#1f1f1f' }} loading={isLoading}>
            <Statistic
              title="ARR"
              value={report?.arr ?? 0}
              prefix={<RiseOutlined />}
              precision={2}
              valueStyle={{ color: '#FFC75F' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card style={{ background: '#1f1f1f' }} loading={isLoading}>
            <Statistic
              title="Churn Rate"
              value={report?.churnRate ?? 0}
              suffix="%"
              precision={1}
              valueStyle={{ color: '#FF6F91' }}
            />
          </Card>
        </Col>
      </Row>

      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col xs={24} lg={16}>
          <Card
            title="MRR Trend (Last 12 Months)"
            style={{ background: '#1f1f1f' }}
            styles={{ header: { borderBottom: '1px solid #303030' } }}
          >
            <ResponsiveContainer width="100%" height={350}>
              <AreaChart data={revenueByMonth}>
                <defs>
                  <linearGradient id="mrrGradient" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#00C9A7" stopOpacity={0.4} />
                    <stop offset="95%" stopColor="#00C9A7" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="#303030" />
                <XAxis dataKey="month" stroke="#666" fontSize={12} />
                <YAxis stroke="#666" fontSize={12} tickFormatter={(v) => `$${v}`} />
                <Tooltip
                  contentStyle={{ background: '#1f1f1f', border: '1px solid #303030', borderRadius: 6 }}
                  labelStyle={{ color: '#fff' }}
                  formatter={(value) => [`$${Number(value).toFixed(2)}`, 'MRR']}
                />
                <Area
                  type="monotone"
                  dataKey="revenue"
                  stroke="#00C9A7"
                  strokeWidth={2}
                  fill="url(#mrrGradient)"
                  name="MRR"
                />
              </AreaChart>
            </ResponsiveContainer>
          </Card>
        </Col>
        <Col xs={24} lg={8}>
          <Card
            title="Revenue by Plan"
            style={{ background: '#1f1f1f' }}
            styles={{ header: { borderBottom: '1px solid #303030' } }}
          >
            <ResponsiveContainer width="100%" height={350}>
              <PieChart>
                <Pie
                  data={revenueByPlan}
                  cx="50%"
                  cy="50%"
                  innerRadius={70}
                  outerRadius={110}
                  paddingAngle={3}
                  dataKey="revenue"
                  nameKey="planName"
                  label={({ name, percent }: any) =>
                    `${name} ${((percent ?? 0) * 100).toFixed(0)}%`
                  }
                >
                  {revenueByPlan.map((_, index) => (
                    <Cell
                      key={`cell-${index}`}
                      fill={CHART_COLORS[index % CHART_COLORS.length]}
                    />
                  ))}
                </Pie>
                <Tooltip
                  contentStyle={{ background: '#1f1f1f', border: '1px solid #303030', borderRadius: 6 }}
                  formatter={(value) => [`$${Number(value).toFixed(2)}`, 'Revenue']}
                />
              </PieChart>
            </ResponsiveContainer>
          </Card>
        </Col>
      </Row>

      <Card
        title="Monthly Revenue"
        style={{ background: '#1f1f1f' }}
        styles={{ header: { borderBottom: '1px solid #303030' } }}
      >
        <ResponsiveContainer width="100%" height={300}>
          <BarChart data={revenueByMonth}>
            <CartesianGrid strokeDasharray="3 3" stroke="#303030" />
            <XAxis dataKey="month" stroke="#666" fontSize={12} />
            <YAxis stroke="#666" fontSize={12} tickFormatter={(v) => `$${v}`} />
            <Tooltip
              contentStyle={{ background: '#1f1f1f', border: '1px solid #303030', borderRadius: 6 }}
              labelStyle={{ color: '#fff' }}
              formatter={(value) => [`$${Number(value).toFixed(2)}`, 'Revenue']}
            />
            <Bar dataKey="revenue" fill="#845EC2" radius={[4, 4, 0, 0]} name="Revenue" />
          </BarChart>
        </ResponsiveContainer>
      </Card>
    </>
  );
}
