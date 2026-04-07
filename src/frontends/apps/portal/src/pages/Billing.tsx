import { useState } from 'react';
import {
  Card,
  Row,
  Col,
  Button,
  Table,
  Progress,
  Typography,
  Tag,
  Modal,
  Form,
  Input,
  Space,
  Descriptions,
  Empty,
  Popconfirm,
  message,
} from 'antd';
import {
  CreditCardOutlined,
  DownloadOutlined,
  CheckOutlined,
} from '@ant-design/icons';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { StatusBadge, PageHeader } from '@payment-gateway/ui';
import type { PlanDto, InvoiceDto } from '@payment-gateway/types';
import { subscriptionsApi, plansApi, billingApi } from '../api';

export function Billing() {
  const queryClient = useQueryClient();
  const [paymentModalOpen, setPaymentModalOpen] = useState(false);
  const [upgradeModalOpen, setUpgradeModalOpen] = useState(false);
  const [selectedPlanId, setSelectedPlanId] = useState<string | null>(null);
  const [paymentForm] = Form.useForm();

  const { data: subscription, isLoading: subLoading } = useQuery({
    queryKey: ['subscription'],
    queryFn: () => subscriptionsApi.getCurrentSubscription(),
  });

  const { data: plans, isLoading: plansLoading } = useQuery({
    queryKey: ['plans'],
    queryFn: () => plansApi.getPlans(),
  });

  const { data: paymentMethods } = useQuery({
    queryKey: ['paymentMethods'],
    queryFn: () => billingApi.getPaymentMethods(),
  });

  const { data: invoices, isLoading: invoicesLoading } = useQuery({
    queryKey: ['invoices'],
    queryFn: () => billingApi.getInvoices({ page: 1, pageSize: 20 }),
  });

  const subscribeMutation = useMutation({
    mutationFn: (planId: string) =>
      subscriptionsApi.createSubscription({ planId, billingCycle: 'monthly' }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['subscription'] });
      setUpgradeModalOpen(false);
      setSelectedPlanId(null);
      message.success('Subscription updated successfully');
    },
    onError: () => message.error('Failed to update subscription'),
  });

  const cancelMutation = useMutation({
    mutationFn: () =>
      subscriptionsApi.cancelSubscription({ cancelAtPeriodEnd: true }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['subscription'] });
      message.success('Subscription will be canceled at end of billing period');
    },
    onError: () => message.error('Failed to cancel subscription'),
  });

  const addPaymentMutation = useMutation({
    mutationFn: (values: { cardNumber: string; expiry: string; cvv: string }) =>
      billingApi.addPaymentMethod({
        token: `tok_${values.cardNumber.slice(-4)}`,
        setDefault: true,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['paymentMethods'] });
      setPaymentModalOpen(false);
      paymentForm.resetFields();
      message.success('Payment method added');
    },
    onError: () => message.error('Failed to add payment method'),
  });

  const handleUpgrade = (planId: string) => {
    setSelectedPlanId(planId);
    setUpgradeModalOpen(true);
  };

  const defaultPayment = paymentMethods?.find((pm) => pm.isDefault) ?? paymentMethods?.[0];

  const invoiceColumns = [
    {
      title: 'Invoice',
      dataIndex: 'number',
      key: 'number',
    },
    {
      title: 'Amount',
      dataIndex: 'amount',
      key: 'amount',
      render: (amount: number, record: InvoiceDto) =>
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
      render: (date: string) => dayjs(date).format('MMM DD, YYYY'),
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_: unknown, record: InvoiceDto) => (
        <Button
          size="small"
          icon={<DownloadOutlined />}
          href={record.pdfUrl}
          target="_blank"
        >
          Download
        </Button>
      ),
    },
  ];

  return (
    <>
      <PageHeader title="Billing" />

      <Row gutter={[16, 16]}>
        <Col xs={24} lg={16}>
          <Card
            title="Current Plan"
            loading={subLoading}
            extra={
              subscription && !subscription.cancelAtPeriodEnd && (
                <Popconfirm
                  title="Cancel Subscription"
                  description="Your subscription will remain active until the end of the current billing period."
                  onConfirm={() => cancelMutation.mutate()}
                  okText="Cancel Subscription"
                  okButtonProps={{ danger: true }}
                >
                  <Button danger size="small">
                    Cancel Subscription
                  </Button>
                </Popconfirm>
              )
            }
          >
            {subscription ? (
              <Descriptions column={2}>
                <Descriptions.Item label="Plan">
                  {subscription.planName}
                </Descriptions.Item>
                <Descriptions.Item label="Status">
                  <StatusBadge status={subscription.status} />
                  {subscription.cancelAtPeriodEnd && (
                    <Tag color="orange" style={{ marginLeft: 8 }}>
                      Cancels at period end
                    </Tag>
                  )}
                </Descriptions.Item>
                <Descriptions.Item label="Current Period">
                  {dayjs(subscription.currentPeriodStart).format('MMM DD, YYYY')} -{' '}
                  {dayjs(subscription.currentPeriodEnd).format('MMM DD, YYYY')}
                </Descriptions.Item>
              </Descriptions>
            ) : (
              <Empty description="No active subscription" />
            )}
          </Card>
        </Col>

        <Col xs={24} lg={8}>
          <Card title="Usage" loading={subLoading}>
            <div style={{ marginBottom: 16 }}>
              <Typography.Text>Transactions</Typography.Text>
              <Progress percent={0} format={() => '0 / --'} />
            </div>
            <div>
              <Typography.Text>API Keys</Typography.Text>
              <Progress percent={0} format={() => '0 / --'} />
            </div>
          </Card>
        </Col>
      </Row>

      <Card title="Available Plans" style={{ marginTop: 16 }} loading={plansLoading}>
        <Row gutter={[16, 16]}>
          {(plans ?? []).map((plan: PlanDto) => {
            const isCurrent = subscription?.planId === plan.id;
            return (
              <Col xs={24} sm={8} key={plan.id}>
                <Card
                  hoverable={!isCurrent}
                  style={{
                    borderColor: isCurrent ? '#1677ff' : undefined,
                    borderWidth: isCurrent ? 2 : 1,
                  }}
                >
                  <Typography.Title level={4}>{plan.name}</Typography.Title>
                  <Typography.Title level={3} style={{ margin: 0 }}>
                    ${plan.priceMonthly ?? plan.monthlyPrice}
                    <Typography.Text
                      style={{ fontSize: 14, fontWeight: 'normal' }}
                    >
                      /month
                    </Typography.Text>
                  </Typography.Title>
                  {plan.description && (
                    <Typography.Paragraph
                      type="secondary"
                      style={{ marginTop: 8 }}
                    >
                      {plan.description}
                    </Typography.Paragraph>
                  )}
                  <ul style={{ paddingLeft: 20, marginBottom: 16 }}>
                    {plan.features.map((f, i) => (
                      <li key={i}>
                        <CheckOutlined style={{ color: '#52c41a', marginRight: 8 }} />
                        {f}
                      </li>
                    ))}
                    <li>
                      <CheckOutlined style={{ color: '#52c41a', marginRight: 8 }} />
                      {plan.transactionLimit.toLocaleString()} transactions/mo
                    </li>
                  </ul>
                  {isCurrent ? (
                    <Button block disabled>
                      Current Plan
                    </Button>
                  ) : (
                    <Button
                      block
                      type="primary"
                      onClick={() => handleUpgrade(plan.id)}
                    >
                      {subscription ? 'Switch Plan' : 'Subscribe'}
                    </Button>
                  )}
                </Card>
              </Col>
            );
          })}
        </Row>
      </Card>

      <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
        <Col xs={24} lg={12}>
          <Card
            title="Payment Method"
            extra={
              <Button
                icon={<CreditCardOutlined />}
                onClick={() => setPaymentModalOpen(true)}
              >
                Add Payment Method
              </Button>
            }
          >
            {defaultPayment ? (
              <Space direction="vertical">
                <Typography.Text>
                  <CreditCardOutlined style={{ marginRight: 8 }} />
                  {defaultPayment.brand.toUpperCase()} **** **** ****{' '}
                  {defaultPayment.last4}
                </Typography.Text>
                <Typography.Text type="secondary">
                  Expires {String(defaultPayment.expiryMonth).padStart(2, '0')}/
                  {defaultPayment.expiryYear}
                </Typography.Text>
              </Space>
            ) : (
              <Empty description="No payment method on file" />
            )}
          </Card>
        </Col>
        <Col xs={24} lg={12}>
          <Card title="Invoice History">
            <Table
              dataSource={invoices?.items ?? []}
              columns={invoiceColumns}
              rowKey="id"
              loading={invoicesLoading}
              pagination={false}
              size="small"
            />
          </Card>
        </Col>
      </Row>

      <Modal
        title="Confirm Plan Change"
        open={upgradeModalOpen}
        onCancel={() => {
          setUpgradeModalOpen(false);
          setSelectedPlanId(null);
        }}
        onOk={() => selectedPlanId && subscribeMutation.mutate(selectedPlanId)}
        confirmLoading={subscribeMutation.isPending}
        okText="Confirm"
      >
        <Typography.Paragraph>
          Are you sure you want to switch to the{' '}
          <strong>
            {plans?.find((p) => p.id === selectedPlanId)?.name}
          </strong>{' '}
          plan? Changes will take effect immediately.
        </Typography.Paragraph>
      </Modal>

      <Modal
        title="Add Payment Method"
        open={paymentModalOpen}
        onCancel={() => {
          setPaymentModalOpen(false);
          paymentForm.resetFields();
        }}
        onOk={() => paymentForm.submit()}
        confirmLoading={addPaymentMutation.isPending}
        okText="Add Card"
      >
        <Form
          form={paymentForm}
          layout="vertical"
          onFinish={(values) => addPaymentMutation.mutate(values)}
        >
          <Form.Item
            name="cardNumber"
            label="Card Number"
            rules={[{ required: true, message: 'Please enter card number' }]}
          >
            <Input placeholder="4111 1111 1111 1111" maxLength={19} />
          </Form.Item>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item
                name="expiry"
                label="Expiry (MM/YY)"
                rules={[{ required: true, message: 'Required' }]}
              >
                <Input placeholder="MM/YY" maxLength={5} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                name="cvv"
                label="CVV"
                rules={[{ required: true, message: 'Required' }]}
              >
                <Input placeholder="123" maxLength={4} />
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Modal>
    </>
  );
}
