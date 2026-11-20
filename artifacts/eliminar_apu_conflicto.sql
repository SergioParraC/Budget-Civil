/*
  Limpieza manual de un ítem del EDT que quedó bloqueado por un conflicto de concurrencia
  (`WBS_CONCURRENCY_CONFLICT`).

  Qué elimina, en este orden y dentro de una transacción:
    1. apu_resources  -> líneas de composición del APU del ítem
    2. wbs_items      -> el ítem del EDT
    3. apus           -> el APU del ítem (y sus aprobaciones, si la tabla existe)
    4. wbs_codes      -> el código del ítem

  Importante: el conflicto no es un dato corrupto. El servidor lo devuelve porque el navegador
  envía el `rowVersion` de `wbs_items` y ya no coincide con la base. Este script sirve para
  borrar el APU nuevo y volver a crearlo desde la interfaz, no para "reparar" versiones.

  Uso:
    1. Ajusta @ProjectId y @ItemCode.
    2. Ejecútalo con @ConfirmDelete = 0 para ver qué se eliminaría.
    3. Cambia @ConfirmDelete = 1 y vuelve a ejecutarlo.

  sqlcmd -S localhost\SQLEXPRESS -d BudgetCivilDb -E -i artifacts\eliminar_apu_conflicto.sql
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

-- ===== Parámetros: ajustar antes de ejecutar =====
DECLARE @ProjectId uniqueidentifier = 'DB0AE140-2AC5-436D-BB67-964163139F5E';
DECLARE @ItemCode varchar(10) = 'dsf';
DECLARE @ConfirmDelete bit = 0; -- 0 = solo previsualiza, 1 = elimina
-- ==================================================

DECLARE @ItemId uniqueidentifier;
DECLARE @ApuId uniqueidentifier;
DECLARE @CodeId uniqueidentifier;

SELECT
    @ItemId = item.id,
    @ApuId = item.apu_id,
    @CodeId = item.code_id
FROM dbo.wbs_items AS item
INNER JOIN dbo.wbs_codes AS code ON code.id = item.code_id
WHERE item.project_id = @ProjectId
  AND code.node_type = 'ITEM'
  AND code.code = @ItemCode;

IF @ItemId IS NULL
BEGIN
    PRINT 'No se encontró el ítem ''' + @ItemCode + ''' en el proyecto ' + CONVERT(varchar(36), @ProjectId) + '. Nada que eliminar.';
    RETURN;
END;

PRINT '--- Datos que se eliminarán ---';

SELECT
    item.id AS item_id,
    code.code,
    item.quantity_work,
    item.last_modified_at,
    item.row_version,
    item.apu_id,
    apu.description AS apu_description,
    apu.status AS apu_status,
    (SELECT COUNT(*) FROM dbo.apu_resources AS line WHERE line.apu_id = item.apu_id) AS apu_lines,
    code.id AS code_id
FROM dbo.wbs_items AS item
INNER JOIN dbo.wbs_codes AS code ON code.id = item.code_id
LEFT JOIN dbo.apus AS apu ON apu.id = item.apu_id
WHERE item.id = @ItemId;

IF @ConfirmDelete = 0
BEGIN
    PRINT 'Modo previsualización: no se eliminó nada. Cambia @ConfirmDelete = 1 para ejecutar el borrado.';
    RETURN;
END;

BEGIN TRANSACTION;

DELETE FROM dbo.apu_resources WHERE apu_id = @ApuId;

DELETE FROM dbo.wbs_items WHERE id = @ItemId;

IF @ApuId IS NOT NULL
BEGIN
    -- La tabla de aprobaciones llega con `E-4b`; se contempla por si ya existe.
    IF OBJECT_ID(N'dbo.apu_approvals', N'U') IS NOT NULL
    BEGIN
        EXEC sp_executesql
            N'DELETE FROM dbo.apu_approvals WHERE apu_id = @apuId;',
            N'@apuId uniqueidentifier',
            @apuId = @ApuId;
    END;

    DELETE FROM dbo.apus WHERE id = @ApuId;
END;

DELETE FROM dbo.wbs_codes WHERE id = @CodeId;

COMMIT TRANSACTION;

PRINT 'Ítem ''' + @ItemCode + ''' eliminado con su APU, sus líneas y su código.';

PRINT '--- Ítems restantes en el proyecto ---';

SELECT
    code.code,
    item.quantity_work,
    item.last_modified_at,
    item.apu_id
FROM dbo.wbs_items AS item
INNER JOIN dbo.wbs_codes AS code ON code.id = item.code_id
WHERE item.project_id = @ProjectId
ORDER BY code.code;
