---
title: Modernization — Audit Artifacts
area: modernization
status: draft
last_updated: 2026-07-23
related_files:
  - docs/rearchitecture/CODE-SPEC-AUDIT.md
  - docs/rearchitecture/DESIGN-SPEC-RECONCILIATION.md
  - docs/rearchitecture/PHASE-13-plan-vs-spec-review.md
  - docs/rearchitecture/CONFORMANCE-FIX-QUEUE.md
  - create-agents.ps1
  - create-auditartifacts.ps1
tags:
  - cobolsharp
  - modernization
---

# Modernization — Audit Artifacts

Two complementary spec-conformance audits feed one prioritized fix queue
([[kb/Modernization/Tasks]]).

## Code ↔ Spec audit — `CODE-SPEC-AUDIT.md` (CA1–CA38)
14 agents (one per behavioral area — accept/display, arithmetic, conditions, exceptions & checking, files-io,
intrinsics, move-convert, perform-flow, picture-usage, tables-refmod, inspect-string, editions-gating, interprogram,
oo) adversarially checking the *implementation* against ISO. **38 findings, 24 blocker/major.** Each is a three-part
record — **Spec** (§ + GR/SR), **Code** (exact file:line of the defect), **Fails** (a concrete program + spec-correct
vs actual output). Example CA31/CA32 (blockers): a multi-level inline `PERFORM VARYING` emitted nested C# `while`
loops, so a bare `break` for `EXIT PERFORM` exited only the innermost loop and `continue` for `EXIT PERFORM CYCLE`
skipped the induction augment — fixed with `goto`-to-label machinery.

## Design ↔ Spec reconciliation — `DESIGN-SPEC-RECONCILIATION.md`
15 agents (one per behavior-bearing design doc). **54 findings across 13 of 15 docs**, categorized (spec-wrong,
mis-cited, incomplete-to-spec, legacy-port-unverified). The 6 findings tagged **CODE-BUG?** (design is spec-wrong AND
the code implements it) become the **V54–V59** entries in the fix queue. Findings are fixed by *rewriting the design
doc to read as always-correct* (no was-X-now-Y — that lives only in DEVLOG). See
[[kb/Context/Doctrine & Anti-Patterns]].

## PHASE-13 review ledger — `PHASE-13-plan-vs-spec-review.md` (§24 = the fix SSOT)
A 16-finder multi-agent review (184 agents launched, 85 completed, ~7.7M tokens; a session outage killed 99 mid-run →
partial coverage). **28 findings confirmed** (1 critical, 14 major, 13 minor). The one critical: diagnostic code
**COBOLNET1573 shipped with two meanings**. §24 is the 10-tier prioritized fix queue.

## The two PowerShell audit scripts (early scaffolding, legacy-era)
- **`create-agents.ps1`** — writes 10 role-scoped agent YAML files into `agents/` (pipeline-architecture,
  preprocessor, grammar-parsing, semantics, flow-bound, ir-lowering, cil-emission-runtime, nist-harness, docs-dx,
  legacy-compat), each owning a phase band and a modernization-ledger slice.
- **`create-auditartifacts.ps1`** — scaffolds `audit/plans/` with four planning templates:
  `ModernizationLedgerTemplate.md`, `NistEnablementRoadmap.md`, `GrammarGapClosurePlan.md`,
  `SemanticValidatorHardeningPlan.md`.

> The two `.ps1` scripts predate the pivot (byte-engine vocabulary: phases 0–21, IR lowering, CIL emission). They
> are historical scaffolding, not the current spec-first audit machinery.

## Key concepts
- CA* = code-vs-spec; V54–V59 = the CODE-BUG? subset of the design-vs-spec audit.
- Every finding = Spec §/GR + exact code loc + a failing program with spec-derived expected output.
- The two audits were bounded *sampling*; the fix queue is the FIRST installment — P14 Step-0 is the exhaustive review.

## See also
- [[kb/Modernization/Tasks]] — the fix queue driven by these audits.
- [[kb/Spec/Overview]] — the ISO authority the audits check against.
- [[kb/Context/Goals]] — the spec-first-only priority.

## Backlinks
- [[kb/Modernization/MOC]] · [[kb/Index]] — link here.
