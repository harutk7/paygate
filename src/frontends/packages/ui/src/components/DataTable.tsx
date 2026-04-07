import React from 'react';
import { Table, TableProps } from 'antd';

export interface DataTableProps<T extends object> extends TableProps<T> {
  loading?: boolean;
  pageSize?: number;
  total?: number;
  currentPage?: number;
  onPageChange?: (page: number, pageSize: number) => void;
}

export function DataTable<T extends object>({
  loading = false,
  pageSize = 10,
  total,
  currentPage = 1,
  onPageChange,
  ...tableProps
}: DataTableProps<T>) {
  return (
    <Table<T>
      loading={loading}
      pagination={
        total !== undefined
          ? {
              current: currentPage,
              pageSize,
              total,
              onChange: onPageChange,
              showSizeChanger: true,
              showTotal: (t) => `Total ${t} items`,
            }
          : false
      }
      {...tableProps}
    />
  );
}
