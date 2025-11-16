# Terraform Quick Start

One-page reference for managing pigeon-pea GitHub infrastructure with Terraform.

## Prerequisites

```bash
# Install Terraform
brew install terraform  # macOS
choco install terraform # Windows

# Verify installation
terraform version
```

## One-Time Setup

### 1. Create GitHub Token

https://github.com/settings/tokens

**Required scopes:**

- `repo` (full control)
- `admin:repo_hook` (webhooks)
- `workflow` (GitHub Actions)

### 2. Configure Variables

```bash
cd infra/terraform

# Copy example
cp terraform.tfvars.example terraform.tfvars

# Edit (add your GitHub token)
nano terraform.tfvars
```

**Minimum configuration:**

```hcl
github_token = "ghp_YOUR_TOKEN_HERE"
github_owner = "your-username"
```

### 3. Initialize

```bash
terraform init
```

## Common Commands

### Review Changes

```bash
terraform plan
```

### Apply Changes

```bash
terraform apply
# Review, type 'yes'
```

### Show Current State

```bash
terraform show
```

### List Resources

```bash
terraform state list
```

### Format Code

```bash
terraform fmt
```

### Validate Configuration

```bash
terraform validate
```

## Quick Operations

### Add GitHub Actions Secret

Edit `terraform.tfvars`:

```hcl
github_actions_secrets = {
  "OPENAI_API_KEY" = "sk-..."
  "NEW_SECRET"     = "value"  # Add this
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
  "new-check"  # Add this
]
```

Then:

```bash
terraform apply
```

### Change Repository Visibility

Edit `terraform.tfvars`:

```hcl
repository_visibility = "public"  # or "private"
```

Then:

```bash
terraform apply
```

## State Management

### Local (Default)

State stored in `terraform.tfstate` (not committed).

**Warning:** Contains secrets, don't share!

### Terraform Cloud (Recommended)

1. **Create account:** https://app.terraform.io
2. **Create organization and workspace**
3. **Update `main.tf`:**
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
4. **Login and migrate:**
   ```bash
   terraform login
   terraform init  # Migrates state
   ```

## Workflow

```bash
# 1. Make changes to terraform.tfvars
# 2. Review changes
terraform plan

# 3. Apply if looks good
terraform apply

# 4. Commit (but NOT terraform.tfvars!)
git add main.tf variables.tf
git commit -m "Update Terraform config"
```

## What Gets Managed

- ✅ Repository settings
- ✅ Branch protection (main)
- ✅ GitHub Actions secrets
- ✅ GitHub Actions variables
- ✅ Repository topics

## Troubleshooting

### Repository not found

```bash
# Check repository exists
# Verify github_owner matches
# Check token permissions
```

### Resource already exists

```bash
# Import existing resource
terraform import <resource_type>.<name> <id>
```

### Invalid credentials

```bash
# Generate new token
# Update terraform.tfvars
terraform apply
```

### State locked

```bash
# If using Terraform Cloud and state is locked
# Wait for other operation to complete, or force unlock:
terraform force-unlock <lock-id>
```

## Safety Tips

### DO:

- ✅ Review `terraform plan` before `apply`
- ✅ Use `.gitignore` (already configured)
- ✅ Use Terraform Cloud for teams
- ✅ Commit `.terraform.lock.hcl`

### DON'T:

- ❌ Commit `terraform.tfvars`
- ❌ Commit `.tfstate` files
- ❌ Use `-auto-approve` without reviewing
- ❌ Manually edit GitHub settings (Terraform will overwrite)

## File Reference

| File                       | Purpose              | Commit?   |
| -------------------------- | -------------------- | --------- |
| `main.tf`                  | Main config          | ✅ Yes    |
| `variables.tf`             | Variable definitions | ✅ Yes    |
| `outputs.tf`               | Output definitions   | ✅ Yes    |
| `terraform.tfvars`         | **Your secrets**     | ❌ **NO** |
| `terraform.tfvars.example` | Example config       | ✅ Yes    |
| `.terraform.lock.hcl`      | Provider versions    | ✅ Yes    |
| `terraform.tfstate`        | State file           | ❌ **NO** |
| `.terraform/`              | Provider cache       | ❌ No     |

## Common Variables

```hcl
# GitHub
github_token = "ghp_..."
github_owner = "username"

# Repository
repository_name = "pigeon-pea"
repository_visibility = "private"
repository_topics = ["roguelike", "dotnet", ...]

# Branch Protection
enable_branch_protection = true
protected_branches = ["main"]
require_status_checks = true
required_status_checks = ["pre-commit", "build", "test"]

# Secrets (sensitive)
github_actions_secrets = {
  "OPENAI_API_KEY" = "sk-..."
}

# Variables (non-sensitive)
github_actions_variables = {
  "DOTNET_VERSION" = "8.0"
  "PYTHON_VERSION" = "3.11"
}
```

## Next Steps

1. ✅ Set up Terraform Cloud (optional)
2. ✅ Run `terraform init && terraform plan`
3. ✅ Apply configuration
4. ✅ Integrate with CI/CD (optional)

## Documentation

- Full guide: [README.md](README.md)
- Terraform docs: https://www.terraform.io/docs
- GitHub provider: https://registry.terraform.io/providers/integrations/github
