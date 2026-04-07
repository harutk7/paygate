import { useState } from 'react';
import {
  Card,
  Form,
  Input,
  Button,
  Divider,
  Typography,
  Table,
  Tag,
  Modal,
  Select,
  Popconfirm,
  Space,
  message,
} from 'antd';
import {
  UserAddOutlined,
  DeleteOutlined,
  EyeOutlined,
  EyeInvisibleOutlined,
} from '@ant-design/icons';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { StatusBadge, PageHeader } from '@payment-gateway/ui';
import type { UserDto } from '@payment-gateway/types';
import { useAuthStore } from '../stores/auth';
import { organizationsApi, usersApi } from '../api';

export function Settings() {
  const queryClient = useQueryClient();
  const user = useAuthStore((s) => s.user);
  const organization = useAuthStore((s) => s.organization);
  const setUser = useAuthStore((s) => s.setUser);
  const setOrganization = useAuthStore((s) => s.setOrganization);

  const [inviteModalOpen, setInviteModalOpen] = useState(false);
  const [secretVisible, setSecretVisible] = useState(false);
  const [inviteForm] = Form.useForm();

  const { data: membersData, isLoading: membersLoading } = useQuery({
    queryKey: ['teamMembers'],
    queryFn: () => usersApi.getUsers({ page: 1, pageSize: 100 }),
  });

  const updateOrgMutation = useMutation({
    mutationFn: (values: { name: string }) =>
      organizationsApi.updateOrg({ name: values.name }),
    onSuccess: (result) => {
      setOrganization(result);
      message.success('Organization updated');
    },
    onError: () => message.error('Failed to update organization'),
  });

  const updateProfileMutation = useMutation({
    mutationFn: (values: { firstName: string; lastName: string }) =>
      usersApi.updateProfile(values),
    onSuccess: (result) => {
      setUser(result);
      message.success('Profile updated');
    },
    onError: () => message.error('Failed to update profile'),
  });

  const inviteMutation = useMutation({
    mutationFn: (values: { email: string; role: string }) =>
      usersApi.inviteUser(values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['teamMembers'] });
      setInviteModalOpen(false);
      inviteForm.resetFields();
      message.success('Invitation sent');
    },
    onError: () => message.error('Failed to send invitation'),
  });

  const memberColumns = [
    {
      title: 'Name',
      key: 'name',
      render: (_: unknown, record: UserDto) =>
        `${record.firstName} ${record.lastName}`,
    },
    {
      title: 'Email',
      dataIndex: 'email',
      key: 'email',
    },
    {
      title: 'Role',
      dataIndex: 'role',
      key: 'role',
      render: (role: string) => (
        <Tag color={role === 'CustomerAdmin' ? 'blue' : 'default'}>
          {role}
        </Tag>
      ),
    },
    {
      title: 'Status',
      dataIndex: 'isActive',
      key: 'isActive',
      render: (isActive: boolean) => (
        <StatusBadge status={isActive ? 'active' : 'suspended'} />
      ),
    },
    {
      title: 'Joined',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (date: string) => dayjs(date).format('MMM DD, YYYY'),
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_: unknown, record: UserDto) =>
        record.id !== user?.id ? (
          <Popconfirm
            title="Remove Member"
            description="Are you sure you want to remove this team member?"
            okText="Remove"
            okButtonProps={{ danger: true }}
            onConfirm={() => {
              message.info('Member removal not implemented in MVP');
            }}
          >
            <Button size="small" danger icon={<DeleteOutlined />}>
              Remove
            </Button>
          </Popconfirm>
        ) : null,
    },
  ];

  return (
    <>
      <PageHeader title="Settings" />

      <Card title="Profile" style={{ marginBottom: 16 }}>
        <Form
          layout="vertical"
          style={{ maxWidth: 400 }}
          initialValues={{
            firstName: user?.firstName ?? '',
            lastName: user?.lastName ?? '',
            email: user?.email ?? '',
          }}
          onFinish={(values) =>
            updateProfileMutation.mutate({
              firstName: values.firstName,
              lastName: values.lastName,
            })
          }
        >
          <Form.Item
            label="First Name"
            name="firstName"
            rules={[{ required: true, message: 'First name is required' }]}
          >
            <Input />
          </Form.Item>
          <Form.Item
            label="Last Name"
            name="lastName"
            rules={[{ required: true, message: 'Last name is required' }]}
          >
            <Input />
          </Form.Item>
          <Form.Item label="Email" name="email">
            <Input disabled />
          </Form.Item>
          <Button
            type="primary"
            htmlType="submit"
            loading={updateProfileMutation.isPending}
          >
            Save Changes
          </Button>
        </Form>
      </Card>

      <Card title="Organization" style={{ marginBottom: 16 }}>
        <Form
          layout="vertical"
          style={{ maxWidth: 400 }}
          initialValues={{
            name: organization?.name ?? '',
            slug: organization?.slug ?? '',
          }}
          onFinish={(values) => updateOrgMutation.mutate({ name: values.name })}
        >
          <Form.Item
            label="Organization Name"
            name="name"
            rules={[{ required: true, message: 'Organization name is required' }]}
          >
            <Input />
          </Form.Item>
          <Form.Item label="Slug" name="slug">
            <Input disabled />
          </Form.Item>
          <Button
            type="primary"
            htmlType="submit"
            loading={updateOrgMutation.isPending}
          >
            Update
          </Button>
        </Form>

        <Divider />

        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            marginBottom: 16,
          }}
        >
          <div>
            <Typography.Title level={5} style={{ margin: 0 }}>
              Team Members
            </Typography.Title>
            <Typography.Text type="secondary">
              Manage your organization's team members.
            </Typography.Text>
          </div>
          <Button
            type="primary"
            icon={<UserAddOutlined />}
            onClick={() => setInviteModalOpen(true)}
          >
            Invite Member
          </Button>
        </div>

        <Table
          dataSource={membersData?.items ?? []}
          columns={memberColumns}
          rowKey="id"
          loading={membersLoading}
          pagination={false}
          size="small"
        />
      </Card>

      <Card title="Gateway Settings">
        <Typography.Text strong>Webhook Secret</Typography.Text>
        <div style={{ marginTop: 8, display: 'flex', alignItems: 'center', gap: 8 }}>
          <Input
            value={secretVisible ? 'whsec_xxxxxxxxxxxxxxxxxxxxxxxx' : '••••••••••••••••••••'}
            readOnly
            style={{ maxWidth: 320, fontFamily: 'monospace' }}
          />
          <Button
            icon={secretVisible ? <EyeInvisibleOutlined /> : <EyeOutlined />}
            onClick={() => setSecretVisible(!secretVisible)}
          >
            {secretVisible ? 'Hide' : 'Reveal'}
          </Button>
        </div>
      </Card>

      <Modal
        title="Invite Team Member"
        open={inviteModalOpen}
        onCancel={() => {
          setInviteModalOpen(false);
          inviteForm.resetFields();
        }}
        onOk={() => inviteForm.submit()}
        confirmLoading={inviteMutation.isPending}
        okText="Send Invitation"
      >
        <Form form={inviteForm} layout="vertical" onFinish={(values) => inviteMutation.mutate(values)}>
          <Form.Item
            name="email"
            label="Email"
            rules={[
              { required: true, message: 'Email is required' },
              { type: 'email', message: 'Please enter a valid email' },
            ]}
          >
            <Input placeholder="colleague@company.com" />
          </Form.Item>
          <Form.Item
            name="role"
            label="Role"
            rules={[{ required: true, message: 'Please select a role' }]}
          >
            <Select
              placeholder="Select a role"
              options={[
                { value: 'CustomerAdmin', label: 'Admin' },
                { value: 'CustomerUser', label: 'User' },
              ]}
            />
          </Form.Item>
        </Form>
      </Modal>
    </>
  );
}
