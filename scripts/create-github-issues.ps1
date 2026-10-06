<#
.SYNOPSIS
  Creates GitHub milestones + issues from ROADMAP.md using the GitHub CLI (gh).

.EXAMPLE
  # from the repo root, after `gh auth login` and pushing the repo:
  ./scripts/create-github-issues.ps1
  ./scripts/create-github-issues.ps1 -DryRun
#>
param(
    [string]$RoadmapPath = (Join-Path $PSScriptRoot "..\ROADMAP.md"),
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

if (-not $DryRun) {
    $repo = gh repo view --json nameWithOwner -q .nameWithOwner
    if (-not $repo) { throw "Run this inside the repo after it's pushed to GitHub (gh repo view failed)." }
    Write-Host "Creating issues in $repo"
}

$milestone = $null
$created = 0
$emDash = " $([char]0x2014) "   # avoid non-ASCII literals so Windows PowerShell 5.1 reads this file correctly
$existingMilestones = @()
if (-not $DryRun) {
    $existingMilestones = @(gh api "repos/$repo/milestones?state=all&per_page=100" --jq '.[].title')
}

foreach ($line in Get-Content $RoadmapPath -Encoding UTF8) {
    if ($line -match '^##\s+(.+)$') {
        $milestone = $Matches[1].Trim()
        Write-Host "`n== $milestone"
        if (-not $DryRun) {
            if ($existingMilestones -notcontains $milestone) {
                gh api "repos/$repo/milestones" -f "title=$milestone" | Out-Null
            }
        }
        continue
    }

    if ($line -match '^- \[ \]\s+(.+)$' -and $milestone) {
        $parts = $Matches[1] -split [regex]::Escape($emDash), 2
        $title = $parts[0].Trim()
        $body = if ($parts.Count -gt 1) { $parts[1].Trim() } else { "" }
        $body += "`n`n_From ROADMAP.md: $($milestone)_"

        Write-Host "  - $title"
        if (-not $DryRun) {
            gh issue create --title $title --body $body --milestone $milestone | Out-Null
        }
        $created++
    }
}

Write-Host "`nDone: $created issues$(if ($DryRun) { ' (dry run)' })."
