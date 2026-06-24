import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useSearchParams } from 'react-router';
import type { OnChangeFn, SortingState } from '@tanstack/react-table';

import { api } from '@/lib/api/client';
import type { PagedResult } from '@/lib/api/paged';

/** Filter values are already operator-suffixed for the backend, e.g. `eq:false`, `gte:10`, `in:a,b`. */
export type DataGridFilters = Record<string, string>;

export interface UseDataGridOptions {
  /** Slice list path under /api, e.g. `/users`. */
  endpoint: string;
  /** Base query key; grid state is appended so each distinct view caches separately. */
  queryKey?: readonly unknown[];
  defaultSort?: { id: string; desc?: boolean };
  pageSize?: number;
}

export interface DataGridState {
  page: number;
  pageSize: number;
  sorting: SortingState;
  search: string;
  filters: DataGridFilters;
}

// Keys the grid owns in the URL query string. Anything else there is treated as a column filter.
const RESERVED = new Set(['page', 'pageSize', 'sort', 'search']);

/**
 * Server-state engine behind `<DataGrid>`: owns paging/sort/search/filter, builds the operator-suffix
 * query string the backend speaks, and fetches a `PagedResult<T>` via TanStack Query.
 *
 * The query state lives in the **URL** (the single source of truth) — so a list is shareable/bookmarkable
 * and survives a reload, and there are no sync effects. Only non-default state is written, keeping links
 * clean (`?page=2&status=in:Paid,Shipped`). The grid component owns view-only concerns (row selection,
 * column visibility) and renders against this.
 */
export function useDataGrid<T>(options: UseDataGridOptions) {
  const { endpoint, defaultSort, pageSize: initialPageSize = 20 } = options;
  const [params, setParams] = useSearchParams();

  // --- derive state from the URL ---
  const page = Math.max(1, toInt(params.get('page'), 1));
  const pageSize = toInt(params.get('pageSize'), initialPageSize);
  const sortParam = params.get('sort');
  const sorting: SortingState = sortParam
    ? [{ id: sortParam.split(':')[0], desc: sortParam.split(':')[1] === 'desc' }]
    : defaultSort
      ? [{ id: defaultSort.id, desc: defaultSort.desc ?? false }]
      : [];
  const search = params.get('search') ?? '';
  const filters: DataGridFilters = {};
  for (const [key, value] of params.entries()) if (!RESERVED.has(key) && value) filters[key] = value;

  const isDefaultSort = (s?: { id: string; desc: boolean }) =>
    !!defaultSort && !!s && s.id === defaultSort.id && s.desc === (defaultSort.desc ?? false);

  // --- setters: each rewrites the URL (replace, so we don't spam history) and resets to page 1 ---
  const update = (mutate: (next: URLSearchParams) => void) =>
    setParams(
      (prev) => {
        const next = new URLSearchParams(prev);
        mutate(next);
        return next;
      },
      { replace: true },
    );

  const setPage = (value: number) =>
    update((p) => {
      if (value <= 1) p.delete('page');
      else p.set('page', String(value));
    });

  const setPageSize = (value: number) =>
    update((p) => {
      if (value === initialPageSize) p.delete('pageSize');
      else p.set('pageSize', String(value));
      p.delete('page');
    });

  const setSorting: OnChangeFn<SortingState> = (updater) => {
    const nextSorting = typeof updater === 'function' ? updater(sorting) : updater;
    const s = nextSorting[0];
    update((p) => {
      if (!s || isDefaultSort(s)) p.delete('sort');
      else p.set('sort', `${s.id}:${s.desc ? 'desc' : 'asc'}`);
      p.delete('page');
    });
  };

  const setSearch = (value: string) =>
    update((p) => {
      if (value.trim()) p.set('search', value);
      else p.delete('search');
      p.delete('page');
    });

  const setFilter = (field: string, value: string | null) =>
    update((p) => {
      if (value === null || value === '') p.delete(field);
      else p.set(field, value);
      p.delete('page');
    });

  // Replace the whole query string from a (saved-view) state, or clear it when called with nothing.
  const reset = (state?: Partial<DataGridState>) => {
    const next = new URLSearchParams();
    if (state) {
      if (state.page && state.page > 1) next.set('page', String(state.page));
      if (state.pageSize && state.pageSize !== initialPageSize) next.set('pageSize', String(state.pageSize));
      const s = state.sorting?.[0];
      if (s && !isDefaultSort(s)) next.set('sort', `${s.id}:${s.desc ? 'desc' : 'asc'}`);
      if (state.search?.trim()) next.set('search', state.search);
      for (const [field, value] of Object.entries(state.filters ?? {})) if (value) next.set(field, value);
    }
    setParams(next, { replace: true });
  };

  // --- API query string (always carries page/pageSize, unlike the URL) ---
  const apiQuery = () => {
    const p = new URLSearchParams();
    p.set('page', String(page));
    p.set('pageSize', String(pageSize));
    if (sorting[0]) p.set('sort', `${sorting[0].id}:${sorting[0].desc ? 'desc' : 'asc'}`);
    if (search.trim()) p.set('search', search.trim());
    for (const [field, value] of Object.entries(filters)) p.set(field, value);
    return p.toString();
  };
  const queryString = apiQuery();
  const path = `${endpoint.replace(/\/$/, '')}/?${queryString}`;

  // Same sort/search/filters as the current view (no paging) → a downloadable export URL.
  const exportHref = (format: string) => {
    const p = new URLSearchParams();
    if (sorting[0]) p.set('sort', `${sorting[0].id}:${sorting[0].desc ? 'desc' : 'asc'}`);
    if (search.trim()) p.set('search', search.trim());
    for (const [field, value] of Object.entries(filters)) p.set(field, value);
    p.set('format', format);
    return `/api${endpoint.replace(/\/$/, '')}/export?${p.toString()}`;
  };

  const query = useQuery({
    queryKey: [...(options.queryKey ?? ['data-grid', endpoint]), queryString],
    queryFn: () => api.get<PagedResult<T>>(path),
    placeholderData: keepPreviousData, // keep the current page visible while the next loads
  });

  const activeFilterCount = Object.keys(filters).length + (search.trim() ? 1 : 0);

  return {
    // data
    items: query.data?.items ?? [],
    pageInfo: query.data,
    isLoading: query.isLoading,
    isError: query.isError,
    error: query.error,
    isFetching: query.isFetching,
    refetch: query.refetch,
    endpoint,
    exportHref,
    // state + setters
    page,
    setPage,
    pageSize,
    setPageSize,
    sorting,
    setSorting,
    search,
    setSearch,
    filters,
    setFilter,
    reset,
    activeFilterCount,
    state: { page, pageSize, sorting, search, filters } satisfies DataGridState,
  };
}

function toInt(value: string | null, fallback: number): number {
  const n = value ? parseInt(value, 10) : NaN;
  return Number.isFinite(n) && n > 0 ? n : fallback;
}

export type DataGridApi<T> = ReturnType<typeof useDataGrid<T>>;
