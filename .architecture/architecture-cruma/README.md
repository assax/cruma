# Cruma Architecture Profile

Human index. The machine-readable manifest is `PROFILE.md`.

- `shared/` — rules registry, solution structure, cross-cutting patterns and policies
- `server/` — ASP.NET Core server
- `clients/` — shared UI, thin clients, desktop
- `infra/` — containers, deployment, backups

Source architecture: `../../docs/arch/plan.md` and `../../docs/arch/DECISIONS.md`.

## Precedence

1. `shared/architecture-rules.md` (Rules)
2. Scope-specific Pattern / Policy documents
3. `shared/coding-conventions.md`

A conflict between this profile and `docs/arch/plan.md` is escalated to the operator.

## Changing the profile

A mandatory statement added to any Pattern, Policy, or Strategy document must also be registered in
`shared/architecture-rules.md` with a new RuleID (cross-registration). RuleIDs are never reused or renumbered.
