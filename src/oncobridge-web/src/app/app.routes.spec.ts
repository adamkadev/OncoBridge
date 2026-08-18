import { describe, expect, it } from 'vitest';

import { routes } from './app.routes';

describe('routes', () => {
  it('serves the import page at the root', () => {
    expect(routes[0].path).toBe('');
    expect(routes[0].pathMatch).toBe('full');
  });

  it('serves the timeline at /imports/:importBatchId/timeline', () => {
    expect(routes[1].path).toBe('imports/:importBatchId/timeline');
  });

  it('serves the inspector at /imports/:importBatchId', () => {
    expect(routes[2].path).toBe('imports/:importBatchId');
  });

  it('matches the timeline before the inspector so the deeper route wins', () => {
    const timeline = routes.findIndex((route) => route.path === 'imports/:importBatchId/timeline');
    const inspector = routes.findIndex((route) => route.path === 'imports/:importBatchId');

    expect(timeline).toBeLessThan(inspector);
  });

  it('does not expose a timeline detail route of its own', () => {
    expect(routes.some((route) => route.path?.includes('timeline/'))).toBe(false);
  });

  it('redirects unknown routes to the import page', () => {
    const wildcard = routes.find((route) => route.path === '**');

    expect(wildcard?.redirectTo).toBe('');
  });

  it('does not expose an /inspector route', () => {
    expect(routes.some((route) => route.path?.startsWith('inspector'))).toBe(false);
  });
});
