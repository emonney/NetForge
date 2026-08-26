import type * as React from 'react';

import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';

type UserAvatarProps = Omit<React.ComponentProps<typeof Avatar>, 'children'> & {
  /** Whatever names the person — display name, falling back to email. Drives the initials. */
  name: string;
  avatarUrl?: string | null;
  /** Initials size, when the default doesn't suit the diameter set in `className`. */
  fallbackClassName?: string;
};

/**
 * A person's avatar: the uploaded image when there is one, initials otherwise. Every surface showing
 * a person goes through this — a hand-rolled `<Avatar>` is how the admin user list ended up rendering
 * initials for users who had a photo. The Angular twin is `app-avatar`.
 */
export function UserAvatar({ name, avatarUrl, fallbackClassName, ...props }: UserAvatarProps) {
  return (
    <Avatar {...props}>
      <AvatarImage src={avatarUrl ?? undefined} alt="" />
      <AvatarFallback className={fallbackClassName}>{initials(name)}</AvatarFallback>
    </Avatar>
  );
}

function initials(value: string): string {
  const parts = value.trim().split(/\s+/);
  if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
  return value.slice(0, 2).toUpperCase();
}
