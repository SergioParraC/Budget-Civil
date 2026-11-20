import {
  ApuDetail,
  ApuResourceItem,
  compositionFactorsFromDetail
} from './apu.model';

const item = (wastePercentage: number): ApuResourceItem => ({
  lineId: '',
  resourceId: '',
  description: '',
  unit: 'UND',
  unitPrice: 0,
  consumption: 1,
  wastePercentage,
  matrixResourceId: null
});

const detail = (resources: ApuDetail['resources']): ApuDetail => ({
  apuId: 'apu-1',
  description: 'APU',
  unit: 'UND',
  status: 'COMPLETO',
  wbsItem: null,
  resources,
  rowVersion: '',
  lastModifiedBy: '',
  lastModifiedAt: ''
});

describe('compositionFactorsFromDetail', () => {
  it('toma el desperdicio y la herramienta menor guardados por el backend', () => {
    const result = compositionFactorsFromDetail(
      detail([
        { resourceType: 'MATERIAL', items: [item(10)] },
        { resourceType: 'LABOR', items: [item(5)] },
        { resourceType: 'AUXILIARY', items: [item(3)] }
      ])
    );

    expect(result).toEqual({ materialWaste: 10, laborToolsPercent: 5 });
  });

  it('ignora las líneas antiguas en cero y usa la primera con valor', () => {
    const result = compositionFactorsFromDetail(
      detail([
        { resourceType: 'MATERIAL', items: [item(0), item(7)] },
        { resourceType: 'LABOR', items: [item(0), item(4)] }
      ])
    );

    expect(result).toEqual({ materialWaste: 7, laborToolsPercent: 4 });
  });

  it('devuelve cero cuando el APU no tiene líneas de ese tipo', () => {
    const result = compositionFactorsFromDetail(
      detail([{ resourceType: 'AUXILIARY', items: [item(3)] }])
    );

    expect(result).toEqual({ materialWaste: 0, laborToolsPercent: 0 });
  });
});
