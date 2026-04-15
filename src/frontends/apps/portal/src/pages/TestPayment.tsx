import { useState } from 'react';
import {
  Card,
  Row,
  Col,
  Button,
  Form,
  Input,
  Select,
  InputNumber,
  Typography,
  Space,
  Alert,
  Tabs,
  Spin,
  Descriptions,
  Tag,
  Divider,
  message,
} from 'antd';
import {
  ThunderboltOutlined,
  CopyOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  CodeOutlined,
} from '@ant-design/icons';
import { PageHeader } from '@payment-gateway/ui';

const CURRENCIES = ['USD', 'EUR', 'GBP', 'CAD', 'AUD'];

export function TestPayment() {
  const [form] = Form.useForm();
  const [apiKey, setApiKey] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<{
    success: boolean;
    status: number;
    data?: unknown;
    error?: string;
    duration?: number;
  } | null>(null);
  const [activeTab, setActiveTab] = useState('form');

  const handleTestPayment = async (values: {
    amount: number;
    currency: string;
    email: string;
    description: string;
  }) => {
    if (!apiKey.trim()) {
      message.error('Please enter your API key');
      return;
    }

    setLoading(true);
    setResult(null);

    const requestBody = {
      amount: values.amount,
      currency: values.currency,
      description: values.description || `Test payment - ${values.amount} ${values.currency}`,
      customerEmail: values.email,
      paymentMethodToken: 'tok_visa',
      metadata: { source: 'paygate-test-payment' },
    };

    const startTime = Date.now();

    try {
      const response = await fetch('/api/v1/charges', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-API-Key': apiKey.trim(),
        },
        body: JSON.stringify(requestBody),
      });

      const duration = Date.now() - startTime;
      const data = await response.json();

      setResult({
        success: response.ok,
        status: response.status,
        data,
        duration,
      });

      if (response.ok) {
        message.success('Test payment succeeded!');
      } else {
        message.error('Test payment failed');
      }
    } catch (err) {
      setResult({
        success: false,
        status: 0,
        error: err instanceof Error ? err.message : 'Network error',
        duration: Date.now() - startTime,
      });
      message.error('Request failed - check your API key and network');
    } finally {
      setLoading(false);
    }
  };

  const curlExample = (apiKeyVal: string, amount: number, currency: string, email: string) =>
    `curl -X POST https://api.paygate.io/api/v1/charges \\
  -H "Authorization: Bearer ${apiKeyVal || 'pg_live_...'}" \\
  -H "Content-Type: application/json" \\
  -d '{
    "amount": ${amount},
    "currency": "${currency}",
    "description": "Test payment",
    "customerEmail": "${email}",
    "paymentMethodToken": "tok_visa",
    "metadata": {
      "source": "paygate-test-payment"
    }
  }'`;

  const jsExample = (apiKeyVal: string, amount: number, currency: string, email: string) =>
    `const response = await fetch('https://api.paygate.io/api/v1/charges', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer ${apiKeyVal || 'pg_live_...'}'
  },
  body: JSON.stringify({
    amount: ${amount},
    currency: '${currency}',
    description: 'Test payment',
    customerEmail: '${email}',
    paymentMethodToken: 'tok_visa',
    metadata: { source: 'paygate-test-payment' }
  })
});

const charge = await response.json();
console.log(charge);`;

  const pythonExample = (apiKeyVal: string, amount: number, currency: string, email: string) =>
    `import requests

response = requests.post(
    'https://api.paygate.io/api/v1/charges',
    headers={
        'Authorization': 'Bearer ${apiKeyVal || 'pg_live_...'}',
        'Content-Type': 'application/json'
    },
    json={
        'amount': ${amount},
        'currency': '${currency}',
        'description': 'Test payment',
        'customerEmail': '${email}',
        'paymentMethodToken': 'tok_visa',
        'metadata': {'source': 'paygate-test-payment'}
    }
)

print(response.json())`;

  const formValues = Form.useWatch([], form) ?? {};
  const amount = formValues?.amount ?? 10.0;
  const currency = formValues?.currency ?? 'USD';
  const email = formValues?.email ?? 'test@example.com';

  const copyCode = (code: string) => {
    navigator.clipboard.writeText(code);
    message.success('Copied to clipboard');
  };

  return (
    <>
      <PageHeader
        title="Test Payment"
        actions={
          <Space>
            <Tag color="blue">Sandbox Mode</Tag>
          </Space>
        }
      />

      <Alert
        message="Test your API key with a demo payment"
        description="Use this page to verify your API key works by making a test charge. Uses a sandbox payment token (tok_visa) so no real money is charged."
        type="info"
        showIcon
        style={{ marginBottom: 16, borderRadius: 8 }}
      />

      <Row gutter={[16, 16]}>
        <Col xs={24} lg={12}>
          <Card
            title="API Key"
            bordered={false}
            style={{ borderRadius: 12 }}
            styles={{ body: { padding: 20 } }}
          >
            <Typography.Paragraph type="secondary" style={{ fontSize: 13, marginBottom: 12 }}>
              Paste your API key below. This was shown when you created the key.
            </Typography.Paragraph>
            <Input.Password
              placeholder="pg_live_xxxxxxxxxxxxxxxx"
              value={apiKey}
              onChange={(e) => setApiKey(e.target.value)}
              style={{ fontFamily: 'monospace' }}
              size="large"
            />
            {apiKey && (
              <Typography.Text type="secondary" style={{ fontSize: 12, marginTop: 8, display: 'block' }}>
                Key detected: {apiKey.slice(0, 12)}••••••••
              </Typography.Text>
            )}
          </Card>

          <Card
            title="Payment Details"
            bordered={false}
            style={{ marginTop: 16, borderRadius: 12 }}
            styles={{ body: { padding: 20 } }}
          >
            <Form
              form={form}
              layout="vertical"
              onFinish={handleTestPayment}
              initialValues={{
                amount: 10.0,
                currency: 'USD',
                email: 'test@example.com',
                description: 'Test payment via PayGate portal',
              }}
            >
              <Row gutter={16}>
                <Col span={16}>
                  <Form.Item
                    name="amount"
                    label="Amount"
                    rules={[{ required: true, message: 'Enter an amount' }]}
                  >
                    <InputNumber
                      min={0.01}
                      step={1}
                      style={{ width: '100%' }}
                      prefix="$"
                      size="large"
                    />
                  </Form.Item>
                </Col>
                <Col span={8}>
                  <Form.Item
                    name="currency"
                    label="Currency"
                    rules={[{ required: true }]}
                  >
                    <Select size="large">
                      {CURRENCIES.map((c) => (
                        <Select.Option key={c} value={c}>
                          {c}
                        </Select.Option>
                      ))}
                    </Select>
                  </Form.Item>
                </Col>
              </Row>
              <Form.Item
                name="email"
                label="Customer Email"
                rules={[
                  { required: true, message: 'Enter an email' },
                  { type: 'email', message: 'Enter a valid email' },
                ]}
              >
                <Input placeholder="customer@example.com" size="large" />
              </Form.Item>
              <Form.Item name="description" label="Description (Optional)">
                <Input placeholder="Test payment via PayGate portal" size="large" />
              </Form.Item>
              <Form.Item>
                <Button
                  type="primary"
                  htmlType="submit"
                  icon={<ThunderboltOutlined />}
                  loading={loading}
                  size="large"
                  block
                  disabled={!apiKey.trim()}
                >
                  Send Test Payment
                </Button>
              </Form.Item>
            </Form>
          </Card>
        </Col>

        <Col xs={24} lg={12}>
          <Card
            title={<><CodeOutlined /> Code Examples</>}
            bordered={false}
            style={{ borderRadius: 12 }}
            styles={{ body: { padding: 0 } }}
          >
            <Tabs
              activeKey={activeTab}
              onChange={setActiveTab}
              style={{ padding: '0 16px' }}
              items={[
                {
                  key: 'form',
                  label: 'Interactive',
                  children: (
                    <div style={{ padding: '8px 0' }}>
                      <Typography.Paragraph type="secondary" style={{ fontSize: 13 }}>
                        Fill in the form on the left and click "Send Test Payment" to make a real API call.
                      </Typography.Paragraph>
                    </div>
                  ),
                },
                {
                  key: 'curl',
                  label: 'cURL',
                  children: (
                    <div style={{ position: 'relative' }}>
                      <pre
                        style={{
                          background: '#1e1e2e',
                          color: '#cdd6f4',
                          padding: 16,
                          borderRadius: 8,
                          fontSize: 12,
                          overflowX: 'auto',
                          lineHeight: 1.6,
                        }}
                      >
                        {curlExample(apiKey ? `${apiKey.slice(0, 8)}...` : '', amount, currency, email)}
                      </pre>
                      <Button
                        type="text"
                        size="small"
                        icon={<CopyOutlined />}
                        onClick={() => copyCode(curlExample(apiKey, amount, currency, email))}
                        style={{ position: 'absolute', top: 8, right: 8, color: '#6b7280' }}
                      />
                    </div>
                  ),
                },
                {
                  key: 'javascript',
                  label: 'JavaScript',
                  children: (
                    <div style={{ position: 'relative' }}>
                      <pre
                        style={{
                          background: '#1e1e2e',
                          color: '#cdd6f4',
                          padding: 16,
                          borderRadius: 8,
                          fontSize: 12,
                          overflowX: 'auto',
                          lineHeight: 1.6,
                        }}
                      >
                        {jsExample(apiKey ? `${apiKey.slice(0, 8)}...` : '', amount, currency, email)}
                      </pre>
                      <Button
                        type="text"
                        size="small"
                        icon={<CopyOutlined />}
                        onClick={() => copyCode(jsExample(apiKey, amount, currency, email))}
                        style={{ position: 'absolute', top: 8, right: 8, color: '#6b7280' }}
                      />
                    </div>
                  ),
                },
                {
                  key: 'python',
                  label: 'Python',
                  children: (
                    <div style={{ position: 'relative' }}>
                      <pre
                        style={{
                          background: '#1e1e2e',
                          color: '#cdd6f4',
                          padding: 16,
                          borderRadius: 8,
                          fontSize: 12,
                          overflowX: 'auto',
                          lineHeight: 1.6,
                        }}
                      >
                        {pythonExample(apiKey ? `${apiKey.slice(0, 8)}...` : '', amount, currency, email)}
                      </pre>
                      <Button
                        type="text"
                        size="small"
                        icon={<CopyOutlined />}
                        onClick={() => copyCode(pythonExample(apiKey, amount, currency, email))}
                        style={{ position: 'absolute', top: 8, right: 8, color: '#6b7280' }}
                      />
                    </div>
                  ),
                },
              ]}
            />
          </Card>

          {loading && (
            <Card bordered={false} style={{ marginTop: 16, borderRadius: 12, textAlign: 'center' }}>
              <Spin size="large" />
              <Typography.Text style={{ display: 'block', marginTop: 12 }} type="secondary">
                Processing test payment...
              </Typography.Text>
            </Card>
          )}

          {result && !loading && (
            <Card
              title="Response"
              bordered={false}
              style={{
                marginTop: 16,
                borderRadius: 12,
                border: result.success ? '1px solid #b7eb8f' : '1px solid #ffa39e',
              }}
              extra={
                result.success ? (
                  <Tag color="green" icon={<CheckCircleOutlined />}>Success</Tag>
                ) : (
                  <Tag color="red" icon={<CloseCircleOutlined />}>Failed</Tag>
                )
              }
            >
              <Descriptions column={1} size="small">
                <Descriptions.Item label="Status">
                  <Tag>{result.status}</Tag>
                </Descriptions.Item>
                {result.duration !== undefined && (
                  <Descriptions.Item label="Duration">
                    {result.duration}ms
                  </Descriptions.Item>
                )}
              </Descriptions>
              <Divider style={{ margin: '12px 0' }} />
              <Typography.Text strong style={{ display: 'block', marginBottom: 8 }}>
                Response Body
              </Typography.Text>
              <pre
                style={{
                  background: '#f5f5f5',
                  padding: 12,
                  borderRadius: 8,
                  fontSize: 12,
                  overflowX: 'auto',
                  border: '1px solid #e5e7eb',
                }}
              >
                {JSON.stringify(result.data, null, 2)}
              </pre>
            </Card>
          )}
        </Col>
      </Row>
    </>
  );
}