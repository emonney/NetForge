import { useTranslation } from 'react-i18next';

import { useDocumentTitle } from '@/hooks/use-document-title';
import { SectionLayout } from '@/components/section-layout';
import { ProfileInfoSection } from '@/components/profile/profile-info-section';
import { PreferencesSection } from '@/components/profile/preferences-section';
import { PasswordSection } from '@/components/profile/password-section';
import { meta } from './meta';

export default function ProfilePage() {
  const { t } = useTranslation();
  useDocumentTitle(meta.title);

  return (
    <div className="grid gap-6">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">{t('profile.title')}</h1>
        <p className="text-muted-foreground mt-1">{t('profile.subtitle')}</p>
      </header>

      <SectionLayout
        side="end"
        sections={[
          { id: 'profile', label: t('profile.sections.profile'), content: <ProfileInfoSection /> },
          { id: 'preferences', label: t('profile.sections.preferences'), content: <PreferencesSection /> },
          { id: 'password', label: t('profile.sections.password'), content: <PasswordSection /> },
        ]}
      />
    </div>
  );
}
