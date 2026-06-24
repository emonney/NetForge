import { Fragment, useRef, useState, type KeyboardEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Loader2, MessagesSquare, Send, Trash2 } from 'lucide-react';
import { toast } from 'sonner';

import { commentsApi, type Comment, type MentionableUser } from '@/lib/api/comments';
import { isApiError } from '@/lib/problem';
import { timeAgo } from '@/lib/format';
import { cn } from '@/lib/utils';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { EmptyState, ErrorState, LoadingSkeleton } from '@/components/data-states';

/**
 * Threaded comments for any entity, keyed by `(entityType, entityId)`. Sits beside the audit timeline on
 * a record's detail page. The composer supports `@mentions` with live autocomplete — mentioned users get
 * a notification linking back here.
 */
export function Comments({ entityType, entityId }: { entityType: string; entityId: string }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const queryKey = ['comments', entityType, entityId];

  const query = useQuery({ queryKey, queryFn: () => commentsApi.list(entityType, entityId) });

  const remove = useMutation({
    mutationFn: (id: number) => commentsApi.remove(id),
    onSuccess: () => {
      toast.success(t('comments.deleted'));
      queryClient.invalidateQueries({ queryKey });
    },
    onError: (error) =>
      toast.error(isApiError(error) ? (error.problem.detail ?? error.message) : t('comments.deleteError')),
  });

  return (
    <div className="grid gap-4">
      <Composer entityType={entityType} entityId={entityId} onPosted={() => queryClient.invalidateQueries({ queryKey })} />

      {query.isLoading ? (
        <LoadingSkeleton rows={3} />
      ) : query.isError ? (
        <ErrorState error={query.error} onRetry={() => query.refetch()} message={t('comments.loadError')} />
      ) : !query.data || query.data.length === 0 ? (
        <EmptyState
          icon={MessagesSquare}
          title={t('comments.emptyTitle')}
          description={t('comments.emptyDesc')}
        />
      ) : (
        <ul className="grid gap-4">
          {query.data.map((comment) => (
            <CommentRow key={comment.id} comment={comment} onDelete={() => remove.mutate(comment.id)} deleting={remove.isPending} />
          ))}
        </ul>
      )}
    </div>
  );
}

function CommentRow({ comment, onDelete, deleting }: { comment: Comment; onDelete: () => void; deleting: boolean }) {
  const { t } = useTranslation();
  return (
    <li className="flex gap-3">
      <Avatar className="size-8 shrink-0">
        {comment.authorAvatarUrl && <AvatarImage src={comment.authorAvatarUrl} alt="" />}
        <AvatarFallback>{initials(comment.authorName)}</AvatarFallback>
      </Avatar>
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <span className="text-sm font-medium">{comment.authorName}</span>
          <span className="text-muted-foreground text-xs" title={new Date(comment.createdAt).toLocaleString()}>
            {timeAgo(comment.createdAt)}
          </span>
          {comment.canDelete && (
            <Button
              variant="ghost"
              size="icon"
              className="text-muted-foreground hover:text-destructive ms-auto size-7"
              onClick={onDelete}
              disabled={deleting}
              aria-label={t('comments.deleteAria')}
            >
              <Trash2 className="size-3.5" />
            </Button>
          )}
        </div>
        <p className="mt-1 text-sm whitespace-pre-wrap break-words">{renderBody(comment.body)}</p>
      </div>
    </li>
  );
}

const MENTION_BEFORE_CARET = /@([\w.-]*)$/;

function Composer({
  entityType,
  entityId,
  onPosted,
}: {
  entityType: string;
  entityId: string;
  onPosted: () => void;
}) {
  const { t } = useTranslation();
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const [value, setValue] = useState('');
  const [mention, setMention] = useState<{ query: string; start: number } | null>(null);
  const [highlight, setHighlight] = useState(0);

  const suggestions = useQuery({
    queryKey: ['comments', 'mentionable', mention?.query ?? ''],
    queryFn: () => commentsApi.mentionable(mention?.query ?? ''),
    enabled: mention !== null,
  });
  const options = mention ? suggestions.data ?? [] : [];

  const post = useMutation({
    mutationFn: (body: string) => commentsApi.create(entityType, entityId, body, window.location.pathname),
    onSuccess: () => {
      setValue('');
      setMention(null);
      onPosted();
    },
    onError: (error) =>
      toast.error(isApiError(error) ? (error.problem.detail ?? error.message) : t('comments.postError')),
  });

  // Detect an in-progress @mention immediately before the caret and open the suggestion list.
  const syncMention = (text: string, caret: number) => {
    const match = text.slice(0, caret).match(MENTION_BEFORE_CARET);
    if (match) {
      setMention({ query: match[1], start: caret - match[0].length });
      setHighlight(0);
    } else {
      setMention(null);
    }
  };

  const onChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setValue(e.target.value);
    syncMention(e.target.value, e.target.selectionStart ?? e.target.value.length);
  };

  const insertMention = (user: MentionableUser) => {
    if (!mention) return;
    const caret = textareaRef.current?.selectionStart ?? value.length;
    const next = `${value.slice(0, mention.start)}@${user.token} ${value.slice(caret)}`;
    setValue(next);
    setMention(null);
    // Restore focus and place the caret right after the inserted mention.
    requestAnimationFrame(() => {
      const pos = mention.start + user.token.length + 2;
      textareaRef.current?.focus();
      textareaRef.current?.setSelectionRange(pos, pos);
    });
  };

  const submit = () => {
    const body = value.trim();
    if (!body || post.isPending) return;
    post.mutate(body);
  };

  const onKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (mention && options.length > 0) {
      if (e.key === 'ArrowDown') {
        e.preventDefault();
        setHighlight((h) => (h + 1) % options.length);
        return;
      }
      if (e.key === 'ArrowUp') {
        e.preventDefault();
        setHighlight((h) => (h - 1 + options.length) % options.length);
        return;
      }
      if (e.key === 'Enter' || e.key === 'Tab') {
        e.preventDefault();
        insertMention(options[highlight]);
        return;
      }
      if (e.key === 'Escape') {
        e.preventDefault();
        setMention(null);
        return;
      }
    }
    // Cmd/Ctrl+Enter submits from anywhere in the textarea.
    if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) {
      e.preventDefault();
      submit();
    }
  };

  return (
    <div className="relative">
      <div className="border-input focus-within:ring-ring/50 grid gap-2 rounded-lg border p-2 focus-within:ring-[3px]">
        <textarea
          ref={textareaRef}
          value={value}
          onChange={onChange}
          onKeyDown={onKeyDown}
          rows={2}
          placeholder={t('comments.placeholder')}
          className="placeholder:text-muted-foreground resize-none bg-transparent px-1 text-sm outline-none"
        />
        <div className="flex items-center justify-between">
          <span className="text-muted-foreground text-xs">{t('comments.sendHint')}</span>
          <Button size="sm" onClick={submit} disabled={!value.trim() || post.isPending}>
            {post.isPending ? <Loader2 className="animate-spin" /> : <Send className="size-4" />}
            {t('comments.post')}
          </Button>
        </div>
      </div>

      {mention && options.length > 0 && (
        <ul className="bg-popover absolute z-10 mt-1 max-h-56 w-64 overflow-y-auto rounded-lg border p-1 shadow-md">
          {options.map((user, i) => (
            <li key={user.id}>
              <button
                type="button"
                // Keep the textarea focused: pick on mousedown before it blurs.
                onMouseDown={(e) => {
                  e.preventDefault();
                  insertMention(user);
                }}
                className={cn(
                  'flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-start text-sm',
                  i === highlight ? 'bg-accent' : 'hover:bg-accent/60',
                )}
              >
                <Avatar className="size-6">
                  {user.avatarUrl && <AvatarImage src={user.avatarUrl} alt="" />}
                  <AvatarFallback className="text-[10px]">{initials(user.name)}</AvatarFallback>
                </Avatar>
                <span className="min-w-0 flex-1 truncate">{user.name}</span>
                <span className="text-muted-foreground truncate text-xs">@{user.token}</span>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

// Bold @mentions inline so they read as references rather than plain text.
function renderBody(body: string) {
  const parts = body.split(/(@[\w.-]+)/g);
  return parts.map((part, i) =>
    part.startsWith('@') ? (
      <span key={i} className="text-primary font-medium">
        {part}
      </span>
    ) : (
      <Fragment key={i}>{part}</Fragment>
    ),
  );
}

function initials(value: string): string {
  const parts = value.trim().split(/\s+/);
  if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
  return value.slice(0, 2).toUpperCase();
}
