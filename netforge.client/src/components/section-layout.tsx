import { type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';

import { cn } from '@/lib/utils';

export interface LayoutSection {
  /** Stable id used in the URL (`?section=<id>`). */
  id: string;
  label: string;
  /** Optional trailing adornment in the rail item (e.g. a count). */
  badge?: ReactNode;
  content: ReactNode;
}

/**
 * Splits a categorized page into a section rail + a single content panel — only the active section
 * renders, so a long settings/permissions scroll becomes one focused panel. The active section lives
 * in the URL (`?section=<id>`, omitted for the first) so a link reopens the same view; the first
 * section is the default. On mobile the rail collapses to a horizontal row of pills.
 *
 * `side` places the rail on the logical start (default — the conventional left in LTR) or end (right
 * in LTR); either mirrors correctly in RTL. The nav stays first in the DOM so it's reachable early by
 * keyboard regardless of which side it's painted on.
 */
export function SectionLayout({
  sections,
  param = 'section',
  side = 'start',
}: {
  sections: LayoutSection[];
  param?: string;
  side?: 'start' | 'end';
}) {
  const { t } = useTranslation();
  const [params, setParams] = useSearchParams();
  const requested = params.get(param);
  const active = sections.find((s) => s.id === requested) ?? sections[0];

  const select = (id: string) =>
    setParams(
      (prev) => {
        const next = new URLSearchParams(prev);
        // First section is the default → keep the URL clean (no param), matching the grid convention.
        if (id === sections[0]?.id) next.delete(param);
        else next.set(param, id);
        return next;
      },
      { replace: true },
    );

  if (sections.length === 0) return null;

  const end = side === 'end';

  return (
    <div
      className={cn(
        'grid gap-4 md:gap-8',
        end ? 'md:grid-cols-[minmax(0,1fr)_12rem]' : 'md:grid-cols-[12rem_minmax(0,1fr)]',
      )}
    >
      <nav
        aria-label={t('common.sections')}
        // Mobile: pills wrap (a small, finite picker — show every choice, no scrollbar). Desktop: a
        // single-column rail.
        className={cn(
          'flex flex-wrap gap-1 md:flex-col md:flex-nowrap md:sticky md:top-4 md:self-start',
          // Painted on the end column but kept first in the DOM (so it leads the tab order).
          end && 'md:col-start-2 md:row-start-1',
        )}
      >
        {sections.map((section) => {
          const isActive = section.id === active.id;
          return (
            <button
              key={section.id}
              type="button"
              onClick={() => select(section.id)}
              aria-current={isActive ? 'page' : undefined}
              className={cn(
                'flex shrink-0 items-center gap-2 rounded-md px-3 py-2 text-start text-sm whitespace-nowrap capitalize transition-colors outline-none focus-visible:ring-[3px] md:w-full',
                isActive
                  ? 'bg-muted text-foreground font-medium'
                  : 'text-muted-foreground hover:bg-muted/50 hover:text-foreground',
              )}
            >
              <span className="truncate">{section.label}</span>
              {section.badge != null && (
                <span className="text-muted-foreground ms-auto text-xs tabular-nums">{section.badge}</span>
              )}
            </button>
          );
        })}
      </nav>
      <div className={cn('min-w-0', end && 'md:col-start-1 md:row-start-1')}>{active.content}</div>
    </div>
  );
}
