# Contributing to OpenSSH-GUI

First off, thank you for considering contributing! 

This is a **hobby project** maintained by one person (currently) in spare time. Every contribution — whether a bug report, feature suggestion, documentation fix, or code PR—is genuinely appreciated.

## Table of Contents

1. [Code of Conduct](#code-of-conduct)
2. [Getting Started](#getting-started)
3. [How to Contribute](#how-to-contribute)
4. [Development Setup](#development-setup)
5. [Pull Request Guidelines](#pull-request-guidelines)
6. [Questions?](#questions)

---

## Code of Conduct

Be respectful, constructive, and patient. We're all learning. This is a friendly space.

---

## Getting Started

### Before You Start
- ✅ **Search existing issues** - maybe someone already reported it!
- ✅ **Update to the latest version** - bug might already be fixed
- ✅ **For questions** → Use [Discussions](https://github.com/frequency403/OpenSSH-GUI/discussions) instead of creating an Issue

### Types of Contributions

| Type | Effort | How to Start |
|------|--------|--------------|
| Bug Report | ~10 min | [Open Issue](https://github.com/frequency403/OpenSSH-GUI/issues/new?template=bug-report.yml) |
| Feature Idea | ~10 min | [Start Discussion](https://github.com/frequency403/OpenSSH-GUI/discussions) |
| Documentation Fix | ~30 min | Edit file → Propose Change |
| Code Fix/Feature | 1h+ | Fork → Branch → PR → Wait for review |
| Testing/Feedback | ~15 min | Test pre-releases, give feedback |

---

## How to Contribute

### Reporting Bugs

Use the [Bug Report Template](.github/ISSUE_TEMPLATE/bug-report.yml). Include:
- Exact steps to reproduce
- Screenshots/logs if relevant
- Your environment (OS, version, installation method)

### Suggesting Features

Start a [Discussion](https://github.com/frequency403/OpenSSH-GUI/discussions) first. We'll create a formal Issue if the idea aligns with the project direction.

### Contributing Code

1. **Find an Issue**
    - Look for [open issues](https://github.com/frequency403/OpenSSH-GUI/issues?q=is%3Aissue+is%3Aopen) or [`help wanted`](https://github.com/frequency403/OpenSSH-GUI/issues?q=is%3Aissue+is%3Aopen+label%3A%22help+wanted%22) labels

2. **Comment on the Issue or Assign yourself**
    - Say "I'd like to work on this" so others don't duplicate effort

3. **Fork & Create Branch**
   ```bash
   git clone https://github.com/YOUR_USERNAME/OpenSSH-GUI.git
   cd OpenSSH-GUI
   git checkout -b feature/your-feature-name

4. Make Changes

- Follow existing code style
- Keep commits focused (one thing per commit)
- Write meaningful commit messages

5. Test Locally
    ```bash
    dotnet restore
    dotnet build
    dotnet run
    ```

6. Submit PR

- Reference the issue number: Closes #123
- Fill out the PR template completely

## Development Setup

### Prerequisites
   - .NET SDK 8.0+ (or .NET 10 for latest)
   - Git
   - IDE: Visual Studio, VS Code, or Rider
### First Build

    ```bash
    # Clone repository
    git clone https://github.com/frequency403/OpenSSH-GUI.git
    cd OpenSSH-GUI
    
    # Restore dependencies
    dotnet restore
    
    # Run development build
    dotnet run
    
    # Build release
    dotnet publish -c Release -r <your-runtime> --self-contained false
    ```

## Architecture Overview
    ```bash
    OpenSSH-GUI/
    ├── OpenSSH_GUI.Core/     # Core logic (key parsing, SSH operations)
    ├── OpenSSH_GUI.Dialogs/  # UI dialogs and modals
    ├── OpenSSH_GUI.SshConfig/ # SSH configuration management
    └── installer/            # Installer scripts/packages
    ```
## Pull Request Guidelines
Do:

- Keep PRs small and focused
- Write clear descriptions
- Link related issues
- Be responsive to review comments

Don't:

- Mix unrelated changes in one PR
- Ignore failing builds/tests
- Expect instant merge (review time varies)
- Review Timeline: As maintainer, I try to respond within 48 hours, but delays happen. Ping me politely if silence extends beyond a week.

## Questions?
Join the conversation in GitHub Discussions!

Or reach out via:

- Open a Discussion thread
- Check existing discussions for similar questions

## Thank You!
Every contribution makes this project better—even reporting a typo in the docs counts! ❤️