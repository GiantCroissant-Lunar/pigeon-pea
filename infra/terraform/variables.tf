# 🔧 Terraform Variables for pigeon-pea Infrastructure
# Define all configurable values here

# ==============================================================================
# GitHub Provider Variables
# ==============================================================================

variable "github_token" {
  description = "GitHub Personal Access Token with repo, admin:repo_hook permissions"
  type        = string
  sensitive   = true

  # Required scopes:
  # - repo (full control)
  # - admin:repo_hook (webhooks)
  # - workflow (GitHub Actions)
  # See: https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token
}

variable "github_owner" {
  description = "GitHub username or organization name"
  type        = string

  # Example: "your-username" or "your-org"
}

# ==============================================================================
# Repository Configuration
# ==============================================================================

variable "repository_name" {
  description = "Name of the GitHub repository"
  type        = string
  default     = "pigeon-pea"
}

variable "repository_description" {
  description = "Repository description"
  type        = string
  default     = "A roguelike game with procedural map generation and dungeon crawling featuring multi-domain architecture (Map, Dungeon) built with .NET, GoRogue, and Terminal.Gui"
}

variable "repository_visibility" {
  description = "Repository visibility: public or private"
  type        = string
  default     = "private"

  validation {
    condition     = contains(["public", "private"], var.repository_visibility)
    error_message = "Repository visibility must be either 'public' or 'private'."
  }
}

variable "repository_topics" {
  description = "Topics/tags for repository discoverability"
  type        = list(string)
  default = [
    "roguelike",
    "game-development",
    "procedural-generation",
    "dotnet",
    "csharp",
    "terminal-ui",
    "goRogue",
    "ecs",
    "arch",
    "dungeon-crawler",
    "map-generation",
    "fantasy-map",
    "terminal-gui",
    "sadconsole",
    "skia",
    "mapsui"
  ]
}

# ==============================================================================
# Repository Features
# ==============================================================================

variable "enable_issues" {
  description = "Enable GitHub Issues"
  type        = bool
  default     = true
}

variable "enable_projects" {
  description = "Enable GitHub Projects"
  type        = bool
  default     = true
}

variable "enable_wiki" {
  description = "Enable GitHub Wiki"
  type        = bool
  default     = false
}

variable "enable_discussions" {
  description = "Enable GitHub Discussions"
  type        = bool
  default     = true
}

# ==============================================================================
# Branch Protection
# ==============================================================================

variable "enable_branch_protection" {
  description = "Enable branch protection rules"
  type        = bool
  default     = true
}

variable "protected_branches" {
  description = "List of branches to protect"
  type        = list(string)
  default     = ["main"]
}

variable "require_pull_request_reviews" {
  description = "Require PR reviews before merging"
  type        = bool
  default     = false  # Set to true for team collaboration
}

variable "required_approving_review_count" {
  description = "Number of required approving reviews"
  type        = number
  default     = 1
}

variable "require_status_checks" {
  description = "Require status checks to pass before merging"
  type        = bool
  default     = true
}

variable "required_status_checks" {
  description = "List of required status checks"
  type        = list(string)
  default = [
    "pre-commit",
    "build",
    "test"
  ]
}

# ==============================================================================
# GitHub Actions Secrets
# ==============================================================================

variable "github_actions_secrets" {
  description = "Map of secret names to values for GitHub Actions"
  type        = map(string)
  sensitive   = true
  default     = {}

  # Example:
  # {
  #   "OPENAI_API_KEY"     = "sk-..."
  #   "GCP_SA_KEY"         = "{...}"
  #   "ANTHROPIC_API_KEY"  = "sk-ant-..."
  # }
}

# ==============================================================================
# Environment Variables (Non-sensitive)
# ==============================================================================

variable "github_actions_variables" {
  description = "Map of variable names to values for GitHub Actions (non-sensitive)"
  type        = map(string)
  default     = {}

  # Example:
  # {
  #   "DOTNET_VERSION"   = "8.0"
  #   "PYTHON_VERSION"   = "3.11"
  #   "QDRANT_VERSION"   = "latest"
  # }
}

# ==============================================================================
# Advanced Settings
# ==============================================================================

variable "allow_merge_commit" {
  description = "Allow merge commits"
  type        = bool
  default     = true
}

variable "allow_squash_merge" {
  description = "Allow squash merging"
  type        = bool
  default     = true
}

variable "allow_rebase_merge" {
  description = "Allow rebase merging"
  type        = bool
  default     = false
}

variable "delete_branch_on_merge" {
  description = "Automatically delete head branches after PR merge"
  type        = bool
  default     = true
}

variable "enable_auto_merge" {
  description = "Enable auto-merge for pull requests"
  type        = bool
  default     = true
}

variable "vulnerability_alerts" {
  description = "Enable vulnerability alerts"
  type        = bool
  default     = true
}
