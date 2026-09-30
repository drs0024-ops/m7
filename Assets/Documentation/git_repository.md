# Git Workflow — M7

**Repo:** `https://github.com/drs0024-ops/m7.git`
**Branch:** `main` (single branch, no feature branches)
**Platform:** macOS, zsh terminal
**Backup strategy:** Push at end of every work session

---

## Daily Workflow

At the end of each session:

```bash
# 1. Stage only what matters (never use "git add .")
git add Assets/Game/ Assets/Scenes/ Assets/Tests/ Assets/Documentation/ ProjectSettings/

# 2. Commit with a short description
git commit -m "short present-tense description" 


# 3. Push to GitHub
git push

One-liner version:

git add Assets/Game/ Assets/Scenes/ Assets/Tests/ Assets/Documentation/ ProjectSettings/ && git commit -m "description" && git push

Commit Message Convention
Present tense, no period
Under 72 characters
Optional multi-line body for large changes
# Simple:
git commit -m "Add invisibility upgrade handler"

# Large batch:
git commit -m "System upgrade: tests, save versioning, cutscenes, analytics

- 21 unit tests (GameStateMachine, SaveMigration)
- Save versioning + migration chain (v1)
- Cutscene system (CutscenePlayer, CutsceneSelector, CutscenePoolSO)
- Analytics hooks (IAnalyticsReporter + Console impl)"

What Gets Committed
Included	Why
Assets/Game/	All C# code, asmdefs, ScriptableObjects
Assets/Scenes/	Scene files
Assets/Tests/	Test code + asmdef
Assets/Documentation/	.md files
ProjectSettings/	Build config, input, player settings
Packages/	manifest.json, packages-lock.json
.gitignore, .gitattributes	Repo config

What Stays Local (in .gitignore)
Excluded	Why
Assets/Art/, Assets/Sprites/, Assets/Animation/	Large binary media
Assets/Game/AudioFiles/, Assets/Game/FREE Adventure Music Pack/	Audio assets
Assets/Fonts/, Assets/Materials/, Assets/Prefabs/	Binary assets
Assets/URP/, Assets/LeanTween/, Assets/TextMesh Pro/, Assets/Plugins/	Third-party packages (restored via Unity Package Manager)
Library/, Temp/, Temp*/	Unity regenerates these
mono_crash.*, *.mem.*	Crash dumps
*-Control/, Bone Yard Escape	Xcode build artifacts
*.slnx, *.csproj, *.sln	IDE files

If you add a new large asset folder: add it to .gitignore before committing.

Useful Commands
Task	Command
Check what's changed	git status
See last 5 commits	git log --oneline -5
See what's in a commit	git show --stat HEAD
Undo last commit (keep changes)	git reset HEAD~1
Undo a staged file	git restore --staged <file>
Undo changes to a file	git restore <file>
Check remote is correct	git remote -v
Verify you're synced	git log origin/main --oneline -1
Check repo size	git count-objects -vH
Find large files	git rev-list --objects --all | git cat-file --batch-check='%(objecttype) %(objectname) %(objectsize) %(rest)' | sort -k3 -n -r | head -20
Remove a file from tracking (keep on disk)	git rm --cached <file>
Force push (dangerous, rewrites history)	git push --force origin main

Troubleshooting
Problem	Fix
fatal: 'origin' does not appear to be a git repository	git remote add origin https://github.com/drs0024-ops/m7.git
error: RPC failed; HTTP 400	git config http.postBuffer 524288000 then push again
Accidentally committed a large file	git rm --cached <file> → add to .gitignore → commit → push. File stays on disk.
Need to rename the repo	GitHub Settings → Danger Zone → Rename. Then: git remote set-url origin https://github.com/drs0024-ops/NEW_NAME.git
Need to work offline	Commit locally. Push when you have internet.
Accidentally deleted a file	git restore <file> (if not committed) or git show HEAD~1:<file> > <file> (if committed)

Future: CI/CD (When Added)
When GitHub Actions is set up (A+ Item 1):

Workflow file: .github/workflows/unity-tests.yml
Triggers: on: [push, pull_request]
Runs: Edit Mode tests via game-ci/unity-test-runner
Fails the build if any test fails
You still push manually — CI just runs tests automatically after each push
Rules
Never git add . — always specify folders. Prevents temp files, crash dumps, and build artifacts.
Push at the end of every session. Local commits are not a backup.
If it's over 50MB, it doesn't go in Git. Add it to .gitignore.
If you break something: git log --oneline -5 → find the last good commit → git reset --hard <hash> (destroys uncommitted work) or git revert <hash> (safe, adds a new commit).
When in doubt, git status first. It tells you exactly where you are.