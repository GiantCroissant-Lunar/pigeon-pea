# Ansible Quick Start

One-page reference for setting up pigeon-pea development environment with Ansible.

## Prerequisites

```bash
pip install ansible
```

## Common Commands

### Full Development Setup

```bash
cd infra/ansible
ansible-playbook setup-dev.yml
```

### Individual Components

```bash
# Python only
ansible-playbook setup-dev.yml --tags python

# .NET only
ansible-playbook setup-dev.yml --tags dotnet

# Docker only
ansible-playbook setup-dev.yml --tags docker

# Pre-commit only
ansible-playbook setup-dev.yml --tags precommit

# Memory infrastructure
ansible-playbook setup-memory.yml
```

### Verification

```bash
# After setup, verify installations
python3.11 --version
dotnet --version
node --version
docker --version
pre-commit --version
```

### Memory Infrastructure

```bash
# Start memory system
ansible-playbook setup-memory.yml

# Test memory
python ../memory/scripts/test-visibility.py

# Check Qdrant
curl http://localhost:6333/health

# Stop memory system
ansible-playbook teardown-memory.yml

# Stop and remove data (dangerous!)
ansible-playbook teardown-memory.yml -e remove_data=true
```

## Platform-Specific Notes

### Linux (Debian/Ubuntu)

Most playbooks work out of the box. May need `--ask-become-pass` for sudo.

```bash
ansible-playbook setup-dev.yml --ask-become-pass
```

### macOS

Uses Homebrew for package management. Ensure Homebrew is installed first:

```bash
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```

### Windows (WSL2)

Run from WSL2 Ubuntu/Debian distribution:

```bash
# Inside WSL2
cd /mnt/c/Users/YourName/path/to/pigeon-pea
cd infra/ansible
ansible-playbook setup-dev.yml
```

## Troubleshooting

### Ansible not found

```bash
pip install --user ansible
export PATH="$PATH:$HOME/.local/bin"
```

### Docker permission denied

```bash
sudo usermod -aG docker $USER
# Log out and back in
```

### Pre-commit not running

```bash
cd ../../  # Go to project root
pre-commit install --install-hooks
pre-commit run --all-files
```

### Qdrant won't start

```bash
docker ps  # Check if already running
docker compose -f ../memory/docker-compose.yml up -d
docker logs pigeon-pea-qdrant
```

## Next Steps

After running Ansible setup:

1. ✅ Verify all tools installed
2. ✅ Test pre-commit: `pre-commit run --all-files`
3. ✅ Test memory: `python infra/memory/scripts/test-visibility.py`
4. ✅ Configure Claude Code MCP (see `infra/memory/README.md`)
5. ✅ Run tests: `dotnet test`

## Customization

Edit these files to customize setup:

- `vars/dev_tools.yml` - Tool versions
- `vars/memory.yml` - Memory infrastructure config
- `group_vars/all.yml` - Global settings

## Help

For detailed documentation:

- Ansible: `infra/ansible/README.md`
- Memory: `infra/memory/README.md`
- Infrastructure: `infra/README.md`
