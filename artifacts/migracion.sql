IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE TABLE [base_currencies] (
        [id] uniqueidentifier NOT NULL,
        [iso_code] nvarchar(10) NOT NULL,
        [name] nvarchar(120) NOT NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_dbo_base_currencies] PRIMARY KEY ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE TABLE [countries] (
        [id] uniqueidentifier NOT NULL,
        [code] nvarchar(10) NOT NULL,
        [name] nvarchar(120) NOT NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_dbo_countries] PRIMARY KEY ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE TABLE [document_types] (
        [id] uniqueidentifier NOT NULL,
        [code] nvarchar(20) NOT NULL,
        [name] nvarchar(120) NOT NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_dbo_document_types] PRIMARY KEY ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE TABLE [permissions] (
        [id] uniqueidentifier NOT NULL,
        [permission_key] nvarchar(100) NOT NULL,
        [description] nvarchar(255) NOT NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [seed_version] nvarchar(20) NOT NULL,
        [created_at_utc] datetime2 NOT NULL,
        [updated_at_utc] datetime2 NOT NULL,
        CONSTRAINT [PK_dbo_permissions] PRIMARY KEY ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE TABLE [roles] (
        [id] uniqueidentifier NOT NULL,
        [code] nvarchar(50) NOT NULL,
        [name] nvarchar(120) NOT NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [is_base_role] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_at_utc] datetime2 NOT NULL,
        [updated_at_utc] datetime2 NOT NULL,
        CONSTRAINT [PK_dbo_roles] PRIMARY KEY ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE TABLE [user_signatures] (
        [id] uniqueidentifier NOT NULL,
        [user_id] uniqueidentifier NOT NULL,
        [signature_name] nvarchar(150) NOT NULL,
        [signature_image_base64] nvarchar(max) NOT NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [updated_at_utc] datetime2 NOT NULL,
        CONSTRAINT [PK_dbo_user_signatures] PRIMARY KEY ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE TABLE [departments] (
        [id] uniqueidentifier NOT NULL,
        [country_id] uniqueidentifier NOT NULL,
        [code] nvarchar(10) NOT NULL,
        [name] nvarchar(120) NOT NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_dbo_departments] PRIMARY KEY ([id]),
        CONSTRAINT [FK_dbo_departments_countries_country_id] FOREIGN KEY ([country_id]) REFERENCES [countries] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE TABLE [role_permissions] (
        [id] uniqueidentifier NOT NULL,
        [role_id] uniqueidentifier NOT NULL,
        [permission_id] uniqueidentifier NOT NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [assigned_at_utc] datetime2 NOT NULL,
        [revoked_at_utc] datetime2 NULL,
        [seed_version] nvarchar(20) NULL,
        CONSTRAINT [PK_dbo_role_permissions] PRIMARY KEY ([id]),
        CONSTRAINT [FK_dbo_role_permissions_permissions_permission_id] FOREIGN KEY ([permission_id]) REFERENCES [permissions] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_role_permissions_roles_role_id] FOREIGN KEY ([role_id]) REFERENCES [roles] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE TABLE [municipalities] (
        [id] uniqueidentifier NOT NULL,
        [department_id] uniqueidentifier NOT NULL,
        [code] nvarchar(10) NOT NULL,
        [name] nvarchar(120) NOT NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_dbo_municipalities] PRIMARY KEY ([id]),
        CONSTRAINT [FK_dbo_municipalities_departments_department_id] FOREIGN KEY ([department_id]) REFERENCES [departments] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE TABLE [companies] (
        [id] uniqueidentifier NOT NULL,
        [business_name] nvarchar(255) NOT NULL,
        [tax_id] nvarchar(20) NOT NULL,
        [company_type] nvarchar(20) NULL,
        [territorial_level] nvarchar(50) NULL,
        [country_id] uniqueidentifier NULL,
        [department_id] uniqueidentifier NULL,
        [municipality_id] uniqueidentifier NULL,
        [representative_mode] nvarchar(20) NULL,
        [representative_user_id] uniqueidentifier NULL,
        [representative_full_name] nvarchar(200) NULL,
        [representative_document_type_id] uniqueidentifier NULL,
        [representative_document_number] nvarchar(50) NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_by_user_id] uniqueidentifier NULL,
        [created_at] datetime2 NOT NULL,
        [last_modified_by_user_id] uniqueidentifier NULL,
        [last_modified_at] datetime2 NULL,
        CONSTRAINT [PK_dbo_companies] PRIMARY KEY ([id]),
        CONSTRAINT [FK_dbo_companies_countries_country_id] FOREIGN KEY ([country_id]) REFERENCES [countries] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_companies_departments_department_id] FOREIGN KEY ([department_id]) REFERENCES [departments] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_companies_document_types_representative_document_type_id] FOREIGN KEY ([representative_document_type_id]) REFERENCES [document_types] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_companies_municipalities_municipality_id] FOREIGN KEY ([municipality_id]) REFERENCES [municipalities] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE TABLE [users] (
        [id] uniqueidentifier NOT NULL,
        [company_id] uniqueidentifier NULL,
        [role_id] uniqueidentifier NOT NULL,
        [email] nvarchar(150) NOT NULL,
        [username] nvarchar(100) NOT NULL,
        [password_hash] nvarchar(255) NOT NULL,
        [account_enabled] bit NOT NULL DEFAULT CAST(1 AS bit),
        [account_locked] bit NOT NULL DEFAULT CAST(0 AS bit),
        [failed_login_attempts] int NOT NULL DEFAULT 0,
        [last_login_at_utc] datetime2 NULL,
        [created_at_utc] datetime2 NOT NULL,
        [updated_at_utc] datetime2 NOT NULL,
        [first_name] nvarchar(100) NOT NULL,
        [middle_name] nvarchar(100) NULL,
        [last_name] nvarchar(100) NOT NULL,
        [second_last_name] nvarchar(100) NULL,
        [document_type] nvarchar(20) NOT NULL,
        [document_number] nvarchar(50) NOT NULL,
        [birth_date] date NOT NULL,
        [country_id] uniqueidentifier NULL,
        [department_id] uniqueidentifier NULL,
        [municipality_id] uniqueidentifier NULL,
        [professional_card] nvarchar(80) NULL,
        [professional_card_issued_at] date NULL,
        [professional_card_file_path] nvarchar(255) NULL,
        CONSTRAINT [PK_dbo_users] PRIMARY KEY ([id]),
        CONSTRAINT [FK_dbo_users_companies_company_id] FOREIGN KEY ([company_id]) REFERENCES [companies] ([id]) ON DELETE SET NULL,
        CONSTRAINT [FK_dbo_users_countries_country_id] FOREIGN KEY ([country_id]) REFERENCES [countries] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_users_departments_department_id] FOREIGN KEY ([department_id]) REFERENCES [departments] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_users_municipalities_municipality_id] FOREIGN KEY ([municipality_id]) REFERENCES [municipalities] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_users_roles_role_id] FOREIGN KEY ([role_id]) REFERENCES [roles] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE TABLE [password_recovery_grants] (
        [id] uniqueidentifier NOT NULL,
        [user_id] uniqueidentifier NOT NULL,
        [enabled_by_user_id] uniqueidentifier NOT NULL,
        [reason] nvarchar(255) NOT NULL,
        [enabled_at_utc] datetime2 NOT NULL,
        [expires_at_utc] datetime2 NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [consumed_at_utc] datetime2 NULL,
        CONSTRAINT [PK_dbo_password_recovery_grants] PRIMARY KEY ([id]),
        CONSTRAINT [FK_dbo_password_recovery_grants_users_enabled_by_user_id] FOREIGN KEY ([enabled_by_user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_password_recovery_grants_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE TABLE [password_recovery_tokens] (
        [id] uniqueidentifier NOT NULL,
        [user_id] uniqueidentifier NOT NULL,
        [token_hash] nvarchar(64) NOT NULL,
        [expires_at_utc] datetime2 NOT NULL,
        [consumed_at_utc] datetime2 NULL,
        [created_at_utc] datetime2 NOT NULL,
        CONSTRAINT [PK_dbo_password_recovery_tokens] PRIMARY KEY ([id]),
        CONSTRAINT [FK_dbo_password_recovery_tokens_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE TABLE [projects] (
        [id] uniqueidentifier NOT NULL,
        [company_id] uniqueidentifier NOT NULL,
        [code] nvarchar(50) NULL,
        [name] nvarchar(255) NOT NULL,
        [description] nvarchar(1000) NULL,
        [objective] nvarchar(1000) NULL,
        [country_id] uniqueidentifier NOT NULL,
        [department_id] uniqueidentifier NOT NULL,
        [municipality_id] uniqueidentifier NOT NULL,
        [status] nvarchar(50) NOT NULL,
        [is_public] bit NOT NULL DEFAULT CAST(0 AS bit),
        [director_owner_user_id] uniqueidentifier NOT NULL,
        [created_by_user_id] uniqueidentifier NOT NULL,
        [last_modified_by_user_id] uniqueidentifier NULL,
        [last_modified_at_utc] datetime2 NULL,
        [created_at_utc] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [updated_at_utc] datetime2 NOT NULL,
        CONSTRAINT [PK_dbo_projects] PRIMARY KEY ([id]),
        CONSTRAINT [FK_dbo_projects_companies_company_id] FOREIGN KEY ([company_id]) REFERENCES [companies] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_projects_countries_country_id] FOREIGN KEY ([country_id]) REFERENCES [countries] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_projects_departments_department_id] FOREIGN KEY ([department_id]) REFERENCES [departments] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_projects_municipalities_municipality_id] FOREIGN KEY ([municipality_id]) REFERENCES [municipalities] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_projects_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_projects_users_director_owner_user_id] FOREIGN KEY ([director_owner_user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_projects_users_last_modified_by_user_id] FOREIGN KEY ([last_modified_by_user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE TABLE [security_audit_events] (
        [id] uniqueidentifier NOT NULL,
        [actor_user_id] uniqueidentifier NULL,
        [company_id] uniqueidentifier NULL,
        [event_type] nvarchar(50) NOT NULL,
        [permission_key] nvarchar(100) NULL,
        [target_type] nvarchar(50) NULL,
        [target_id] uniqueidentifier NULL,
        [reason_code] nvarchar(80) NULL,
        [result] nvarchar(20) NOT NULL,
        [occurred_at_utc] datetime2 NOT NULL,
        [metadata_json] nvarchar(max) NULL,
        CONSTRAINT [PK_dbo_security_audit_events] PRIMARY KEY ([id]),
        CONSTRAINT [FK_dbo_security_audit_events_users_actor_user_id] FOREIGN KEY ([actor_user_id]) REFERENCES [users] ([id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE TABLE [project_configurations] (
        [project_id] uniqueidentifier NOT NULL,
        [base_currency_id] uniqueidentifier NOT NULL,
        [budget_limit] decimal(18,2) NULL,
        [aiu_admin_percent] decimal(5,2) NULL,
        [aiu_unforeseen_percent] decimal(5,2) NULL,
        [aiu_profit_percent] decimal(5,2) NULL,
        [signature_mode] nvarchar(50) NOT NULL,
        [signer_user_id] uniqueidentifier NULL,
        [requires_auditor] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_dbo_project_configurations] PRIMARY KEY ([project_id]),
        CONSTRAINT [FK_dbo_project_configurations_base_currencies_base_currency_id] FOREIGN KEY ([base_currency_id]) REFERENCES [base_currencies] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_project_configurations_projects_project_id] FOREIGN KEY ([project_id]) REFERENCES [projects] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_dbo_project_configurations_users_signer_user_id] FOREIGN KEY ([signer_user_id]) REFERENCES [users] ([id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE TABLE [project_members] (
        [id] uniqueidentifier NOT NULL,
        [project_id] uniqueidentifier NOT NULL,
        [user_id] uniqueidentifier NOT NULL,
        [role_in_project] nvarchar(50) NOT NULL,
        [assigned_by_user_id] uniqueidentifier NOT NULL,
        [assigned_at_utc] datetime2 NOT NULL,
        CONSTRAINT [PK_dbo_project_members] PRIMARY KEY ([id]),
        CONSTRAINT [FK_dbo_project_members_projects_project_id] FOREIGN KEY ([project_id]) REFERENCES [projects] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_project_members_users_assigned_by_user_id] FOREIGN KEY ([assigned_by_user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_project_members_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE TABLE [project_status_transitions] (
        [id] uniqueidentifier NOT NULL,
        [project_id] uniqueidentifier NOT NULL,
        [previous_status] nvarchar(50) NOT NULL,
        [new_status] nvarchar(50) NOT NULL,
        [changed_by_user_id] uniqueidentifier NOT NULL,
        [changed_at_utc] datetime2 NOT NULL,
        [reason] nvarchar(500) NULL,
        [transition_kind] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_dbo_project_status_transitions] PRIMARY KEY ([id]),
        CONSTRAINT [FK_dbo_project_status_transitions_projects_project_id] FOREIGN KEY ([project_id]) REFERENCES [projects] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_project_status_transitions_users_changed_by_user_id] FOREIGN KEY ([changed_by_user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE TABLE [project_visibility_transitions] (
        [id] uniqueidentifier NOT NULL,
        [project_id] uniqueidentifier NOT NULL,
        [previous_is_public] bit NOT NULL,
        [new_is_public] bit NOT NULL,
        [changed_by_user_id] uniqueidentifier NOT NULL,
        [changed_at_utc] datetime2 NOT NULL,
        [reason] nvarchar(500) NULL,
        CONSTRAINT [PK_dbo_project_visibility_transitions] PRIMARY KEY ([id]),
        CONSTRAINT [FK_dbo_project_visibility_transitions_projects_project_id] FOREIGN KEY ([project_id]) REFERENCES [projects] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_project_visibility_transitions_users_changed_by_user_id] FOREIGN KEY ([changed_by_user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_dbo_base_currencies_iso_code] ON [base_currencies] ([iso_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_companies_country_id] ON [companies] ([country_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_companies_created_by_user_id] ON [companies] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_companies_department_id] ON [companies] ([department_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_companies_is_active_business_name] ON [companies] ([is_active], [business_name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_companies_last_modified_by_user_id] ON [companies] ([last_modified_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_companies_municipality_id] ON [companies] ([municipality_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_companies_representative_document_type_id] ON [companies] ([representative_document_type_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_companies_representative_user_id] ON [companies] ([representative_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_dbo_companies_tax_id] ON [companies] ([tax_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_dbo_countries_code] ON [countries] ([code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_dbo_departments_country_id_code] ON [departments] ([country_id], [code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_dbo_document_types_code] ON [document_types] ([code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_dbo_municipalities_department_id_code] ON [municipalities] ([department_id], [code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_password_recovery_grants_enabled_by_user_id] ON [password_recovery_grants] ([enabled_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_password_recovery_grants_user_id] ON [password_recovery_grants] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_password_recovery_tokens_user_id] ON [password_recovery_tokens] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_dbo_password_recovery_tokens_token_hash] ON [password_recovery_tokens] ([token_hash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_dbo_permissions_permission_key] ON [permissions] ([permission_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_project_configurations_base_currency_id] ON [project_configurations] ([base_currency_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_project_configurations_signer_user_id] ON [project_configurations] ([signer_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_project_members_assigned_by_user_id] ON [project_members] ([assigned_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_project_members_user_id] ON [project_members] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_dbo_project_members_project_id_user_id] ON [project_members] ([project_id], [user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_project_status_transitions_changed_by_user_id] ON [project_status_transitions] ([changed_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_project_status_transitions_project_id_changed_at_utc] ON [project_status_transitions] ([project_id], [changed_at_utc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_project_visibility_transitions_changed_by_user_id] ON [project_visibility_transitions] ([changed_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_project_visibility_transitions_project_id_changed_at_utc] ON [project_visibility_transitions] ([project_id], [changed_at_utc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_projects_company_id_name] ON [projects] ([company_id], [name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_projects_company_id_status] ON [projects] ([company_id], [status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_projects_country_id] ON [projects] ([country_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_projects_created_by_user_id] ON [projects] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_projects_department_id] ON [projects] ([department_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_projects_director_owner_user_id] ON [projects] ([director_owner_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_projects_last_modified_by_user_id] ON [projects] ([last_modified_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_projects_municipality_id] ON [projects] ([municipality_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_projects_status_is_public] ON [projects] ([status], [is_public]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_role_permissions_permission_id] ON [role_permissions] ([permission_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_dbo_role_permissions_role_id_permission_id] ON [role_permissions] ([role_id], [permission_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_dbo_roles_code] ON [roles] ([code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_security_audit_events_actor_user_id] ON [security_audit_events] ([actor_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_security_audit_events_occurred_at_utc] ON [security_audit_events] ([occurred_at_utc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_dbo_user_signatures_user_id] ON [user_signatures] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_users_company_id_role_id_account_enabled] ON [users] ([company_id], [role_id], [account_enabled]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_users_country_id] ON [users] ([country_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_users_department_id] ON [users] ([department_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_users_document_number] ON [users] ([document_number]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_users_municipality_id] ON [users] ([municipality_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE INDEX [IX_dbo_users_role_id] ON [users] ([role_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_dbo_users_email] ON [users] ([email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_dbo_users_username] ON [users] ([username]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    ALTER TABLE [companies] ADD CONSTRAINT [FK_dbo_companies_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    ALTER TABLE [companies] ADD CONSTRAINT [FK_dbo_companies_users_last_modified_by_user_id] FOREIGN KEY ([last_modified_by_user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    ALTER TABLE [companies] ADD CONSTRAINT [FK_dbo_companies_users_representative_user_id] FOREIGN KEY ([representative_user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300438_e1b_reconciliacion_baseline'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'202610300438_e1b_reconciliacion_baseline', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300527_e1b_email_verification'
)
BEGIN
    CREATE TABLE [email_verification_tokens] (
        [id] uniqueidentifier NOT NULL,
        [user_id] uniqueidentifier NOT NULL,
        [token_hash] nvarchar(64) NOT NULL,
        [expires_at_utc] datetime2 NOT NULL,
        [consumed_at_utc] datetime2 NULL,
        [created_at_utc] datetime2 NOT NULL,
        CONSTRAINT [PK_dbo_email_verification_tokens] PRIMARY KEY ([id]),
        CONSTRAINT [FK_dbo_email_verification_tokens_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300527_e1b_email_verification'
)
BEGIN
    CREATE INDEX [IX_dbo_email_verification_tokens_user_id] ON [email_verification_tokens] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300527_e1b_email_verification'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_dbo_email_verification_tokens_token_hash] ON [email_verification_tokens] ([token_hash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202610300527_e1b_email_verification'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'202610300527_e1b_email_verification', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031201251_e3b_users_company'
)
BEGIN
    ALTER TABLE [users] DROP CONSTRAINT [FK_dbo_users_companies_company_id];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031201251_e3b_users_company'
)
BEGIN
    DROP INDEX [IX_dbo_users_company_id_role_id_account_enabled] ON [users];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031201251_e3b_users_company'
)
BEGIN
    DROP INDEX [IX_dbo_users_role_id] ON [users];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031201251_e3b_users_company'
)
BEGIN
    CREATE TABLE [users_company] (
        [id] uniqueidentifier NOT NULL,
        [user_id] uniqueidentifier NOT NULL,
        [company_id] uniqueidentifier NOT NULL,
        [account_enable] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_at] datetime2 NOT NULL,
        [updated_at] datetime2 NOT NULL,
        CONSTRAINT [PK_dbo_users_company] PRIMARY KEY ([id]),
        CONSTRAINT [FK_dbo_users_company_companies_company_id] FOREIGN KEY ([company_id]) REFERENCES [companies] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_users_company_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031201251_e3b_users_company'
)
BEGIN
    INSERT INTO users_company (id, user_id, company_id, account_enable, created_at, updated_at)
    SELECT NEWID(), u.id, u.company_id, 1, u.updated_at_utc, u.updated_at_utc
    FROM users u
    WHERE u.company_id IS NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031201251_e3b_users_company'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[users]') AND [c].[name] = N'company_id');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [users] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [users] DROP COLUMN [company_id];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031201251_e3b_users_company'
)
BEGIN
    CREATE INDEX [IX_dbo_users_role_id_account_enabled] ON [users] ([role_id], [account_enabled]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031201251_e3b_users_company'
)
BEGIN
    CREATE INDEX [IX_dbo_users_company_company_id_account_enable] ON [users_company] ([company_id], [account_enable]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031201251_e3b_users_company'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_dbo_users_company_user_id] ON [users_company] ([user_id]) WHERE [account_enable] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031201251_e3b_users_company'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031201251_e3b_users_company', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202611040532_resources_add_e3a'
)
BEGIN
    CREATE TABLE [resource_types] (
        [id] uniqueidentifier NOT NULL,
        [code] nvarchar(30) NOT NULL,
        [name] nvarchar(100) NOT NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_dbo_resource_types] PRIMARY KEY ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202611040532_resources_add_e3a'
)
BEGIN
    CREATE TABLE [resources] (
        [id] uniqueidentifier NOT NULL,
        [project_id] uniqueidentifier NOT NULL,
        [resource_type_id] uniqueidentifier NOT NULL,
        [municipality_id] uniqueidentifier NOT NULL,
        [description] nvarchar(255) NOT NULL,
        [unit] nvarchar(20) NULL,
        [unit_price] decimal(18,2) NULL,
        [brand] nvarchar(100) NULL,
        [distributor] nvarchar(150) NULL,
        [user_key] nvarchar(50) NULL,
        [details] nvarchar(max) NULL,
        [contact_number] nvarchar(50) NULL,
        [quotation_status] nvarchar(30) NOT NULL,
        [origin_resource_id] uniqueidentifier NULL,
        [last_modified_by] uniqueidentifier NOT NULL,
        [last_modified_at] datetime2 NOT NULL,
        [row_version] rowversion NOT NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [deleted_at] datetime2 NULL,
        CONSTRAINT [PK_dbo_resources] PRIMARY KEY ([id]),
        CONSTRAINT [CK_resources_quotation_status] CHECK (quotation_status IN ('FALTA_INFORMACION','POR_COTIZAR','COTIZADO')),
        CONSTRAINT [CK_resources_unit_price] CHECK (unit_price IS NULL OR unit_price >= 0),
        CONSTRAINT [FK_dbo_resources_municipalities_municipality_id] FOREIGN KEY ([municipality_id]) REFERENCES [municipalities] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_resources_projects_project_id] FOREIGN KEY ([project_id]) REFERENCES [projects] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_resources_resource_types_resource_type_id] FOREIGN KEY ([resource_type_id]) REFERENCES [resource_types] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_resources_resources_origin_resource_id] FOREIGN KEY ([origin_resource_id]) REFERENCES [resources] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_resources_users_last_modified_by] FOREIGN KEY ([last_modified_by]) REFERENCES [users] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202611040532_resources_add_e3a'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_dbo_resource_types_code] ON [resource_types] ([code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202611040532_resources_add_e3a'
)
BEGIN
    CREATE INDEX [IX_dbo_resources_last_modified_by] ON [resources] ([last_modified_by]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202611040532_resources_add_e3a'
)
BEGIN
    CREATE INDEX [IX_dbo_resources_municipality_id] ON [resources] ([municipality_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202611040532_resources_add_e3a'
)
BEGIN
    CREATE INDEX [IX_dbo_resources_origin_resource_id] ON [resources] ([origin_resource_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202611040532_resources_add_e3a'
)
BEGIN
    CREATE INDEX [IX_dbo_resources_project_id_description] ON [resources] ([project_id], [description]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202611040532_resources_add_e3a'
)
BEGIN
    CREATE INDEX [IX_dbo_resources_project_id_quotation_status] ON [resources] ([project_id], [quotation_status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202611040532_resources_add_e3a'
)
BEGIN
    CREATE INDEX [IX_dbo_resources_project_id_resource_type_id] ON [resources] ([project_id], [resource_type_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202611040532_resources_add_e3a'
)
BEGIN
    CREATE INDEX [IX_dbo_resources_resource_type_id] ON [resources] ([resource_type_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'202611040532_resources_add_e3a'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'202611040532_resources_add_e3a', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113040910_e3a_suppliers'
)
BEGIN
    ALTER TABLE [resources] ADD [supplier_id] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113040910_e3a_suppliers'
)
BEGIN
    CREATE TABLE [suppliers] (
        [id] uniqueidentifier NOT NULL,
        [company_id] uniqueidentifier NOT NULL,
        [business_name] nvarchar(200) NOT NULL,
        [contact_name] nvarchar(150) NULL,
        [contact_phone] nvarchar(30) NULL,
        [email] nvarchar(254) NULL,
        [address] nvarchar(300) NULL,
        [observations] nvarchar(1000) NULL,
        [country_id] uniqueidentifier NULL,
        [department_id] uniqueidentifier NULL,
        [municipality_id] uniqueidentifier NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [row_version] rowversion NOT NULL,
        [created_by] uniqueidentifier NOT NULL,
        [created_at] datetime2 NOT NULL,
        [last_modified_by] uniqueidentifier NOT NULL,
        [last_modified_at] datetime2 NOT NULL,
        CONSTRAINT [PK_dbo_suppliers] PRIMARY KEY ([id]),
        CONSTRAINT [FK_dbo_suppliers_companies_company_id] FOREIGN KEY ([company_id]) REFERENCES [companies] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_suppliers_countries_country_id] FOREIGN KEY ([country_id]) REFERENCES [countries] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_suppliers_departments_department_id] FOREIGN KEY ([department_id]) REFERENCES [departments] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_suppliers_municipalities_municipality_id] FOREIGN KEY ([municipality_id]) REFERENCES [municipalities] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_suppliers_users_created_by] FOREIGN KEY ([created_by]) REFERENCES [users] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_dbo_suppliers_users_last_modified_by] FOREIGN KEY ([last_modified_by]) REFERENCES [users] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113040910_e3a_suppliers'
)
BEGIN
    CREATE INDEX [IX_dbo_resources_supplier_id] ON [resources] ([supplier_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113040910_e3a_suppliers'
)
BEGIN
    CREATE INDEX [IX_dbo_suppliers_company_id_business_name] ON [suppliers] ([company_id], [business_name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113040910_e3a_suppliers'
)
BEGIN
    CREATE INDEX [IX_dbo_suppliers_company_id_country_id] ON [suppliers] ([company_id], [country_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113040910_e3a_suppliers'
)
BEGIN
    CREATE INDEX [IX_dbo_suppliers_company_id_department_id] ON [suppliers] ([company_id], [department_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113040910_e3a_suppliers'
)
BEGIN
    CREATE INDEX [IX_dbo_suppliers_company_id_is_active] ON [suppliers] ([company_id], [is_active]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113040910_e3a_suppliers'
)
BEGIN
    CREATE INDEX [IX_dbo_suppliers_company_id_municipality_id] ON [suppliers] ([company_id], [municipality_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113040910_e3a_suppliers'
)
BEGIN
    CREATE INDEX [IX_dbo_suppliers_country_id] ON [suppliers] ([country_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113040910_e3a_suppliers'
)
BEGIN
    CREATE INDEX [IX_dbo_suppliers_created_by] ON [suppliers] ([created_by]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113040910_e3a_suppliers'
)
BEGIN
    CREATE INDEX [IX_dbo_suppliers_department_id] ON [suppliers] ([department_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113040910_e3a_suppliers'
)
BEGIN
    CREATE INDEX [IX_dbo_suppliers_last_modified_by] ON [suppliers] ([last_modified_by]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113040910_e3a_suppliers'
)
BEGIN
    CREATE INDEX [IX_dbo_suppliers_municipality_id] ON [suppliers] ([municipality_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113040910_e3a_suppliers'
)
BEGIN
    ALTER TABLE [resources] ADD CONSTRAINT [FK_dbo_resources_suppliers_supplier_id] FOREIGN KEY ([supplier_id]) REFERENCES [suppliers] ([id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113040910_e3a_suppliers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261113040910_e3a_suppliers', N'10.0.10');
END;

COMMIT;
GO

