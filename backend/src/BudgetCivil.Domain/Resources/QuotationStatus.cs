namespace BudgetCivil.Domain.Resources;

/// <summary>
/// Estado de cotización del insumo. `COTIZADO` se declara para el contrato de `E-4a`; `E-3a` solo
/// produce `FALTA_INFORMACION` y `POR_COTIZAR` (`REQ-INSUMOS-04`).
/// </summary>
public enum QuotationStatus
{
    FaltaInformacion = 1,
    PorCotizar = 2,
    Cotizado = 3
}
