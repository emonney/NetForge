import { useEffect } from 'react';

/** Sets the browser tab title for the lifetime of a screen, restoring the prior title on unmount. */
export function useDocumentTitle(title: string) {
  useEffect(() => {
    const previous = document.title;
    document.title = `${title} · NetForge`;
    return () => {
      document.title = previous;
    };
  }, [title]);
}
