# Git Workflow

## Preferred Local Workflow

Use Visual Studio 2026 for normal source-control work:

- Branch creation and switching
- Reviewing changed files
- Commits
- Pulls and pushes
- Merge conflict resolution
- GitHub repository connection

## Branch Model

- `prod`: production release branch
- `dev`: active development integration branch
- Feature branches: created from `dev`

Release promotion is done by merging `dev` into `prod` only when a release is ready.

## Scripted Git

When using PowerShell or automation in this workspace, call the Visual Studio bundled Git executable directly:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe" status
```

Or use the helper script:

```powershell
.\tools\git-vs2026.ps1 status
.\tools\git-vs2026.ps1 switch dev
.\tools\git-vs2026.ps1 switch -c feature/example
```

## GitHub Remote

After the GitHub repository is created, connect it from Visual Studio 2026 or add the remote with the helper script:

```powershell
.\tools\git-vs2026.ps1 remote add origin https://github.com/<owner>/<repo>.git
.\tools\git-vs2026.ps1 push -u origin dev
.\tools\git-vs2026.ps1 push -u origin prod
```
