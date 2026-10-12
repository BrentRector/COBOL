## Entry 384 — M2-PRE-1 re-scoped: the "preprocessor crashes on valid source" claim is REFUTED (empirical repro)

Before implementing M2-PRE-1 (the plan's preprocessor-robustness "trio", tagged 🐛 "valid source crashes the
parser"), a 3-agent investigation (`m2-pre1-investigation`: empirical repro + pipeline map + spec) **built real
COBOL-2002 programs for each case and ran them through the CLI**. The "crashes" framing is wrong — nothing
crashes (every exit was 0 or a clean COBOL0001, never an ICE/abort). Findings, now recorded in the plan §3.5:

- **(a) mid-file `>>SOURCE FORMAT` = MIS-PARSE (real).** `ReferenceFormatProcessor` honors only the first
  directive, whole-file; a genuinely fixed region after a switch keeps its seq-number columns → stray tokens.
  Spec §7.3.24.3 R1/R5 requires per-region scope. Fix needs per-line format state (non-trivial).
- **(b) conditional-compilation directives inside copied text = CLEAN-ERROR (real).** `Compilation.cs` runs
  `ConditionalCompilationProcessor` (285) **before** `CopyProcessor` (295), so `>>IF`/`>>DEFINE` in a copybook
  pass through as text → stray `>`. Spec §7.2 order is COPY-inclusion (Step 1) → DEFINE/IF/EVALUATE (Step 2a),
  i.e. COPY-then-CC. Fix = swap the two passes (low NIST risk — CC is a no-op on directive-free source), **but**
  the current order is a deliberate, §7.3.16-cited decision (to let `>>IF` gate a COPY); reconcile §7.3.16 vs
  §7.2 first (gating still works COPY-first — the expanded text is in the IF false path, §7.2 may-omit).
- **(c) unknown `>>` directive = ALREADY CORRECT.** Known-ignored consumed; unknown surfaces as a clean
  COBOL0001 by design. Spec leaves this implementor-defined (only `>>IMP` reserved). Dropped from the trio.

**Why this matters / lesson:** the 🐛 "crash" urgency was illusory — these are clean-error/mis-parse defects on
*rare* constructs (who switches source format mid-file, or puts `>>IF` inside a copybook?), and (b)'s fix
reverses a deliberate decision. So M2-PRE-1 drops below the clean, plan-endorsed new-category items in priority;
re-scoped to ◐ in the plan, deferred for a careful dedicated pass. Verifying an audit claim by *running it*
before building on it saved a rushed, regression-risky swap of working code. No code changed; plan + DEVLOG
only. Next: M2-DATA-4 Boolean/bit.

