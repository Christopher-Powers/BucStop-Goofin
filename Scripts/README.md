# BucStop Volume Management

This directory contains scripts for managing Docker volumes, backups, and rollbacks for the BucStop application. These scripts help you persist logs and playcount data while allowing you to roll back the application to previous versions.

## Prerequisites

- Docker and Docker Compose installed
- Git installed
- Bash (for Linux/macOS) or PowerShell (for Windows)

## Overview

The scripts provide the following functionality:

1. **Backup** - Create backups of logs and playcount data
2. **Restore** - Restore from previous backups
3. **Rollback** - Roll back the application to a previous version while preserving data
4. **Manage backups** - List and clean up old backups

## Usage

### For Linux/macOS Users

Make the script executable:

```bash
chmod +x volume-manager.sh
```

Then use it as follows:

```bash
# Create a full backup
./volume-manager.sh backup

# Backup only logs
./volume-manager.sh backup-logs

# Backup only playcount data
./volume-manager.sh backup-playcount

# Restore from a backup
./volume-manager.sh restore ./backups/bucstop_backup_20250416_123456

# Roll back to a specific version (git commit, tag, or branch)
./volume-manager.sh rollback v1.0.0

# List all available backups
./volume-manager.sh list-backups

# Clean up old backups (older than 30 days)
./volume-manager.sh clean-backups
```

### For Windows Users

Use the PowerShell script:

```powershell
# Create a full backup
.\volume-manager.ps1 backup

# Backup only logs
.\volume-manager.ps1 backup-logs

# Backup only playcount data
.\volume-manager.ps1 backup-playcount

# Restore from a backup
.\volume-manager.ps1 restore .\backups\bucstop_backup_20250416_123456

# Roll back to a specific version (git commit, tag, or branch)
.\volume-manager.ps1 rollback v1.0.0

# List all available backups
.\volume-manager.ps1 list-backups

# Clean up old backups (older than 30 days)
.\volume-manager.ps1 clean-backups
```

## Running in EC2

To use these scripts in an EC2 environment:

1. Clone the repository to your EC2 instance
2. Install Docker and Docker Compose
3. Run the application using Docker Compose
4. Use the scripts to manage backups and rollbacks

Example setup for Amazon Linux 2:

```bash
# Install git
sudo yum install -y git

# Clone your repository
git clone https://your-repo-url.git
cd your-repo-directory

# Install Docker
sudo amazon-linux-extras install docker
sudo service docker start
sudo usermod -a -G docker ec2-user

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Log out and log back in to apply docker group

# Start the application
docker-compose up -d

# Create a backup
./Scripts/volume-manager.sh backup
```

## How It Works

- The solution uses Docker volumes to persist data outside of containers
- When containers are updated or rebuilt, the data in volumes remains intact
- Backup scripts create compressed archives of volume data
- Restore scripts extract these archives back to volumes
- Rollback scripts manage git versioning while preserving volume data

## Best Practices

1. Create backups before making significant changes
2. Use descriptive tags when committing versions
3. Schedule regular backups using cron or Windows Task Scheduler
4. Store backups in a separate location from your application
5. Test the restore process regularly to ensure backups are valid 