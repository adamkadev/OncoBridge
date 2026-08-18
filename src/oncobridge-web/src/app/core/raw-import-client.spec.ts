import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ImportCreatedResponse } from '../api';
import { RawImportClient } from './raw-import-client';

const whitespaceHeavyBundle = `{
    "resourceType"   :   "Bundle" ,

    "type" : "collection" ,
    "entry"    : [
        {
            "fullUrl" : "urn:uuid:aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa" ,
            "resource" : {
                "resourceType" : "Patient" ,
                "id"           : "patient-001"
            }
        }
    ]
}
`;

describe('RawImportClient', () => {
  let client: RawImportClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    client = TestBed.inject(RawImportClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  function fileOf(text: string, name = 'bundle-acceptance-defects.json'): [File, Uint8Array] {
    const bytes = new TextEncoder().encode(text);

    return [new File([bytes], name, { type: 'application/fhir+json' }), bytes];
  }

  async function captureRequest() {
    return await vi.waitFor(() =>
      httpMock.expectOne((request) => request.url === '/api/v1/imports'),
    );
  }

  it('posts the exact file bytes without reserializing them', async () => {
    const [file, bytes] = fileOf(whitespaceHeavyBundle);

    const promise = client.import(file, null);
    const request = await captureRequest();

    const sent = new Uint8Array(request.request.body as ArrayBuffer);

    expect(sent.byteLength).toBe(bytes.byteLength);
    expect([...sent]).toEqual([...bytes]);
    expect(new TextDecoder().decode(sent)).toBe(whitespaceHeavyBundle);

    request.flush({ importBatchId: 'created' } satisfies ImportCreatedResponse);
    await promise;
  });

  it('posts as application/fhir+json', async () => {
    const [file] = fileOf(whitespaceHeavyBundle);

    const promise = client.import(file, null);
    const request = await captureRequest();

    expect(request.request.headers.get('Content-Type')).toBe('application/fhir+json');
    expect(request.request.method).toBe('POST');

    request.flush({ importBatchId: 'created' });
    await promise;
  });

  it('sends the file name and a trimmed source system label as query parameters', async () => {
    const [file] = fileOf(whitespaceHeavyBundle, 'batch-7.json');

    const promise = client.import(file, '  registry-a  ');
    const request = await captureRequest();

    expect(request.request.params.get('fileName')).toBe('batch-7.json');
    expect(request.request.params.get('sourceSystemLabel')).toBe('registry-a');

    request.flush({ importBatchId: 'created' });
    await promise;
  });

  it('omits a blank source system label so the API records its own default', async () => {
    const [file] = fileOf(whitespaceHeavyBundle);

    const promise = client.import(file, '   ');
    const request = await captureRequest();

    expect(request.request.params.has('sourceSystemLabel')).toBe(false);

    request.flush({ importBatchId: 'created' });
    await promise;
  });

  it('returns the created import batch id', async () => {
    const [file] = fileOf(whitespaceHeavyBundle);

    const promise = client.import(file, null);
    const request = await captureRequest();

    request.flush({ importBatchId: '9d3f2c18-4b7a-4c51-9f2e-8a1d6b0e5c73' });

    await expect(promise).resolves.toEqual({
      importBatchId: '9d3f2c18-4b7a-4c51-9f2e-8a1d6b0e5c73',
    });
  });
});
