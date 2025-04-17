# Simple Backup Script - Only backs up logs and playcount locally
# PowerShell version for Windows

# Directory to store backups
$BACKUP_DIR = "C:\bucstop-backups"
if (!(Test-Path -Path $BACKUP_DIR)) {
    New-Item -ItemType Directory -Path $BACKUP_DIR | Out-Null
}

# Get timestamp for backup files
$TIMESTAMP = Get-Date -Format "yyyyMMdd_HHmmss"
$BACKUP_PATH = "$BACKUP_DIR\bucstop_backup_$TIMESTAMP"
if (!(Test-Path -Path $BACKUP_PATH)) {
    New-Item -ItemType Directory -Path $BACKUP_PATH | Out-Null
}

Write-Host "Creating backup of Docker volumes..."

# Backup logs volume
Write-Host "Backing up logs volume..."
docker run --rm `
    -v bucstop_logs:/source:ro `
    -v "${BACKUP_PATH}:/backup" `
    alpine tar -czf /backup/logs.tar.gz -C /source .

# Backup playcount volume
Write-Host "Backing up playcount volume..."
docker run --rm `
    -v bucstop_playcount:/source:ro `
    -v "${BACKUP_PATH}:/backup" `
    alpine tar -czf /backup/playcount.tar.gz -C /source .

Write-Host "Backup created successfully at $BACKUP_PATH"
Write-Host "Backup process completed."

# Optional: Keep only the 10 most recent backups
Write-Host "Cleaning old backups, keeping the 10 most recent..."
Get-ChildItem -Path $BACKUP_DIR -Directory | 
    Sort-Object -Property LastWriteTime -Descending | 
    Select-Object -Skip 10 | 
    ForEach-Object { 
        Write-Host "Removing old backup: $_"
        Remove-Item -Path $_.FullName -Recurse -Force 
    }
Write-Host "Cleanup completed." 