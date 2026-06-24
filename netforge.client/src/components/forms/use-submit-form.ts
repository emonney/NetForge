import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import type { FieldValues, UseFormReturn } from 'react-hook-form';
import { toast } from 'sonner';

import { applyApiErrorToForm, type FormBannerState } from '@/lib/api/form-errors';

interface UseSubmitFormOptions<TValues extends FieldValues, TInput, TResult> {
  form: UseFormReturn<TValues>;
  mutationFn: (input: TInput) => Promise<TResult>;
  /** Field names that may receive server-side messages; defaults to the form's top-level keys. */
  fields?: readonly string[];
  successMessage?: string;
  onSuccess?: (result: TResult) => void;
  /** Reshape form values into the API payload (trimming, null-coalescing, etc.). */
  transform?: (values: TValues) => TInput;
}

/**
 * Standardizes the submit flow (§7.2): clears the banner, runs the mutation, toasts on success, and
 * routes a `ProblemDetails` error back onto the form — field messages inline, the rest into the
 * banner. Returns `submit` (an RHF-validated handler) plus `isPending` and `banner` for the UI.
 */
export function useSubmitForm<TValues extends FieldValues, TInput = TValues, TResult = unknown>({
  form,
  mutationFn,
  fields,
  successMessage,
  onSuccess,
  transform,
}: UseSubmitFormOptions<TValues, TInput, TResult>) {
  const [banner, setBanner] = useState<FormBannerState | null>(null);

  const mutation = useMutation({
    mutationFn,
    onSuccess: (result) => {
      if (successMessage) toast.success(successMessage);
      onSuccess?.(result);
    },
    onError: (error) =>
      setBanner(applyApiErrorToForm(error, form.setError, fields ?? Object.keys(form.getValues()))),
  });

  const submit = form.handleSubmit((values) => {
    setBanner(null);
    mutation.mutate(transform ? transform(values) : (values as unknown as TInput));
  });

  return { submit, isPending: mutation.isPending, banner, setBanner, mutation };
}
