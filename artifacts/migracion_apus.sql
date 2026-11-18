BEGIN TRANSACTION;
DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[wbs_items]') AND [c].[name] = N'quantity_work');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [wbs_items] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [wbs_items] ALTER COLUMN [quantity_work] decimal(18,10) NOT NULL;

ALTER TABLE [project_configurations] ADD [quantity_decimals] int NOT NULL DEFAULT 2;

ALTER TABLE [project_configurations] ADD [rounding_decimals] int NOT NULL DEFAULT 2;

CREATE TABLE [apus] (
    [id] uniqueidentifier NOT NULL,
    [project_id] uniqueidentifier NOT NULL,
    [description] nvarchar(max) NOT NULL,
    [unit] varchar(5) NOT NULL,
    [yield] decimal(18,4) NOT NULL,
    [status] nvarchar(30) NOT NULL,
    [last_modified_by] uniqueidentifier NOT NULL,
    [last_modified_at] datetime2 NOT NULL,
    [row_version] rowversion NOT NULL,
    CONSTRAINT [PK_dbo_apus] PRIMARY KEY ([id]),
    CONSTRAINT [CK_apus_status] CHECK (status IN ('COMPLETO','FALTA_INFORMACION')),
    CONSTRAINT [CK_apus_yield] CHECK (yield > 0),
    CONSTRAINT [FK_dbo_apus_projects_project_id] FOREIGN KEY ([project_id]) REFERENCES [projects] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_dbo_apus_users_last_modified_by] FOREIGN KEY ([last_modified_by]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [matrix_components] (
    [id] uniqueidentifier NOT NULL,
    [matrix_resource_id] uniqueidentifier NOT NULL,
    [component_resource_id] uniqueidentifier NOT NULL,
    [consumption] decimal(18,10) NOT NULL,
    [waste_percentage] decimal(5,2) NOT NULL DEFAULT 0.0,
    CONSTRAINT [PK_dbo_matrix_components] PRIMARY KEY ([id]),
    CONSTRAINT [CK_matrix_components_consumption] CHECK (consumption > 0),
    CONSTRAINT [CK_matrix_components_waste] CHECK (waste_percentage >= 0 AND waste_percentage <= 100),
    CONSTRAINT [FK_dbo_matrix_components_resources_component_resource_id] FOREIGN KEY ([component_resource_id]) REFERENCES [resources] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_dbo_matrix_components_resources_matrix_resource_id] FOREIGN KEY ([matrix_resource_id]) REFERENCES [resources] ([id]) ON DELETE CASCADE
);

CREATE TABLE [apu_resources] (
    [id] uniqueidentifier NOT NULL,
    [apu_id] uniqueidentifier NOT NULL,
    [resource_id] uniqueidentifier NOT NULL,
    [consumption] decimal(18,10) NOT NULL,
    [waste_percentage] decimal(5,2) NOT NULL DEFAULT 0.0,
    CONSTRAINT [PK_dbo_apu_resources] PRIMARY KEY ([id]),
    CONSTRAINT [CK_apu_resources_consumption] CHECK (consumption > 0),
    CONSTRAINT [CK_apu_resources_waste] CHECK (waste_percentage >= 0 AND waste_percentage <= 100),
    CONSTRAINT [FK_dbo_apu_resources_apus_apu_id] FOREIGN KEY ([apu_id]) REFERENCES [apus] ([id]) ON DELETE CASCADE,
    CONSTRAINT [FK_dbo_apu_resources_resources_resource_id] FOREIGN KEY ([resource_id]) REFERENCES [resources] ([id]) ON DELETE NO ACTION
);

ALTER TABLE [project_configurations] ADD CONSTRAINT [CK_project_configurations_quantity_decimals] CHECK (quantity_decimals >= 0 AND quantity_decimals <= 10);

ALTER TABLE [project_configurations] ADD CONSTRAINT [CK_project_configurations_rounding_decimals] CHECK (rounding_decimals >= 0 AND rounding_decimals <= 10);

CREATE INDEX [IX_dbo_apu_resources_apu_id] ON [apu_resources] ([apu_id]);

CREATE INDEX [IX_dbo_apu_resources_resource_id] ON [apu_resources] ([resource_id]);

CREATE INDEX [IX_dbo_apus_last_modified_by] ON [apus] ([last_modified_by]);

CREATE INDEX [IX_dbo_apus_project_id_status] ON [apus] ([project_id], [status]);

CREATE INDEX [IX_dbo_matrix_components_component_resource_id] ON [matrix_components] ([component_resource_id]);

CREATE INDEX [IX_dbo_matrix_components_matrix_resource_id] ON [matrix_components] ([matrix_resource_id]);

ALTER TABLE [wbs_items] ADD CONSTRAINT [FK_dbo_wbs_items_apus_apu_id] FOREIGN KEY ([apu_id]) REFERENCES [apus] ([id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261115092848_apus_add_e3b', N'10.0.10');

COMMIT;
GO

