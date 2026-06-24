import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

/** Responsive field layout: stacks on mobile, optionally two columns from `sm` up. */
export function FormGrid({
  columns = 1,
  className,
  children,
}: {
  columns?: 1 | 2;
  className?: string;
  children: ReactNode;
}) {
  return (
    <div className={cn('grid gap-4', columns === 2 && 'sm:grid-cols-2', className)}>{children}</div>
  );
}
