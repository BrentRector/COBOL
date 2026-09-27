---
name: spec-lookup
description: Use BEFORE implementing, debugging, or adjudicating any COBOL semantics, syntax, output, or "is this a bug" question - derives the expected behavior from the ISO spec and produces a citable section/rule before any code is read or written.
---

> **Generic base:** [`spec-oracle`](https://github.com/BrentRector/claude-skills/blob/main/skills/spec-oracle/SKILL.md) in [BrentRector/claude-skills](https://github.com/BrentRector/claude-skills) — the reusable discipline, with the owner's bar in its sibling `engineering-standards`. THIS skill is the WiseOwl COBOL application of it and wins on conflict.

# Spec lookup

**Order of operations is the whole point.** Derive the expected result from the spec and write down the citation
BEFORE reading the implementation, building a repro, or looking at the diff. A repro VERIFIES a spec-derived
expectation; it never supplies one.

The recurring drift is jumping into repros to OBSERVE behavior instead of DERIVING correct behavior. If you catch
yourself starting from a failing diff and reverse-engineering what the code does, stop and restart here.

## 1. Find the governing rule

`specs/ISO_COBOL.md` (private submodule; `git submodule update --init --recursive` if absent).

Rules live as **Syntax Rules (SR)** and **General Rules (GR)** per statement (§14.9.x) and clause (§11/§12/§13.x),
plus §8 concepts (classes and categories §8.5, conditions §8.8.4, reference and ref-mod §8.4, standard conversions
§8.5.1, expressions §8.8), §15 intrinsics, Annex A (required documented behavior), Annex E (edition deltas), and
Annex F (obsolete/archaic).

**Read the SPECIFIC governing rule, not the nearest general sentence.** A "gap" derived from a general sentence
usually dissolves once the exact argument rule or syntax rule is read.

## 2. If a general format (a DIAGRAM) is load-bearing

The OCR'd rule TEXT is faithful — do not "correct" apparent garbles, several are in the printed standard too. The
DIAGRAMS were lossy, and always in the direction of **falsely restrictive** syntax: legal source made to look
illegal.

1. Read the repaired `Figure notes` block under the diagram. A full re-render pass corrected those; they are
   authoritative and usually already answer the question.
2. Only to settle a genuine doubt, render the page: `python scripts/render-spec-page.py <page>` (anchor `page-N`
   equals PDF page N) and LOOK at it.

**Never escalate a figure-reading question to the owner.** The diagram answers it. Never derive a general format
from prose alone.

## 3. Write down the expected result and the citation

Produce, before touching code: the exact §/GR/SR, the derived expected value or behavior, and the edition
applicability (does this differ across 85/2002/2014/2023? check Annex E). That triple is what a golden's expected
value is computed from — never an oracle's output.

## 4. Only now read the code

Ask "does the code match the cited rule", not "what does the code do". Check the dispatch's arms AND its default
against the rule. A byte-neutral refactor proves no-regression, never correctness.

## When the standard itself is defective

It happens. Record it in `docs/CONFORMANCE.md` rather than silently coding around it, or a future maintainer will
"fix" the compiler back to the wrong behavior.

## Never

Claim spec compatibility without a citation. If a construct is accepted as a common extension, say exactly that.
