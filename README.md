# Eagles Nest

Eagles Nest is planned as a Microsoft-native web application for managing U.S. Military Vets Motorcycle Club members, chapters, dues, fines, rides, and officer-reviewed member updates.

## Source Control

This repository is configured for Visual Studio 2026 as the primary Git experience.

- Use `prod` for production releases.
- Use `dev` for active integrated development.
- Create feature branches from `dev`.
- Merge completed feature work back into `dev`.
- Promote `dev` to `prod` only for true releases.

The local shell does not currently expose `git` on `PATH`, so scripted commands should use the Visual Studio 2026 bundled Git executable:

```powershell
C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe
```

For convenience, use `tools\git-vs2026.ps1` from this repository root.
