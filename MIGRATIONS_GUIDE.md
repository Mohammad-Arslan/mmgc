# Database Migrations - Visual Studio

## Apply Migrations (Visual Studio)

1. Open **Package Manager Console**: `Tools` → `NuGet Package Manager` → `Package Manager Console`
2. Set **Default project** to `MMGC`
3. Run:
   ```
   Update-Database
   ```

## Why Migrations May Fail

**Duplicate column error**: The `AddTestimonialsAndContactMessages` migration previously tried to add `RowVersion` to `Appointments`, but that column was already added by an earlier migration (`Phase2_AddProcedureRequestNotificationAndDocumentModels`). This has been fixed.

**If you already ran CreateTablesManually.sql**: The manual script inserts the migration into `__EFMigrationsHistory`, so EF will skip it. Use `Update-Database` normally—it will apply any other pending migrations.

## Add New Migration

When you change models (add/remove properties, new entities):

```
Add-Migration YourMigrationName
Update-Database
```

## Command Line (Alternative)

```bash
dotnet ef database update
dotnet ef migrations add YourMigrationName
```

## Notes

- Migrations run automatically on application startup
- Ensure `appsettings.json` has correct `DefaultConnection` string
- First run creates the database and all tables
