import { useEffect, useState } from 'react';

/** Current time in milliseconds, re-rendering every `intervalMs` while `active`. */
export function useNow(active: boolean, intervalMs = 1_000) {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    if (!active) return;
    setNow(Date.now());
    const timer = window.setInterval(() => setNow(Date.now()), intervalMs);
    return () => window.clearInterval(timer);
  }, [active, intervalMs]);
  return now;
}
