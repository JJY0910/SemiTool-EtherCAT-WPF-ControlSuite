# Security Policy

## Supported Branch

Security and safety reports should target the default `main` branch unless a maintainer directs otherwise.

## Reporting

If you find a vulnerability, unsafe hardware behavior, or a path that can unexpectedly actuate outputs, please avoid publishing exploit details in a public issue first. Contact the repository maintainer through GitHub with:

- affected commit or branch
- reproduction steps
- expected versus actual behavior
- whether the issue requires real hardware, Simulator mode, or both
- logs or screenshots with secrets removed

## Safety-Sensitive Areas

Treat these areas as high risk:

- real EtherCAT connection and vendor DLL loading
- output writes, cylinder, vacuum, door, and servo commands
- auto sequence scheduling
- startup and reset paths
- preserved equipment profile values

## Secrets And Local Files

Do not commit vendor DLLs, machine-local settings, private network details, logs with operator data, or credentials.

## Verification Boundary

Public repository evidence is simulator-side WPF verification unless explicitly documented otherwise. Real hardware commissioning requires supervised access to the equipment and the checklist in `.github/ISSUE_TEMPLATE/real-hardware-commissioning.md`.
