# Contributing to EcoShift-VR

Thank you for contributing! This document describes the branching strategy, folder ownership, and workflow conventions for all six project roles.

---

## Table of Contents

- [Branch Strategy](#branch-strategy)
- [Branch Naming](#branch-naming)
- [Folder Ownership](#folder-ownership)
- [Workflow](#workflow)
- [Commit Messages](#commit-messages)
- [Pull Requests](#pull-requests)
- [Code Review](#code-review)

---

## Branch Strategy

EcoShift-VR uses a **trunk-based development** model with two long-lived branches and short-lived feature branches.

| Branch | Purpose | Who can push directly? |
|---|---|---|
| `main` | Stable, demo-ready builds only | Nobody — merge via PR with 2 approvals |
| `develop` | Integration branch; always buildable | Nobody — merge via PR with 1 approval |

All day-to-day work happens in **feature branches** cut from `develop` and merged back into `develop` via Pull Request.

---

## Branch Naming

Use the pattern:

```
<role>/<short-description>
```

| Role | Prefix | Responsible for |
|---|---|---|
| P1 – Unity Lead | `p1/` | Repo setup, Unity project config, build pipeline, device testing, performance |
| P2 – Shared Systems | `p2/` | Oculus SDK, EcosystemManager, Analytics Logger, FactPanel UI, scenario wiring |
| P3 – Terrestrial Dev | `p3/` | Pangolin FP camera, quadrupedal locomotion, digging, Scenario 1 & 2 gameplay |
| P4 – Marine Dev | `p4/` | Shark FP camera, swimming controller, underwater shader, Scenario 1 & 2 gameplay |
| P5 – UX + Assets | `p5/` | Animal geometry (paws/fins/snouts), environments, props, lighting, video recording |
| P6 – Research + Design | `p6/` | Surveys, IRB, interview guide, scenario brief, narration script, user study |

### Examples

```
p1/unity-project-init
p1/build-pipeline-android
p2/ecosystem-manager-base
p2/fact-panel-ui
p3/pangolin-fp-camera
p3/scenario-1-digging
p4/shark-swim-controller
p4/underwater-shader
p5/pangolin-rig
p5/coral-reef-environment
p6/irb-survey-draft
p6/narration-script-v1
```

> **Hotfix branches** that target `main` directly must use the prefix `hotfix/` and require 2 PR approvals before merge.

---

## Folder Ownership

Each role owns one or more top-level folders under `Assets/`. Avoid editing another role's folder without coordinating with the owner first.

| Folder | Primary owner(s) |
|---|---|
| `Assets/_Shared/` | P2 – Shared Systems (SDK wrappers, shared managers, UI prefabs) |
| `Assets/Terrestrial/` | P3 – Terrestrial Dev (pangolin scripts, animations, prefabs) |
| `Assets/Marine/` | P4 – Marine Dev (shark scripts, shaders, underwater FX) |
| `Assets/Video/` | P5 – UX + Assets (video capture assets, recording utilities) |
| `Assets/Scenes/` | P1 – Unity Lead (scene files, lighting settings, build configuration) |

P5 contributes art assets (meshes, textures, materials) to `Terrestrial/`, `Marine/`, and `Scenes/` — coordinate with P3/P4/P1 before merging art PRs that touch those folders.

P6 stores research artifacts (survey instruments, IRB documents, scripts) in the `Docs/` or `Research/` folder at the repo root (not under `Assets/`).

---

## Workflow

1. **Create a branch** from the latest `develop`:
   ```bash
   git checkout develop
   git pull origin develop
   git checkout -b p3/scenario-2-digging
   ```

2. **Work in small commits** (see [Commit Messages](#commit-messages)).

3. **Keep your branch up to date** with `develop` using merge (not rebase):
   ```bash
   git fetch origin
   git merge origin/develop
   ```

4. **Open a Pull Request** against `develop` when your work is ready for review.

5. **Address review feedback**, get approval(s), then merge using **Squash and Merge** so `develop` history stays readable.

6. **Delete your feature branch** after it is merged.

---

## Commit Messages

Follow the format:

```
<type>(<scope>): <short summary>
```

| Type | Use for |
|---|---|
| `feat` | New feature or gameplay mechanic |
| `fix` | Bug fix |
| `art` | New or updated art assets (meshes, textures, audio) |
| `docs` | Documentation or research files only |
| `config` | Unity project settings, build config, package manifests |
| `refactor` | Code restructuring without behavior change |
| `test` | Test scenes or automated tests |

**Scope** should be the folder or system being changed (e.g., `shared`, `terrestrial`, `marine`, `scenes`, `video`).

Examples:
```
feat(terrestrial): add pangolin digging animation state machine
fix(shared): resolve EcosystemManager null-ref on scene load
art(marine): import shark low-poly mesh and UV maps
config(p1): update Android build target to API 32
docs(p6): add IRB consent form v2
```

---

## Pull Requests

- **Title:** same format as commit messages above.
- **Description:** briefly describe what changed and why; link any related issues.
- **Approvals required:**
  - Feature branch → `develop`: **1 approval**
  - `develop` → `main`: **2 approvals** (P1 must be one of them)
  - `hotfix/*` → `main`: **2 approvals**
- **Merge strategy:** Squash and Merge for feature branches; Merge Commit for `develop` → `main` releases.
- Do not merge your own PR without at least one reviewer approval.

---

## Code Review

- Review within **2 business days** of a PR being opened.
- Check that changes stay within the reviewer's folder scope; flag cross-folder changes for the folder owner.
- For Unity scenes (`.unity`) and prefabs (`.prefab`), confirm there are no accidental object deletions or transform resets introduced by merge conflicts.
- For scripts, ensure public APIs used by other roles remain backward-compatible, or coordinate breaking changes with affected roles before merging.
