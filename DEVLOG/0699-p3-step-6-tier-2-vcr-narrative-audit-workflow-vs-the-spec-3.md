## Entry 699 — 2026-07-08 14:18 PDT — P3 Step 6 (Tier-2) — VCR narrative audit (workflow) vs the spec; 3 spec-grounded prose fixes

**PHASE 03 step 6, the Tier-2 verification the owner required for Option A** ("we need some verification that the
narrative is accurate"). Ran a 13-agent background workflow (`wf_acc42f62-8a4`, 657k tok) — one agent per ~10-row
batch — each cross-checking a `VERSION_CHANGE_REFERENCE.md` row's `Old → New behavior` narrative + `§` + edition-delta
against the exact `specLines` (from the VCR appendix) and the named § in `specs/ISO_COBOL.md`.

**Result: 133 rows audited — 125 accurate, 3 divergent, 5 unverifiable.** The narrative is high-fidelity. The 5
"unverifiable" are all FLAG-02 (2002→2014) rows (97–101): the audit CONFIRMED their flagging rule + § are correct and
found only that the underlying 2002-vs-2014 behavior isn't in the 2023 spec — exactly the "delta under-documented;
confirm against the older standard" caveat those rows already carry. No action (correct as-is).

**3 divergent — all in the `Old` (prior-behavior) description, not the change itself; each fix grounded in a quoted
spec line, applied:**
- **Row 54** (COBOL words 63 chars, §8.3.2.1): "Old: 30 chars in prior editions" was wrong for a 2014→2023 row — 30 was
  COBOL-85; 2002/2014 already allowed 31. → "31 chars in the 2002/2014 editions; 30 in COBOL-85."
- **Row 68** (FUNCTION EXCEPTION-FILE, §15.28): "Old: last-referenced file connector" mischaracterized the no-arg form
  — §15.28 says the connector "associated with the last exception status" (a successful reference doesn't change what
  EXCEPTION-FILE reports). → corrected to the normative wording.
- **Row 69** (FUNCTION EXCEPTION-FILE-N, §15.29): same defect / same fix (national string).

This is the **Tier-2** leg (audit the hand-owned free prose once, thoroughly, against the spec). The **Tier-1** leg
(the structural spine — derived status + edition-delta/§/code + citation-exists + coverage, all CI-gated by
`VcrDriftTests`) and the VCR transform + `gen-vcr.ps1` + `VcrStatusEmitter` are the next Step-6 commits. No code
change here (VCR prose only); the audit ran read-only, then the fixes landed after it finished (no read-during-write).

