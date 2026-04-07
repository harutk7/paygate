import React from 'react';
import { Card, Statistic } from 'antd';
import { ArrowUpOutlined, ArrowDownOutlined } from '@ant-design/icons';

export interface StatCardProps {
  title: string;
  value: number | string;
  prefix?: React.ReactNode;
  suffix?: string;
  precision?: number;
  trend?: 'up' | 'down';
  trendValue?: string;
  loading?: boolean;
}

export function StatCard({
  title,
  value,
  prefix,
  suffix,
  precision,
  trend,
  trendValue,
  loading = false,
}: StatCardProps) {
  return (
    <Card loading={loading}>
      <Statistic
        title={title}
        value={value}
        prefix={prefix}
        suffix={
          <>
            {suffix}
            {trend && trendValue && (
              <span style={{ fontSize: 14, marginLeft: 8, color: trend === 'up' ? '#3f8600' : '#cf1322' }}>
                {trend === 'up' ? <ArrowUpOutlined /> : <ArrowDownOutlined />} {trendValue}
              </span>
            )}
          </>
        }
        precision={precision}
      />
    </Card>
  );
}
