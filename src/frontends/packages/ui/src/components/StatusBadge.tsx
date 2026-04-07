import React from 'react';
import { Tag } from 'antd';

const statusColors: Record<string, string> = {
  active: 'green',
  completed: 'green',
  succeeded: 'green',
  paid: 'green',
  pending: 'orange',
  processing: 'blue',
  trialing: 'blue',
  open: 'blue',
  draft: 'default',
  canceled: 'default',
  void: 'default',
  failed: 'red',
  past_due: 'red',
  suspended: 'red',
  refunded: 'purple',
  partially_refunded: 'purple',
};

export interface StatusBadgeProps {
  status: string;
}

export function StatusBadge({ status }: StatusBadgeProps) {
  const color = statusColors[status] || 'default';
  return <Tag color={color}>{status.replace(/_/g, ' ').toUpperCase()}</Tag>;
}
