# GitHub Module Variables

variable "repository_name" {
  description = "Name of the GitHub repository"
  type        = string
}

variable "repository_description" {
  description = "Repository description"
  type        = string
}

variable "repository_visibility" {
  description = "Repository visibility (public/private)"
  type        = string
}

variable "github_owner" {
  description = "GitHub owner (username or organization)"
  type        = string
}

variable "repository_topics" {
  description = "List of topics/tags for the repository"
  type        = list(string)
  default     = []
}

# Repository Features
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

# Branch Protection
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
  description = "Require pull request reviews"
  type        = bool
  default     = false
}

variable "required_approving_review_count" {
  description = "Number of required approving reviews"
  type        = number
  default     = 1
}

variable "require_status_checks" {
  description = "Require status checks before merging"
  type        = bool
  default     = true
}

variable "required_status_checks" {
  description = "List of required status check contexts"
  type        = list(string)
  default     = []
}

# GitHub Actions
variable "github_actions_secrets" {
  description = "Map of GitHub Actions secrets"
  type        = map(string)
  sensitive   = true
  default     = {}
}

variable "github_actions_variables" {
  description = "Map of GitHub Actions variables (non-sensitive)"
  type        = map(string)
  default     = {}
}
