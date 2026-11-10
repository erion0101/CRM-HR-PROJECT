import { ArrowLeft, ShieldX } from 'lucide-react';
import { Link } from 'react-router';

export function ForbiddenPage() {
  return <div className="full-state"><span><ShieldX size={30} /></span><h1>Access restricted</h1><p>Your account does not have permission to view this area. The API applies the same protection to the underlying data.</p><Link className="button button--primary button--medium" to="/"><ArrowLeft size={16} /> Return to overview</Link></div>;
}

export function NotFoundPage() {
  return <div className="full-state"><strong>404</strong><h1>Page not found</h1><p>The page may have moved, or the address may be incorrect.</p><Link className="button button--primary button--medium" to="/"><ArrowLeft size={16} /> Return to overview</Link></div>;
}
