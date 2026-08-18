import { describe, expect, it } from 'vitest';

import { routes } from './app.routes';

describe('routes', () => {
  it('serves the import page at the root', () => {
    expect(routes[0].path).toBe('');
    expect(routes[0].pathMatch).toBe('full');
  });

  it('serves the inspector at /imports/:importBatchId', () => {
    expect(routes[1].path).toBe('imports/:importBatchId');
  });

  it('redirects unknown routes to the import page', () => {
    const wildcard = routes.find((route) => route.path === '**');

    expect(wildcard?.redirectTo).toBe('');
  });

  it('does not expose an /inspector route', () => {
    expect(routes.some((route) => route.path?.startsWith('inspector'))).toBe(false);
  });
});
