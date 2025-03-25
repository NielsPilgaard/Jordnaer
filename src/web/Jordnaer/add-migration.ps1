param(
    [Parameter(Mandatory=$true)]
    [string]$MigrationName
)

try {
    Write-Host "Adding EFCore Migration '$MigrationName'" -ForegroundColor Cyan

    # Run the first command and capture its output
    $output = & dotnet ef migrations add $MigrationName 2>&1

    # Check if the command was successful
    if ($LASTEXITCODE -ne 0) {
		$output | ForEach-Object { Write-Host $_ }
		
        throw "Failed to add migration."
    }

    Write-Host "`nGenerating migration script..." -ForegroundColor Cyan
    $scriptOutput = & dotnet ef migrations script --idempotent --output Migrations/migration_script.sql 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host "`nCreated EFCore Migration script at 'Migrations/migration_script.sql'" -ForegroundColor Green
    } else {
		$scriptOutput | ForEach-Object { Write-Host $_ }
        throw "Migration script generation encountered issues."
    }
}
catch {
    Write-Host "`nAn error occurred:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}
finally {
    Write-Host "`nPress any key to continue..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}
