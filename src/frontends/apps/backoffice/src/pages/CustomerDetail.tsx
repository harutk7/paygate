import { useParams, useNavigate } from 'react-router';
import { Card, Descriptions, Row, Col, Statistic, Button, Modal, Table, Space, message } from 'antd';
import { ArrowLeftOutlined, StopOutlined, CheckCircleOutlined } from '@ant-design/icons';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { PageHeader, StatusBadge } from '@payment-gateway/ui';
import { adminApi } from '../api';

export function CustomerDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data: customer, isLoading } = useQuery({
    queryKey: ['admin', 'customer', id],
    queryFn: () => adminApi.getCustomer(id!),
    enabled: !!id,
  });

  const statusMutation = useMutation({
    mutationFn: (status: 'active' | 'suspended') =>
      adminApi.updateCustomerStatus(id!, { status }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'customer', id] });
      queryClient.invalidateQueries({ queryKey: ['admin', 'customers'] });
      message.success('Customer status updated');
    },
    onError: () => {
      message.error('Failed to update customer status');
    },
  });

  const handleStatusChange = (newStatus: 'active' | 'suspended') => {
    Modal.confirm({
      title: `${newStatus === 'suspended' ? 'Suspend' : 'Activate'} Customer`,
      content: `Are you sure you want to ${newStatus === 'suspended' ? 'suspend' : 'activate'} ${customer?.organizationName}?`,
      okText: 'Confirm',
      okType: newStatus === 'suspended' ? 'danger' : 'primary',
      onOk: () => statusMutation.mutateAsync(newStatus),
    });
  };

  const transactionColumns = [
    { title: 'ID', dataIndex: 'externalId', key: 'externalId', ellipsis: true },
    {
      title: 'Amount',
      dataIndex: 'amount',
      key: 'amount',
      render: (v: number, r: any) => `$${v?.toFixed(2)} ${r.currency ?? ''}`,
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (s: string) => <StatusBadge status={s} />,
    },
    { title: 'Type', dataIndex: 'type', key: 'type' },
    {
      title: 'Date',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (d: string) => new Date(d).toLocaleString(),
    },
  ];

  return (
    <>
      <PageHeader
        title="Customer Detail"
        breadcrumbs={[
          { label: 'Customers', href: '/customers' },
          { label: customer?.organizationName || id || '' },
        ]}
        actions={
          <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/customers')}>
            Back to Customers
          </Button>
        }
      />

      <Row gutter={[16, 16]} style={{ marginBottom: 16 }}>
        <Col xs={24} lg={12}>
          <Card
            title="Organization Info"
            style={{ background: '#1f1f1f' }}
            loading={isLoading}
            extra={
              customer && (
                <Space>
                  {customer.status === 'active' ? (
                    <Button
                      danger
                      icon={<StopOutlined />}
                      onClick={() => handleStatusChange('suspended')}
                      loading={statusMutation.isPending}
                    >
                      Suspend
                    </Button>
                  ) : (
                    <Button
                      type="primary"
                      icon={<CheckCircleOutlined />}
                      onClick={() => handleStatusChange('active')}
                      loading={statusMutation.isPending}
                    >
                      Activate
                    </Button>
                  )}
                </Space>
              )
            }
          >
            <Descriptions column={1} size="small">
              <Descriptions.Item label="Organization">{customer?.organizationName}</Descriptions.Item>
              <Descriptions.Item label="Admin Email">{customer?.email}</Descriptions.Item>
              <Descriptions.Item label="Status">
                {customer?.status && <StatusBadge status={customer.status} />}
              </Descriptions.Item>
              <Descriptions.Item label="Created">
                {customer?.createdAt && new Date(customer.createdAt).toLocaleDateString()}
              </Descriptions.Item>
            </Descriptions>
          </Card>
        </Col>
        <Col xs={24} lg={12}>
          <Card title="Subscription" style={{ background: '#1f1f1f' }} loading={isLoading}>
            <Descriptions column={1} size="small">
              <Descriptions.Item label="Plan">{customer?.planName}</Descriptions.Item>
              <Descriptions.Item label="Status">
                {customer?.status && <StatusBadge status={customer.status} />}
              </Descriptions.Item>
            </Descriptions>
          </Card>
        </Col>
      </Row>

      <Row gutter={[16, 16]} style={{ marginBottom: 16 }}>
        <Col xs={24} sm={8}>
          <Card style={{ background: '#1f1f1f' }} loading={isLoading}>
            <Statistic
              title="Transaction Count"
              value={customer?.totalTransactions ?? 0}
              valueStyle={{ color: '#00C9A7' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={8}>
          <Card style={{ background: '#1f1f1f' }} loading={isLoading}>
            <Statistic
              title="Total Volume"
              value={customer?.totalVolume ?? 0}
              prefix="$"
              precision={2}
              valueStyle={{ color: '#845EC2' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={8}>
          <Card style={{ background: '#1f1f1f' }} loading={isLoading}>
            <Statistic
              title="Plan"
              value={customer?.planName ?? '-'}
              valueStyle={{ color: '#FFC75F' }}
            />
          </Card>
        </Col>
      </Row>

      <Card
        title="Recent Transactions"
        style={{ background: '#1f1f1f' }}
        styles={{ header: { borderBottom: '1px solid #303030' } }}
      >
        <Table
          dataSource={[]}
          columns={transactionColumns}
          rowKey="id"
          pagination={{ pageSize: 20 }}
          locale={{ emptyText: 'No transactions found' }}
        />
      </Card>
    </>
  );
}
