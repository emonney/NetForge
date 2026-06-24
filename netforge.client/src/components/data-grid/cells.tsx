import { Badge } from '@/components/ui/badge';
import { timeAgo } from '@/lib/format';
import { cn } from '@/lib/utils';

/** Date cell: relative ("3d ago") by default with the full timestamp on hover; em-dash when empty. */
export function DateCell({ value, relative = true }: { value?: string | null; relative?: boolean }) {
  if (!value) return <span className="text-muted-foreground">—</span>;
  const date = new Date(value);
  return (
    <span className="text-muted-foreground" title={date.toLocaleString()}>
      {relative ? timeAgo(value) : date.toLocaleDateString()}
    </span>
  );
}

export type BadgeTone = 'default' | 'muted' | 'success' | 'warning' | 'destructive' | 'info';

const TONES: Record<BadgeTone, string> = {
  default: '',
  muted: 'bg-muted text-muted-foreground',
  success: 'border-transparent bg-emerald-500/15 text-emerald-700 dark:text-emerald-400',
  warning: 'border-transparent bg-amber-500/15 text-amber-700 dark:text-amber-400',
  destructive: 'border-transparent bg-red-500/15 text-red-700 dark:text-red-400',
  info: 'border-transparent bg-blue-500/15 text-blue-700 dark:text-blue-400',
};

/** Status/enum cell rendered as a toned badge (readable in both themes). */
export function BadgeCell({ label, tone = 'default' }: { label: string; tone?: BadgeTone }) {
  return (
    <Badge variant={tone === 'default' ? 'secondary' : 'outline'} className={cn(TONES[tone])}>
      {label}
    </Badge>
  );
}
