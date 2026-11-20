# ROL

Eres un ingeniero de datos experto en Microsoft SQL Server (T-SQL) y en unidades de medida usadas en
presupuestos de obra civil en Colombia (APU, análisis de precios unitarios). Tu única tarea es
escribir un script T-SQL de datos (DML) idempotente que llene el catálogo `dbo.measurement_units` de
la base `BudgetCivilDb`. No escribas código de aplicación ni explicaciones fuera del SQL.

# CONTEXTO TÉCNICO (no lo cambies)

- Motor: SQL Server 2022/2025. Base: `BudgetCivilDb`. Esquema: `dbo`. Tablas y columnas en `snake_case`.
- La tabla YA EXISTE: no ejecutes `CREATE`, `ALTER`, `DROP`, `TRUNCATE` ni `DELETE`.
- Collation de la base: `Modern_Spanish_CI_AS`. El índice único `UQ_dbo_measurement_units_short_unit`
  NO distingue mayúsculas de minúsculas: `M2` y `m2` son la misma sigla y colisionan.
- `short_unit` es `varchar(10)` y `unit` es `varchar(100)` (no unicode, collation española): usa
  `N'...'` en los literales, pero escribe solo caracteres latinos (tildes y ñ permitidas). No uses
  emojis, símbolos como `㎡`, letras griegas ni caracteres de otros alfabetos.
- `id` es `int IDENTITY(1,1)`: NO lo incluyas en el `INSERT` y NO uses `SET IDENTITY_INSERT`.
- Entrega un único archivo con codificación UTF-8 con BOM. El archivo se aplicará con
  `sqlcmd -S localhost\SQLEXPRESS -d BudgetCivilDb -f 65001 -i archivo.sql` (o desde SSMS): sin
  `-f 65001` los acentos y la ñ se corrompen al leer el `.sql`.

# ESQUEMA EXACTO

```sql
CREATE TABLE dbo.measurement_units (
    id         int          NOT NULL IDENTITY(1,1)
                            CONSTRAINT PK_dbo_measurement_units PRIMARY KEY,
    short_unit varchar(10)  NOT NULL,
    unit       varchar(100) NOT NULL,
    CONSTRAINT CK_measurement_units_short_unit_not_empty
        CHECK (LEN(LTRIM(RTRIM(short_unit))) > 0),
    CONSTRAINT CK_measurement_units_unit_not_empty
        CHECK (LEN(LTRIM(RTRIM(unit))) > 0)
);
CREATE UNIQUE INDEX UQ_dbo_measurement_units_short_unit
    ON dbo.measurement_units (short_unit);
```

Reglas de negocio: `short_unit` tiene entre 1 y 10 caracteres y es único sin distinguir mayúsculas;
`unit` tiene entre 1 y 100 caracteres. El catálogo es una lista de sugerencias para APU e insumos (no
es una FK), por eso no debe llenarse con unidades inventadas ni variantes redundantes.

# ALCANCE: UNIDADES PARA OBRA CIVIL

`short_unit` = sigla corta en mayúsculas (1–10 caracteres). `unit` = nombre en español, en singular,
con inicial mayúscula (1–100 caracteres). Sin puntos finales ni espacios sobrantes.

Incluye como mínimo obligatorio estas 48 filas (puedes agregar unidades adicionales solo si son de
uso real en obra civil colombiana y no duplican una existente):

| short_unit | unit |
|---|---|
| `UND` | Unidad |
| `PAR` | Par |
| `PZA` | Pieza |
| `JGO` | Juego |
| `CAJA` | Caja |
| `SACO` | Saco |
| `BULTO` | Bulto |
| `BOLSA` | Bolsa |
| `ROLLO` | Rollo |
| `PLIEGO` | Pliego |
| `KIT` | Kit |
| `CUBETA` | Cubeta |
| `M` | Metro |
| `ML` | Metro lineal |
| `M2` | Metro cuadrado |
| `M3` | Metro cúbico |
| `CM` | Centímetro |
| `CM2` | Centímetro cuadrado |
| `CM3` | Centímetro cúbico |
| `MM` | Milímetro |
| `KM` | Kilómetro |
| `HA` | Hectárea |
| `PUL` | Pulgada |
| `PIE` | Pie |
| `PIE2` | Pie cuadrado |
| `YD3` | Yarda cúbica |
| `KG` | Kilogramo |
| `G` | Gramo |
| `T` | Tonelada |
| `LB` | Libra |
| `L` | Litro |
| `GL` | Galón |
| `BARRIL` | Barril |
| `H` | Hora |
| `HH` | Hora-hombre |
| `HM` | Hora-máquina |
| `DIA` | Día |
| `SEM` | Semana |
| `MES` | Mes |
| `AÑO` | Año |
| `VJE` | Viaje |
| `TUBO` | Tubo |
| `VARILLA` | Varilla |
| `LAMINA` | Lámina |
| `PTO` | Punto |
| `NUDO` | Nudo |
| `SERV` | Servicio |
| `GLOBAL` | Global |

No agregues siglas que solo cambien en mayúsculas/minúsculas (por ejemplo `ML` y `ml`) ni sinónimos
redundantes (`UND` y `UN`, `T` y `TON`, `GL` y `GAL`): el índice único es case-insensitive.

# REGLAS OBLIGATORIAS DEL SCRIPT

1. T-SQL válido para SQL Server; un solo archivo; solo `INSERT` y `SELECT` (más la transacción).
2. **Idempotente**: `INSERT ... SELECT ... WHERE NOT EXISTS (SELECT 1 FROM dbo.measurement_units ...)`
   comparando por `short_unit`. Re-ejecutar el script no debe duplicar filas ni lanzar error.
3. Inserta únicamente `(short_unit, unit)`; el `id` lo genera la columna `IDENTITY`.
4. No actualices ni borres filas existentes (el catálogo puede estar ya referenciado por APU e
   insumos, aunque no exista FK).
5. Envuelve todo en `SET NOCOUNT ON; SET XACT_ABORT ON;` + `BEGIN TRANSACTION` / `COMMIT` con
   `BEGIN TRY ... BEGIN CATCH ... ROLLBACK ... THROW; END CATCH`. Sin `GO` dentro de la transacción.
6. Orden del archivo, con comentarios: a) cabecera (propósito, fecha, criterio de selección);
   b) inserción del catálogo; c) bloque `-- VERIFICACIÓN` solo con `SELECT`.
7. Recorta espacios y respeta los límites 10 / 100 caracteres (los `CHECK` y el índice único
   detendrían el script si se violan).

# PATRÓN SQL EXIGIDO

```sql
INSERT INTO dbo.measurement_units (short_unit, unit)
SELECT v.short_unit, v.unit
FROM (VALUES
    (N'UND', N'Unidad'),
    (N'M2',  N'Metro cuadrado')
) AS v(short_unit, unit)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.measurement_units AS mu WHERE mu.short_unit = v.short_unit);
```

# VERIFICACIÓN OBLIGATORIA (bloque final del script)

Incluye `-- VERIFICACIÓN` con consultas que devuelvan:

1. El catálogo completo ordenado por sigla:
   `SELECT id, short_unit, unit FROM dbo.measurement_units ORDER BY short_unit;`
2. Conteo total (`SELECT COUNT(*) ...`), que debe ser igual al número de filas del bloque `VALUES`
   (48 como mínimo) y ningún duplicado no insertado.
3. Duplicados sin distinguir mayúsculas:
   `SELECT short_unit, COUNT(*) FROM dbo.measurement_units GROUP BY short_unit HAVING COUNT(*) > 1;`
4. Filas inválidas:
   `WHERE LEN(LTRIM(RTRIM(short_unit))) = 0 OR LEN(short_unit) > 10
          OR LEN(LTRIM(RTRIM(unit))) = 0 OR LEN(unit) > 100`
5. Espacios sobrantes:
   `WHERE short_unit <> LTRIM(RTRIM(short_unit)) OR unit <> LTRIM(RTRIM(unit))`
6. Siglas con caracteres no latinos (opcional, informativo):
   `WHERE short_unit LIKE N'%[^A-Z0-9]%'`

# FORMATO DE ENTREGA

- Responde SOLO con el SQL dentro de un bloque ```sql ... ``` (sin texto antes ni después).
- Cierra el archivo con un comentario `-- TOTALES: measurement_units=?` con el número de filas
  insertadas y, si agregaste unidades extra, un comentario `-- ADICIONALES:` con la justificación de
  cada una.

