import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { MockInstance, beforeEach, describe, expect, it, vi } from 'vitest';

import { ImportPage } from './import-page';

describe('ImportPage', () => {
  let fixture: ComponentFixture<ImportPage>;
  let httpMock: HttpTestingController;
  let navigate: MockInstance;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    httpMock = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    fixture = TestBed.createComponent(ImportPage);
    await settle(fixture);
  });

  async function settle(target: ComponentFixture<unknown>): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, 0));
    await target.whenStable();
  }

  function element<T extends Element>(selector: string): T {
    const found = fixture.nativeElement.querySelector(selector) as T | null;

    if (!found) {
      throw new Error(`Expected to find '${selector}'.`);
    }

    return found;
  }

  function text(): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  function submitButton(): HTMLButtonElement {
    return element<HTMLButtonElement>('button[type="submit"]');
  }

  async function chooseFile(name = 'bundle-acceptance-defects.json'): Promise<void> {
    const input = element<HTMLInputElement>('#bundle-file');
    const file = new File([new TextEncoder().encode('{"resourceType":"Bundle"}')], name, {
      type: 'application/fhir+json',
    });

    Object.defineProperty(input, 'files', { value: [file], configurable: true });
    input.dispatchEvent(new Event('change'));

    await settle(fixture);
  }

  function importRequest() {
    return httpMock.expectOne((request) => request.url === '/api/v1/imports');
  }

  async function submit(): Promise<void> {
    element('form').dispatchEvent(new Event('submit', { cancelable: true }));
    await settle(fixture);
  }

  it('names the product and states the conformance scope truthfully', () => {
    expect(text()).toContain('OncoBridge');
    expect(text()).toContain('Oncology interoperability and data-quality workbench');
    expect(text()).toContain('OncoBridge conformance checks — a subset of mCODE STU4');
    expect(text()).toContain('Not full mCODE profile validation');
  });

  it('cannot import until a file is selected', () => {
    expect(submitButton().disabled).toBe(true);
    expect(text()).toContain('No file selected');
    expect(text()).toContain('Select a file to enable import');
  });

  it('shows the chosen file name and enables import', async () => {
    await chooseFile();

    expect(text()).toContain('bundle-acceptance-defects.json');
    expect(submitButton().disabled).toBe(false);
  });

  it('says the file is posted verbatim rather than calling stored JSON the raw bytes', () => {
    expect(text()).toContain('posted verbatim as application/fhir+json');
    expect(text()).not.toContain('byte-exact resource');
    expect(text()).not.toContain('raw bytes');
  });

  it('reports progress while the import is running and disables the form', async () => {
    await chooseFile();
    await submit();

    expect(text()).toContain('Importing…');
    expect(submitButton().disabled).toBe(true);
    expect(element<HTMLInputElement>('#bundle-file').disabled).toBe(true);

    importRequest().flush({ importBatchId: 'created' });
    await settle(fixture);
  });

  it('navigates to the inspector for the created import', async () => {
    await chooseFile();
    await submit();

    importRequest().flush({ importBatchId: '9d3f2c18-4b7a-4c51-9f2e-8a1d6b0e5c73' });
    await settle(fixture);

    expect(navigate).toHaveBeenCalledWith(['/imports', '9d3f2c18-4b7a-4c51-9f2e-8a1d6b0e5c73']);
  });

  it('shows the API problem detail when the import is rejected', async () => {
    await chooseFile();
    await submit();

    importRequest().flush(
      {
        title: 'FHIR Bundle import failed',
        status: 400,
        detail: 'Payload is not a FHIR Bundle.',
      },
      { status: 400, statusText: 'Bad Request' },
    );
    await settle(fixture);

    expect(text()).toContain('FHIR Bundle import failed');
    expect(text()).toContain('Payload is not a FHIR Bundle.');
    expect(text()).toContain('POST /api/v1/imports · 400');
    expect(navigate).not.toHaveBeenCalled();
  });

  it('re-enables import after a failure so the file can be posted again', async () => {
    await chooseFile();
    await submit();

    importRequest().error(new ProgressEvent('error'), {
      status: 500,
      statusText: 'Server Error',
    });
    await settle(fixture);

    expect(submitButton().disabled).toBe(false);
    expect(text()).toContain('FHIR Bundle import failed');
  });

  it('does not present a marketing homepage', () => {
    expect(text()).not.toContain('Get started');
    expect(text()).toContain('Synthetic and public data only');
  });

  it('treats an unexpected error shape as a generic import failure', async () => {
    await chooseFile();
    await submit();

    importRequest().error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown' });
    await settle(fixture);

    expect(text()).toContain('FHIR Bundle import failed');
  });
});
