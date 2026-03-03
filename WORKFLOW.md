# Team Workflow (GitHub + Unity)

This repo uses a PR-based workflow to keep `main` stable.
All work happens in short-lived branches and is merged via Pull Requests (PRs).
Only the Maintainer merges into `main`.

---

## Roles & Responsibilities

### Maintainer (Main Branch Owner)
- Owns and protects `main`.
- Reviews PRs and merges approved changes into `main`.
- Resolves merge conflicts when needed (or requests changes from authors).
- Maintains repo standards (branch protection rules, templates, CI, releases/tags).

### Contributors
- Work in feature branches (no direct commits to `main`).
- Open PRs early and keep them small/focused.
- Respond to review feedback quickly.
- Rebase/update branches when requested.

---

## Branching Strategy

### Protected Branches
- `main`: always stable and shippable
  - PR required
  - Maintainer-only merge

### Working Branches (Required)
Create a branch from the latest `main`.

#### Branch naming
Format:
- `<type>/<issue-id>-<short-slug>`

Types:
- `feat/` — new gameplay/system/UI feature
- `fix/` — bug fix
- `chore/` — tooling/config/repo maintenance
- `art/` — art/audio/assets-only work
- `docs/` — documentation changes
- `test/` — experiments/testing work

Examples:
- `feat/123-player-dash`
- `fix/87-camera-follow-jitter`
- `chore/140-update-unity-gitignore`
- `art/52-new-enemy-sprites`

> If there is no issue/ticket, create one first. If you *must* work without a ticket, use `000` temporarily (e.g., `chore/000-build-fix`) and open an issue ASAP.

---

## Local Workflow (Golden Path)

### 1) Sync `main`
```bash
git checkout main
git pull --rebase origin main