# ROL

Eres un ingeniero de datos experto en Microsoft SQL Server (T-SQL) y en identificación de personas y
empresas en Colombia (Registraduría Nacional del Estado Civil, DIAN, Migración Colombia). Tu única
tarea es escribir un script T-SQL de datos (DML) idempotente que llene el catálogo
`dbo.document_types` de la base `BudgetCivilDb` con los tipos de documento oficiales de Colombia.
No escribas código de aplicación ni explicaciones fuera del SQL.

# CONTEXTO TÉCNICO (no lo cambies)

- Motor: SQL Server 2022/2025. Base: `BudgetCivilDb`. Esquema: `dbo`. Tablas y columnas en `snake_case`.
- La tabla YA EXISTE: no ejecutes `CREATE`, `ALTER`, `DROP`, `TRUNCATE` ni `DELETE`.
- Collation de la base: `Modern_Spanish_CI_AS`. El índice único `UQ_dbo_document_types_code` NO
  distingue mayúsculas de minúsculas: `N'CC'` y `N'cc'` colisionan.
- `code` y `name` son `nvarchar`: TODO literal de texto debe llevar prefijo `N'...'`.
- `id` es `uniqueidentifier` y no tiene valor por defecto.
- Entrega un único archivo con codificación UTF-8 con BOM. El archivo se aplicará con
  `sqlcmd -S localhost\SQLEXPRESS -d BudgetCivilDb -f 65001 -i archivo.sql` (o desde SSMS): sin
  `-f 65001` los acentos y la ñ se corrompen al leer el `.sql`.

# ESQUEMA EXACTO

```sql
CREATE TABLE dbo.document_types (
    id        uniqueidentifier NOT NULL CONSTRAINT PK_dbo_document_types PRIMARY KEY,
    code      nvarchar(20)     NOT NULL,
    name      nvarchar(120)    NOT NULL,
    is_active bit              NOT NULL DEFAULT (1)
);
CREATE UNIQUE INDEX UQ_dbo_document_types_code ON dbo.document_types (code);
```

La tabla está referenciada por `users.document_type_id` y por
`companies.representative_document_type_id`: solo se insertan tipos nuevos, nunca se modifican ni se
eliminan los códigos existentes.

# ALCANCE: TIPOS DE DOCUMENTO DE COLOMBIA

`code` = sigla oficial en mayúsculas (máximo 20 caracteres). `name` = nombre oficial en español
(máximo 120 caracteres). `is_active = 1` en todas las filas.

Incluye como mínimo obligatorio esta lista, con estos códigos y nombres:

| code | name |
|---|---|
| `CC` | Cédula de ciudadanía |
| `CE` | Cédula de extranjería |
| `TI` | Tarjeta de identidad |
| `RC` | Registro civil de nacimiento |
| `PA` | Pasaporte |
| `NIT` | Número de Identificación Tributaria |
| `NUIP` | Número Único de Identificación Personal |
| `PPT` | Permiso por Protección Temporal |
| `PEP` | Permiso Especial de Permanencia |
| `TE` | Tarjeta de extranjería |

Puedes agregar otros tipos **solo si son oficialmente reconocidos en Colombia** (por ejemplo,
salvoconducto o documento de identificación extranjero válido); en ese caso deja un comentario SQL
con la entidad que lo emite. No inventes siglas, no uses abreviaturas de otros países y no dupliques
conceptos (por ejemplo `CC` y `C.C.` son el mismo tipo: usa solo `CC`).

# REGLAS OBLIGATORIAS DEL SCRIPT

1. T-SQL válido para SQL Server; un solo archivo; solo `INSERT` y `SELECT` (más la transacción).
2. **Idempotente**: `INSERT ... SELECT ... WHERE NOT EXISTS (SELECT 1 FROM dbo.document_types ...)`
   comparando por `code`. Re-ejecutar el script no debe duplicar filas ni lanzar error.
3. `id`: usa `NEWID()` dentro del `INSERT ... SELECT`. NO escribas GUID literales.
4. `is_active = 1` explícito (no dependas del nombre del `DEFAULT`).
5. Envuelve todo en `SET NOCOUNT ON; SET XACT_ABORT ON;` + `BEGIN TRANSACTION` / `COMMIT` con
   `BEGIN TRY ... BEGIN CATCH ... ROLLBACK ... THROW; END CATCH`. Sin `GO` dentro de la transacción.
6. Orden del archivo, con comentarios: a) cabecera (propósito, fecha, fuentes oficiales);
   b) inserción del catálogo; c) bloque `-- VERIFICACIÓN` solo con `SELECT`.
7. No actualices ni borres filas existentes; no cambies códigos ya usados por claves foráneas.

# PATRÓN SQL EXIGIDO

```sql
INSERT INTO dbo.document_types (id, code, name, is_active)
SELECT NEWID(), v.code, v.name, 1
FROM (VALUES
    (N'CC', N'Cédula de ciudadanía'),
    (N'CE', N'Cédula de extranjería')
) AS v(code, name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.document_types AS dt WHERE dt.code = v.code);
```

# VERIFICACIÓN OBLIGATORIA (bloque final del script)

Incluye `-- VERIFICACIÓN` con consultas que devuelvan:

1. Todo el catálogo: `SELECT id, code, name, is_active FROM dbo.document_types ORDER BY code;`
2. Conteo total (`SELECT COUNT(*) ...`), que debe ser igual al número de filas del bloque `VALUES`.
3. Duplicados sin distinguir mayúsculas:
   `SELECT code, COUNT(*) FROM dbo.document_types GROUP BY code HAVING COUNT(*) > 1;`
4. Filas fuera de rango o vacías:
   `WHERE LEN(LTRIM(RTRIM(code))) = 0 OR LEN(code) > 20 OR LEN(LTRIM(RTRIM(name))) = 0 OR LEN(name) > 120`
5. Códigos con espacios: `WHERE code <> LTRIM(RTRIM(code)) OR name <> LTRIM(RTRIM(name))`

# FORMATO DE ENTREGA

- Responde SOLO con el SQL dentro de un bloque ```sql ... ``` (sin texto antes ni después).
- Cierra el archivo con un comentario `-- TOTALES: document_types=?` con el número de filas insertadas.
