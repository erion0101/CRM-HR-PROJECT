interface SkeletonProps {
  className?: string;
  count?: number;
}

export function Skeleton({ className = '', count = 1 }: SkeletonProps) {
  return <>{Array.from({ length: count }, (_, index) => <span className={`skeleton ${className}`} key={index} aria-hidden="true" />)}</>;
}
