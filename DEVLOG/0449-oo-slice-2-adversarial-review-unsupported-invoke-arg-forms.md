## Entry 449 — OO slice 2 adversarial review: unsupported INVOKE arg forms now fail loudly (COBOL0111), not silently

Ran the full **Agent-tool adversarial review** of slice 2 (commit b766d16) that entry 448 had only self-reviewed.
The data-reference + RETURNING path checked out (IL stack balance, RETURNING ordering, `ldarg.1` for the instance
`args`, NEW/instance separation, the `CreateLinkageFields` refactor byte-identical for programs, per-instance state
across multiple paragraphs — all verified by the reviewer compiling/running probes). But it caught **two real
silent-miscompile bugs on grammar-legal COBOL** the three positive conformance tests didn't exercise
(`feedback_transparency` — slice 2's first cut was wrong here):

- **BLOCKER:** `INVOKE … USING <literal>` / `USING BY VALUE expr` — `BindInvoke` only handled `dataReference()` args,
  so a literal / BY VALUE arg was **silently dropped**. Because RETURNING is marshalled as the trailing array
  element, dropping a USING arg shifted every slot: the method's first USING param bound to the RETURNING pointer
  and the real RETURNING fell off the end (a default null-buffer `ManagedPointer`) → runtime crash, with NO
  compile diagnostic.
- **MAJOR:** `INVOKE … USING BY CONTENT x` was silently compiled as BY REFERENCE (the INVOKE arg pipeline carried
  no pass-mode), so the callee mutated the caller's storage when it must get a private copy.

**Fix — fail loudly, honoring slice 2's documented scope (BY REFERENCE data references):** these forms need a
synthesized value location (a scratch buffer for the literal/expression copy), which `ResolveExpressionLocation`
does not provide today (it resolves only identifier / ref-mod) — the same gap the CALL path has — so they are a
later OO slice. `CallBinder.BindInvoke` now detects any non-(BY REFERENCE data-reference) argument and reports a new
**`COBOL0111` (Error)** — *"INVOKE argument form not yet supported: only BY REFERENCE data-reference arguments are
implemented (literal, BY VALUE, and BY CONTENT arguments are a later OO slice)"* — instead of silently dropping it.
A clean, actionable compile error replaces a silent crash/wrong-result.

**Regression net:** `tests/CobolSharp.Tests.Integration/OoTests.cs` — 3 theory cases assert COBOL0111 fires for
literal / BY VALUE / BY CONTENT USING args, + 1 fact that the supported BY REFERENCE form runs (`R=0007`).
Lower-severity scope-notes from the review (recorded for later slices): method resolution is by-name-only (a COBOL
method literally named `DISPATCH`/`INITIALIZESTATE` would collide with the synthesized instance helpers); SELF/SUPER/
data-name-selector INVOKE targets still bind to null (pre-existing silent drop); `_linkage_*` fields are static
(non-reentrant — fine for the sequential model, flag before concurrency/SELF-recursive INVOKE).

Guard ALL GREEN — **1204 unit / 517 integration / 364 NIST** (+4 OoTests). `guard-fast.sh`.

