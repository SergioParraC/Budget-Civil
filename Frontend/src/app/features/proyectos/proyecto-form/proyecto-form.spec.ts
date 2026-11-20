import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { CatalogService } from '../../../core/services/catalog.service';
import { ProjectService } from '../../../core/services/project.service';
import { ProyectoForm } from './proyecto-form';

const paged = <T>(items: T[]) => ({ items, page: 1, pageSize: 100, totalItems: items.length });

describe('ProyectoForm AIU', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ProyectoForm],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            currentUser: signal({
              userId: 'u-1',
              email: 'a@b.co',
              username: 'ana',
              role: 'Director',
              companyId: 'c-1',
              accountEnabled: true
            })
          }
        },
        {
          provide: ProjectService,
          useValue: { create: () => of({ projectId: 'p-1' }) }
        },
        {
          provide: CatalogService,
          useValue: {
            countries: () => of(paged([])),
            currencies: () =>
              of(paged([{ id: 'cop', isoCode: 'COP', name: 'Peso colombiano' }])),
            departments: () => of(paged([])),
            municipalities: () => of(paged([]))
          }
        }
      ]
    });
  });

  it('identifica cada componente del AIU y limita los porcentajes a 1 decimal', () => {
    const fixture = TestBed.createComponent(ProyectoForm);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement;
    expect(host.querySelector('label[for="adminPercent"]')?.textContent).toContain('Administración');
    expect(host.querySelector('label[for="unforeseenPercent"]')?.textContent).toContain('Imprevistos');
    expect(host.querySelector('label[for="profitPercent"]')?.textContent).toContain('Utilidad');

    const input = host.querySelector('#adminPercent') as HTMLInputElement;
    expect(input).withContext('input de administración').toBeTruthy();

    const decimalSeparator =
      new Intl.NumberFormat().formatToParts(1.5).find((part) => part.type === 'decimal')?.value ?? '.';
    input.value = `12${decimalSeparator}34`;
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(fixture.componentInstance.form.controls.adminPercent.value).toBe(12.3);
  });
});
