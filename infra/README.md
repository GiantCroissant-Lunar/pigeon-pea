# Infrastructure

Infrastructure automation and configuration for the pigeon-pea project.

## Directory Structure

```
infra/
├── ansible/                # Development environment automation
│   ├── setup-dev.yml       # Main development setup playbook
│   ├── setup-memory.yml    # Memory infrastructure setup
│   ├── roles/              # Ansible roles
│   └── README.md           # Ansible documentation
├── memory/                 # Memory system infrastructure
│   ├── scripts/            # MCP server and test scripts
│   ├── docker-compose.yml  # Qdrant vector database
│   └── README.md           # Memory system documentation
└── terraform/              # GitHub infrastructure as code
    ├── main.tf             # Main Terraform configuration
    ├── modules/github/     # GitHub resources module
    └── README.md           # Terraform documentation
```

## Quick Start

### Option 1: Automated Setup (Recommended)

Use Ansible to automatically set up your development environment:

```bash
# Install Ansible
pip install ansible

# Run full development environment setup
cd infra/ansible
ansible-playbook setup-dev.yml

# Or just setup memory infrastructure
ansible-playbook setup-memory.yml
```

See [`ansible/README.md`](ansible/README.md) for detailed Ansible usage.

### Option 2: Manual Setup

If you prefer manual setup or need to install specific components:

1. **Development Tools**
   - Python 3.11+
   - .NET 8.0 SDK
   - Node.js 18+ LTS
   - Docker & Docker Compose

2. **Pre-commit Hooks**

   ```bash
   pip install pre-commit
   cd ../..  # Go to project root
   pre-commit install
   ```

3. **Memory Infrastructure** (optional)
   ```bash
   cd memory
   docker compose up -d
   ```

See [`memory/README.md`](memory/README.md) for memory system details.

3. **GitHub Infrastructure** (optional)
   ```bash
   cd terraform
   cp terraform.tfvars.example terraform.tfvars
   # Edit terraform.tfvars with your GitHub token
   terraform init
   terraform plan
   terraform apply
   ```

See [`terraform/README.md`](terraform/README.md) for Terraform usage.

## Components

### Ansible (`ansible/`)

Automated development environment setup using Ansible.

**Features:**

- Python, .NET, Node.js, Docker installation
- Pre-commit hooks configuration
- Memory infrastructure (Qdrant) deployment
- Platform-specific handling (Linux, macOS, Windows/WSL)

**Available Playbooks:**

- `setup-dev.yml` - Full development environment
- `setup-memory.yml` - Memory infrastructure only
- `setup-precommit.yml` - Pre-commit hooks only
- `teardown-memory.yml` - Remove memory infrastructure

See [`ansible/README.md`](ansible/README.md) for full documentation.

### Memory (`memory/`)

Long-term memory system for AI agents (Claude Code, Windsurf, etc.) using:

- File-based JSONL storage (Phase 1)
- Qdrant vector database (Phase 2 - semantic search)
- MCP server for tool integration

**Features:**

- Full visibility into memory operations (via logs)
- Extremely low cost (~$0.10-$1/month with embeddings)
- Shared across multiple tools/editors
- Easy to activate/deactivate

**Status:** Prepared but not activated (waiting for dotnet reorg merge)

See [`memory/README.md`](memory/README.md) for activation instructions.

### Terraform (`terraform/`)

GitHub infrastructure management using Terraform (infrastructure as code).

**Features:**

- Repository settings and configuration
- Branch protection rules
- GitHub Actions secrets management
- GitHub Actions variables
- Infrastructure versioning and reproducibility

**Manages:**

- Repository visibility, features, topics
- Branch protection (main branch)
- Required status checks (pre-commit, build, test)
- GitHub Actions secrets (API keys, credentials)
- Repository webhooks (future)

**Status:** Ready to use

See [`terraform/README.md`](terraform/README.md) for detailed usage.

## Usage Scenarios

### New Developer Onboarding

```bash
# Clone the repository
git clone <repo-url>
cd pigeon-pea

# Run automated setup
cd infra/ansible
ansible-playbook setup-dev.yml

# Verify setup
python --version
dotnet --version
pre-commit run --all-files
```

### Setting Up Memory System

```bash
# Setup memory infrastructure
cd infra/ansible
ansible-playbook setup-memory.yml

# Test memory system
python ../memory/scripts/test-visibility.py

# Configure Claude Code (see memory/README.md)
```

### Managing GitHub Infrastructure

```bash
# Setup GitHub repository configuration
cd infra/terraform

# Configure variables
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars with your GitHub token

# Initialize and apply
terraform init
terraform plan
terraform apply
```

### CI/CD Integration

The Ansible playbooks can be used in CI/CD pipelines:

```yaml
# Example GitHub Actions
- name: Setup development environment
  run: |
    pip install ansible
    cd infra/ansible
    ansible-playbook setup-dev.yml --tags python,precommit
```

## Platform Support

| Platform              | Ansible    | Memory     | Terraform | Status      |
| --------------------- | ---------- | ---------- | --------- | ----------- |
| Linux (Debian/Ubuntu) | ✅ Full    | ✅ Full    | ✅ Full   | Tested      |
| macOS                 | ✅ Full    | ✅ Full    | ✅ Full   | Tested      |
| Windows (WSL2)        | ⚠️ Partial | ✅ Full    | ✅ Full   | Recommended |
| Windows (native)      | ❌ Manual  | ⚠️ Limited | ✅ Full   | Partial     |

## Troubleshooting

### Ansible fails with "permission denied"

Some tasks require `sudo`. Ensure you can run `sudo` without password or use:

```bash
ansible-playbook setup-dev.yml --ask-become-pass
```

### Docker not accessible after installation

On Linux, you need to log out and back in for Docker group membership to take effect:

```bash
# Verify group membership
groups

# If 'docker' is not listed, log out and back in
```

### Memory infrastructure won't start

Check if Docker is running:

```bash
docker ps
# If error, start Docker service (Linux)
sudo systemctl start docker
```

## Development

### Adding New Ansible Roles

1. Create role directory: `mkdir -p ansible/roles/myrole/tasks`
2. Create `tasks/main.yml` with tasks
3. Add role to appropriate playbook
4. Document in `ansible/README.md`

### Testing Ansible Playbooks

```bash
# Dry run (check mode)
ansible-playbook setup-dev.yml --check

# Run specific role
ansible-playbook setup-dev.yml --tags python

# Verbose output
ansible-playbook setup-dev.yml -v
```

## References

- Ansible setup based on: [`C:\lunar-snake\personal-work\infra-projects\giantcroissant-lunar-infra\ansible`](file:///C:/lunar-snake/personal-work/infra-projects/giantcroissant-lunar-infra/ansible)
- Terraform setup based on: [`C:\lunar-snake\personal-work\infra-projects\giantcroissant-lunar-ai\infra\terraform`](file:///C:/lunar-snake/personal-work/infra-projects/giantcroissant-lunar-ai/infra/terraform)
- Ansible documentation: https://docs.ansible.com/
- Terraform documentation: https://www.terraform.io/docs
- Memory system design: `memory/README.md`
- Pre-commit hooks: `../../.pre-commit-config.yaml`
