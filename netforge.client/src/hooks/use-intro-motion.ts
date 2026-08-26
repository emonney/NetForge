import { useState } from 'react';

/**
 * The first-load choreography's timing, in one place so it can be tuned as a whole.
 *
 * The phases are what stop this reading as a slow network. A single uniform stagger across everything
 * *is* what content-arriving looks like — items appearing one by one, no structure. Splitting it into
 * movements with deliberate handoffs (the board's headline row enters under the rail's tail; the rest
 * joins at that row's midpoint) is what makes it legible as something authored. The movements overlap
 * on purpose — within a phase and between them — so several things are always in flight and no region
 * of the screen is ever sitting empty waiting its turn. A queue reads as loading; a wave reads as
 * motion.
 */
export const INTRO = {
  /** Phase 1 — the rail, from the first shell paint. */
  nav: { start: 0, stagger: 52, duration: 520 },
  /**
   * Phase 2 — the board's headline row, entering under the rail's tail rather than after it.
   *
   * Waiting for the rail to finish outright left the board empty for about a second: the skeleton had
   * gone, the widgets had mounted, and they were sitting at opacity 0 waiting their turn. Content that
   * is ready but withheld is exactly what slow loading looks like — the overlap is what keeps a region
   * of the screen from ever being dead.
   */
  lead: { start: 620, stagger: 140, duration: 520 },
  /** Phase 3 — everything below, joining at the headline row's midpoint. */
  rest: { start: 1120, stagger: 70, duration: 520 },
} as const;

const played = new Set<string>();
/** When the choreography began — the first shell paint, not module load or sign-in. */
let anchor: number | null = null;

/**
 * Gates entrance choreography to **once per session, per key**.
 *
 * The distinction this enforces is the whole reason the entrance animations are safe to have. A build-in
 * is a greeting: expressive is fine when you see it once after signing in. The same motion replayed on
 * every navigation — which is what happens if you tie it to component mount, since the cache makes
 * revisits instant — turns into a tax on every click, and the app that felt crafted starts feeling slow.
 *
 * So: rare moments can afford motion, frequent ones can't. Page transitions are frequent and stay
 * nearly invisible; this covers the rare ones.
 *
 * The Angular twin is the `IntroMotion` service in `core/util/intro-motion.ts`.
 */
export function useIntroMotion(key: string): boolean {
  // Claimed in the initializer so it settles on first render and survives re-renders, and so
  // StrictMode's double-invoke can't hand the same key out twice.
  const [claimed] = useState(() => {
    if (played.has(key)) return false;
    played.add(key);
    anchor ??= performance.now();
    return true;
  });
  return claimed;
}

/**
 * The delay an element should use, in ms, measured from the start of the whole choreography rather
 * than from its own mount.
 *
 * Phases have to share one clock or they drift: the dashboard mounts later than the rail, and a
 * plain CSS delay starts counting when the element gets the animation. A board that appeared 400ms
 * late would push its whole phase 400ms late and the handoff would come apart. Anchoring to the first
 * paint means a late arrival simply enters further into its phase, which is what keeps the timing
 * fixed no matter how the data lands.
 */
export function introDelay(phase: { start: number; stagger: number }, index: number): number {
  const target = phase.start + index * phase.stagger;
  const elapsed = anchor === null ? 0 : performance.now() - anchor;
  return Math.max(0, target - elapsed);
}
