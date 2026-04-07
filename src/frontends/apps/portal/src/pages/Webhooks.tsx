import { useState } from 'react';
import {
  Table,
  Button,
  Modal,
  Form,
  Input,
  Checkbox,
  Switch,
  Tag,
  Popconfirm,
  Space,
  message,
} from 'antd';
import {
  PlusOutlined,
  DeleteOutlined,
  EditOutlined,
  SendOutlined,
} from '@ant-design/icons';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { StatusBadge, PageHeader } from '@payment-gateway/ui';
import type { WebhookDto, WebhookDeliveryDto } from '@payment-gateway/types';
import { webhooksApi } from '../api';

const EVENT_OPTIONS = [
  { label: 'Transaction Succeeded', value: 'transaction.succeeded' },
  { label: 'Transaction Failed', value: 'transaction.failed' },
  { label: 'Refund Created', value: 'refund.created' },
];

export function Webhooks() {
  const queryClient = useQueryClient();
  const [modalOpen, setModalOpen] = useState(false);
  const [editingWebhook, setEditingWebhook] = useState<WebhookDto | null>(null);
  const [expandedRowKeys, setExpandedRowKeys] = useState<string[]>([]);
  const [form] = Form.useForm();

  const { data: webhooks, isLoading } = useQuery({
    queryKey: ['webhooks'],
    queryFn: () => webhooksApi.getWebhooks(),
  });

  const createMutation = useMutation({
    mutationFn: (values: { url: string; events: string[] }) =>
      webhooksApi.createWebhook(values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['webhooks'] });
      setModalOpen(false);
      form.resetFields();
      message.success('Webhook endpoint created');
    },
    onError: () => message.error('Failed to create webhook'),
  });

  const updateMutation = useMutation({
    mutationFn: ({
      id,
      values,
    }: {
      id: string;
      values: { url?: string; events?: string[]; isActive?: boolean };
    }) => webhooksApi.updateWebhook(id, values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['webhooks'] });
      setModalOpen(false);
      setEditingWebhook(null);
      form.resetFields();
      message.success('Webhook endpoint updated');
    },
    onError: () => message.error('Failed to update webhook'),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => webhooksApi.deleteWebhook(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['webhooks'] });
      message.success('Webhook endpoint deleted');
    },
    onError: () => message.error('Failed to delete webhook'),
  });

  const testMutation = useMutation({
    mutationFn: (id: string) => webhooksApi.testWebhook(id),
    onSuccess: () => message.success('Test webhook sent successfully'),
    onError: () => message.error('Test webhook failed'),
  });

  const handleOpenCreate = () => {
    setEditingWebhook(null);
    form.resetFields();
    form.setFieldsValue({ isActive: true });
    setModalOpen(true);
  };

  const handleOpenEdit = (webhook: WebhookDto) => {
    setEditingWebhook(webhook);
    form.setFieldsValue({
      url: webhook.url,
      events: webhook.events,
      isActive: webhook.isActive,
    });
    setModalOpen(true);
  };

  const handleSubmit = (values: { url: string; events: string[]; isActive: boolean }) => {
    if (editingWebhook) {
      updateMutation.mutate({ id: editingWebhook.id, values });
    } else {
      createMutation.mutate({ url: values.url, events: values.events });
    }
  };

  const columns = [
    {
      title: 'URL',
      dataIndex: 'url',
      key: 'url',
      ellipsis: true,
    },
    {
      title: 'Events',
      dataIndex: 'events',
      key: 'events',
      render: (events: string[]) => (
        <Space wrap>
          {events.map((e) => (
            <Tag key={e}>{e}</Tag>
          ))}
        </Space>
      ),
    },
    {
      title: 'Status',
      dataIndex: 'isActive',
      key: 'isActive',
      render: (isActive: boolean) => (
        <StatusBadge status={isActive ? 'active' : 'canceled'} />
      ),
    },
    {
      title: 'Created',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (date: string) => dayjs(date).format('MMM DD, YYYY'),
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_: unknown, record: WebhookDto) => (
        <Space>
          <Button
            size="small"
            icon={<EditOutlined />}
            onClick={(e) => {
              e.stopPropagation();
              handleOpenEdit(record);
            }}
          >
            Edit
          </Button>
          <Button
            size="small"
            icon={<SendOutlined />}
            loading={testMutation.isPending}
            onClick={(e) => {
              e.stopPropagation();
              testMutation.mutate(record.id);
            }}
          >
            Test
          </Button>
          <Popconfirm
            title="Delete Webhook"
            description="Are you sure you want to delete this webhook endpoint?"
            onConfirm={() => deleteMutation.mutate(record.id)}
            okText="Delete"
            okButtonProps={{ danger: true }}
          >
            <Button
              size="small"
              danger
              icon={<DeleteOutlined />}
              onClick={(e) => e.stopPropagation()}
            >
              Delete
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  const expandedRowRender = (record: WebhookDto) => (
    <DeliveryLog webhookId={record.id} />
  );

  return (
    <>
      <PageHeader
        title="Webhooks"
        actions={
          <Button type="primary" icon={<PlusOutlined />} onClick={handleOpenCreate}>
            Add Endpoint
          </Button>
        }
      />

      <Table
        dataSource={webhooks ?? []}
        columns={columns}
        rowKey="id"
        loading={isLoading}
        pagination={false}
        expandable={{
          expandedRowRender,
          expandedRowKeys,
          onExpandedRowsChange: (keys) => setExpandedRowKeys(keys as string[]),
        }}
      />

      <Modal
        title={editingWebhook ? 'Edit Webhook Endpoint' : 'Add Webhook Endpoint'}
        open={modalOpen}
        onCancel={() => {
          setModalOpen(false);
          setEditingWebhook(null);
          form.resetFields();
        }}
        onOk={() => form.submit()}
        confirmLoading={createMutation.isPending || updateMutation.isPending}
        okText={editingWebhook ? 'Update' : 'Create'}
      >
        <Form form={form} layout="vertical" onFinish={handleSubmit}>
          <Form.Item
            name="url"
            label="Endpoint URL"
            rules={[
              { required: true, message: 'Please enter a URL' },
              { type: 'url', message: 'Please enter a valid URL' },
            ]}
          >
            <Input placeholder="https://example.com/webhooks" />
          </Form.Item>
          <Form.Item
            name="events"
            label="Events"
            rules={[{ required: true, message: 'Select at least one event' }]}
          >
            <Checkbox.Group options={EVENT_OPTIONS} />
          </Form.Item>
          {editingWebhook && (
            <Form.Item name="isActive" label="Active" valuePropName="checked">
              <Switch />
            </Form.Item>
          )}
        </Form>
      </Modal>
    </>
  );
}

function DeliveryLog({ webhookId }: { webhookId: string }) {
  const { data, isLoading } = useQuery({
    queryKey: ['webhookDeliveries', webhookId],
    queryFn: () => webhooksApi.getDeliveries(webhookId, { page: 1, pageSize: 20 }),
  });

  const deliveryColumns = [
    {
      title: 'Event Type',
      dataIndex: 'event',
      key: 'event',
      render: (event: string) => <Tag>{event}</Tag>,
    },
    {
      title: 'Status Code',
      dataIndex: 'statusCode',
      key: 'statusCode',
      render: (code: number) => (
        <Tag color={code >= 200 && code < 300 ? 'green' : 'red'}>{code}</Tag>
      ),
    },
    {
      title: 'Success',
      dataIndex: 'success',
      key: 'success',
      render: (success: boolean) => (
        <StatusBadge status={success ? 'completed' : 'failed'} />
      ),
    },
    {
      title: 'Date',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (date: string) => dayjs(date).format('MMM DD, YYYY HH:mm:ss'),
    },
  ];

  return (
    <Table
      dataSource={data?.items ?? []}
      columns={deliveryColumns}
      rowKey="id"
      loading={isLoading}
      pagination={false}
      size="small"
    />
  );
}
