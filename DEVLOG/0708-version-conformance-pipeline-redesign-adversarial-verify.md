## Entry 708 — 2026-07-08 20:59 PDT — Version-conformance PIPELINE REDESIGN (adversarial-verify → owner architecture review) + planning-doc integration

**Trigger.** The closing ultracode adversarial-verify pass over the built PHASE-03 steps (2/6/8/10, workflow
`wf_195aa8a2`) came back **clean on Step 2** (EditionValidator re-home — byte-neutral) and **Step 8** (INV-1 continuity),
but confirmed a real defect in **Step 10**: the RAISING loud-hole arm (Entry 706) mis-fires at `--std 85`, because
`InRule(procedureDivision)` performs no real narrowing — a garbled body statement whose offending token is a §8.9
user-word `RAISING` (`PERFORM RAISING RAISING.`, a data/paragraph legally named RAISING at 85) got a
confidently-wrong `COBOLNET0900 "PROCEDURE DIVISION RAISING requires COBOL-2002"`. Empirically the SAME mis-fire
affects the whole `ReservedWordEditionHints` residue (verified: SHARING/RETRY/UNLOCK/XOR/B-AND all fire a spurious 0900
on `PERFORM <userword> <userword>` below their edition). The class-doc reliability claim ("keys off the construct's OWN
tokens, so a typo matches nothing") is thereby falsified for every one of these tokens, since each is a legal user word
below its edition. (The Step-6 VCR drift-guard also drew 4 low/nit robustness findings; the Step-10b SYNC-on-group
location nit is a pre-existing systemic property of the whole `EditionContext` string channel, not a regression.)

**Owner architecture review (the decisive turn).** Rather than accept a positional-heuristic patch (I had one built +
tested — narrowing RAISING to "DIVISION-before-DOT", confining the siblings by operand/`WITH`/I-O-verb adjacency; 25
tests green), the owner correctly called the whole approach **ad hoc** and drove four critiques that are one root cause:
(1) the guesser reconstructs intent from a *failed* parse; (2) "four grammars or a better design?"; (3) "why wait until
binding — use a tree walker"; (4) "no reason to generate code when there are compile errors." Root cause: the pipeline
has **no clean error-gated phase boundaries**, and version gating is smeared across THREE mechanisms (grammar
`{isXXXX()}?` predicates + the post-hoc guesser + ~24 binder `Check` calls), while **bind+emit are FUSED**
(`CompilerDriver.cs:114` renders C# via `CSharpEmitter.Emit` then discards it at `:115` on error — codegen runs on an
errored tree; confirmed no `.dll` is written, but the render work happens and error nodes emit "loud NotImplemented"
C#).

**Feasibility analysis (workflow `wf_1fe945e7`, 7 residue clusters).** 5 of 7 gates (XOR, UNLOCK, PD-RAISING, PROPERTY,
SELECT-SHARING) are PURE gating — the predicate can be removed and the construct gated at its recognition point with
identity known. 2 (boolean-condition ENTRY, RETRY-on-OPEN) are genuinely disambiguation-load-bearing and need a
FORWARD, identity-carrying lookahead (generalizing the existing `boolExprAhead()`), never a post-hoc guess. No
construct needs the guesser to survive.

**Design (decisions locked with the owner).** New canonical doc
`docs/rearchitecture/DESIGN-version-conformance-pipeline.md`. Target pipeline: **superset parse** (edition predicates
removed; a committed-match construct-id **annotation** local to each version-gated rule — an ACTION, not a speculative
predicate, so it dodges the DEVLOG-679 hoisted-predicate trap; version *numbers* stay single-sourced in
`constructs.json`) → **edition-AGNOSTIC bind** → **ONE `VersionConformancePass` over the bound tree** (owner decision:
post-bind, so the pass has full identity + semantic context for gates like SYNC-on-group; reject strict / accept-inert
permissive) → **emit-only-if-clean** → backend. `ReservedWordEditionHints` is **deleted**. Owner-chosen implementation
order: **RESIDUE-FIRST**.

**This commit = the planning-doc integration** (owner: "update all current rearchitecture planning docs to integrate
this new design"). Swept: the new design doc; `COBOLNET_REARCHITECTURE_PLAN.md` (STATUS banner, §1 north-star frontend
+ compiler, §4 roadmap row + cross-cutting driver note over P03/04/06/07, §5 index 9→10); `COBOLNET_DESIGN.md` §1.1
(pipeline → 6 phases with the pass + edition-agnostic bind + emit-if-clean); `resume-prompt.md` STATE banner;
`DOC_INDEX.md`; and integration notes in `DESIGN-edition-framework.md` / `DESIGN-frontend-grammar.md` (+ a SUPERSEDED
mark on hard-invariant #5) / `DESIGN-binder-bound-tree.md` / the `PHASE-03` doc (via parallel agents, workflow
`wf_f73eacc3`). The interim positional-heuristic patch is **superseded and reverted** — it was a heuristic on a
heuristic; the residue-first migration replaces the mechanism outright. Docs-only commit; the migration lands next.

