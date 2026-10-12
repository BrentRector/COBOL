## Entry 440 — MASTER_PLAN.md: the top-level SSOT + autonomous-execution playbook, grounded against all ~159 prior docs

The owner restated the North Star, emphatically and as a standing directive: **"this must be a commercial-quality,
production-level COBOL compiler sustainable for decades. I don't care if you break everything and rewrite/re-architect
it to reach that goal. There is no back-compat requirement as this is a brand-new compiler."** And: *"Create and
document persistently the plan to reach that goal... The new session must plan to implement all remaining work with
maximum autonomy, maximum parallelism practical and reach the goal in the session. There are many prior planning docs
already written. Encompass and consider all of their research."*

Authored **`docs/MASTER_PLAN.md`** as THE single top-level entry point — a SSOT + autonomous-session execution
playbook, not a reinvention. It has: §0 North Star · §1 autonomous operating manual (guard-as-law via
`guard-fast.sh`; three-tier parallelism; version-factored grammar; adversarial-verify discipline) · §2 honest baseline
· §3 the phased roadmap (**A** enable-max-parallelism → **B** finish the data-model migration → **C** full ISO-2023
conformance → **D** targeted architecture cleanup → **E** build the product surface → **F** rename to COBOL.NET) · §4
parallelism map · §5 done-bar · §6 risks · §7 start-here · §8 status log · §9 a categorized index of **all ~170 prior
docs** · §10 a grounded commercial/decades assessment · §11 early-resolve decisions.

**To genuinely "encompass all prior research" I ran two parallel workflows** (read-only; the safe tier): an **11-agent
synthesis** of the 159 `docs/` files cross-checked against `src/`, and a **4-dimension commercial-bar assessment**
(architecture · conformance · testing/CI · production-readiness). Key grounded findings folded into §9/§10:
- **The compiler core + runtime are production-grade (~85–90% of a COBOL-85 compiler), NOT a god-class crisis.** The big
  decompositions are already DONE and tested: **M001** (IR-expression hierarchy, zero `BoundExpression` leakage),
  **M003** (`CilEmitter` 4083-line god class → 11 focused emitters + `EmissionContext`), **M004** (`BoundTreeBuilder`
  → 9 focused binders, 234-line orchestrator). So Phase D is **targeted cleanup, not a teardown** — corrected the
  §2/§3 framing that had implied otherwise.
- **The ~100 long-titled `CobolSharp COBOL … Architecture.md` essays are complete *designs* but ~0 lines of code.** They
  are a prior doc-generation pass (see `docs/Resume-Prompt-For-Docs.md`). The entire **product surface** they describe —
  debugger, LSP, CI/CD, packaging, security, perf-gate, compliance, plus JSON/XML lowering and the 12-doc Screen
  subsystem — is the **bulk of the North-Star gap** (Phase E + the back of C), and is mostly **net-new build**.
- **Conformance executes `ISO2023_CONFORMANCE_PLAN.md` — do NOT re-audit.** M2 order (assessment): OO slice-1 first →
  FILE-2002 → ARITH-2 → PRE-1 → UDF → VALIDATE → EC.
- A **disabled CI template already exists** (`.github/workflows/build-and-test.yml.disabled`); CLI csproj is
  `PackAsTool` (missing `SingleFile`) — concrete Phase-A/E starting points.

**Transparency (`feedback_transparency`):** the two workflows disagreed in tone — the assessment read "~85% ready, one
session to commercial delivery," the synthesis read "the product surface is design-only, weeks of net-new work." §10.5
reconciles them honestly: the assessment's optimism is true only for the narrow "OO-complete CLI + packaging + docs"
scope; the **full** commercial/decades/full-ISO-2023 bar is a multi-phase program. Also flagged that a couple of
assessment sub-agents read stale state ("DEVLOG at 223"), so §10 trusts §2's current baseline for status. This squares
`AUDIT_REPORT.md`'s "DO NOT REWRITE" verdict with the owner's "rewrite anything": the latter is a *license* to
re-architect targeted subsystems for decades-quality, not a mandate to greenfield-restart a sound core.

**Context saved for a fresh session** (`feedback_session_context`): prepended a MASTER_PLAN "START HERE" pointer to
`CLAUDE.md` (auto-loaded), `resume-prompt.md`, and the auto-memory `MEMORY.md`; recorded the North Star + grammar-
version-factoring + parallel-guard directives as memories. No code touched — this is a planning/handoff commit; guard
unchanged from Entry 439 (1196 / 509 / 364, green). **NEXT = execute MASTER_PLAN Phase A → Phase B1 (OO semantic/emit,
turnkey `docs/OO_IMPLEMENTATION_DESIGN.md` §6.6).**

