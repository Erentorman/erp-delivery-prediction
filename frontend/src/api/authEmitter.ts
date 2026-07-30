/**
 * Lightweight auth event bus.
 *
 * Axios interceptors run outside of React's component tree and therefore
 * cannot access hooks such as `useNavigate` directly.
 *
 * When the API returns a 401, the interceptor fires an `unauthorized` event
 * on this emitter. The `AuthProvider` listens for that event and uses
 * React Router's `useNavigate` to redirect — keeping navigation inside
 * the React lifecycle and avoiding hard full-page reloads.
 */
export const authEmitter = new EventTarget();

export const AUTH_EVENTS = {
  UNAUTHORIZED: 'unauthorized',
} as const;
