import { useEffect, useRef, useState, type ReactNode } from 'react';
import {
  flexRender,
  getCoreRowModel,
  useReactTable,
  type ColumnDef,
  type RowSelectionState,
  type VisibilityState,
} from '@tanstack/react-table';
import { useTranslation } from 'react-i18next';
import {
  ArrowDown,
  ArrowUp,
  ChevronLeft,
  ChevronRight,
  ChevronsUpDown,
  LayoutGrid,
  Rows3,
  Search,
  SlidersHorizontal,
  Star,
  X,
  type LucideIcon,
} from 'lucide-react';

import { cn } from '@/lib/utils';
import type { DataGridApi, DataGridState } from './use-data-grid';
import { EmptyState, ErrorState, LoadingSkeleton } from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';

const PAGE_SIZES = [10, 20, 50, 100];

export interface DataGridProps<T> {
  grid: DataGridApi<T>;
  columns: ColumnDef<T>[];
  /** Stable row id (so selection survives paging/refetch). */
  getRowId: (row: T) => string;
  searchPlaceholder?: string;
  /** Rendered when ≥1 row is selected; receives the selected ids and a clear callback. */
  bulkActions?: (selectedIds: string[], clear: () => void) => ReactNode;
  /** Extra toolbar controls (e.g. faceted filters) shown next to the search box. */
  toolbar?: ReactNode;
  onRowClick?: (row: T) => void;
  empty?: { icon: LucideIcon; title: string; description: string; action?: ReactNode };
  /** Enables localStorage-backed saved views (and the table/card view choice) under this key. */
  viewKey?: string;
  enableColumnHiding?: boolean;
  /** Initial table column visibility, e.g. `{ description: false }` to ship a verbose column hidden. */
  initialColumnVisibility?: VisibilityState;
  /** Shows an Export menu (CSV/Excel/PDF) that downloads the current view from `{endpoint}/export`. */
  exportable?: boolean;
  /**
   * When provided, a Table/Cards toggle appears (and mobile always uses cards). `visibleColumns` is the
   * set of column ids visible in the active view, so a card can show/hide fields with the Columns menu.
   */
  renderCard?: (row: T, visibleColumns: Set<string>) => ReactNode;
  /** The view shown on first visit, before any saved choice (default 'table'). Only applies with renderCard. */
  defaultView?: 'table' | 'cards';
}

export function DataGrid<T>({
  grid,
  columns,
  getRowId,
  searchPlaceholder,
  bulkActions,
  toolbar,
  onRowClick,
  empty,
  viewKey,
  enableColumnHiding = true,
  initialColumnVisibility,
  exportable = false,
  renderCard,
  defaultView = 'table',
}: DataGridProps<T>) {
  const { t } = useTranslation();
  const [rowSelection, setRowSelection] = useState<RowSelectionState>({});

  // Table vs card view (only when renderCard is provided); the choice persists per viewKey, falling back to
  // defaultView on the first visit (before any saved choice).
  const [view, setView] = useState<'table' | 'cards'>(() => {
    if (!renderCard) return 'table';
    const saved = viewKey ? localStorage.getItem(`netforge:grid-view:${viewKey}`) : null;
    return saved === 'cards' || saved === 'table' ? saved : defaultView;
  });
  const setViewMode = (next: 'table' | 'cards') => {
    setView(next);
    if (viewKey) localStorage.setItem(`netforge:grid-view:${viewKey}`, next);
  };

  // Each view remembers its own visible columns. The table starts from the page's defaults (e.g. a
  // verbose column shipped hidden) and seeds from the pinned default view on a bare URL; the card
  // starts with everything shown so its details are rich. The Columns menu and the card both read the
  // active map, so toggling a column updates whichever view you're looking at — and switching views
  // restores that view's own selection.
  const [tableColumnVisibility, setTableColumnVisibility] = useState<VisibilityState>(() => {
    const base = initialColumnVisibility ?? {};
    if (viewKey && isBareUrl()) {
      const def = readDefaultView(viewKey);
      if (def?.state.columnVisibility) return { ...base, ...def.state.columnVisibility };
    }
    return base;
  });
  const [cardColumnVisibility, setCardColumnVisibility] = useState<VisibilityState>({});
  const isCards = view === 'cards' && !!renderCard;
  const columnVisibility = isCards ? cardColumnVisibility : tableColumnVisibility;
  const setColumnVisibility = isCards ? setCardColumnVisibility : setTableColumnVisibility;

  // Apply the pinned default view's query state once on a bare URL. Runs on mount and on remount
  // (navigating away and back), giving "my view persists across navigation"; an explicit shared/
  // filtered link carries params and is left untouched. grid.reset writes the URL (not React state),
  // so this doesn't fight the no-setState-in-effect rule; the matching column visibility is seeded
  // in the table useState initializer above.
  const defaultApplied = useRef(false);
  useEffect(() => {
    if (defaultApplied.current || !viewKey || !isBareUrl()) return;
    defaultApplied.current = true;
    const def = readDefaultView(viewKey);
    if (def?.state) grid.reset(def.state);
    // grid.reset is recreated each render; the ref guard makes this a true once-on-mount effect.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [viewKey]);

  // TanStack Table's hook isn't annotated for the React Compiler lint; its internal state is stable.
  /* eslint-disable react-hooks/incompatible-library */
  const table = useReactTable({
    data: grid.items,
    columns,
    state: { sorting: grid.sorting, rowSelection, columnVisibility },
    manualSorting: true,
    manualPagination: true,
    manualFiltering: true,
    // Server-side sort reads clearer as a 2-state toggle (asc ↔ desc) — no confusing "unsorted"
    // step that silently falls back to the backend default.
    enableSortingRemoval: false,
    enableRowSelection: true,
    getRowId,
    rowCount: grid.pageInfo?.totalItems ?? 0,
    onSortingChange: grid.setSorting,
    onRowSelectionChange: setRowSelection,
    onColumnVisibilityChange: setColumnVisibility,
    getCoreRowModel: getCoreRowModel(),
  });
  /* eslint-enable react-hooks/incompatible-library */

  const selectedIds = Object.keys(rowSelection);
  const clearSelection = () => setRowSelection({});

  const info = grid.pageInfo;
  const dataColumns = table.getAllLeafColumns().filter((c) => c.id !== '__select' && c.getCanHide());
  // Column ids visible in the active view — handed to renderCard so a card can mirror the Columns menu.
  const visibleColumnIds = new Set(table.getVisibleLeafColumns().map((c) => c.id));

  // A responsive card list rendered via renderCard; reused for the card view (all sizes) and as the
  // mobile fallback for the table view (a table can't render on a phone, and a designed card beats the
  // generic label/value rows).
  const cardList = (className: string) =>
    renderCard ? (
      <ul className={cn('grid gap-3', className)}>
        {grid.items.map((item) => (
          <li
            key={getRowId(item)}
            onClick={onRowClick ? () => onRowClick(item) : undefined}
            className={cn(onRowClick && 'cursor-pointer')}
          >
            {renderCard(item, visibleColumnIds)}
          </li>
        ))}
      </ul>
    ) : null;


  // Optional export menu — built in TS so the toolbar JSX needs no build-time conditional. The
  // `if (exportable)` keeps the prop referenced even when export is stripped from the Basic edition.
  const slots: Record<string, ReactNode> = {};
  if (exportable) {
    // The export menu is only built when the Export feature is included in this edition.
  }

  return (
    <div className="space-y-3">
      {/* Toolbar */}
      <div className="flex flex-wrap items-center gap-2">
        <SearchBox
          value={grid.search}
          onChange={grid.setSearch}
          placeholder={searchPlaceholder ?? `${t('common.search')}…`}
        />
        {toolbar}
        <div className="ms-auto flex items-center gap-2">
          {grid.isFetching && !grid.isLoading && (
            <span className="text-muted-foreground text-xs">{t('common.loading')}</span>
          )}
          {renderCard && (
            <div className="flex items-center rounded-md border p-0.5">
              <Button
                variant={view === 'table' ? 'secondary' : 'ghost'}
                size="icon"
                className="size-7"
                onClick={() => setViewMode('table')}
                aria-label={t('grid.tableView')}
              >
                <Rows3 className="size-4" />
              </Button>
              <Button
                variant={view === 'cards' ? 'secondary' : 'ghost'}
                size="icon"
                className="size-7"
                onClick={() => setViewMode('cards')}
                aria-label={t('grid.cardView')}
              >
                <LayoutGrid className="size-4" />
              </Button>
            </div>
          )}
          {viewKey && (
            <SavedViews
              viewKey={viewKey}
              current={{ ...grid.state, columnVisibility: tableColumnVisibility }}
              onApply={(v) => {
                grid.reset(v);
                setTableColumnVisibility(v.columnVisibility ?? initialColumnVisibility ?? {});
                clearSelection();
              }}
            />
          )}
          {slots.exportMenu}
          {enableColumnHiding && dataColumns.length > 0 && (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="outline" size="sm">
                  <SlidersHorizontal className="size-4" />
                  <span className="hidden sm:inline">{t('grid.columns')}</span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-44">
                <DropdownMenuLabel>{t('grid.columns')}</DropdownMenuLabel>
                <DropdownMenuSeparator />
                {dataColumns.map((column) => (
                  <DropdownMenuCheckboxItem
                    key={column.id}
                    checked={column.getIsVisible()}
                    onCheckedChange={(value) => column.toggleVisibility(!!value)}
                    onSelect={(e) => e.preventDefault()}
                  >
                    {labelOf(column.columnDef.meta) ?? column.id}
                  </DropdownMenuCheckboxItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
          )}
        </div>
      </div>

      {/* Bulk action bar */}
      {bulkActions && selectedIds.length > 0 && (
        <div className="bg-accent/60 flex items-center gap-3 rounded-lg border px-3 py-2">
          <span className="text-sm font-medium">{t('grid.selected', { count: selectedIds.length })}</span>
          <div className="flex items-center gap-2">{bulkActions(selectedIds, clearSelection)}</div>
          <Button variant="ghost" size="sm" className="ms-auto" onClick={clearSelection}>
            <X className="size-4" />
            {t('grid.clearSelection')}
          </Button>
        </div>
      )}

      {/* Body */}
      {grid.isLoading ? (
        <div className="rounded-lg border p-4">
          <LoadingSkeleton variant="table" rows={grid.pageSize > 10 ? 8 : grid.pageSize} cols={columns.length} />
        </div>
      ) : grid.isError ? (
        <div className="rounded-lg border">
          <ErrorState error={grid.error} onRetry={() => grid.refetch()} />
        </div>
      ) : grid.items.length === 0 ? (
        <div className="rounded-lg border">
          {empty ? (
            <EmptyState icon={empty.icon} title={empty.title} description={empty.description} action={empty.action} />
          ) : (
            <EmptyState icon={Search} title={t('grid.emptyTitle')} description={t('grid.emptyDesc')} />
          )}
        </div>
      ) : isCards ? (
        // Card grid (responsive) — replaces both the table and the auto mobile cards.
        cardList('grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4')
      ) : (
        <>
          {/* Desktop table */}
          <div className="hidden overflow-hidden rounded-lg border sm:block">
            <table className="w-full text-sm">
              <thead className="bg-muted/40">
                {table.getHeaderGroups().map((hg) => (
                  <tr key={hg.id} className="border-b">
                    {hg.headers.map((header) => {
                      const canSort = header.column.getCanSort();
                      const sorted = header.column.getIsSorted();
                      return (
                        <th
                          key={header.id}
                          className="text-muted-foreground h-10 px-3 text-start align-middle font-medium whitespace-nowrap"
                        >
                          {header.isPlaceholder ? null : canSort ? (
                            <button
                              type="button"
                              onClick={header.column.getToggleSortingHandler()}
                              className="hover:text-foreground -ms-1 inline-flex items-center gap-1 rounded px-1 outline-none focus-visible:ring-[3px]"
                            >
                              {flexRender(header.column.columnDef.header, header.getContext())}
                              {sorted === 'asc' ? (
                                <ArrowUp className="size-3.5" />
                              ) : sorted === 'desc' ? (
                                <ArrowDown className="size-3.5" />
                              ) : (
                                <ChevronsUpDown className="size-3.5 opacity-50" />
                              )}
                            </button>
                          ) : (
                            flexRender(header.column.columnDef.header, header.getContext())
                          )}
                        </th>
                      );
                    })}
                  </tr>
                ))}
              </thead>
              <tbody>
                {table.getRowModel().rows.map((row) => (
                  <tr
                    key={row.id}
                    onClick={onRowClick ? () => onRowClick(row.original) : undefined}
                    className={cn(
                      'border-b last:border-0 transition-colors',
                      row.getIsSelected() ? 'bg-accent/40' : 'hover:bg-muted/40',
                      onRowClick && 'cursor-pointer',
                    )}
                  >
                    {row.getVisibleCells().map((cell) => (
                      <td key={cell.id} className="px-3 py-2.5 align-middle">
                        {flexRender(cell.column.columnDef.cell, cell.getContext())}
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Mobile (table view): identity-led rows. The first data column leads (e.g. logo + name), the
              rest become a compact two-column label/value grid; actions stay top-end. Far clearer than
              squashing the rich first cell into a value column. Card view renders its own cards above. */}
          <ul className="space-y-2.5 sm:hidden">
            {table.getRowModel().rows.map((row) => {
              const cells = row.getVisibleCells();
              const select = cells.find((c) => c.column.id === '__select');
              const actions = cells.find((c) => c.column.id === '__actions');
              const data = cells.filter((c) => c.column.id !== '__select' && c.column.id !== '__actions');
              const [identity, ...rest] = data;
              return (
                <li
                  key={row.id}
                  onClick={onRowClick ? () => onRowClick(row.original) : undefined}
                  className={cn(
                    'flex flex-col gap-2.5 rounded-lg border p-3',
                    row.getIsSelected() && 'ring-primary/40 ring-2',
                    onRowClick && 'cursor-pointer',
                  )}
                >
                  <div className="flex items-start justify-between gap-2">
                    <div className="flex min-w-0 items-start gap-2">
                      {select && (
                        <div onClick={(e) => e.stopPropagation()}>
                          {flexRender(select.column.columnDef.cell, select.getContext())}
                        </div>
                      )}
                      {identity && (
                        <div className="min-w-0">{flexRender(identity.column.columnDef.cell, identity.getContext())}</div>
                      )}
                    </div>
                    {actions && (
                      <div className="-mt-1 -me-1 shrink-0" onClick={(e) => e.stopPropagation()}>
                        {flexRender(actions.column.columnDef.cell, actions.getContext())}
                      </div>
                    )}
                  </div>
                  {rest.length > 0 && (
                    <dl className="grid grid-cols-2 gap-x-4 gap-y-2 border-t pt-2.5">
                      {rest.map((cell) => (
                        <div key={cell.id} className="flex min-w-0 flex-col gap-0.5">
                          <dt className="text-muted-foreground text-xs">
                            {labelOf(cell.column.columnDef.meta) ?? cell.column.id}
                          </dt>
                          <dd className="truncate text-sm">{flexRender(cell.column.columnDef.cell, cell.getContext())}</dd>
                        </div>
                      ))}
                    </dl>
                  )}
                </li>
              );
            })}
          </ul>
        </>
      )}

      {/* Footer / pagination */}
      {info && info.totalItems > 0 && (
        <div className="flex flex-wrap items-center justify-between gap-3 px-1">
          <p className="text-muted-foreground text-sm">
            {t('grid.rangeOf', {
              from: (info.page - 1) * info.pageSize + 1,
              to: Math.min(info.page * info.pageSize, info.totalItems),
              total: info.totalItems,
            })}
          </p>
          <div className="flex items-center gap-2">
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="outline" size="sm">
                  {t('grid.perPage', { n: grid.pageSize })}
                  <ChevronsUpDown className="size-3.5 opacity-60" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                {PAGE_SIZES.map((size) => (
                  <DropdownMenuItem key={size} onClick={() => grid.setPageSize(size)}>
                    {t('grid.perPage', { n: size })}
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
            <span className="text-muted-foreground text-sm tabular-nums">
              {t('grid.pageOf', { page: info.page, total: Math.max(info.totalPages, 1) })}
            </span>
            <Button variant="outline" size="icon" className="size-8" disabled={!info.hasPrev} onClick={() => grid.setPage(info.page - 1)} aria-label={t('grid.prev')}>
              <ChevronLeft className="size-4" />
            </Button>
            <Button variant="outline" size="icon" className="size-8" disabled={!info.hasNext} onClick={() => grid.setPage(info.page + 1)} aria-label={t('grid.next')}>
              <ChevronRight className="size-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}

// --- Search with debounce ---
function SearchBox({ value, onChange, placeholder }: { value: string; onChange: (v: string) => void; placeholder: string }) {
  const [local, setLocal] = useState(value);
  // Adopt an external value change (e.g. applying a saved view) during render — no effect needed.
  const [synced, setSynced] = useState(value);
  if (value !== synced) {
    setSynced(value);
    setLocal(value);
  }

  // Push the debounced value up. onChange fires only inside the timeout (never synchronously), so
  // this doesn't trip the no-setState-in-effect rule; the guard avoids a redundant page reset.
  useEffect(() => {
    const id = setTimeout(() => {
      if (local !== value) onChange(local);
    }, 300);
    return () => clearTimeout(id);
  }, [local, value, onChange]);

  return (
    <div className="relative w-full sm:w-64">
      <Search className="text-muted-foreground pointer-events-none absolute start-2.5 top-1/2 size-4 -translate-y-1/2" />
      <Input
        value={local}
        onChange={(e) => setLocal(e.target.value)}
        placeholder={placeholder}
        className="ps-8"
      />
    </div>
  );
}

// --- Saved views (localStorage) ---
interface SavedView {
  name: string;
  state: Partial<DataGridState> & { columnVisibility?: VisibilityState };
  /** At most one view is the default; it auto-applies when the list opens on a bare URL. */
  isDefault?: boolean;
}

function SavedViews({
  viewKey,
  current,
  onApply,
}: {
  viewKey: string;
  current: SavedView['state'];
  onApply: (state: SavedView['state']) => void;
}) {
  const { t } = useTranslation();
  const storageKey = viewsStorageKey(viewKey);
  const [views, setViews] = useState<SavedView[]>(() => readViews(storageKey));

  const persist = (next: SavedView[]) => {
    setViews(next);
    localStorage.setItem(storageKey, JSON.stringify(next));
  };
  const save = () => {
    const name = window.prompt(t('grid.viewName'))?.trim();
    if (!name) return;
    persist([...views.filter((v) => v.name !== name), { name, state: current }]);
  };
  // Single-select: starring a view clears any other default; starring the current default clears it.
  const toggleDefault = (name: string) =>
    persist(views.map((v) => ({ ...v, isDefault: v.name === name ? !v.isDefault : false })));

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="outline" size="sm">
          {t('grid.views')}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-60">
        <DropdownMenuLabel>{t('grid.savedViews')}</DropdownMenuLabel>
        <DropdownMenuSeparator />
        {views.length === 0 ? (
          <p className="text-muted-foreground px-2 py-1.5 text-xs">{t('grid.noViews')}</p>
        ) : (
          views.map((view) => (
            <div key={view.name} className="flex items-center">
              <Button
                variant="ghost"
                size="icon"
                className={cn('ms-1 size-7', view.isDefault ? 'text-primary' : 'text-muted-foreground')}
                onClick={() => toggleDefault(view.name)}
                aria-label={t('grid.setDefaultView')}
                aria-pressed={!!view.isDefault}
                title={t('grid.setDefaultView')}
              >
                <Star className={cn('size-3.5', view.isDefault && 'fill-current')} />
              </Button>
              <DropdownMenuItem className="flex-1" onClick={() => onApply(view.state)}>
                {view.name}
              </DropdownMenuItem>
              <Button
                variant="ghost"
                size="icon"
                className="text-muted-foreground hover:text-destructive me-1 size-7"
                onClick={() => persist(views.filter((v) => v.name !== view.name))}
                aria-label={t('grid.deleteView')}
              >
                <X className="size-3.5" />
              </Button>
            </div>
          ))
        )}
        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={save}>{t('grid.saveCurrentView')}</DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

const viewsStorageKey = (viewKey: string) => `netforge:grid-views:${viewKey}`;

function readViews(key: string): SavedView[] {
  try {
    const raw = localStorage.getItem(key);
    return raw ? (JSON.parse(raw) as SavedView[]) : [];
  } catch {
    return [];
  }
}

/** The pinned default view for a grid, if one is set. */
function readDefaultView(viewKey: string): SavedView | undefined {
  return readViews(viewsStorageKey(viewKey)).find((v) => v.isDefault);
}

/** True when the page opened with no grid query string — so applying a default view is safe. */
function isBareUrl(): boolean {
  return typeof window !== 'undefined' && window.location.search.replace(/^\?/, '') === '';
}

function labelOf(meta: unknown): string | undefined {
  return meta && typeof meta === 'object' && 'label' in meta ? String((meta as { label: unknown }).label) : undefined;
}
