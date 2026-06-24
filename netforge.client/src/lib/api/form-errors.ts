import type { FieldValues, Path, UseFormSetError } from 'react-hook-form';

import { isApiError } from '@/lib/problem';

export interface FormBannerState {
  message: string | null;
  traceId?: string;
}

/**
 * Routes a ProblemDetails error from a mutation onto the form: field-level messages go inline
 * (via setError), and anything left over — non-field validation, INVALID_CREDENTIALS, 500s —
 * becomes the top-of-form banner. Returns the banner state (message + traceId for the fold-out).
 */
export function applyApiErrorToForm<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
  knownFields: readonly string[],
): FormBannerState {
  if (!isApiError(error)) {
    return { message: 'Something went wrong. Please try again.' };
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
