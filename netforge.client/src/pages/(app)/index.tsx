import { type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowUpRight, BookOpen, KeyRound, ShieldCheck, Sparkles } from 'lucide-react';

import { useAuth } from '@/hooks/use-auth';
import { useDocumentTitle } from '@/hooks/use-document-title';

export default function HomePage() {
  const { t } = useTranslation();
  useDocumentTitle(t('nav.home'));
  const { user } = useAuth();
  const firstName = user?.displayName?.split(' ')[0];

  // The customizable widget dashboard is a Pro feature; the lean edition shows a starter placeholder instead.
  const slots: Record<string, ReactNode> = {};

  return (
    <div className="grid gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">
          {firstName ? t('home.welcomeName', { name: firstName }) : t('home.welcome')}
        </h1>
        <p className="text-muted-foreground mt-1">{t('home.subtitle')}</p>
      </div>

      {slots.dashboard ?? <StarterHome />}
    </div>
  );
}

// Placeholder home for editions without the widget dashboard — replace it with your app's real home/dashboard.
function StarterHome() {
  const included = [
    { icon: ShieldCheck, label: 'Auth — register, confirm, reset, profile' },
    { icon: KeyRound, label: 'Roles & permissions (RBAC) + admin UI' },
    { icon: ShieldCheck, label: 'Settings, health checks & rate limiting' },
    { icon: KeyRound, label: 'Theming, dark mode & i18n' },
  ];
  // Upstream NetForge configurator. Assembled at runtime so the project rename (which rewrites the literal
  // "NetForge"/"netforge" → your app's name across the scaffold) leaves this upstream URL intact.
  const configuratorUrl = 'https://net' + 'forge.ebenmonney.com/?edition=pro';
  return (
    <div className="grid gap-4">
      <div className="bg-card rounded-xl border p-5">
        <h2 className="font-semibold">You're running the NetForge starter</h2>
        <p className="text-muted-foreground mt-1 text-sm">
          This is a placeholder — swap it for your app's home. Your starter already ships a polished,
          authenticated foundation:
        </p>
        <ul className="text-muted-foreground mt-3 grid gap-1.5 text-sm sm:grid-cols-2">
          {included.map(({ icon: Icon, label }) => (
            <li key={label} className="flex items-center gap-2">
              <Icon className="size-4 shrink-0 text-emerald-500" />
              {label}
            </li>
          ))}
        </ul>
      </div>

      <div className="grid gap-3 sm:grid-cols-3">
        <StepCard icon={BookOpen} title="Read the docs" desc="USER_GUIDE.md · RECIPES.md" />
        <StepCard icon={KeyRound} title="Manage access" desc="Roles at /admin/roles" href="/admin/roles" />
        <StepCard icon={Sparkles} title="Add a feature" desc="Copy Features/_Template" />
      </div>

      {/* Dev-only Pro teaser — shown only in development, so it never reaches your app's users in production. */}
      {import.meta.env.DEV && (
        <div className="rounded-xl border border-dashed p-5">
          <div className="flex items-center gap-2 text-sm font-semibold">
            <Sparkles className="size-4 text-pink-600 dark:text-pink-400" />
            Available in Pro
          </div>
          <p className="text-muted-foreground mt-1.5 text-sm leading-relaxed">
            You're on the Community edition. Pro adds multi-tenancy, an audit trail, the widget dashboard, outgoing
            webhooks, global ⌘K search, notifications, background jobs, file uploads, export/import, and the
            runtime theme manager.
          </p>
          <a
            href={configuratorUrl}
            target="_blank"
            rel="noreferrer"
            className="text-primary mt-3 inline-flex items-center gap-1 text-sm font-medium hover:underline"
          >
            Upgrade to Pro
            <ArrowUpRight className="size-3.5" />
          </a>
          <p className="text-muted-foreground/70 mt-2 text-xs">Shown only in development — your users won't see it.</p>
        </div>
      )}
    </div>
  );
}

function StepCard({
  icon: Icon,
  title,
  desc,
  href,
}: {
  icon: typeof BookOpen;
  title: string;
  desc: string;
  href?: string;
}) {
  const body = (
    <>
      <Icon className="text-muted-foreground size-5" />
      <div className="mt-2 text-sm font-medium">{title}</div>
      <div className="text-muted-foreground text-xs">{desc}</div>
    </>
  );
  const className = 'bg-card hover:border-primary/50 block rounded-xl border p-4 transition-colors';
  return href ? (
    <a href={href} className={className}>
      {body}
    </a>
  ) : (
    <div className={className}>{body}</div>
  );
}
