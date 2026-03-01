# Team Workflow (GitHub + Unity)

This repository uses a PR-based workflow to keep `main` stable.
Everyone works in feature branches and opens Pull Requests (PRs). Only the Maintainer merges into `main`.

---

## Roles & Responsibilities

### Maintainer (Main Branch Owner)
- Owns and protects the `main` branch.
- Reviews PRs and merges approved changes into `main`.
- Resolves merge conflicts when needed (or assigns back to authors).
- Maintains repo standards (branch rules, templates, CI, releases/tags).

### Contributors
- Work on their own branches (no direct commits to `main`).
- Open PRs for review.
- Keep PRs focused and easy to review.
- Respond to review feedback and update PRs promptly.

---

## Branching Strategy

### Protected Branches
- `main`: always stable, shippable.
  - PR required
  - Maintainer-only merge

### Working Branches
Create a branch from the latest `main`.

**Branch naming (required)**
- `feature/<short-description>` — new gameplay/system/UI feature
- `fix/<short-description>` — bug fix
- `chore/<short-description>` — tooling, configs, repo maintenance
- `art/<short-description>` — art/audio/assets-only work
- `docs/<short-description>` — documentation changes
- `test/<short-description>` — experiments/testing work (if/when used)

**Examples**
- `feature/player-dash`
- `fix/camera-follow-jitter`
- `chore/update-unity-gitignore`
- `art/new-enemy-sprites`

---

## Local Workflow (Step-by-step)

### 1) Sync `main`
```bash
git checkout main
git pull origin main