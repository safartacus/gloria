import { ApiError } from '../api/client.ts';

export type ErrorKind = 'expected' | 'session' | 'offline' | 'unexpected';

export interface FriendlyError {
  kind: ErrorKind;
  title: string;
  message: string;
  /** Correlation id to quote to IT — only set for unexpected (server) errors. */
  reference?: string;
}

const CONTACT_IT = 'Sorun devam ederse BT departmanı ile iletişime geçin';

/**
 * Turns any thrown value into a message we are willing to show a user.
 * - business / validation errors (400, 403, 409, 422) keep the API's specific text
 * - 401 → "session ended"
 * - network failure → "server unreachable"
 * - 500 / everything else → a generic line + a reference code, never the raw internal error
 */
export function toFriendlyError(error: unknown): FriendlyError {
  if (error instanceof ApiError) {
    if (error.status === 401) {
      return {
        kind: 'session',
        title: 'Oturum sona erdi',
        message: error.detail || 'Lütfen yeniden giriş yapın.',
      };
    }

    if ([400, 403, 409, 422].includes(error.status)) {
      return {
        kind: 'expected',
        title: 'İşlem tamamlanamadı',
        message: error.detail,
      };
    }

    if (error.status === 404) {
      return { kind: 'expected', title: 'Bulunamadı', message: error.detail || 'Kayıt bulunamadı.' };
    }

    // 5xx and anything unmapped: hide internals
    return {
      kind: 'unexpected',
      title: 'Beklenmeyen bir hata oluştu',
      message: `İşleminiz tamamlanamadı. ${CONTACT_IT}.`,
      reference: error.correlationId,
    };
  }

  // fetch() rejects with a TypeError when the server can't be reached
  if (error instanceof TypeError) {
    return {
      kind: 'offline',
      title: 'Sunucuya ulaşılamadı',
      message: `Bağlantınızı kontrol edip tekrar deneyin. ${CONTACT_IT}.`,
    };
  }

  return {
    kind: 'unexpected',
    title: 'Beklenmeyen bir hata oluştu',
    message: `İşleminiz tamamlanamadı. ${CONTACT_IT}.`,
  };
}

/** One-line version for toasts / inline form errors (appends the reference code when present). */
export function friendlyMessage(error: unknown): string {
  const f = toFriendlyError(error);
  return f.reference ? `${f.message} (Referans: ${f.reference})` : f.message;
}
