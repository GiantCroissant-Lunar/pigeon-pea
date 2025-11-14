# 🎮 pigeon-pea - Terraform GitHub Automation
# Manages GitHub repository infrastructure as code
# Based on lunar-snake infrastructure patterns

terraform {
  required_version = ">= 1.0"
  required_providers {
    github = {
      source  = "integrations/github"
      version = "~> 6.0"
    }
  }

  # Terraform Cloud configuration for remote state management
  # Uncomment and configure when ready to use Terraform Cloud
  # cloud {
  #   organization = "your-terraform-cloud-org"
  #   workspaces {
  #     name = "pigeon-pea"
  #   }
  # }

  # Alternative: Local backend (default)
  # State will be stored in terraform.tfstate locally
  # For team collaboration, use Terraform Cloud or S3 backend
}

# GitHub Provider Configuration
provider "github" {
  token = var.github_token
  owner = var.github_owner
}

# 🏗️ GitHub Repository Management Module
module "github_repository" {
  source = "./modules/github"

  # Repository configuration
  repository_name        = var.repository_name
  repository_description = var.repository_description
  repository_visibility  = var.repository_visibility
  github_owner          = var.github_owner

  # Topics/tags
  repository_topics = var.repository_topics

  # Branch protection
  enable_branch_protection = var.enable_branch_protection
  protected_branches       = var.protected_branches

  # Secrets for GitHub Actions
  github_actions_secrets = var.github_actions_secrets

  # Features
  enable_issues      = var.enable_issues
  enable_projects    = var.enable_projects
  enable_wiki        = var.enable_wiki
  enable_discussions = var.enable_discussions
}
