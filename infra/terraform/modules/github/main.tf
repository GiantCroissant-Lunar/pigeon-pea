# GitHub Repository Management Module
# Manages repository settings, branch protection, and secrets

terraform {
  required_providers {
    github = {
      source  = "integrations/github"
      version = "~> 6.0"
    }
  }
}

# ==============================================================================
# Repository Configuration
# ==============================================================================

# Note: This assumes the repository already exists
# If creating a new repository, uncomment the resource below

# resource "github_repository" "repo" {
#   name        = var.repository_name
#   description = var.repository_description
#   visibility  = var.repository_visibility
#
#   has_issues      = var.enable_issues
#   has_projects    = var.enable_projects
#   has_wiki        = var.enable_wiki
#   has_discussions = var.enable_discussions
#
#   allow_merge_commit     = true
#   allow_squash_merge     = true
#   allow_rebase_merge     = false
#   delete_branch_on_merge = true
#   allow_auto_merge       = true
#   allow_update_branch    = true
#
#   vulnerability_alerts = true
#
#   topics = var.repository_topics
#
#   lifecycle {
#     prevent_destroy = true
#   }
# }

# Data source to reference existing repository
data "github_repository" "repo" {
  full_name = "${var.github_owner}/${var.repository_name}"
}

# ==============================================================================
# Repository Settings (for existing repositories)
# ==============================================================================

# Note: github_repository_settings requires the repository to exist first
# This manages settings for an existing repository

# ==============================================================================
# Branch Protection Rules
# ==============================================================================

resource "github_branch_protection" "protected_branches" {
  for_each = var.enable_branch_protection ? toset(var.protected_branches) : []

  repository_id = data.github_repository.repo.node_id
  pattern       = each.value

  # Require pull request reviews
  required_pull_request_reviews {
    dismiss_stale_reviews           = true
    require_code_owner_reviews      = false
    required_approving_review_count = var.require_pull_request_reviews ? var.required_approving_review_count : 0
  }

  # Require status checks
  dynamic "required_status_checks" {
    for_each = var.require_status_checks ? [1] : []
    content {
      strict   = true
      contexts = var.required_status_checks
    }
  }

  # Enforce for administrators
  enforce_admins = false

  # Additional settings
  require_signed_commits  = false
  require_conversation_resolution = true

  # Lock branch (read-only)
  lock_branch = false

  # Allow force pushes and deletions
  allows_force_pushes = false
  allows_deletions    = false
}

# ==============================================================================
# GitHub Actions Secrets
# ==============================================================================

resource "github_actions_secret" "secrets" {
  for_each = var.github_actions_secrets

  repository      = data.github_repository.repo.name
  secret_name     = each.key
  plaintext_value = each.value
}

# ==============================================================================
# GitHub Actions Variables (non-sensitive)
# ==============================================================================

resource "github_actions_variable" "variables" {
  for_each = var.github_actions_variables

  repository    = data.github_repository.repo.name
  variable_name = each.key
  value         = each.value
}

# ==============================================================================
# Repository Topics
# ==============================================================================

# Topics are managed via the repository resource above
# This is included for completeness if managing an existing repo separately

# ==============================================================================
# Repository Webhooks (optional - for future use)
# ==============================================================================

# Example webhook configuration (commented out)
# Uncomment and configure when needed

# resource "github_repository_webhook" "webhook" {
#   repository = data.github_repository.repo.name
#
#   configuration {
#     url          = "https://your-endpoint.com/webhook"
#     content_type = "json"
#     insecure_ssl = false
#     secret       = var.webhook_secret
#   }
#
#   events = ["push", "pull_request", "issues"]
#   active = true
# }
