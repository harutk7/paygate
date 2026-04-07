import { useState } from 'react';
import { Outlet, useNavigate, useLocation } from 'react-router';
import { Layout, Menu, Tag, Button } from 'antd';
import {
  DashboardOutlined,
  TeamOutlined,
  AppstoreOutlined,
  SwapOutlined,
  DollarOutlined,
  AuditOutlined,
  LogoutOutlined,
} from '@ant-design/icons';
import { useAuthStore } from '../stores/auth';

const { Sider, Header, Content } = Layout;

const menuItems = [
  { key: '/', icon: <DashboardOutlined />, label: 'Dashboard' },
  { key: '/customers', icon: <TeamOutlined />, label: 'Customers' },
  { key: '/plans', icon: <AppstoreOutlined />, label: 'Plans' },
  { key: '/transactions', icon: <SwapOutlined />, label: 'Transactions' },
  { key: '/revenue', icon: <DollarOutlined />, label: 'Revenue' },
  { key: '/audit-log', icon: <AuditOutlined />, label: 'Audit Log' },
];

export function BackofficeLayout() {
  const [collapsed, setCollapsed] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();
  const logout = useAuthStore((s) => s.logout);
  const user = useAuthStore((s) => s.user);

  const selectedKey = menuItems.find(
    (item) => item.key !== '/' && location.pathname.startsWith(item.key),
  )?.key || '/';

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider
        collapsible
        collapsed={collapsed}
        onCollapse={setCollapsed}
        style={{ background: '#141414' }}
      >
        <div
          style={{
            height: 48,
            margin: 16,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          <span style={{ color: '#fff', fontWeight: 700, fontSize: collapsed ? 14 : 18, letterSpacing: 1 }}>
            {collapsed ? 'BO' : 'BACKOFFICE'}
          </span>
        </div>
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={[selectedKey]}
          items={menuItems}
          onClick={({ key }) => navigate(key)}
          style={{ background: '#141414' }}
        />
      </Sider>
      <Layout>
        <Header
          style={{
            padding: '0 24px',
            background: '#1f1f1f',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <span style={{ color: 'rgba(255,255,255,0.85)', fontWeight: 600, fontSize: 16 }}>
              BACKOFFICE
            </span>
            <Tag color="orange" style={{ margin: 0 }}>SANDBOX</Tag>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
            <span style={{ color: 'rgba(255,255,255,0.65)' }}>
              {user?.firstName} {user?.lastName}
            </span>
            <span style={{ color: 'rgba(255,255,255,0.45)' }}>
              {user?.email}
            </span>
            <Button
              type="text"
              icon={<LogoutOutlined />}
              onClick={() => {
                logout();
                navigate('/login');
              }}
              style={{ color: 'rgba(255,255,255,0.65)' }}
            >
              {collapsed ? '' : 'Logout'}
            </Button>
          </div>
        </Header>
        <Content style={{ margin: 24, minHeight: 280 }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
}
