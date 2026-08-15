import type { FieldValues, Path, UseFormSetError } from 'react-hook-form';

import { isApiError } from '@/lib/problem';
import i18n from '@/i18n.config';

export interface FormBannerState {
  message: string | null;
  traceId?: string;
}

/**
 * Routes a ProblemDetails error from a mutation onto the form: field-level messages go inline
 * (via setError), and anything left over — non-field validation, INVALID_CREDENTIALS — becomes the
 * top-of-form banner. Returns the banner state (message + traceId for the fold-out).
 *
 * A 5xx or a non-ProblemDetails failure (network / proxy down) is a server-side problem, not the
 * user's input, so it always yields the neutral server message — never the backend's field errors
 * or a credential-specific line. That keeps "check your email and password" from surfacing when the
 * real cause is the server being unreachable.
 */
export function applyApiErrorToForm<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
  knownFields: readonly string[],
): FormBannerState {
  if (!isApiError(error)) {
    return { message: i18n.t('common.serverError') };
  }

  if (error.status >= 500) {
    return { message: i18n.t('common.serverError'), traceId: error.traceId };
  }

  const fields = error.fieldErrors;
  if (!fields || Object.keys(fields).length === 0) {
    return { message: error.problem.detail ?? error.message, traceId: error.traceId };
  }

  const leftovers: string[] = [];
  for (const [field, messages] of Object.entries(fields)) {
    if (field && knownFields.includes(field)) {
      setError(field as Path<T>, { message: messages.join(' ') });
    } else {
      leftovers.push(...messages);
    }
  }

  return { message: leftovers.length > 0 ? leftovers.join(' ') : null, traceId: error.traceId };
}
