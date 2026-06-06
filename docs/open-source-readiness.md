# Open-Source Readiness Checklist

| Area | Status | Evidence |
| --- | --- | --- |
| License | Ready | `LICENSE` |
| Contributing guide | Ready | `CONTRIBUTING.md` |
| Security policy | Ready | `SECURITY.md` |
| Code of conduct | Ready | `CODE_OF_CONDUCT.md` |
| Changelog | Ready | `CHANGELOG.md` |
| Architecture docs | Ready | `docs/architecture.md` |
| Roadmap | Ready | `docs/roadmap.md` |
| Threat model | Ready | `docs/threat-model.md` |
| Maintainer playbook | Ready | `docs/maintainer-playbook.md` |
| CI | Ready | `.github/workflows/dotnet-ci.yml` |
| Issue templates | Ready | `.github/ISSUE_TEMPLATE/*` |
| Pull request template | Ready | `.github/PULL_REQUEST_TEMPLATE.md` |
| Simulator evidence | Ready | `docs/debug/latest/full-pipeline/full-pipeline-qa-summary.md` |
| Real hardware commissioning | Access-limited | `.github/ISSUE_TEMPLATE/real-hardware-commissioning.md` |

## Maintainer Notes

- Public screenshots should always reflect the current WPF runtime, not older intermediate layouts.
- Simulator evidence is valuable, but it must remain clearly labeled as simulator-side verification.
- Do not remove historical debug evidence unless it is replaced with newer evidence that covers the same behavior.
- Keep preserved equipment values protected by tests and review.
