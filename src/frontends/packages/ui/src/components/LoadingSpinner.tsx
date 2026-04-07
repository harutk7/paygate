import React from 'react';
import { Spin } from 'antd';

export interface LoadingSpinnerProps {
  tip?: string;
}

export function LoadingSpinner({ tip = 'Loading...' }: LoadingSpinnerProps) {
  return (
    <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: 200 }}>
      <Spin size="large" tip={tip}>
        <div style={{ padding: 50 }} />
      </Spin>
    </div>
  );
}
