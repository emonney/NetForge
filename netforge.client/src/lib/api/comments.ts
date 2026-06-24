import { api } from './client';

/** A comment on an entity, keyed by (entityType, entityId). `canDelete` is resolved per caller. */
export interface Comment {
  id: number;
  entityType: string;
  entityId: string;
  authorId: string;
  authorName: string;
  authorAvatarUrl: string | null;
  body: string;
  canDelete: boolean;
  createdAt: string;
}

/** A user the composer can @mention; `token` is what gets inserted after the "@". */
export interface MentionableUser {
  id: string;
  name: string;
  token: string;
  avatarUrl: string | null;
}

export const commentsApi = {
  list: (entityType: string, entityId: string) =>
    api.get<Comment[]>(`/comments/${encodeURIComponent(entityType)}/${encodeURIComponent(entityId)}`),
  create: (entityType: string, entityId: string, body: string, url: string) =>
    api.post<Comment>(`/comments/${encodeURIComponent(entityType)}/${encodeURIComponent(entityId)}`, { body, url }),
  remove: (id: number) => api.del<void>(`/comments/${id}`),
  mentionable: (q: string) => api.get<MentionableUser[]>(`/comments/mentionable?q=${encodeURIComponent(q)}`),
};
