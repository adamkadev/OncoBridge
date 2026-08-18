import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { ImportCreatedResponse } from '../api';

export const fhirJsonMediaType = 'application/fhir+json';

export const importsUrl = '/api/v1/imports';

@Injectable({ providedIn: 'root' })
export class RawImportClient {
  private readonly http = inject(HttpClient);

  async import(file: File, sourceSystemLabel: string | null): Promise<ImportCreatedResponse> {
    const payload = await file.arrayBuffer();

    let params = new HttpParams().set('fileName', file.name);

    if (sourceSystemLabel !== null && sourceSystemLabel.trim() !== '') {
      params = params.set('sourceSystemLabel', sourceSystemLabel.trim());
    }

    return await firstValueFrom(
      this.http.post<ImportCreatedResponse>(importsUrl, payload, {
        params,
        headers: { 'Content-Type': fhirJsonMediaType },
      }),
    );
  }
}
