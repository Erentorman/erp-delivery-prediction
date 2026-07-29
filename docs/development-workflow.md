# Development Workflow

**Task:** T-107
**Status:** Draft for team review
**Audience:** All contributors to the ERP Delivery Prediction MVP

---

## 1. Purpose

This document defines the shared Git workflow, code collaboration rules, reviewer responsibilities, and Definition of Done for the ERP Delivery Prediction team.

The goal is to keep a small team moving quickly while protecting `main` and `develop` from unreviewed, unfinished, or unrelated changes. It replaces ad-hoc branching decisions with one shared, predictable flow.

This document does not define CI/CD pipelines, repository permission administration, advanced branch protection rules, enterprise release processes, or production deployment procedures. See [Out of Scope](#out-of-scope).

---

## 2. Branch Strategy

The team uses **persistent personal developer branches** as the normal way of working, not a new branch per Linear task.

Normal branch flow:

```
develop
  → developer/<name>
  → pull request into develop
  → develop
  → controlled release into main
```

Each developer completes their assigned Linear tasks directly in their own personal branch and opens pull requests from that branch into `develop`.

A task-specific `feature/T-xxx-short-description` branch is **optional** and used only in specific higher-risk situations (see [Section 7](#7-when-to-create-a-feature-branch)). It is not required for every task.

---

## 3. Branch Responsibilities

### `main`
- Contains stable and approved code.
- Represents controlled project releases.
- No one may push directly to `main`.
- Changes reach `main` only through a reviewed and controlled merge from `develop`.

### `develop`
- Shared integration branch.
- Completed and reviewed work is merged here.
- Team members must not normally push directly to `develop`.
- Pull requests must target `develop` unless the project lead explicitly approves another flow.

### `developer/<name>`
- Persistent personal working branch for each developer.
- The developer performs assigned Linear tasks in this branch.
- The branch is updated regularly from `develop`.
- The developer may commit multiple related task changes, but each PR must contain only the intended task's changes.
- Unrelated or unfinished work must not accidentally enter a PR.
- A developer must review the diff between their branch and `develop` before opening a PR.

### `feature/T-xxx-short-description`
- Optional, not mandatory.
- Used only when task isolation is necessary.
- Must be created from the latest `develop`.
- Must eventually open a PR into `develop`.
- Must not be created unnecessarily for every small task.

---

## 4. Branch Naming Convention

| Branch type | Pattern | Example |
|---|---|---|
| Personal developer branch | `developer/<name>` | `developer/eren`, `developer/yusufyuceur`, `developer/pinar` |
| Optional isolated feature branch | `feature/T-xxx-short-description` | `feature/T-108-ai-feature-builder` |

Personal developer branch names use the developer's first name or agreed handle, lowercase, no spaces.

---

## 5. Starting a New Task

1. Confirm the Linear task is assigned to you and its acceptance criteria are clear.
2. Decide whether the task can be done in your personal developer branch (default) or requires an isolated `feature/T-xxx` branch (see [Section 7](#7-when-to-create-a-feature-branch)).
3. Sync your personal branch with `develop` before starting (see [Section 6](#6-working-in-a-personal-developer-branch)).
4. Implement the task with focused, task-scoped commits.

---

## 6. Working in a Personal Developer Branch

Standard flow before starting work:

```bash
git checkout develop
git pull origin develop
git checkout developer/<name>
git merge develop
```

Then the developer works on the assigned task, commits, pushes, and opens a PR:

```
developer/<name> → develop
```

Before opening the PR, the developer must check exactly which changes will enter `develop`:

```bash
git diff develop...developer/<name>
git log develop..developer/<name> --oneline
```

This step is mandatory — persistent branches accumulate history, and this is the only reliable way to confirm the PR is clean. See [Section 13](#13-conflict-avoidance-rules).

---

## 7. When to Create a Feature Branch

A task-specific `feature/T-xxx-short-description` branch should only be created when:

- The work is large or high-risk.
- The change must be isolated from other work.
- Multiple developers will collaborate on the same task.
- The project lead explicitly requests a separate branch.
- The developer branch currently contains unrelated unfinished work that must not be included in the PR.

A feature branch is not required for every task. Default to working directly in your personal developer branch.

---

## 8. Keeping a Branch Updated

Update your personal developer branch from `develop` regularly — at the start of each task at minimum, and more often for longer-running tasks:

```bash
git checkout develop
git pull origin develop
git checkout developer/<name>
git merge develop
```

If the team prefers rebasing instead of merging to keep history linear, this is allowed as an **optional** method for individuals who are comfortable with it. Rebasing is not mandatory and must not be forced on the whole team.

---

## 9. Commit Message Convention

Required commit prefixes:

- `feat:`
- `fix:`
- `test:`
- `docs:`
- `refactor:`
- `chore:`

Examples:

```
docs: add development workflow
feat: add delivery prediction endpoint
fix: correct PostgreSQL health check
test: add prediction service tests
refactor: simplify order calculation service
chore: update Docker configuration
```

Keep commits small and task-focused. Do not mix unrelated task changes in one commit.

---

## 10. Pull Request Policy

- Team members normally open PRs from `developer/<name>` into `develop`.
- The PR base branch must be `develop`.
- The developer must verify the base and compare branches before merging.
- A PR must not accidentally target another developer's branch.
- Reviewer approval does not change the PR target branch.
- Before merging, verify again that the target is `develop`.
- The PR title should include the Linear task ID.
- The PR description must contain: **Summary**, **Changes**, **Validation**, and **Known Limitations**.
- A PR must contain only the intended task's changes.
- The author must inspect their own **Files Changed** tab before requesting review.
- Reviewer comments must be resolved or answered before merge.
- A task is not completed merely because it was merged into a developer branch — it is completed only after the reviewed change reaches `develop`.

---

## 11. Reviewer Responsibilities

Reviewers must:

- Check task scope and acceptance criteria.
- Check whether the PR targets `develop`.
- Check whether unrelated changes are included.
- Check architecture consistency (see `CLAUDE.md` and `docs/SAD-v1.1.md`).
- Check naming and readability.
- Check configuration and secrets.
- Check tests and local validation evidence.
- Distinguish blockers from recommendations and optional improvements.
- Not require out-of-scope production work during bootstrap/MVP tasks.
- Confirm that the PR can be safely merged into `develop`.

### Project Lead Rules

- The project lead reviews team PRs.
- Low-risk work completed by the project lead may be merged locally into `develop` after local verification.
- Shared-core, architectural, security-sensitive, and other high-risk changes should receive a second review, even from the project lead.
- Direct pushes to `main` are prohibited for everyone, including the project lead.

---

## 12. Risk-Based Review Rules

Not all changes carry the same risk. Apply judgment proportional to impact:

| Risk level | Examples | Review expectation |
|---|---|---|
| Low | Docs, config comments, isolated small fixes | Standard review; project lead may merge after local verification |
| Medium | New endpoints, new tests, non-shared feature code | Standard review, focused on scope and correctness |
| High | Domain/Application core logic, prediction engine, shared contracts, security, database schema | Second review recommended; extra care on architecture consistency and regression risk |

When in doubt about risk level, treat the change as higher risk and ask for a second opinion.

---

## 13. Conflict-Avoidance Rules

Persistent developer branches create a risk: if a developer branch contains multiple unfinished or unrelated tasks, all differences may appear in the next PR.

Prevention rules:

- Merge or isolate the current task before beginning unrelated work.
- Do not mix unrelated task changes in one commit.
- Check the PR **Files Changed** section before requesting review.
- Use task-specific feature branches when work must remain isolated.
- Keep commits small and task-focused.
- Update the personal branch from `develop` regularly.
- Do not open a PR containing previous unfinished work.
- After a PR is merged, synchronize the personal developer branch with `develop` before starting the next task.

---

## 14. Minimum Local Verification Before Push

Only run checks relevant to the task being completed.

**.NET** (when relevant):

```bash
dotnet restore
dotnet build
dotnet test
```

**Docker** (when relevant):

```bash
docker compose config -q
docker compose build
docker compose up
```

**Python / AI service** (when relevant):

- Activate or create the virtual environment.
- Install dependencies.
- Start the service.
- Test the health endpoint.

**Frontend** (when relevant):

```bash
npm ci
npm run build
npm run lint
```

---

## 15. Definition of Done

A task is done only when all of the following are true:

- Task scope and acceptance criteria are satisfied.
- Changes are limited to the intended task.
- Code or documentation is understandable.
- No real secrets are committed.
- Required local checks pass (see [Section 14](#14-minimum-local-verification-before-push)).
- Changes are committed with the correct prefix (see [Section 9](#9-commit-message-convention)).
- The developer branch is pushed.
- A PR is opened against `develop`.
- The author reviews their own diff.
- Required reviewer approval is received.
- Review comments are resolved.
- The target branch is verified as `develop`.
- The change is merged into `develop`.
- The personal developer branch is synchronized with `develop` afterward.
- The Linear task contains the PR or document link and final status.
- The team reviews and acknowledges the T-107 workflow.

---

## 16. Finishing and Merging a Task

1. Confirm all Definition of Done items are satisfied.
2. Verify the PR base branch is `develop`.
3. Confirm reviewer approval and that all comments are resolved or answered.
4. Merge the PR into `develop`.
5. Synchronize your personal developer branch with `develop`:

   ```bash
   git checkout develop
   git pull origin develop
   git checkout developer/<name>
   git merge develop
   ```

6. Update the Linear task with the PR link and final status.

---

## 17. Forbidden Actions

The following are prohibited:

- Direct push to `main`.
- Accidental PRs into another developer's branch.
- Combining unrelated tasks in one PR.
- Starting unrelated work when it will contaminate the next PR.
- Committing real `.env` files or secrets.
- Committing `bin`, `obj`, `.venv`, IDE, or other generated files.
- Merging unverified or unfinished work.
- Creating unnecessary branches without an isolation need.

---

## 18. Example End-to-End Workflow

Example: developer Eren completes Linear task T-110 in his personal branch.

```bash
# 1. Sync personal branch with develop
git checkout develop
git pull origin develop
git checkout developer/eren
git merge develop

# 2. Do the work, committing in small task-scoped steps
git add .
git commit -m "feat: add working calendar service"

# 3. Push personal branch
git push origin developer/eren

# 4. Before opening the PR, check exactly what will enter develop
git diff develop...developer/eren
git log develop..developer/eren --oneline

# 5. Open PR: developer/eren -> develop
#    Title: "T-110: Add working calendar service"
#    Description includes Summary, Changes, Validation, Known Limitations

# 6. Reviewer checks scope, target branch, architecture, tests, secrets

# 7. After approval and merge, sync personal branch again
git checkout develop
git pull origin develop
git checkout developer/eren
git merge develop

# 8. Update Linear task T-110 with PR link and mark as Done
```

---

## 19. Quick Checklist

Before opening a PR:

- [ ] Personal branch was synced from `develop` before starting.
- [ ] Commits use the correct prefix and are task-focused.
- [ ] `git diff develop...developer/<name>` reviewed — no unrelated/unfinished work included.
- [ ] Relevant local checks pass (.NET / Docker / Python / frontend, as applicable).
- [ ] No secrets, `.env` files, or generated/build artifacts committed.
- [ ] PR base branch is `develop`.
- [ ] PR title includes the Linear task ID.
- [ ] PR description has Summary, Changes, Validation, Known Limitations.

Before merging:

- [ ] Reviewer approval received.
- [ ] All review comments resolved or answered.
- [ ] Target branch re-verified as `develop`.
- [ ] After merge, personal developer branch synchronized with `develop`.
- [ ] Linear task updated with PR link and final status.

---

## Out of Scope

This document does not cover:

- GitHub Actions implementation.
- Repository permission administration.
- Advanced branch protection configuration.
- Enterprise release process.
- Production deployment process.
