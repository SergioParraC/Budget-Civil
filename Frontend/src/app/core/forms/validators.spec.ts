import { FormControl } from '@angular/forms';

import { nonNegativeNumber, positiveNumber } from './validators';

describe('validators de cantidad', () => {
  it('nonNegativeNumber acepta cero y rechaza valores negativos', () => {
    expect(nonNegativeNumber(new FormControl(0))).toBeNull();
    expect(nonNegativeNumber(new FormControl('0'))).toBeNull();
    expect(nonNegativeNumber(new FormControl(2.5))).toBeNull();
    expect(nonNegativeNumber(new FormControl(-0.1))).toEqual({ nonNegativeNumber: true });
  });

  it('positiveNumber sigue exigiendo más que cero para el consumo', () => {
    expect(positiveNumber(new FormControl(0))).toEqual({ positiveNumber: true });
    expect(positiveNumber(new FormControl(1))).toBeNull();
  });
});
