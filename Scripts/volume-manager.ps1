# BucStop Volume Manager - A script to help with backup, restore, and rollback operations
# PowerShell version for Windows users

# Function to show usage information
function Show-Usage {
    Write-Host "BucStop Volume Manager"
    Write-Host "Usage: .\volume-manager.ps1 [command]"
    Write-Host ""
    Write-Host "Commands:"
    Write-Host "  backup                 - Create a backup of all persistent data"
    Write-Host "  backup-logs            - Create a backup of logs only"
    Write-Host "  backup-playcount       - Create a backup of playcount only"
    Write-Host "  restore [backup_file]  - Restore data from a backup file"
    Write-Host "  rollback [version]     - Roll back the application to a specified version"
    Write-Host "                           while preserving persistent data"
    Write-Host "  list-backups           - List all available backups"
    Write-Host "  clean-backups          - Remove backups older than 30 days"
    Write-Host ""
}

# Directory to store backups
$BACKUP_DIR = ".\backups"
if (!(Test-Path -Path $BACKUP_DIR)) {
    New-Item -ItemType Directory -Path $BACKUP_DIR | Out-Null
}

# Get timestamp for backup files
$TIMESTAMP = Get-Date -Format "yyyyMMdd_HHmmss"

# Function to create a backup of Docker volumes
function Backup-Volumes {
    Write-Host "Creating backup of Docker volumes..."
    
    # Create backup directory for this backup
    $BACKUP_PATH = "$BACKUP_DIR\bucstop_backup_$TIMESTAMP"
    if (!(Test-Path -Path $BACKUP_PATH)) {
        New-Item -ItemType Directory -Path $BACKUP_PATH | Out-Null
    }
    
    # Backup logs volume
    Write-Host "Backing up logs volume..."
    docker run --rm `
        -v bucstop_logs:/source:ro `
        -v "${PWD}/${BACKUP_PATH}:/backup" `
        alpine tar -czf /backup/logs.tar.gz -C /source .
    
    # Backup playcount volume
    Write-Host "Backing up playcount volume..."
    docker run --rm `
        -v bucstop_playcount:/source:ro `
        -v "${PWD}/${BACKUP_PATH}:/backup" `
        alpine tar -czf /backup/playcount.tar.gz -C /source .
    
    Write-Host "Backup created successfully at $BACKUP_PATH"
}

# Function to backup logs only
function Backup-Logs {
    Write-Host "Creating backup of logs volume..."
    
    # Create backup directory for this backup
    $BACKUP_PATH = "$BACKUP_DIR\logs_backup_$TIMESTAMP"
    if (!(Test-Path -Path $BACKUP_PATH)) {
        New-Item -ItemType Directory -Path $BACKUP_PATH | Out-Null
    }
    
    # Backup logs volume
    docker run --rm `
        -v bucstop_logs:/source:ro `
        -v "${PWD}/${BACKUP_PATH}:/backup" `
        alpine tar -czf /backup/logs.tar.gz -C /source .
    
    Write-Host "Logs backup created successfully at $BACKUP_PATH"
}

# Function to backup playcount only
function Backup-Playcount {
    Write-Host "Creating backup of playcount volume..."
    
    # Create backup directory for this backup
    $BACKUP_PATH = "$BACKUP_DIR\playcount_backup_$TIMESTAMP"
    if (!(Test-Path -Path $BACKUP_PATH)) {
        New-Item -ItemType Directory -Path $BACKUP_PATH | Out-Null
    }
    
    # Backup playcount volume
    docker run --rm `
        -v bucstop_playcount:/source:ro `
        -v "${PWD}/${BACKUP_PATH}:/backup" `
        alpine tar -czf /backup/playcount.tar.gz -C /source .
    
    Write-Host "Playcount backup created successfully at $BACKUP_PATH"
}

# Function to restore backup
function Restore-Backup {
    param(
        [string]$BackupPath
    )
    
    if ([string]::IsNullOrEmpty($BackupPath)) {
        Write-Host "Error: Please specify a backup directory to restore from." -ForegroundColor Red
        Show-Usage
        return
    }
    
    if (!(Test-Path -Path $BackupPath)) {
        Write-Host "Error: Backup directory $BackupPath does not exist." -ForegroundColor Red
        return
    }
    
    Write-Host "Restoring from backup $BackupPath..."
    
    # Check if the application is running
    $containersRunning = docker-compose ps | Select-String -Pattern "Up"
    
    if ($containersRunning) {
        Write-Host "Stopping application before restore..."
        docker-compose down
    }
    
    # Restore logs volume
    if (Test-Path -Path "$BackupPath\logs.tar.gz") {
        Write-Host "Restoring logs volume..."
        docker run --rm `
            -v bucstop_logs:/destination `
            -v "${PWD}/${BackupPath}:/backup" `
            alpine sh -c "rm -rf /destination/* && tar -xzf /backup/logs.tar.gz -C /destination"
    }
    
    # Restore playcount volume
    if (Test-Path -Path "$BackupPath\playcount.tar.gz") {
        Write-Host "Restoring playcount volume..."
        docker run --rm `
            -v bucstop_playcount:/destination `
            -v "${PWD}/${BackupPath}:/backup" `
            alpine sh -c "rm -rf /destination/* && tar -xzf /backup/playcount.tar.gz -C /destination"
    }
    
    Write-Host "Backup restored successfully"
    Write-Host "Starting application..."
    docker-compose up -d
}

# Function to rollback
function Rollback-Version {
    param(
        [string]$Version
    )
    
    if ([string]::IsNullOrEmpty($Version)) {
        Write-Host "Error: Please specify a version to roll back to." -ForegroundColor Red
        Show-Usage
        return
    }
    
    # Create a backup before rolling back
    Write-Host "Creating backup before rollback..."
    Backup-Volumes
    
    # Stop the application
    Write-Host "Stopping application..."
    docker-compose down
    
    # Check out the specified version
    Write-Host "Rolling back to version $Version..."
    git checkout $Version
    
    # Start the application with the rolled back version
    Write-Host "Starting rolled back version..."
    docker-compose up -d
    
    Write-Host "Rollback complete. Application is now running version $Version with preserved data."
}

# Function to list backups
function List-Backups {
    Write-Host "Available backups:"
    Get-ChildItem -Path $BACKUP_DIR
}

# Function to clean old backups
function Clean-Backups {
    Write-Host "Cleaning backups older than 30 days..."
    
    $cutoffDate = (Get-Date).AddDays(-30)
    Get-ChildItem -Path $BACKUP_DIR -Directory | Where-Object {
        $_.Name -match "(bucstop|logs|playcount)_backup_" -and $_.CreationTime -lt $cutoffDate
    } | ForEach-Object {
        Write-Host "Removing $_..."
        Remove-Item -Path $_.FullName -Recurse -Force
    }
    
    Write-Host "Old backups cleaned."
}

# Parse command line arguments
$command = $args[0]

switch ($command) {
    "backup" {
        Backup-Volumes
    }
    "backup-logs" {
        Backup-Logs
    }
    "backup-playcount" {
        Backup-Playcount
    }
    "restore" {
        Restore-Backup -BackupPath $args[1]
    }
    "rollback" {
        Rollback-Version -Version $args[1]
    }
    "list-backups" {
        List-Backups
    }
    "clean-backups" {
        Clean-Backups
    }
    default {
        Show-Usage
    }
} 