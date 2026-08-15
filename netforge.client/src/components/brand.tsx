import { Hexagon } from 'lucide-react';

import { cn } from '@/lib/utils';

/**
 * Brand lockup: geometric mark + wordmark. `tone="onDark"` is for placement on the always-dark
 * brand panel, where the theme's primary token would otherwise invert the chip.
 */
export function Brand({
  className,
  markOnly = false,
  tone = 'default',
  name,
  logoUrl,
}: {
  className?: string;
  markOnly?: boolean;
  tone?: 'default' | 'onDark';
  /** Override the wordmark (e.g. the active tenant's name for white-label branding). */
  name?: string;
  /** Override the mark with a logo image (e.g. the active tenant's logo). */
  logoUrl?: string | null;
}) {
  const chip =
    tone === 'onDark' ? 'bg-white/10 text-white' : 'bg-primary text-primary-foreground';

  return (
    // `min-w-0` so a long white-label tenant name ellipsizes here instead of spilling past the shell.
    <span className={cn('inline-flex min-w-0 items-center gap-2 font-semibold tracking-tight', className)}>
      {logoUrl ? (
        <img src={logoUrl} alt="" className="size-7 shrink-0 rounded-lg object-cover" />
      ) : (
        <span className={cn('grid size-7 shrink-0 place-items-center rounded-lg', chip)}>
          <Hexagon className="size-4" />
        </span>
      )}
      {!markOnly && <span className="truncate">{name ?? 'NetForge'}</span>}
    </span>
  );
}
