/** Short relative time such as "just now", "5 min ago", "3 h ago", "2 d ago", falling back to a date. */
export function timeAgo(isoDate: string, now = Date.now()): string {
  const minutes = Math.floor((now - Date.parse(isoDate)) / 60_000);
  if (minutes < 1) return 'just now';
  if (minutes < 60) return `${minutes} min ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours} h ago`;
  const days = Math.floor(hours / 24);
  if (days < 7) return `${days} d ago`;
  return new Date(isoDate).toLocaleDateString();
}
