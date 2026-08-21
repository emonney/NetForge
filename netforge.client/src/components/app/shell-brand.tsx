import { Brand } from '@/components/brand';

/**
 * The brand lockup in the app shell. White-label: in multi-tenant mode it shows the active tenant's name +
 * logo — the accent already re-tints via the appearance applier. That includes the platform ("default")
 * tenant, which is seeded with the product name and no logo: editing it in the tenant manager rebrands the
 * app's own header, the same way any other tenant does. Single-tenant editions keep the product brand.
 */
export function ShellBrand({ className, markOnly }: { className?: string; markOnly?: boolean }) {
  let name: string | undefined;
  let logoUrl: string | null | undefined;
  return <Brand className={className} markOnly={markOnly} name={name} logoUrl={logoUrl} />;
}

