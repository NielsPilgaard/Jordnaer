Create a new EF Core database migration.

$ARGUMENTS is the migration name (required).

## Steps

1. If no migration name is provided in $ARGUMENTS, ask the user for one.
2. Change to the web project directory: `cd src/web/Jordnaer`
3. Run: `dotnet ef migrations add $ARGUMENTS`
4. If successful, generate the idempotent SQL script: `dotnet ef migrations script --idempotent --output Migrations/migration_script.sql`
5. Report the results:
   - List the generated migration files
   - Confirm the SQL script was generated at `Migrations/migration_script.sql`
6. Remind the user: **Do not apply the migration manually** - it is applied automatically in development via `InitializeDatabaseAsync()`, and must be applied manually in production.
