# Companion

Companion is a small WPF desktop utility (tmkbCompanion) containing UI views and MVVM components used by the project. This repository includes the Visual Studio solution and a WPF app targeting .NET 8.

## Contents

- `tmkbCompanion/` — WPF application and project files
- `MVVM/` — View, ViewModel and Core helpers used by the app
- `stitch_reference/` — static HTML reference files

## Prerequisites

- Windows 10/11
- .NET SDK 8.x (install from https://dotnet.microsoft.com)
- Visual Studio 2022/2023 (recommended) with the .NET desktop development workload, or use `dotnet` CLI

## Build & Run (CLI)

Open a terminal at the repository root and run:

```powershell
# restore and build the solution
dotnet restore
dotnet build tmkbCompanion/tmkbCompanion.csproj -c Debug

# run the app
dotnet run --project tmkbCompanion/tmkbCompanion.csproj -c Debug
```

Or open `tmkbCompanion.slnx` in Visual Studio and press F5.

## Tests

There are no automated tests in this repository. If you add test projects, include `dotnet test` instructions here.

## Contribution

- Create feature branches from `master` and open a PR when ready.
- Keep commits small and descriptive. Use the existing branch naming convention (e.g., `version-1.1-working`, `feature/xyz`).

## Backup & Releases

- Before large merges, create a backup branch (we create `backup-master-before-merge-YYYYMMDDHHMMSS` when merging).
- Tag releases as `v<major>.<minor>.<patch>`.

## License

This project is licensed under the MIT License. See the `LICENSE` file in the repository root for details.

---
