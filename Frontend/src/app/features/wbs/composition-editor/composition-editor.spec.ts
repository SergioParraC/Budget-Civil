import { Component, WritableSignal, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';

import { CompositionLine } from '../../../core/models/apu.model';
import { ApuPreviewDefaultsService } from '../../../core/services/apu-preview-defaults.service';
import { ResourceService } from '../../../core/services/resource.service';
import { CompositionEditor } from './composition-editor';

const line = (
  key: string,
  resourceTypeCode: string,
  resourceType: string,
  consumption = 1,
  unitPrice = 10
): CompositionLine => ({
  key,
  lineId: null,
  resourceId: `resource-${key}`,
  description: `Insumo ${key}`,
  unit: 'UND',
  unitPrice,
  resourceType,
  resourceTypeCode,
  matrixResourceId: null,
  consumption,
  wastePercentage: 0
});

@Component({
  standalone: true,
  imports: [CompositionEditor],
  template: `
    <app-composition-editor
      projectId="project-1"
      [(materialWaste)]="materialWaste"
      [(laborToolsPercent)]="laborToolsPercentValue"
    />
  `
})
class CompositionEditorHost {
  readonly materialWaste = signal(0);
  readonly laborToolsPercentValue = signal(0);
}

describe('CompositionEditor', () => {
  let laborToolsPercent: WritableSignal<number>;

  beforeEach(() => {
    laborToolsPercent = signal(0);
    TestBed.configureTestingModule({
      imports: [CompositionEditor],
      providers: [
        provideNoopAnimations(),
        {
          provide: ResourceService,
          useValue: {
            types: () => of([]),
            list: () => of({ items: [], page: 1, pageSize: 10, totalItems: 0 })
          }
        },
        {
          provide: ApuPreviewDefaultsService,
          useValue: {
            laborToolsPercent,
            setLaborToolsPercent: (value: number) => laborToolsPercent.set(value)
          }
        }
      ]
    });
  });

  it('conserva el foco al escribir la cantidad y agrupa los insumos por tipo', () => {
    const fixture = TestBed.createComponent(CompositionEditor);
    fixture.componentRef.setInput('projectId', 'project-1');
    fixture.componentRef.setInput('lines', [
      line('m-1', 'MATERIAL', 'Materiales'),
      line('t-1', 'TOOL', 'Herramienta')
    ]);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.textContent).toContain('Materiales');
    expect(host.textContent).toContain('Herramienta');
    expect(host.querySelectorAll('#materialWaste').length).toBe(1);

    const quantityInputs = Array.from(
      host.querySelectorAll('input[aria-label="Cantidad"]')
    ) as HTMLInputElement[];
    expect(quantityInputs.length).toBe(2);

    const input = quantityInputs[0];
    input.focus();
    input.value = '2';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(document.activeElement).toBe(input);
    expect(host.contains(input)).toBeTrue();
  });

  it('muestra herramienta menor solo para mano de obra y calcula los totales preliminares', () => {
    laborToolsPercent.set(20);
    const fixture = TestBed.createComponent(CompositionEditor);
    fixture.componentRef.setInput('projectId', 'project-1');
    fixture.componentRef.setInput('materialWaste', 10);
    fixture.componentRef.setInput('laborToolsPercent', 20);
    fixture.componentRef.setInput('aiuAdminPercent', 10);
    fixture.componentRef.setInput('aiuUnforeseenPercent', 5);
    fixture.componentRef.setInput('aiuProfitPercent', 5);
    fixture.componentRef.setInput('lines', [
      line('m-1', 'MATERIAL', 'Materiales', 2, 100),
      line('l-1', 'LABOR', 'Mano de obra', 1, 50),
      line('t-1', 'TOOL', 'Herramienta', 3, 10)
    ]);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelectorAll('#laborToolsPercent').length).toBe(1);
    expect(host.textContent).toContain('Herramienta menor %');

    const component = fixture.componentInstance;
    expect(component.lineTotal(component.lines()[0])).toBe(200);
    expect(component.lineTotal(component.lines()[1])).toBe(50);
    expect(component.lineTotal(component.lines()[2])).toBe(30);
    expect(component.groupExtra('MATERIAL')).toEqual({ percentage: 10, unitPrice: 200, total: 20 });
    expect(component.groupExtra('LABOR')).toEqual({ percentage: 20, unitPrice: 50, total: 10 });
    expect(component.groupExtra('TOOL')).toBeNull();
    expect(component.groupSubtotal('MATERIAL')).toBe(220);
    expect(component.groupSubtotal('LABOR')).toBe(60);
    expect(component.groupSubtotal('TOOL')).toBe(30);
    expect(component.directCost()).toBe(310);
    expect(component.indirectCost()).toBe(62);
    expect(component.preliminaryTotal()).toBe(372);

    expect(host.textContent).toContain('Desperdicio de materiales');
    expect(host.textContent).toContain('(10 %)');
    expect(host.textContent).toContain('(20 %)');
    expect(host.textContent).toContain('Subtotal Materiales');
    expect(host.textContent).toContain('Subtotal Mano de obra');
    expect(host.textContent).toContain('Costo directo APU');
    expect(host.textContent).toContain('Costo indirecto APU');
  });

  it('recuerda el porcentaje de herramienta menor para el siguiente APU', () => {
    const fixture = TestBed.createComponent(CompositionEditor);
    fixture.componentRef.setInput('projectId', 'project-1');
    fixture.detectChanges();

    fixture.componentInstance.onLaborToolsChange(15);

    expect(fixture.componentInstance.laborToolsPercent()).toBe(15);
    expect(laborToolsPercent()).toBe(15);
  });

  it('propaga el desperdicio y la herramienta menor al formulario que lo contiene', () => {
    const fixture = TestBed.createComponent(CompositionEditorHost);
    fixture.detectChanges();

    const editor = fixture.debugElement.children[0].componentInstance as CompositionEditor;
    editor.onWasteChange(10);
    editor.onLaborToolsChange(15);
    fixture.detectChanges();

    expect(fixture.componentInstance.materialWaste()).toBe(10);
    expect(fixture.componentInstance.laborToolsPercentValue()).toBe(15);
  });
});
