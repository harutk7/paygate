import { useState } from 'react';
import {
  Table,
  Button,
  Modal,
  Form,
  Input,
  DatePicker,
  Popconfirm,
  Space,
  Typography,
  Alert,
  Empty,
  message,
} from 'antd';
import {
  PlusOutlined,
  CopyOutlined,
  DeleteOutlined,
  ReloadOutlined,
} from '@ant-design/icons';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { StatusBadge, PageHeader } from '@payment-gateway/ui';
import type { ApiKeyDto } from '@payment-gateway/types';
import { apiKeysApi } from '../api';

export function ApiKeys() {
  const queryClient = useQueryClient();
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [newKey, setNewKey] = useState<string | null>(null);
  const [form] = Form.useForm();

  const { data, isLoading } = useQuery({
    queryKey: ['apiKeys'],
    queryFn: () => apiKeysApi.getApiKeys({ page: 1, pageSize: 100 }),
  });

  const createMutation = useMutation({
    mutationFn: (values: { name: string; expiresAt?: string }) =>
      apiKeysApi.createApiKey({
        name: values.name,
        environment: 'live',
        expiresAt: values.expiresAt,
      }),
    onSuccess: (result) => {
      setNewKey(result.secretKey);
      setCreateModalOpen(false);
      form.resetFields();
      queryClient.invalidateQueries({ queryKey: ['apiKeys'] });
      message.success('API key created successfully');
    },
    onError: () => {
      message.error('Failed to create API key');
    },
  });

  const revokeMutation = useMutation({
    mutationFn: (id: string) => apiKeysApi.revokeApiKey(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['apiKeys'] });
      message.success('API key revoked');
    },
    onError: () => {
      message.error('Failed to revoke API key');
    },
  });

  const rotateMutation = useMutation({
    mutationFn: (id: string) => apiKeysApi.rotateApiKey(id),
    onSuccess: (result) => {
      setNewKey(result.secretKey);
      queryClient.invalidateQueries({ queryKey: ['apiKeys'] });
      message.success('API key rotated successfully');
    },
    onError: () => {
      message.error('Failed to rotate API key');
    },
  });

  const handleCopy = (key: string) => {
    navigator.clipboard.writeText(key);
    message.success('Copied to clipboard');
  };

  const handleCreate = (values: { name: string; expiresAt?: dayjs.Dayjs }) => {
    createMutation.mutate({
      name: values.name,
      expiresAt: values.expiresAt?.toISOString(),
    });
  };

  const columns = [
    {
      title: 'Name',
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: 'Key Prefix',
      dataIndex: 'prefix',
      key: 'prefix',
      render: (prefix: string) => (
        <span style={{ fontFamily: 'monospace' }}>{prefix}...</span>
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
      title: 'Expires',
      dataIndex: 'expiresAt',
      key: 'expiresAt',
      render: (date: string | null) =>
        date ? dayjs(date).format('MMM DD, YYYY') : 'Never',
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_: unknown, record: ApiKeyDto) => (
        <Space>
          {record.isActive && (
            <>
              <Popconfirm
                title="Rotate API Key"
                description="This will create a new key. The old key will remain valid for 24 hours."
                onConfirm={() => rotateMutation.mutate(record.id)}
                okText="Rotate"
              >
                <Button size="small" icon={<ReloadOutlined />}>
                  Rotate
                </Button>
              </Popconfirm>
              <Popconfirm
                title="Revoke API Key"
                description="This action cannot be undone. The key will stop working immediately."
                onConfirm={() => revokeMutation.mutate(record.id)}
                okText="Revoke"
                okButtonProps={{ danger: true }}
              >
                <Button size="small" danger icon={<DeleteOutlined />}>
                  Revoke
                </Button>
              </Popconfirm>
            </>
          )}
        </Space>
      ),
    },
  ];

  const hasKeys = (data?.items?.length ?? 0) > 0;

  return (
    <>
      <PageHeader
        title="API Keys"
        actions={
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => setCreateModalOpen(true)}
          >
            Create API Key
          </Button>
        }
      />

      {hasKeys ? (
        <Table
          dataSource={data?.items ?? []}
          columns={columns}
          rowKey="id"
          loading={isLoading}
          pagination={false}
        />
      ) : (
        <Empty
          description="No API keys yet. Create your first key to get started."
          style={{ marginTop: 48 }}
        >
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => setCreateModalOpen(true)}
          >
            Create API Key
          </Button>
        </Empty>
      )}

      <Modal
        title="Create API Key"
        open={createModalOpen}
        onCancel={() => {
          setCreateModalOpen(false);
          form.resetFields();
        }}
        onOk={() => form.submit()}
        confirmLoading={createMutation.isPending}
        okText="Create"
      >
        <Form form={form} layout="vertical" onFinish={handleCreate}>
          <Form.Item
            name="name"
            label="Key Name"
            rules={[{ required: true, message: 'Please enter a name for this key' }]}
          >
            <Input placeholder="e.g. Production, Staging, Mobile App" />
          </Form.Item>
          <Form.Item name="expiresAt" label="Expiry Date (Optional)">
            <DatePicker
              style={{ width: '100%' }}
              disabledDate={(current) => current && current < dayjs().endOf('day')}
            />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="API Key Created"
        open={!!newKey}
        onCancel={() => setNewKey(null)}
        footer={[
          <Button key="copy" type="primary" icon={<CopyOutlined />} onClick={() => handleCopy(newKey!)}>
            Copy Key
          </Button>,
          <Button key="close" onClick={() => setNewKey(null)}>
            Close
          </Button>,
        ]}
      >
        <Alert
          message="Save this key now!"
          description="This key will not be shown again. Please copy it and store it securely."
          type="warning"
          showIcon
          style={{ marginBottom: 16 }}
        />
        <Input.TextArea
          value={newKey ?? ''}
          readOnly
          autoSize={{ minRows: 2 }}
          style={{ fontFamily: 'monospace' }}
        />
      </Modal>
    </>
  );
}
