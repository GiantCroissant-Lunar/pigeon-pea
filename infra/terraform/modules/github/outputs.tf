# GitHub Module Outputs

output "repository_url" {
  description = "Repository URL"
  value       = data.github_repository.repo.url
}

output "repository_html_url" {
  description = "Repository HTML URL"
  value       = data.github_repository.repo.html_url
}

output "repository_ssh_clone_url" {
  description = "Repository SSH clone URL"
  value       = data.github_repository.repo.ssh_clone_url
}

output "repository_https_clone_url" {
  description = "Repository HTTPS clone URL"
  value       = data.github_repository.repo.http_clone_url
}

output "repository_visibility" {
  description = "Repository visibility"
  value       = data.github_repository.repo.visibility
}

output "repository_topics" {
  description = "Repository topics"
  value       = data.github_repository.repo.topics
}

output "protected_branches" {
  description = "List of protected branches"
  value       = var.enable_branch_protection ? var.protected_branches : []
}

output "configured_secrets" {
  description = "List of configured secret names (values are sensitive)"
  value       = keys(var.github_actions_secrets)
  sensitive   = false
}

output "configured_variables" {
  description = "List of configured variable names"
  value       = keys(var.github_actions_variables)
}
