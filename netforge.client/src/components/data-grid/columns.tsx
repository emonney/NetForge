import type { ColumnDef, RowData } from '@tanstack/react-table';

import i18n from '@/i18n.config';
import { Checkbox } from '@/components/ui/checkbox';

// Per-column metadata: `label` drives the column-visibility menu and the mobile card field labels.
declare module '@tanstack/react-table' {
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  interface ColumnMeta<TData extends RowData, TValue> {
    label?: string;
  }
}

/** Leading checkbox column: select-all-on-page in the header, per-row in the body. */
export function selectColumn<T>(): ColumnDef<T> {
  return {
    id: '__select',
    enableSorting: false,
    enableHiding: false,
    meta: { label: '' },
    header: ({ table }) => (
      <Checkbox
        checked={
          table.getIsAllPageRowsSelected()
            ? true
            : table.getIsSomePageRowsSelected()
              ? 'indeterminate'
              : false
        }
        onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)}
        onClick={(e) => e.stopPropagation()}
        aria-label={i18n.t('grid.selectAll')}
      />
    ),
    cell: ({ row }) => (
      <Checkbox
        checked={row.getIsSelected()}
        onCheckedChange={(value) => row.toggleSelected(!!value)}
        onClick={(e) => e.stopPropagation()}
        aria-label={i18n.t('grid.selectRow')}
      />
    ),
  };
}
