import { useTranslation } from 'react-i18next';
import { Check, ChevronDown } from 'lucide-react';

import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from '@/components/ui/command';

export interface FacetOption {
  value: string;
  label: string;
}

export interface FacetFilterProps {
  /** Short label shown on the trigger, e.g. "Status". */
  label: string;
  options: FacetOption[];
  /** The grid's raw filter value for this field (e.g. "in:a,b", "a", or undefined). */
  value?: string;
  /** Raw value to hand to grid.setFilter; null clears the filter. */
  onChange: (next: string | null) => void;
  className?: string;
}

// Above this many options the popover grows a search box. Bounded facets (status, a handful of
// categories) stay a clean list; unbounded ones (the audit "Who" actor list) become typeable.
const SEARCHABLE_THRESHOLD = 8;

/** Parse a grid filter value (`in:a,b` or a bare `a`) into its selected option values. */
function parseSelected(value: string | undefined): string[] {
  if (!value) return [];
  const raw = value.startsWith('in:') ? value.slice(3) : value;
  return raw.split(',').map((s) => s.trim()).filter(Boolean);
}

/**
 * Multi-select faceted filter for a DataGrid column. Emits the backend's `in:` operator so several
 * values union (e.g. `in:Paid,Shipped`); a single value still uses `in:` (the backend treats a
 * one-element list like eq). Clearing emits null. Pairs with `grid.setFilter(field, next)`.
 *
 * Built on the command palette primitive so a long list (e.g. every actor in the audit "Who" facet)
 * is searchable; the search box only appears past {@link SEARCHABLE_THRESHOLD} options. The popover
 * stays open while you toggle, like the column-visibility menu.
 */
export function FacetFilter({ label, options, value, onChange, className }: FacetFilterProps) {
  const { t } = useTranslation();
  const selected = parseSelected(value);
  const selectedSet = new Set(selected);
  const searchable = options.length > SEARCHABLE_THRESHOLD;

  const toggle = (optionValue: string) => {
    const next = selectedSet.has(optionValue)
      ? selected.filter((v) => v !== optionValue)
      : [...selected, optionValue];
    onChange(next.length === 0 ? null : `in:${next.join(',')}`);
  };

  const summary =
    selected.length === 0
      ? t('grid.filterAll')
      : selected.length === 1
        ? (options.find((o) => o.value === selected[0])?.label ?? selected[0])
        : t('grid.filterCount', { count: selected.length });

  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button variant="outline" size="sm" className={cn('gap-1.5', className)}>
          <span className="text-muted-foreground">{label}:</span>
          <span className="max-w-32 truncate font-medium">{summary}</span>
          <ChevronDown className="size-3.5 opacity-60" />
        </Button>
      </PopoverTrigger>
      <PopoverContent align="start" className="w-56 p-0">
        <Command>
          {searchable && <CommandInput placeholder={t('grid.filterSearch')} />}
          <CommandList>
            <CommandEmpty>{t('grid.filterNoMatch')}</CommandEmpty>
            <CommandGroup>
              {options.map((option) => {
                const isSelected = selectedSet.has(option.value);
                return (
                  <CommandItem
                    key={option.value}
                    value={`${option.value} ${option.label}`}
                    onSelect={() => toggle(option.value)}
                    className="gap-2 px-2 py-1.5"
                  >
                    <span
                      className={cn(
                        'border-primary flex size-4 items-center justify-center rounded-sm border',
                        isSelected ? 'bg-primary text-primary-foreground' : 'opacity-50',
                      )}
                    >
                      {isSelected && <Check className="size-3.5" />}
                    </span>
                    <span className="truncate">{option.label}</span>
                  </CommandItem>
                );
              })}
            </CommandGroup>
          </CommandList>
          {selected.length > 0 && (
            <div className="border-t p-1">
              <Button variant="ghost" size="sm" className="w-full justify-center" onClick={() => onChange(null)}>
                {t('grid.filterClear')}
              </Button>
            </div>
          )}
        </Command>
      </PopoverContent>
    </Popover>
  );
}
