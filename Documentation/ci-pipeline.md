# CI/CD Pipeline for BucStop on AWS EC2

This document outlines the implementation of a Continuous Integration and Continuous Deployment (CI/CD) pipeline for the BucStop application when deployed on Amazon EC2 instances.

## Pipeline Overview

```
[Code] → [Build] → [Test] → [Deploy] → [Monitor]
```

## Implementation Steps

### 1. Source Control Setup

- Use GitHub/GitLab for source code management
- Configure branch protection rules
  - Require pull request reviews before merging
  - Require status checks to pass before merging

### 2. CI Pipeline with GitHub Actions or Jenkins

#### GitHub Actions Implementation

Create a workflow file at `.github/workflows/ci-pipeline.yml` (example):

```yaml
name: BucStop CI Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup Node.js
      uses: actions/setup-node@v3
      with:
        node-version: '16'
        cache: 'npm'
    
    - name: Install dependencies
      run: npm ci
    
    - name: Run linting
      run: npm run lint
    
    - name: Run tests
      run: npm test
    
    - name: Build application
      run: npm run build
```
> Above, we can see the workflow is being excuted (via GitHub Actions) on an Ubuntu runner server. It checks out the code out, sets up a Node.js environment, and install dependencies. After completing those tasks, its rolls straight into linting (to check code quality), test execution, and application building.
#### Alternative: Jenkins Setup

- Deploy Jenkins as a containerized service (seperately from BucStop) on your AWS EC2
  - *Best practice would be to deploy this on an entirely seperate EC2 for better isolation/performance/scaling etc.* 
- Configure webhooks from your GitHub/GitLab repository
- Create a Jenkinsfile at the root of your project

### 3. Deployment to EC2

Extend the CI workflow to include deployment steps:

```yaml
  deploy-to-ec2:
    needs: build-and-test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Configure AWS credentials
      uses: aws-actions/configure-aws-credentials@v1
      with:
        aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
        aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
        aws-region: us-east-1
    
    - name: Build and package application
      run: |
        npm ci
        npm run build
        tar -czf build.tar.gz build
    
    - name: Upload build artifact to S3
      run: |
        aws s3 cp build.tar.gz s3://bucstop-deployments/build-${{ github.sha }}.tar.gz
    
    - name: Deploy to EC2 instances
      run: |
        aws ssm send-command \
          --document-name "AWS-RunShellScript" \
          --targets "Key=tag:Application,Values=BucStop" \
          --parameters commands="cd /var/www/bucstop && aws s3 cp s3://bucstop-deployments/build-${{ github.sha }}.tar.gz . && tar -xzf build-${{ github.sha }}.tar.gz && pm2 restart bucstop"
```
>This step is extending the CI workflow for EC2 compatability. The `deploy-to-EC2` job depends on the `build-and-test` job and will only run if the tests pass. This will only be triggered when code is pushed to main (production) based on the following like in the yaml: `if: github.ref == 'refs/heads/main'`. The deployment process checks out the code AWS credentials using secrets stored in GitHub (don't forget to manual add these). Next, it builds & packages up the application into a tar.gz and uploads the package to an S3 bucket with the commit SHA in the filesname for versioning (this will also have to be setup seperately - don't forget to enable versioning). It utilizes the AWS System Manager (SSM) to remotely execute commands on EC2. The commands we can see being delegated at the end navigate to the application directory, download the package from S3, extract it, and restarts the application using PM2.

### 4. EC2 Instance Setup

- Launch EC2 instances using Amazon Linux 2 or Ubuntu Server
- Configure security groups to allow appropriate inbound traffic
- Install necessary dependencies:
  ```bash
  # Update system packages
  sudo yum update -y  # For Amazon Linux
  # OR
  sudo apt update && sudo apt upgrade -y  # For Ubuntu
  
  # Install Node.js
  curl -sL https://deb.nodesource.com/setup_16.x | sudo -E bash -
  sudo apt-get install -y nodejs
  
  # Install PM2
  sudo npm install -g pm2
  
  # Install AWS CLI
  sudo apt install -y awscli
  ```
- If you haven't already, I would suggest running  [EC2-init.sh](../Scripts/ec2_init.sh) for other project specific dependencies that need to be installed for BucStop to run on EC2 (not all directly related to CI)

- Create an IAM role for EC2 with permissions to access the S3 deployment bucket
- Configure the application directory:
  ```bash
  sudo mkdir -p /var/www/bucstop
  sudo chown -R ec2-user:ec2-user /var/www/bucstop  # For Amazon Linux
  # OR
  sudo chown -R ubuntu:ubuntu /var/www/bucstop  # For Ubuntu
  ```

### 5. Monitoring and Alerting

- Set up CloudWatch for monitoring EC2 instances
- Configure CloudWatch Alarms for critical metrics
- Implement application-level logging using Winston or similar

### 6. Rollback Strategy

- Maintain versioned deployments in S3
- Create a rollback script/command:
  ```bash
  aws ssm send-command \
    --document-name "AWS-RunShellScript" \
    --targets "Key=tag:Application,Values=BucStop" \
    --parameters commands="cd /var/www/bucstop && aws s3 cp s3://bucstop-deployments/build-{PREVIOUS_SHA}.tar.gz . && tar -xzf build-{PREVIOUS_SHA}.tar.gz && pm2 restart bucstop"
  ```
- Alternatively, leverage the rollback the snapshot service integrated into BucStop.

## Security Considerations

- Store secrets in GitHub Secrets or AWS Parameter Store
- Implement VPC for network isolation
  - *Tradeoff = much more networking complexity*
- Use Security Groups to restrict access
- Apply the principle of least privilege for IAM roles
  - *Tradeoff = role complexity and additional time to troubleshoot*


## Future Improvements

- Implement blue/green deployments
- Add automated UI testing
- Set up database migrations as part of the deployment process
- Consider containerization with Docker on ECS/EKS instead of EC2 