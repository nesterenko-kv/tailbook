/**
 * Validation utilities for the booking flow.
 * Supports accessible inline field-level error messages.
 */

export type FieldValidation = {
  message: string;
};

export type FieldErrors = {
  fullName?: FieldValidation;
  phone?: FieldValidation;
  instagram?: FieldValidation;
  comments?: FieldValidation;
  consent?: FieldValidation;
  createAccount?: FieldValidation;
  petName?: FieldValidation;
  animalType?: FieldValidation;
  breed?: FieldValidation;
  coatType?: FieldValidation;
  sizeCategory?: FieldValidation;
  weightKg?: FieldValidation;
};

export function getFirstFieldError(errors: FieldErrors): string | null {
  const firstKey = Object.keys(errors).find((key) => errors[key as keyof FieldErrors]?.message);
  return firstKey ? (errors[firstKey as keyof FieldErrors])?.message ?? null : null;
}

interface ValidateInput {
  value: string | undefined | null;
  required?: boolean;
  minLength?: number;
  maxLength?: number;
  pattern?: RegExp;
  label: string;
}

export function validateInput({ value, required, minLength, maxLength, pattern, label }: ValidateInput): FieldValidation | null {
  if (required && !value?.trim()) {
    return { message: `Вкажіть ${label}.` };
  }
  if (value?.trim() && minLength && value.trim().length < minLength) {
    return { message: `${label} має бути не менше ${minLength} символів.` };
  }
  if (value?.trim() && maxLength && value.trim().length > maxLength) {
    return { message: `${label} має бути не більше ${maxLength} символів.` };
  }
  if (value?.trim() && pattern && !pattern.test(value.trim())) {
    return { message: `Введіть коректний ${label.toLowerCase()}.` };
  }
  return null;
}
