# Run this script in PowerShell as Administrator to apply the database migration

# Navigate to project directory
cd "D:\mmgc-main"

Write-Host "Step 1: Building project..." -ForegroundColor Green
dotnet build

Write-Host "`nStep 2: Applying migrations..." -ForegroundColor Green
dotnet ef database update

Write-Host "`nStep 3: Starting application..." -ForegroundColor Green
dotnet run
