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
  let selectFiles: (chosen: readonly File[]) => void;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    httpMock = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    fixture = TestBed.createComponent(ImportPage);
    await settle(fixture);

    selectFiles = emulateNativeFilePicker(fileInput());
  });

  function emulateNativeFilePicker(input: HTMLInputElement): (chosen: readonly File[]) => void {
    let chosen: readonly File[] = [];
    let value = '';

    Object.defineProperty(input, 'files', { configurable: true, get: () => chosen });
    Object.defineProperty(input, 'value', {
      configurable: true,
      get: () => value,
      set: (next: string) => {
        value = next;

        if (next === '') {
          chosen = [];
        }
      },
    });

    return (files: readonly File[]) => {
      chosen = files;
      value = files.length > 0 ? `C:\\fakepath\\${files[0].name}` : '';
    };
  }

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

  function fileInput(): HTMLInputElement {
    return element<HTMLInputElement>('#bundle-file');
  }

  async function chooseFile(name = 'bundle-acceptance-defects.json'): Promise<void> {
    const input = fileInput();
    const before = input.value;

    selectFiles([
      new File([new TextEncoder().encode('{"resourceType":"Bundle"}')], name, {
        type: 'application/fhir+json',
      }),
    ]);

    if (input.value !== before) {
      input.dispatchEvent(new Event('change'));
    }

    await settle(fixture);
  }

  async function clearFile(): Promise<void> {
    element<HTMLButtonElement>('.file.selected button').click();
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

  describe('clearing the selection', () => {
    it('shows the selected file on the native input before it is cleared', async () => {
      await chooseFile();

      expect(fileInput().value).toContain('bundle-acceptance-defects.json');
      expect(fileInput().files).toHaveLength(1);
    });

    it('empties the native file input as well as the signal', async () => {
      await chooseFile();
      await clearFile();

      expect(fileInput().value).toBe('');
      expect(fileInput().files).toHaveLength(0);
      expect(text()).toContain('No file selected');
      expect(submitButton().disabled).toBe(true);
    });

    it('accepts the same file name again after clearing', async () => {
      await chooseFile();
      await clearFile();
      await chooseFile();

      expect(text()).toContain('bundle-acceptance-defects.json');
      expect(submitButton().disabled).toBe(false);
    });

    it('drops a previous failure when the selection is cleared', async () => {
      await chooseFile();
      await submit();

      importRequest().error(new ProgressEvent('error'), {
        status: 500,
        statusText: 'Server Error',
      });
      await settle(fixture);

      await clearFile();

      expect(text()).not.toContain('FHIR Bundle import failed');
    });
  });

  describe('metadata the API refuses', () => {
    const overLongName = `${'n'.repeat(501)}.json`;

    it('caps the source system label at the length the API records', () => {
      expect(element<HTMLInputElement>('#source-system-label').maxLength).toBe(200);
    });

    it('refuses to post a file name longer than the API records', async () => {
      await chooseFile(overLongName);

      expect(text()).toContain('the API records at most 500');
      expect(submitButton().disabled).toBe(true);
    });

    it('posts nothing while the file name is refused', async () => {
      await chooseFile(overLongName);
      await submit();

      httpMock.verify();
    });

    it('enables import again once an acceptable file is chosen', async () => {
      await chooseFile(overLongName);
      await chooseFile();

      expect(submitButton().disabled).toBe(false);
      expect(text()).not.toContain('the API records at most 500');
    });
  });
});
