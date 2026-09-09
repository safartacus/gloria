import { useCallback, useState } from 'react';

export function useForm<T extends Record<string, unknown>>(initial: T) {
  const [values, setValues] = useState<T>(initial);
  const [errors, setErrors] = useState<Partial<Record<keyof T, string>>>({});

  const setValue = useCallback(<K extends keyof T>(key: K, value: T[K]) => {
    setValues((v) => ({ ...v, [key]: value }));
    setErrors((e) => (e[key] ? { ...e, [key]: undefined } : e));
  }, []);

  const reset = useCallback((next?: T) => {
    setValues(next ?? initial);
    setErrors({});
  }, [initial]);

  return { values, setValue, setValues, errors, setErrors, reset };
}
