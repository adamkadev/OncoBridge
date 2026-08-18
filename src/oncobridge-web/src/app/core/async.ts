import { HttpErrorResponse } from '@angular/common/http';

import { ProblemDetails } from '../api';

export interface ApiFailure {
  readonly status: number | null;
  readonly title: string;
  readonly detail: string | null;
}

export type Async<T> =
  | { readonly kind: 'idle' }
  | { readonly kind: 'loading' }
  | { readonly kind: 'loaded'; readonly value: T }
  | { readonly kind: 'failed'; readonly failure: ApiFailure };

export const idle: Async<never> = { kind: 'idle' };

export const loading: Async<never> = { kind: 'loading' };

export function loaded<T>(value: T): Async<T> {
  return { kind: 'loaded', value };
}

export function failed<T>(failure: ApiFailure): Async<T> {
  return { kind: 'failed', failure };
}

export function valueOf<T>(state: Async<T>): T | null {
  return state.kind === 'loaded' ? state.value : null;
}

export function failureOf<T>(state: Async<T>): ApiFailure | null {
  return state.kind === 'failed' ? state.failure : null;
}

export function toApiFailure(error: unknown, fallbackTitle: string): ApiFailure {
  if (!(error instanceof HttpErrorResponse)) {
    return { status: null, title: fallbackTitle, detail: null };
  }

  const problem = problemDetailsOf(error);

  return {
    status: error.status,
    title: problem?.title ?? fallbackTitle,
    detail: problem?.detail ?? null,
  };
}

function problemDetailsOf(error: HttpErrorResponse): ProblemDetails | null {
  const body: unknown = error.error;

  return typeof body === 'object' && body !== null ? (body as ProblemDetails) : null;
}
