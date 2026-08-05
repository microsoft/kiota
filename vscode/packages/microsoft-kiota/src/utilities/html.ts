import { randomBytes } from 'node:crypto';

/**
 * Escapes HTML-significant characters in a string so it can be safely
 * interpolated into webview HTML without introducing script/markup injection.
 */
export function escapeHtml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

/**
 * Generates a cryptographically random nonce suitable for use in a
 * webview Content-Security-Policy `script-src` directive.
 */
export function getNonce(): string {
  // 16 bytes => 128-bit nonce
  return randomBytes(16).toString('hex');
}
