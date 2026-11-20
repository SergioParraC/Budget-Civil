import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MessageService } from 'primeng/api';
import { of } from 'rxjs';

import { CatalogService } from '../../../core/services/catalog.service';
import { CompanyService } from '../../../core/services/company.service';
import { UserService } from '../../../core/services/user.service';
import { CompanyFormDialog } from './company-form-dialog';

const paged = <T>(items: T[]) => ({ items, page: 1, pageSize: 100, totalItems: items.length });

describe('CompanyFormDialog', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CompanyFormDialog],
      providers: [
        provideNoopAnimations(),
        MessageService,
        { provide: CompanyService, useValue: { create: () => of({}), update: () => of({}) } },
        {
          provide: CatalogService,
          useValue: {
            countries: () => of(paged([])),
            documentTypes: () => of(paged([{ id: 'dt-1', code: 'CC', name: 'Cedula' }])),
            departments: () => of(paged([])),
            municipalities: () => of(paged([]))
          }
        },
        {
          provide: UserService,
          useValue: { getById: () => of(null), searchRepresentativeCandidates: () => of([]) }
        }
      ]
    });
  });

  it('no reinicia los campos al cambiar la referencia de la compañía mientras el diálogo está abierto', () => {
    const fixture = TestBed.createComponent(CompanyFormDialog);
    fixture.componentRef.setInput('visible', true);
    fixture.detectChanges();
    TestBed.tick();

    const component = fixture.componentInstance;
    component.form.patchValue({ businessName: 'ACME', taxId: '900123', representativeFullName: 'Ana Perez' });

    fixture.componentRef.setInput('company', {
      companyId: 'c-1',
      businessName: '',
      taxId: '',
      companyType: 'PRIVADA',
      territorialLevel: null,
      countryId: 'pais-1',
      departmentId: 'depto-1',
      municipalityId: 'muni-1',
      representativeMode: 'MANUAL_DATA',
      representativeUserId: null,
      representativeFullName: '',
      representativeDocumentTypeId: '',
      representativeDocumentNumber: ''
    });
    fixture.detectChanges();
    TestBed.tick();

    expect(component.form.controls.businessName.value).toBe('ACME');
    expect(component.form.controls.taxId.value).toBe('900123');
    expect(component.form.controls.representativeFullName.value).toBe('Ana Perez');
  });

  it('vuelve a preparar el formulario cuando el diálogo se cierra y se abre de nuevo', () => {
    const fixture = TestBed.createComponent(CompanyFormDialog);
    fixture.componentRef.setInput('visible', true);
    fixture.detectChanges();
    TestBed.tick();

    const component = fixture.componentInstance;
    component.form.patchValue({ businessName: 'ACME' });

    fixture.componentRef.setInput('visible', false);
    fixture.detectChanges();
    TestBed.tick();

    fixture.componentRef.setInput('visible', true);
    fixture.detectChanges();
    TestBed.tick();

    expect(component.form.controls.businessName.value).toBe('');
  });
});
