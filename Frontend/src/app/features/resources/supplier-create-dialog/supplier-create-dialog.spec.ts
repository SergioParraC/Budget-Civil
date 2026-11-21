import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';

import { SupplierSummary } from '../../../core/models/resource.model';
import { SupplierService } from '../../../core/services/supplier.service';
import { SupplierCreateDialog } from './supplier-create-dialog';

describe('SupplierCreateDialog', () => {
  it('crea el proveedor con los datos normalizados y lo emite', () => {
    const create = jasmine.createSpy('create').and.returnValue(
      of({
        supplierId: 's-1',
        businessName: 'ACME',
        contactName: null,
        contactPhone: '3001234567',
        email: null,
        address: null,
        countryId: null,
        countryName: null,
        departmentId: null,
        departmentName: null,
        municipalityId: null,
        municipalityName: null,
        isActive: true
      })
    );

    TestBed.configureTestingModule({
      imports: [SupplierCreateDialog],
      providers: [
        provideNoopAnimations(),
        {
          provide: SupplierService,
          useValue: { create, getById: jasmine.createSpy('getById'), update: jasmine.createSpy('update') }
        }
      ]
    });

    const fixture = TestBed.createComponent(SupplierCreateDialog);
    const component = fixture.componentInstance;
    const emitted: SupplierSummary[] = [];
    component.saved.subscribe((supplier) => emitted.push(supplier));

    component.form.patchValue({ businessName: ' ACME ', contactPhone: '3001234567' });
    component.submit();

    expect(create).toHaveBeenCalledWith({
      businessName: 'ACME',
      contactName: null,
      contactPhone: '3001234567',
      email: null,
      address: null,
      observations: null
    });
    expect(emitted.length).toBe(1);
  });

  it('actualiza el proveedor seleccionado conservando su ubicación y rowVersion', () => {
    const supplier = {
      supplierId: 's-1',
      companyId: 'c-1',
      businessName: 'ACME',
      contactName: 'Ana',
      contactPhone: '3001234567',
      email: 'ana@acme.com',
      address: 'Calle 1',
      observations: 'Nota',
      countryId: 'co-1',
      countryName: 'Colombia',
      departmentId: 'dep-1',
      departmentName: 'Tolima',
      municipalityId: 'mun-1',
      municipalityName: 'Ibagué',
      isActive: true,
      rowVersion: 'AAAA',
      createdBy: 'u-1',
      createdAt: '2026-11-20T00:00:00Z',
      lastModifiedBy: 'u-1',
      lastModifiedAt: '2026-11-20T00:00:00Z'
    };
    const getById = jasmine.createSpy('getById').and.returnValue(of(supplier));
    const update = jasmine.createSpy('update').and.returnValue(of({ ...supplier, businessName: 'ACME SAS' }));
    const create = jasmine.createSpy('create');

    TestBed.configureTestingModule({
      imports: [SupplierCreateDialog],
      providers: [provideNoopAnimations(), { provide: SupplierService, useValue: { create, getById, update } }]
    });

    const fixture = TestBed.createComponent(SupplierCreateDialog);
    const component = fixture.componentInstance;
    const emitted: SupplierSummary[] = [];
    component.saved.subscribe((value) => emitted.push(value));

    fixture.componentRef.setInput('supplierId', 's-1');
    component.prepare();
    component.form.patchValue({ businessName: ' ACME SAS ' });
    component.submit();

    expect(getById).toHaveBeenCalledWith('s-1');
    expect(update).toHaveBeenCalledWith('s-1', {
      businessName: 'ACME SAS',
      contactName: 'Ana',
      contactPhone: '3001234567',
      email: 'ana@acme.com',
      address: 'Calle 1',
      observations: 'Nota',
      countryId: 'co-1',
      departmentId: 'dep-1',
      municipalityId: 'mun-1',
      rowVersion: 'AAAA'
    });
    expect(create).not.toHaveBeenCalled();
    expect(emitted.length).toBe(1);
    expect(emitted[0].businessName).toBe('ACME SAS');
  });
});
