import { useState } from 'react';
import { Outlet, useNavigate, useLocation } from 'react-router';
import { Layout, Menu, Dropdown, Avatar, Space, Typography, Breadcrumb } from 'antd';
import {
  DashboardOutlined,
  SwapOutlined,
  KeyOutlined,
  ApiOutlined,
  CreditCardOutlined,
  SettingOutlined,
  LogoutOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { useAuthStore } from '../stores/auth';

const { Sider, Header, Content } = Layout;

const menuItems = [
  { key: '/', icon: <DashboardOutlined />, label: 'Dashboard' },
  { key: '/transactions', icon: <SwapOutlined />, label: 'Transactions' },
  { key: '/api-keys', icon: <KeyOutlined />, label: 'API Keys' },
  { key: '/webhooks', icon: <ApiOutlined />, label: 'Webhooks' },
  { key: '/billing', icon: <CreditCardOutlined />, label: 'Billing' },
  { key: '/settings', icon: <SettingOutlined />, label: 'Settings' },
];

const breadcrumbMap: Record<string, string> = {
  '/': 'Dashboard',
  '/transactions': 'Transactions',
  '/api-keys': 'API Keys',
  '/webhooks': 'Webhooks',
  '/billing': 'Billing',
  '/settings': 'Settings',
};

export function PortalLayout() {
  const [collapsed, setCollapsed] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();
  const logout = useAuthStore((s) => s.logout);
  const user = useAuthStore((s) => s.user);
  const organization = useAuthStore((s) => s.organization);

  const handleLogout = async () => {
    await logout();
    window.location.href = 'http://localhost:18050/login';
  };

  const currentPage = breadcrumbMap[location.pathname] || 'Dashboard';

  const userMenuItems = [
    {
      key: 'profile',
      icon: <UserOutlined />,
      label: 'Profile',
      onClick: () => navigate('/settings'),
    },
    { type: 'divider' as const },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: 'Logout',
      onClick: handleLogout,
    },
  ];

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider collapsible collapsed={collapsed} onCollapse={setCollapsed}>
        <div
          style={{
            height: 32,
            margin: 16,
            color: '#fff',
            fontWeight: 'bold',
            textAlign: 'center',
            fontSize: collapsed ? 14 : 16,
          }}
        >
          {collapsed ? 'PG' : 'PaymentGateway'}
        </div>
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={[location.pathname]}
          items={menuItems}
          onClick={({ key }) => navigate(key)}
        />
      </Sider>
      <Layout>
        <Header
          style={{
            padding: '0 24px',
            background: '#fff',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            borderBottom: '1px solid #f0f0f0',
          }}
        >
          <Typography.Text strong>
            {organization?.name || 'My Organization'}
          </Typography.Text>
          <Dropdown menu={{ items: userMenuItems }} placement="bottomRight">
            <Space style={{ cursor: 'pointer' }}>
              <Avatar icon={<UserOutlined />} size="small" />
              <span>
                {user ? `${user.firstName} ${user.lastName}` : 'User'}
              </span>
            </Space>
          </Dropdown>
        </Header>
        <Content style={{ margin: 24 }}>
          <Breadcrumb
            style={{ marginBottom: 16 }}
            items={[
              { title: 'Portal' },
              { title: currentPage },
            ]}
          />
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
}
