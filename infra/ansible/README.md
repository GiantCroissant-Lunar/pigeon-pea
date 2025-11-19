# Ansible Development Environment Setup

Automated setup for pigeon-pea development environment using Ansible.

## Prerequisites

- Python 3.8+
- Ansible 2.9+ (install via `pip install ansible`)
- Windows: WSL2 recommended (or run on Linux/macOS)

## Quick Start

```bash
# Install Ansible (if not already installed)
pip install ansible

# Run the full development environment setup
cd infra/ansible
ansible-playbook setup-dev.yml

# Or run specific roles
ansible-playbook setup-dev.yml --tags python
ansible-playbook setup-dev.yml --tags docker
ansible-playbook setup-dev.yml --tags memory
```

## What Gets Installed

### Core Development Tools

- **Python**: Python 3.11+, pip, virtualenv
- **.NET**: .NET 8.0 SDK
- **Node.js**: Node.js 18+ LTS, npm
- **Docker**: Docker Engine, Docker Compose

### Project-Specific

- **Pre-commit**: Pre-commit hooks framework
- **Memory Infrastructure**: Qdrant setup via Docker Compose
- **Project Dependencies**: Python packages, .NET packages, npm packages

## Available Playbooks

| Playbook              | Description                         |
| --------------------- | ----------------------------------- |
| `setup-dev.yml`       | Full development environment setup  |
| `setup-memory.yml`    | Only memory infrastructure (Qdrant) |
| `setup-precommit.yml` | Only pre-commit hooks               |
| `teardown-memory.yml` | Remove memory infrastructure        |

## Directory Structure

```
infra/ansible/
├── setup-dev.yml           # Main development setup playbook
├── setup-memory.yml        # Memory infrastructure setup
├── setup-precommit.yml     # Pre-commit setup
├── teardown-memory.yml     # Cleanup playbook
├── inventory/
│   └── hosts.yml           # Inventory (localhost)
├── group_vars/
│   └── all.yml             # Global variables
├── vars/
│   ├── dev_tools.yml       # Development tool versions
│   └── memory.yml          # Memory infrastructure config
└── roles/
    ├── python/             # Python setup
    ├── dotnet/             # .NET SDK setup
    ├── docker/             # Docker setup
    ├── nodejs/             # Node.js setup
    ├── precommit/          # Pre-commit hooks
    └── memory/             # Memory infrastructure
```

## Roles

### python

Installs Python 3.11+, pip, virtualenv, and project Python dependencies.

### dotnet

Installs .NET 8.0 SDK and verifies installation.

### docker

Installs Docker Engine and Docker Compose (Linux/macOS).

### nodejs

Installs Node.js 18 LTS and npm.

### precommit

Installs pre-commit framework and sets up hooks for the project.

### memory

Sets up memory infrastructure (Qdrant) using Docker Compose.

## Configuration

Edit variables in:

- `group_vars/all.yml` - Global settings
- `vars/dev_tools.yml` - Tool versions
- `vars/memory.yml` - Memory infrastructure settings

## Platform Support

- ✅ **Linux**: Full support
- ✅ **macOS**: Full support
- ⚠️ **Windows**: Use WSL2 or run roles individually

## Troubleshooting

### Ansible not found

```bash
pip install --user ansible
export PATH="$PATH:$HOME/.local/bin"
```

### Permission denied (Docker)

```bash
sudo usermod -aG docker $USER
# Log out and back in
```

### Pre-commit hooks not running

```bash
cd ../../  # Go to project root
pre-commit install
```

## Manual Verification

After running setup, verify installations:

```bash
# Check versions
python --version    # Should be 3.11+
dotnet --version    # Should be 8.0+
node --version      # Should be 18+
docker --version    # Should show Docker version
docker compose version

# Check pre-commit
pre-commit --version
pre-commit run --all-files

# Check memory infrastructure
cd infra/memory
docker compose ps  # Should show qdrant running
```

## Integration with Project

This Ansible setup is designed to work with:

- Pre-commit configuration in `.pre-commit-config.yaml`
- Memory infrastructure in `infra/memory/`
- Development tools required by `dotnet/`, Python scripts, and Node.js packages

## Next Steps

After running setup:

1. Verify all tools are installed correctly
2. Activate memory infrastructure if needed:
   ```bash
   cd infra/memory
   docker compose up -d
   ```
3. Set up your IDE/editor configurations
4. Run tests to verify everything works:
   ```bash
   pre-commit run --all-files
   dotnet test
   ```

## Contributing

When adding new roles:

1. Create role directory under `roles/`
2. Add `tasks/main.yml` with tasks
3. Update `setup-dev.yml` to include the role
4. Document in this README

## References

- Ansible Documentation: https://docs.ansible.com/
- Project structure based on: `C:\lunar-snake\personal-work\infra-projects\giantcroissant-lunar-infra\ansible`
