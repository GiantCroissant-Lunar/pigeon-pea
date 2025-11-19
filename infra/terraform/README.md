# Terraform GitHub Infrastructure

Manage pigeon-pea GitHub repository infrastructure as code using Terraform.

## Overview

This Terraform configuration manages:

- ✅ **Repository settings** (visibility, features, topics)
- ✅ **Branch protection rules** (main branch, PR requirements)
- ✅ **GitHub Actions secrets** (API keys, credentials)
- ✅ **GitHub Actions variables** (non-sensitive config)
- ✅ **Repository webhooks** (future: CI/CD integration)

**Pattern:** Based on proven lunar-snake infrastructure patterns from giantcroissant-lunar-ai.

## Quick Start

See [QUICKSTART.md](QUICKSTART.md) for a condensed getting-started guide.

## Prerequisites

1. **Terraform** 1.0+ installed

   ```bash
   # Check version
   terraform version

   # Install via package manager
   brew install terraform  # macOS
   choco install terraform # Windows
   ```

2. **GitHub Personal Access Token** with permissions:
   - `repo` (full control of repositories)
   - `admin:repo_hook` (webhooks)
   - `workflow` (GitHub Actions)

   Create at: https://github.com/settings/tokens

3. **Existing GitHub repository** (pigeon-pea)
   - This configuration manages an existing repository
   - To create a new repo, uncomment the `github_repository` resource in `modules/github/main.tf`

## Setup Instructions

### Step 1: Configure Variables

```bash
# Copy example variables
cp terraform.tfvars.example terraform.tfvars

# Edit with your values
nano terraform.tfvars
```

**Required variables:**

```hcl
github_token = "ghp_YOUR_TOKEN_HERE"
github_owner = "your-username"
```

**Optional but recommended:**

```hcl
github_actions_secrets = {
  "OPENAI_API_KEY" = "sk-..."  # For memory system embeddings
}
```

### Step 2: Initialize Terraform

```bash
# Initialize providers and modules
terraform init
```

This downloads the GitHub provider and initializes modules.

### Step 3: Review Changes

```bash
# See what Terraform will do
terraform plan
```

Review carefully before applying!

### Step 4: Apply Configuration

```bash
# Apply changes
terraform apply

# Review and confirm
# Type 'yes' when prompted
```

### Step 5: Verify

```bash
# Show current state
terraform show

# List managed resources
terraform state list
```

## What Gets Managed

### Repository Settings

Managed via `modules/github`:

- Repository description and topics
- Features (Issues, Projects, Discussions, Wiki)
- Merge settings (squash, rebase, delete on merge)
- Security settings (vulnerability alerts)

### Branch Protection (main)

Default protection rules:

- ✅ Require pull request reviews (optional, configurable)
- ✅ Require status checks to pass
  - `pre-commit` (formatting, linting)
  - `build` (.NET build)
  - `test` (test suite)
- ✅ Dismiss stale reviews
- ✅ Require conversation resolution
- ❌ No force pushes
- ❌ No branch deletion

### GitHub Actions Secrets

Securely managed secrets for CI/CD:

- `OPENAI_API_KEY` - For memory system embeddings
- `ANTHROPIC_API_KEY` - For Claude integration (optional)
- `GCP_SA_KEY` - For Unity cloud builds (future)
- `FIREBASE_TOKEN` - For app distribution (future)

**Note:** Secret values are not stored in Terraform state in plaintext if using Terraform Cloud.

### GitHub Actions Variables

Non-sensitive configuration:

- `DOTNET_VERSION` = "8.0"
- `PYTHON_VERSION` = "3.11"
- `NODE_VERSION` = "18"
- `QDRANT_VERSION` = "latest"

## State Management

### Option 1: Local State (Default)

State stored in `terraform.tfstate` locally.

**Pros:**

- Simple, no setup required
- Works offline

**Cons:**

- Not suitable for teams
- State file contains secrets
- No collaboration features

**For solo development**, this is fine.

### Option 2: Terraform Cloud (Recommended for Teams)

Configure in `main.tf`:

```hcl
terraform {
  cloud {
    organization = "your-org"
    workspaces {
      name = "pigeon-pea"
    }
  }
}
```

**Pros:**

- Remote state storage
- State encryption
- Collaboration support
- Version history
- Free for small teams

**Setup:**

1. Create account at https://app.terraform.io
2. Create organization and workspace
3. Update `main.tf` with your org/workspace
4. Run `terraform login`
5. Run `terraform init` to migrate state

See [TERRAFORM-CLOUD-SETUP.md](TERRAFORM-CLOUD-SETUP.md) for detailed instructions.

## Common Operations

### Update Repository Settings

1. Edit `terraform.tfvars`
2. Run `terraform plan` to review
3. Run `terraform apply` to apply

### Add GitHub Actions Secret

Edit `terraform.tfvars`:

```hcl
github_actions_secrets = {
  "OPENAI_API_KEY"    = "sk-..."
  "NEW_SECRET_NAME"   = "value"  # Add new secret
}
```

Then:

```bash
terraform apply
```

### Update Branch Protection

Edit `terraform.tfvars`:

```hcl
required_status_checks = [
  "pre-commit",
  "build",
  "test",
  "integration-test"  # Add new required check
]
```

Then:

```bash
terraform apply
```

### Remove a Secret

Remove from `github_actions_secrets` in `terraform.tfvars`, then:

```bash
terraform apply
```

### Import Existing Resources

If you've manually configured something in GitHub and want Terraform to manage it:

```bash
# Example: Import branch protection
terraform import 'module.github_repository.github_branch_protection.protected_branches["main"]' "your-repo:main"
```

## Directory Structure

```
infra/terraform/
├── main.tf                    # Main configuration
├── variables.tf               # Input variables
├── outputs.tf                 # Output values
├── terraform.tfvars.example   # Example configuration (commit)
├── terraform.tfvars           # Actual configuration (DO NOT commit)
├── .gitignore                 # Ignore sensitive files
├── .terraform.lock.hcl        # Provider version lock (commit)
├── README.md                  # This file
├── QUICKSTART.md              # Quick reference
└── modules/
    └── github/                # GitHub resources module
        ├── main.tf            # Repository, branch protection, secrets
        ├── variables.tf       # Module inputs
        └── outputs.tf         # Module outputs
```

## Security Best Practices

### DO:

- ✅ Use Terraform Cloud or remote backend for state
- ✅ Use environment variables for secrets in CI/CD
- ✅ Review `terraform plan` before `terraform apply`
- ✅ Commit `.terraform.lock.hcl` for version consistency
- ✅ Use `.gitignore` to exclude sensitive files

### DON'T:

- ❌ Commit `terraform.tfvars` (contains secrets)
- ❌ Commit `.tfstate` files (contain secrets)
- ❌ Store secrets in version control
- ❌ Use `terraform apply -auto-approve` without reviewing
- ❌ Share state files publicly

## Troubleshooting

### Error: "Repository not found"

**Cause:** Repository doesn't exist or token lacks permissions.

**Fix:**

1. Verify repository exists: `https://github.com/your-username/pigeon-pea`
2. Check token permissions (repo, admin:repo_hook, workflow)
3. Verify `github_owner` matches repository owner

### Error: "Resource already exists"

**Cause:** Resource already managed manually in GitHub.

**Fix:** Import the existing resource:

```bash
terraform import <resource_type>.<name> <id>
```

### Error: "Invalid credentials"

**Cause:** GitHub token expired or invalid.

**Fix:**

1. Generate new token at https://github.com/settings/tokens
2. Update `terraform.tfvars` with new token
3. Run `terraform apply`

### State File Corruption

**Prevention:**

- Use Terraform Cloud for state management
- Enable state locking
- Don't manually edit state files

**Recovery:**

```bash
# Backup current state
cp terraform.tfstate terraform.tfstate.backup

# Pull fresh state (if using remote backend)
terraform state pull > terraform.tfstate

# If all else fails, recreate from scratch
terraform import <resources>
```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Terraform

on:
  push:
    branches: [main]
    paths: ['infra/terraform/**']
  pull_request:
    paths: ['infra/terraform/**']

jobs:
  terraform:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: infra/terraform

    steps:
      - uses: actions/checkout@v3

      - uses: hashicorp/setup-terraform@v2
        with:
          terraform_version: 1.5.0

      - name: Terraform Init
        run: terraform init

      - name: Terraform Format
        run: terraform fmt -check

      - name: Terraform Plan
        run: terraform plan
        env:
          TF_VAR_github_token: ${{ secrets.TF_GITHUB_TOKEN }}
          TF_VAR_github_owner: ${{ github.repository_owner }}

      - name: Terraform Apply
        if: github.ref == 'refs/heads/main'
        run: terraform apply -auto-approve
        env:
          TF_VAR_github_token: ${{ secrets.TF_GITHUB_TOKEN }}
          TF_VAR_github_owner: ${{ github.repository_owner }}
```

## References

- Terraform Documentation: https://www.terraform.io/docs
- GitHub Provider: https://registry.terraform.io/providers/integrations/github/latest/docs
- Terraform Cloud: https://app.terraform.io
- Based on: `C:\lunar-snake\personal-work\infra-projects\giantcroissant-lunar-ai\infra\terraform`

## Next Steps

1. ✅ Set up Terraform Cloud (optional but recommended)
2. ✅ Configure GitHub token and variables
3. ✅ Run `terraform init && terraform plan`
4. ✅ Review and apply changes
5. ✅ Set up CI/CD for automated infrastructure management

## Support

For issues or questions:

- Check [Troubleshooting](#troubleshooting) section
- Review Terraform documentation
- Check GitHub provider documentation
- Review `terraform plan` output carefully
