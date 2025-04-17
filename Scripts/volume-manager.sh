#!/bin/bash
# BucStop Volume Manager - A script to help with backup, restore, and rollback operations

# Exit on any error
set -e

# Function to show usage information
function show_usage {
    echo "BucStop Volume Manager"
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  backup                 - Create a backup of all persistent data"
    echo "  backup-logs            - Create a backup of logs only"
    echo "  backup-playcount       - Create a backup of playcount only"
    echo "  restore [backup_file]  - Restore data from a backup file"
    echo "  rollback [version]     - Roll back the application to a specified version"
    echo "                           while preserving persistent data"
    echo "  list-backups           - List all available backups"
    echo "  clean-backups          - Remove backups older than 30 days"
    echo ""
}

# Directory to store backups
BACKUP_DIR="./backups"
mkdir -p "$BACKUP_DIR"

# Get timestamp for backup files
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")

# Function to create a backup of Docker volumes
function backup_volumes {
    echo "Creating backup of Docker volumes..."
    
    # Create backup directory for this backup
    BACKUP_PATH="$BACKUP_DIR/bucstop_backup_$TIMESTAMP"
    mkdir -p "$BACKUP_PATH"
    
    # Backup logs volume
    echo "Backing up logs volume..."
    docker run --rm \
        -v bucstop_logs:/source:ro \
        -v "$(pwd)/$BACKUP_PATH:/backup" \
        alpine tar -czf /backup/logs.tar.gz -C /source .
    
    # Backup playcount volume
    echo "Backing up playcount volume..."
    docker run --rm \
        -v bucstop_playcount:/source:ro \
        -v "$(pwd)/$BACKUP_PATH:/backup" \
        alpine tar -czf /backup/playcount.tar.gz -C /source .
    
    echo "Backup created successfully at $BACKUP_PATH"
}

# Function to backup logs only
function backup_logs {
    echo "Creating backup of logs volume..."
    
    # Create backup directory for this backup
    BACKUP_PATH="$BACKUP_DIR/logs_backup_$TIMESTAMP"
    mkdir -p "$BACKUP_PATH"
    
    # Backup logs volume
    docker run --rm \
        -v bucstop_logs:/source:ro \
        -v "$(pwd)/$BACKUP_PATH:/backup" \
        alpine tar -czf /backup/logs.tar.gz -C /source .
    
    echo "Logs backup created successfully at $BACKUP_PATH"
}

# Function to backup playcount only
function backup_playcount {
    echo "Creating backup of playcount volume..."
    
    # Create backup directory for this backup
    BACKUP_PATH="$BACKUP_DIR/playcount_backup_$TIMESTAMP"
    mkdir -p "$BACKUP_PATH"
    
    # Backup playcount volume
    docker run --rm \
        -v bucstop_playcount:/source:ro \
        -v "$(pwd)/$BACKUP_PATH:/backup" \
        alpine tar -czf /backup/playcount.tar.gz -C /source .
    
    echo "Playcount backup created successfully at $BACKUP_PATH"
}

# Function to restore backup
function restore_backup {
    if [ -z "$1" ]; then
        echo "Error: Please specify a backup directory to restore from."
        show_usage
        exit 1
    fi
    
    BACKUP_PATH="$1"
    
    if [ ! -d "$BACKUP_PATH" ]; then
        echo "Error: Backup directory $BACKUP_PATH does not exist."
        exit 1
    fi
    
    echo "Restoring from backup $BACKUP_PATH..."
    
    # Check if the application is running
    if docker-compose ps | grep -q "Up"; then
        echo "Stopping application before restore..."
        docker-compose down
    fi
    
    # Restore logs volume
    if [ -f "$BACKUP_PATH/logs.tar.gz" ]; then
        echo "Restoring logs volume..."
        docker run --rm \
            -v bucstop_logs:/destination \
            -v "$(pwd)/$BACKUP_PATH:/backup" \
            alpine sh -c "rm -rf /destination/* && tar -xzf /backup/logs.tar.gz -C /destination"
    fi
    
    # Restore playcount volume
    if [ -f "$BACKUP_PATH/playcount.tar.gz" ]; then
        echo "Restoring playcount volume..."
        docker run --rm \
            -v bucstop_playcount:/destination \
            -v "$(pwd)/$BACKUP_PATH:/backup" \
            alpine sh -c "rm -rf /destination/* && tar -xzf /backup/playcount.tar.gz -C /destination"
    fi
    
    echo "Backup restored successfully"
    echo "Starting application..."
    docker-compose up -d
}

# Function to rollback
function rollback {
    if [ -z "$1" ]; then
        echo "Error: Please specify a version to roll back to."
        show_usage
        exit 1
    fi
    
    VERSION="$1"
    
    # Create a backup before rolling back
    echo "Creating backup before rollback..."
    backup_volumes
    
    # Stop the application
    echo "Stopping application..."
    docker-compose down
    
    # Check out the specified version
    echo "Rolling back to version $VERSION..."
    git checkout $VERSION
    
    # Start the application with the rolled back version
    echo "Starting rolled back version..."
    docker-compose up -d
    
    echo "Rollback complete. Application is now running version $VERSION with preserved data."
}

# Function to list backups
function list_backups {
    echo "Available backups:"
    ls -la "$BACKUP_DIR"
}

# Function to clean old backups
function clean_backups {
    echo "Cleaning backups older than 30 days..."
    find "$BACKUP_DIR" -type d -name "bucstop_backup_*" -mtime +30 -exec rm -rf {} \;
    find "$BACKUP_DIR" -type d -name "logs_backup_*" -mtime +30 -exec rm -rf {} \;
    find "$BACKUP_DIR" -type d -name "playcount_backup_*" -mtime +30 -exec rm -rf {} \;
    echo "Old backups cleaned."
}

# Parse command line arguments
case "$1" in
    "backup")
        backup_volumes
        ;;
    "backup-logs")
        backup_logs
        ;;
    "backup-playcount")
        backup_playcount
        ;;
    "restore")
        restore_backup "$2"
        ;;
    "rollback")
        rollback "$2"
        ;;
    "list-backups")
        list_backups
        ;;
    "clean-backups")
        clean_backups
        ;;
    *)
        show_usage
        ;;
esac 