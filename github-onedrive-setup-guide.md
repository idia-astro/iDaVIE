# GitHub + OneDrive Separation Guide
*iDaVIE Sub-team 3 — Cache Me If You Can*
*Written 26 May 2026 — take this to a new chat to get help setting it up*

---

## The Problem

The current working folder lives inside OneDrive:
```
C:\Users\catha\OneDrive - University of Limerick\iDaVIE\
```

OneDrive and Git do not work well together:
- OneDrive syncs files continuously and can lock files mid-operation
- Git's internal index files get partially written and corrupted by OneDrive sync
- Conflicts appear in `.git/` that are impossible to resolve cleanly
- On a second machine, OneDrive may sync a broken `.git/` state rather than a clean repo
- The result is a repo that looks fine on one machine and is broken on another

---

## The Fix — Two Separate Things, Two Separate Places

| What | Where | Why |
|------|-------|-----|
| Git repo (code, docs, diagrams) | Outside OneDrive — e.g. `C:\Projects\idavie-subteam3\` | Git needs full, uninterrupted file access |
| OneDrive folder | Keep as-is for personal notes, Cowork sessions | OneDrive is fine for plain files with no `.git/` folder |

---

## Step 1 — Get Collaborator Access

The team repo is at:
```
https://github.com/david-bol12/iDaVIE---Team-Alpha
```
Branch: `Team3-Docs-and-Examples`

Ask **david-bol12** to add you as a collaborator (Settings → Collaborators) before running any push commands.

---

## Step 2 — Run the Setup Script (Recommended)

A PowerShell script `setup-github.ps1` is in the OneDrive folder. It does every step automatically:

```powershell
# Right-click setup-github.ps1 → "Run with PowerShell"
# Or in a terminal:
cd "C:\Users\catha\OneDrive - University of Limerick\iDaVIE"
.\setup-github.ps1
```

If you prefer to run the steps manually, continue below. Otherwise skip to **Day-to-Day Workflow**.

---

## Step 2 (Manual) — Clone to a Folder Outside OneDrive

Open a terminal (PowerShell or Command Prompt) and run:

```powershell
# Create a projects folder outside OneDrive if you don't have one
mkdir C:\Projects

# Clone the team repo, checking out the sub-team 3 branch
git clone --branch Team3-Docs-and-Examples https://github.com/david-bol12/iDaVIE---Team-Alpha.git C:\Projects\idavie-team-alpha
```

You now have the repo at `C:\Projects\idavie-team-alpha\`.

---

## Step 3 (Manual) — Copy Your Existing Files In

Copy the contents of your OneDrive folder into the new Git folder:

```powershell
# Copy everything except any accidental .git folder
robocopy "C:\Users\catha\OneDrive - University of Limerick\iDaVIE" "C:\Projects\idavie-team-alpha" /E /XD .git
```

Check the result looks right:
```powershell
ls C:\Projects\idavie-team-alpha
```

---

## Step 4 (Manual) — Add a .gitignore

Before committing, create a `.gitignore` so Unity and OS junk doesn't get tracked.
Create the file at `C:\Projects\idavie-team-alpha\.gitignore` with this content:

```
# OS
.DS_Store
Thumbs.db
desktop.ini

# OneDrive conflict files
*conflict*

# Unity
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
*.csproj
*.unityproj
*.sln
*.suo
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.opendb
*.VC.db

# Rider / VS
.idea/
.vs/
*.user

# Logs
*.log
```

---

## Step 5 (Manual) — First Commit and Push

```powershell
cd C:\Projects\idavie-team-alpha

git add .
git commit -m "Initial commit — Sprint 2 state, 26 May 2026"
git push origin Team3-Docs-and-Examples
```

Your files are now on GitHub.

---

## Step 6 — On Your Second Machine (or Teammates)

On another machine, do **not** copy from OneDrive. Clone fresh from GitHub:

```powershell
mkdir C:\Projects
git clone --branch Team3-Docs-and-Examples https://github.com/david-bol12/iDaVIE---Team-Alpha.git C:\Projects\idavie-team-alpha
```

This gives everyone a clean, consistent copy.

---

## Day-to-Day Workflow After Setup

```powershell
# Start of a working session — get latest changes
cd C:\Projects\idavie-subteam3
git pull

# ... do your work ...

# End of session — save your work
git add .
git commit -m "Brief description of what changed"
git push
```

---

## Giving Teammates Access

Damien, Ciallian, and Chris need collaborator access too. Ask **david-bol12** to add each person:

1. Go to https://github.com/david-bol12/iDaVIE---Team-Alpha
2. Settings → Collaborators → Add people
3. Each teammate then clones:
   ```powershell
   git clone --branch Team3-Docs-and-Examples https://github.com/david-bol12/iDaVIE---Team-Alpha.git C:\Projects\idavie-team-alpha
   ```

---

## What to do With the OneDrive Folder

Do **not** delete it — it's still used by Cowork for docs-only sessions. Once everything is confirmed on GitHub:

1. Keep the OneDrive folder for Cowork/plain-file work only
2. Remove any `.git` folder from it if one exists
3. Do all code/diagram/doc editing in `C:\Projects\idavie-team-alpha\` and push to GitHub
4. If Cowork produces a new deliverable file, copy it into the Git folder and commit

---

## Quick Reference

| Task | Command |
|------|---------|
| Get latest from GitHub | `git pull` |
| See what's changed locally | `git status` |
| Stage all changes | `git add .` |
| Commit staged changes | `git commit -m "message"` |
| Push to GitHub | `git push` |
| See commit history | `git log --oneline` |
| Undo last commit (keep files) | `git reset --soft HEAD~1` |
