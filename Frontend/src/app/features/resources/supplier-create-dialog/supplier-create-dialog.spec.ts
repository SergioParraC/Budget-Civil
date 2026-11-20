import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';

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
      providers: [provideNoopAnimations(), { provide: SupplierService, useValue: { create } }]
    });

    const fixture = TestBed.createComponent(SupplierCreateDialog);
    const component = fixture.componentInstance;
    const emitted: unknown[] = [];
    component.created.subscribe((supplier) => emitted.push(supplier));

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
});
