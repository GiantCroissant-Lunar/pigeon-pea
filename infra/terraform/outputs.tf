# 📊 Terraform Outputs for pigeon-pea Infrastructure
# Display useful information after apply

output "repository_url" {
  description = "GitHub repository URL"
  value       = module.github_repository.repository_url
}

output "repository_html_url" {
  description = "GitHub repository HTML URL"
  value       = module.github_repository.repository_html_url
}

output "repository_ssh_clone_url" {
  description = "GitHub repository SSH clone URL"
  value       = module.github_repository.repository_ssh_clone_url
}

output "repository_https_clone_url" {
  description = "GitHub repository HTTPS clone URL"
  value       = module.github_repository.repository_https_clone_url
}

output "repository_visibility" {
  description = "Repository visibility (public/private)"
  value       = module.github_repository.repository_visibility
}

output "protected_branches" {
  description = "List of protected branches"
  value       = module.github_repository.protected_branches
}

output "configured_secrets" {
  description = "List of configured GitHub Actions secrets (names only)"
  value       = module.github_repository.configured_secrets
}

output "repository_topics" {
  description = "Repository topics/tags"
  value       = module.github_repository.repository_topics
}
