---
name: spec-lookup
description: Use BEFORE implementing, debugging, or adjudicating any COBOL semantics, syntax, output, or "is this a bug" question - derives the expected behavior from the ISO spec and produces a citable section/rule before any code is read or written.
---

> ⛔ **BASE SKILL FIRST.** Invoke `brent-tools:spec-oracle` (Skill tool) before reading on — its order of operations
> (derive and cite BEFORE reading code), the specific-rule rule, the checked citation, "only now read the code", the
> failure modes and the latitude precedence are the procedure. If the plugin is not loaded (a cloud session receives
> no project marketplace), Read `tools/claude-skills/skills/spec-oracle/SKILL.md` instead
> (`git submodule update --init tools/claude-skills` if the path is missing). THEN apply this overlay: where the
> COBOL standard, its tools and the owner's decisions live. It wins on conflict. Pinned: **brent-tools 1.15.0**
> (`tools/claude-skills`, kb/Work/PB1699).

# Spec lookup

The recurring drift here is jumping into repros to OBSERVE behavior instead of DERIVING correct behavior (CLAUDE.md
rule 1). If you catch yourself starting from a failing diff, stop and restart at the base's step 1.

## 1. Find the governing rule

`specs/ISO_COBOL.md` — TRACKED in this repository. (The private `specs-private` submodule holds only the licensed
PDF, needed only to render a page.)

Rules live as **Syntax Rules (SR)** and **General Rules (GR)** per statement (§14.9.x) and clause (§11/§12/§13.x),
plus §8 concepts (classes and categories §8.5, conditions §8.8.4, reference and ref-mod §8.4, standard conversions
§8.5.1, expressions §8.8), §15 intrinsics, Annex A (required documented behavior), Annex E (edition deltas), and
Annex F (obsolete/archaic). `python scripts/spec/where.py <clause> [rule]` lists the code that already cites a rule.

## 2. If a general format (a DIAGRAM) is load-bearing

The OCR'd rule TEXT is faithful — do not "correct" apparent garbles, several are in the printed standard too. The
DIAGRAMS were lossy, and always in the direction of **falsely restrictive** syntax: legal source made to look
illegal.

1. Read the repaired `Figure notes` block under the diagram. A full re-render pass corrected those; they are
   authoritative and usually already answer the question.
2. Only to settle a genuine doubt, render the page: `python scripts/render-spec-page.py <page>` (anchor `page-N`
   equals PDF page N; needs `specs-private`) and LOOK at it.

**Never escalate a figure-reading question to the owner.** The diagram answers it. Never derive a general format
from prose alone.

## 3. The citation, checked by the project's tool

The base's triple (citation · derived expectation · applicability) is what a golden's expected value is computed
from — never an oracle's output. Applicability here is the edition: does this differ across 85/2002/2014/2023?
Check Annex E. **Check the citation mechanically:** `python scripts/spec/cite.py --check <clause> "<text>"`
(CLAUDE.md rule 1 — a citation you did not run `--check` on is not a citation, and an INHERITED one is the usual
failure).

## 4. Latitude and defects in the standard

- **Implementor latitude** follows the owner's precedence (CLAUDE.md rule 1, 2026-08-08 via `kb/Work/R13`): the ISO
  spec where it controls → otherwise GnuCOBOL → if GnuCOBOL has nothing, IBM Enterprise COBOL or Micro Focus. It
  settles latitude only, never GnuCOBOL's non-ISO extensions. Record the choice in `docs/CONFORMANCE.md`.
- **A defect in the standard** is recorded in `docs/CONFORMANCE.md`, never silently coded around.
- A construct accepted as a common extension is called exactly that.
