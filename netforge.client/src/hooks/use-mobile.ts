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
