import type { ComponentProps, ReactNode } from 'react';
import { useFormContext, type ControllerRenderProps, type FieldPath, type FieldValues } from 'react-hook-form';

import { Input } from '@/components/ui/input';
import {
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';

export interface FieldProps<TValues extends FieldValues>
  extends Omit<ComponentProps<typeof Input>, 'name' | 'children'> {
  name: FieldPath<TValues>;
  label?: ReactNode;
  description?: ReactNode;
  required?: boolean;
  /** Custom control (Select, Switch, Textarea, …). Omit to render a default text `<Input>`. */
  children?: (field: ControllerRenderProps<TValues, FieldPath<TValues>>) => ReactNode;
}

/**
 * Form field primitive (§7.2): label + required marker + control + hint + inline validation message,
 * wired to the RHF form in context — no `control` prop to thread. Renders a text `<Input>` by
 * default; pass a render-child for any other control. Errors come from the resolver and from
 * `useSubmitForm` mapping server `ProblemDetails` onto fields.
 */
export function Field<TValues extends FieldValues = FieldValues>({
  name,
  label,
  description,
  required,
  children,
  ...inputProps
}: FieldProps<TValues>) {
  const { control } = useFormContext<TValues>();

  return (
    <FormField
      control={control}
      name={name}
      render={({ field }) => (
        <FormItem>
          {label && (
            <FormLabel>
              {label}
              {required && <span className="text-destructive"> *</span>}
            </FormLabel>
          )}
          <FormControl>
            {children ? children(field) : <Input {...inputProps} {...field} value={field.value ?? ''} />}
          </FormControl>
          {description && <FormDescription>{description}</FormDescription>}
          <FormMessage />
        </FormItem>
      )}
    />
  );
}
