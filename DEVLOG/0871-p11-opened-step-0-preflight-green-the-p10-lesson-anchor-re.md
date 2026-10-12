## Entry 871 — 2026-07-17 09:42 PDT — P11 OPENED — Step 0 preflight green; the P10-lesson anchor re-scout ran FIRST (11 parallel scouts) and its findings are PERSISTED as PHASE-11-scout-notes.md

PHASE-11 (deferred-intrinsics backlog to zero + the Tier-C decision) opened on the tree at `45fe74dd`. This
entry checkpoints the session state durably — the executing session may hand off mid-phase, and the next one
must resume without re-discovery (read `resume-prompt.md` → the P11 STATUS banner → the scout notes).

**Step 0 preflight — ALL GREEN:** solution build 0/0 · greenfield conformance **3467/3467** (13m) ·
greenfield unit **292/292** · `guard-fast.sh` **NIST 353 MATCH, 0 regressions**, legacy unit 1196 +
integration 636 ALL GREEN.

**Step-1 enumeration — DONE:** the live `IntrinsicBind.Deferred` set is **17 rows**
(`IntrinsicCatalog.cs:133-174`) — exactly the phase doc's §3.1 table of 22 minus the 5 rows P10 already
landed (CHAR-NATIONAL, DISPLAY-OF, NATIONAL-OF, EXCEPTION-FILE-N, EXCEPTION-LOCATION-N — verified `Runtime`
in the catalog source, not taken from the doc). The "43" in the roadmap remains stale, as the doc itself
warned.

**The P10 recurring lesson applied BEFORE any code:** P10's Step-1 audit drifted in ~5 of 6 re-checked
claims, so P11 re-scouted every anchor first — an 11-agent parallel workflow (7 spec scouts over
`specs/ISO_COBOL.md`, 4 code scouts over the catalog/binder/renderer/runtime, the test wiring, and the
Tier-C guard surface). The full verified output is persisted at
**`docs/rearchitecture/PHASE-11-scout-notes.md`** (indexed in DOC_INDEX; deleted at P11 close after its
content flows into the design docs). Headline findings the phase doc had wrong or underspecified:

- `ComputeTier` lives at `DataBinder.cs:2376`, not the doc's ~1752; the guard-site inventory is now
  enumerated file:line in the notes.
- BOOLEAN-OF-INTEGER §15.13.4 r1 explicitly zero-fills OR TRUNCATES ON THE LEFT to argument-2 positions —
  result = argument-1 mod 2^argument-2, overflow is NORMAL (Annex D.10's 544→low-6-bits example), no EC.
  The §15.13.3 "positive" vs "positive nonzero" drafting ambiguity for argument-1=0 is resolved: accept 0
  (all-zero bits), EC only for negatives.
- Per-family hand-derived golden values (with the governing rule cited per value) are tabulated in the
  notes — the goldens get authored FROM those tables, not re-derived ad hoc.
- The scout-notes ⚠ gotcha/discrepancy blocks OVERRIDE the P11 phase doc where they conflict.

**Resume point:** execute the phase doc's Step 1 (add `IntrinsicBind.Unsupported`, flip the 5 A.4.9 locale
rows) → Steps 2, 5, 6, 7, 8 (one family per battery-gated commit) → Tier-C step groups C/D → Step 9. The
P11 STATUS banner and `resume-prompt.md` both carry this state.

