import { Outlet } from 'react-router';

// Optional layout wrapping this route's subtree (replaces the Next-style layout.tsx).
// Requires <Outlet /> to render children. Delete if the route needs no layout.
export default function TemplateLayout() {
  return <Outlet />;
}
