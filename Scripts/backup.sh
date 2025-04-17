#!/bin/bash
# Simple Backup Script - Only backs up logs and playcount locally

# Exit on any error
set -e

# Directory to store backups
BACKUP_DIR="/home/ec2-user/bucstop-backups"
mkdir -p "$BACKUP_DIR"

# Get timestamp for backup files
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_PATH="$BACKUP_DIR/bucstop_backup_$TIMESTAMP"
mkdir -p "$BACKUP_PATH"

echo "Creating backup of Docker volumes..."

# Backup logs volume
echo "Backing up logs volume..."
docker run --rm \
    -v bucstop_logs:/source:ro \
    -v "$BACKUP_PATH:/backup" \
    alpine tar -czf /backup/logs.tar.gz -C /source .

# Backup playcount volume
echo "Backing up playcount volume..."
docker run --rm \
    -v bucstop_playcount:/source:ro \
    -v "$BACKUP_PATH:/backup" \
    alpine tar -czf /backup/playcount.tar.gz -C /source .

echo "Backup created successfully at $BACKUP_PATH"
echo "Backup process completed."

# Optional: Keep only the 10 most recent backups
echo "Cleaning old backups, keeping the 10 most recent..."
ls -dt "$BACKUP_DIR"/* | tail -n +11 | xargs -r rm -rf
echo "Cleanup completed." 