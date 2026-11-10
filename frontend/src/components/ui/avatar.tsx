import { useMemo, useState, type CSSProperties } from 'react';

interface AvatarProps {
  name: string;
  size?: 'small' | 'medium' | 'large' | 'xlarge';
  src?: string | null;
}

const palettes = [
  ['#dceee8', '#2c6e5f'],
  ['#e5e9f5', '#53658d'],
  ['#f2e3da', '#8b5a42'],
  ['#eae3f2', '#705785'],
  ['#e8eddd', '#607044'],
];

export function Avatar({ name, size = 'medium', src }: AvatarProps) {
  const [imageFailed, setImageFailed] = useState(false);
  const initials = name.split(/\s+/).filter(Boolean).slice(0, 2).map((part) => part[0]?.toUpperCase()).join('') || '?';
  const palette = useMemo(() => palettes[[...name].reduce((total, character) => total + character.charCodeAt(0), 0) % palettes.length], [name]);

  return (
    <span className={`avatar avatar--${size}`} aria-label={name} style={{ '--avatar-bg': palette[0], '--avatar-ink': palette[1] } as CSSProperties}>
      {src && !imageFailed ? <img src={src} alt="" onError={() => setImageFailed(true)} /> : <>
        <svg viewBox="0 0 48 48" aria-hidden="true"><circle cx="24" cy="18" r="9" /><path d="M8 47c1-12 7-18 16-18s15 6 16 18" /></svg>
        <b>{initials}</b>
      </>}
    </span>
  );
}
