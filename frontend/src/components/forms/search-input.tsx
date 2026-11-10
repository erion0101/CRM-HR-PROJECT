import { Search } from 'lucide-react';
import type { InputHTMLAttributes } from 'react';

export function SearchInput({ className = '', ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return (
    <label className={`search-control ${className}`}>
      <Search size={17} aria-hidden="true" />
      <input type="search" {...props} />
    </label>
  );
}
