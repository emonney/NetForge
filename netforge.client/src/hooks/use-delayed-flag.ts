import { useEffect, useRef, useState } from 'react';

export interface DelayedFlagOptions {
  /** Wait this long before showing. A load that finishes sooner never paints a skeleton at all. */
  delayMs?: number;
  /** Once shown, stay up at least this long, so the skeleton can't blink in and straight back out. */
  minMs?: number;
}

/**
 * Gates a loading flag so it only reaches the UI when the wait is long enough to be worth showing.
 *
 * A skeleton that appears for 40ms reads as a flicker — it draws the eye, costs a repaint, and makes a
 * fast response feel *less* smooth than showing nothing would have. Below `delayMs` the load is
 * imperceptible and the flag never flips; past it, `minMs` keeps the skeleton on screen long enough to
 * be legible rather than snapping away mid-fade.
 *
 * The Angular twin is `delayedFlag()` in `core/util/delayed-flag.ts` — keep the two in step.
 *
 * ```ts
 * const showSkeleton = useDelayedFlag(query.isLoading);
 * ```
 */
export function useDelayedFlag(active: boolean, options: DelayedFlagOptions = {}): boolean {
  const { delayMs = 150, minMs = 320 } = options;
  const [shown, setShown] = useState(false);
  const shownAt = useRef(0);

  useEffect(() => {
    if (active) {
      if (shown) return;
      const timer = setTimeout(() => {
        shownAt.current = Date.now();
        setShown(true);
      }, delayMs);
      return () => clearTimeout(timer);
    }

    if (!shown) return;
    const remaining = Math.max(0, minMs - (Date.now() - shownAt.current));
    if (remaining === 0) {
      setShown(false);
      return;
    }
    const timer = setTimeout(() => setShown(false), remaining);
    return () => clearTimeout(timer);
  }, [active, shown, delayMs, minMs]);

  return shown;
}
