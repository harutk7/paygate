import { useState } from 'react';
import { Button, Modal, Form, Input, InputNumber, Select, Switch, Space, message } from 'antd';
import { PlusOutlined, EditOutlined, MinusCircleOutlined } from '@ant-design/icons';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { PageHeader, StatusBadge, DataTable } from '@payment-gateway/ui';
import type { PlanDto, CreatePlanRequest, UpdatePlanRequest } from '@payment-gateway/types';
import { adminApi } from '../api';

export function Plans() {
  const queryClient = useQueryClient();
  const [modalOpen, setModalOpen] = useState(false);
  const [editingPlan, setEditingPlan] = useState<PlanDto | null>(null);
  const [form] = Form.useForm();

  const { data, isLoading } = useQuery({
    queryKey: ['admin', 'plans'],
    queryFn: () => adminApi.getPlans({ pageSize: 100 }),
  });

  const createMutation = useMutation({
    mutationFn: (values: CreatePlanRequest) => adminApi.createPlan(values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'plans'] });
      message.success('Plan created');
      closeModal();
    },
    onError: () => message.error('Failed to create plan'),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdatePlanRequest }) =>
      adminApi.updatePlan(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'plans'] });
      message.success('Plan updated');
      closeModal();
    },
    onError: () => message.error('Failed to update plan'),
  });

  const openCreate = () => {
    setEditingPlan(null);
    form.resetFields();
    form.setFieldsValue({ isActive: true, features: [''] });
    setModalOpen(true);
  };

  const openEdit = (plan: PlanDto) => {
    setEditingPlan(plan);
    form.setFieldsValue({
      name: plan.name,
      description: plan.description,
      tier: plan.name,
      monthlyPrice: plan.monthlyPrice,
      annualPrice: plan.annualPrice,
      transactionLimit: plan.transactionLimit,
      rateLimit: plan.rateLimit,
      features: plan.features?.length ? plan.features : [''],
      isActive: plan.isActive,
    });
    setModalOpen(true);
  };

  const closeModal = () => {
    setModalOpen(false);
    setEditingPlan(null);
    form.resetFields();
  };

  const handleSubmit = async () => {
    const values = await form.validateFields();
    const payload = {
      name: values.name,
      description: values.description || '',
      monthlyPrice: values.monthlyPrice,
      annualPrice: values.annualPrice,
      transactionLimit: values.transactionLimit,
      rateLimit: values.rateLimit,
      features: (values.features ?? []).filter((f: string) => f?.trim()),
    };

    if (editingPlan) {
      updateMutation.mutate({
        id: editingPlan.id,
        data: { ...payload, isActive: values.isActive },
      });
    } else {
      createMutation.mutate(payload);
    }
  };

  const columns = [
    { title: 'Plan Name', dataIndex: 'name', key: 'name' },
    {
      title: 'Monthly Price',
      dataIndex: 'monthlyPrice',
      key: 'monthlyPrice',
      render: (v: number) => `$${v?.toFixed(2)}`,
      align: 'right' as const,
    },
    {
      title: 'Annual Price',
      dataIndex: 'annualPrice',
      key: 'annualPrice',
      render: (v: number) => `$${v?.toFixed(2)}`,
      align: 'right' as const,
    },
    {
      title: 'Txn Limit',
      dataIndex: 'transactionLimit',
      key: 'transactionLimit',
      align: 'right' as const,
      render: (v: number) => v?.toLocaleString(),
    },
    {
      title: 'Rate Limit',
      dataIndex: 'rateLimit',
      key: 'rateLimit',
      align: 'right' as const,
      render: (v: number) => `${v}/min`,
    },
    {
      title: 'Status',
      dataIndex: 'isActive',
      key: 'isActive',
      render: (active: boolean) => (
        <StatusBadge status={active ? 'active' : 'canceled'} />
      ),
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_: unknown, record: PlanDto) => (
        <Button
          size="small"
          icon={<EditOutlined />}
          onClick={() => openEdit(record)}
        >
          Edit
        </Button>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Plans"
        actions={
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
            Create Plan
          </Button>
        }
      />

      <DataTable<PlanDto>
        dataSource={data?.items ?? []}
        columns={columns}
        rowKey="id"
        loading={isLoading}
        total={data?.totalCount}
      />

      <Modal
        title={editingPlan ? 'Edit Plan' : 'Create Plan'}
        open={modalOpen}
        onCancel={closeModal}
        onOk={handleSubmit}
        confirmLoading={createMutation.isPending || updateMutation.isPending}
        width={600}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item
            name="name"
            label="Name"
            rules={[{ required: true, message: 'Plan name is required' }]}
          >
            <Input placeholder="e.g., Business Pro" />
          </Form.Item>

          <Form.Item name="description" label="Description">
            <Input.TextArea rows={2} placeholder="Plan description" />
          </Form.Item>

          <Form.Item name="tier" label="Tier">
            <Select
              placeholder="Select tier"
              options={[
                { label: 'Starter', value: 'Starter' },
                { label: 'Business', value: 'Business' },
                { label: 'Enterprise', value: 'Enterprise' },
              ]}
            />
          </Form.Item>

          <Space size="large" style={{ width: '100%' }}>
            <Form.Item
              name="monthlyPrice"
              label="Monthly Price ($)"
              rules={[{ required: true }]}
            >
              <InputNumber min={0} step={0.01} precision={2} style={{ width: 160 }} />
            </Form.Item>
            <Form.Item
              name="annualPrice"
              label="Annual Price ($)"
              rules={[{ required: true }]}
            >
              <InputNumber min={0} step={0.01} precision={2} style={{ width: 160 }} />
            </Form.Item>
          </Space>

          <Space size="large" style={{ width: '100%' }}>
            <Form.Item
              name="transactionLimit"
              label="Transaction Limit"
              rules={[{ required: true }]}
            >
              <InputNumber min={0} step={100} style={{ width: 160 }} />
            </Form.Item>
            <Form.Item
              name="rateLimit"
              label="Rate Limit (req/min)"
              rules={[{ required: true }]}
            >
              <InputNumber min={0} step={10} style={{ width: 160 }} />
            </Form.Item>
          </Space>

          <Form.List name="features">
            {(fields, { add, remove }) => (
              <>
                <label style={{ display: 'block', marginBottom: 8 }}>Features</label>
                {fields.map((field) => (
                  <Space key={field.key} style={{ display: 'flex', marginBottom: 8 }}>
                    <Form.Item {...field} noStyle>
                      <Input placeholder="Feature description" style={{ width: 440 }} />
                    </Form.Item>
                    {fields.length > 1 && (
                      <MinusCircleOutlined onClick={() => remove(field.name)} />
                    )}
                  </Space>
                ))}
                <Button type="dashed" onClick={() => add('')} icon={<PlusOutlined />} block>
                  Add Feature
                </Button>
              </>
            )}
          </Form.List>

          {editingPlan && (
            <Form.Item name="isActive" label="Active" valuePropName="checked" style={{ marginTop: 16 }}>
              <Switch />
            </Form.Item>
          )}
        </Form>
      </Modal>
    </>
  );
}
