import * as React from "react"

const MOBILE_BREAKPOINT = 768

// useSyncExternalStore (instead of useEffect + setState) so the value is read straight from the
// media query without a cascading render — and it satisfies the React Compiler lint rules.
export function useIsMobile() {
  const subscribe = React.useCallback((onChange: () => void) => {
    const mql = window.matchMedia(`(max-width: ${MOBILE_BREAKPOINT - 1}px)`)
    mql.addEventListener("change", onChange)
    return () => mql.removeEventListener("change", onChange)
  }, [])

  return React.useSyncExternalStore(
    subscribe,
    () => window.innerWidth < MOBILE_BREAKPOINT,
    () => false,
  )
}

// Same store-backed read for an arbitrary query. Used where two components must agree on one
// threshold (the topbar's overflow ladder): a CSS breakpoint on one side and a JS one on the other
// would drift, and the control would show up twice or not at all.
export function useMediaQuery(query: string) {
  const subscribe = React.useCallback(
    (onChange: () => void) => {
      const mql = window.matchMedia(query)
      mql.addEventListener("change", onChange)
      return () => mql.removeEventListener("change", onChange)
    },
    [query],
  )

  return React.useSyncExternalStore(
    subscribe,
    React.useCallback(() => window.matchMedia(query).matches, [query]),
    () => false,
  )
}
