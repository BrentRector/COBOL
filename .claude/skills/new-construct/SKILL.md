---
name: new-construct
description: Use when adding or edition-gating a COBOL construct, grammar rule, or reserved word - covers grammar fragment placement, the version predicate, the constructs.json row, the mandatory edition-gate test sweep, and goldens.
---

> **Generic base:** [`engineering-standards`](https://github.com/BrentRector/claude-skills/blob/main/skills/engineering-standards/SKILL.md) + [`variant-analysis`](https://github.com/BrentRector/claude-skills/blob/main/skills/variant-analysis/SKILL.md) in [BrentRector/claude-skills](https://github.com/BrentRector/claude-skills) — the reusable discipline, with the owner's bar in its sibling `engineering-standards`. THIS skill is the WiseOwl COBOL application of it and wins on conflict.

# New construct

All `.g4` changes are **pre-authorized** — implement directly, no approval round-trip. The protection is the gate
discipline, not the approval step.

## 1. Grammar placement

**Before you edit a file, ask which drift rules govern it:** `python scripts/spec/drift_rules.py <path> …` (generated from every `*DriftTests` summary; index at `docs/DRIFT_RULES.md`). Read the SPECIFIC rules it prints and honor them in the change — a drift test is a structural rule the gate will enforce anyway, and learning it from a red gate costs a cycle.

Post-85 features go in a dedicated `Core/*.g4` fragment imported by the core grammar, with a minimal
`{isXXXX()}?`-gated hook alternative in the core rule they extend. **Never inline post-85 rules into the COBOL-85
core** — the first OO attempt did that and caused deterministic LL regressions in COBOL-85 class-condition tests.

**ANTLR takes the first matching alternative.** Put the more specific alternative first; better, make the ambiguity
impossible by splitting over-broad rules. A new `statement` alternative must lead with a DISTINCT token, never
`IDENTIFIER` — when a verb-word doubles as a user-word, use token + `cobolWord` name slot.

Add rules INCREMENTALLY with a gate run after each; the shared parser regresses easily.

Direction of travel: the target architecture is a SUPERSET grammar where the edition predicates are REMOVED and
edition conformance becomes one pass over the bound tree. Read
`docs/rearchitecture/DESIGN-version-conformance-pipeline.md` before adding new gates.

`src/Cobol.Net.Frontend/Generated/` is a build output — gitignored, never committed. Edit the `.g4` and build; a
failed regeneration FAILS the build by design. `java` and `pwsh` are build prerequisites.

## 2. Parser and emitter ship together

Never land a parsed statement without its bound node, its emitter, and an end-to-end test asserting the OUTPUT
VALUE. Parsing something and emitting a no-op is worse than a compile error — the program runs and silently
produces wrong results.

## 3. The constructs.json row

Add the row to `tests/version-matrix/constructs.json` with `introducedIn` / `removedIn` / behavior variants and an
embedded `source` that compiles clean at the introducing edition and produces the gating diagnostic below it.

Every construct owes BOTH obligations: the per-edition behavior, and the correct diagnostic when the targeted
edition lacks or removed it. Diagnosing is half the product.

Claim the diagnostic code from `session-probe.ps1`, never by reading a list.

## 4. ⛔ The edition-gate sweep — the step that gets missed

Setting or raising `introducedIn` makes the compiler REJECT that construct below the new edition, breaking every
existing test and golden that compiles it there. **`VersionMatrixTests` passing does NOT clear this** — it only
checks the construct's own rows.

The sharpest trap: the differential tests compile their source at **edition 85 by default**, so a differential test
using a 2002+ construct suddenly fails to compile.

In the SAME change set:
1. `grep -rl "<CONSTRUCT SYNTAX>" tests/` — both `tests/**/*.cs` AND `tests/conformance/<edition>/`
2. For each hit compiling below the new edition, bump it to compile at >= `introducedIn`
3. Re-bake its golden (identical output proves the change was behavior-preserving)
4. Delete the orphaned old-edition golden — folder-level orphan detection will not catch a single stale file

## 5. Goldens and docs

Every post-1985 feature ships with a conformance test in the same commit (NIST covers only COBOL-85). Register it
in the right manifest — see the `land-a-fix` skill. Keep grammar docs in sync in the same commit.
