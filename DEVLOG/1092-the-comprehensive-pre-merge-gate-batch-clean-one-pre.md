## Entry 1092 — 2026-07-28 21:17 PDT — The comprehensive pre-merge gate: batch clean, one pre-existing red blocks the merge

Eleven commits had landed on wave-local gates only. This is the gate that decides whether `phase-14` is
mergeable, and it earned its keep three times over.

**Results.** characterization 33/33 · `guard-fast.sh` ALL GREEN with **NIST 353 MATCH / 0 REGRESSION** (exactly
the recorded baseline) · legacy Unit 1203/1203 and Integration 503/504 · greenfield Unit **580/580** ·
greenfield Conformance **1 failure**, `VcrDriftTests.EverySpecLineRef_IsWithinTheSpec`.

**⚠ THE UNIT SUITE CAUGHT WHAT CA9's WAVE-LOCAL GATE COULD NOT.** Two `CobolPtrTests` —
`UpBy_Null_IsEcDataPtrNull` and `UpByScaled_IsTheExactGr19ValueRule` — call the runtime directly with checking
OFF and asserted an unconditional throw, which is precisely the behaviour CA9 changed under the owner's
checking-OFF rule. CA9's wave-local gate ran targeted filters and never loaded them. Rewritten to assert BOTH
arms, which is stronger than before: §14.9.39.4 GR19 gives both in one sentence, so checking ON raises the named
condition and checking OFF returns the operand unchanged. `Deref_Null_And_OutOfBounds_AreLoud` is untouched and
still asserts the throw — the two tests now DOCUMENT the owner's split instead of contradicting it.

**The Conformance red is PRE-EXISTING and unrelated to the batch.** The VCR appendix carries ~180 spec LINE
references reaching line 50,407; the spec has 47,195 lines. At this session's START commit (`199dcd43`) it had
47,142 — so the references were already ~3,200 out before any work here, and the transcription repairs
(47,142 → 47,195) changed nothing about the verdict. It is the same "re-key onto the clause hierarchy" item the
de-paged tools already went through, never done for the VCR. It is now NEXT item 1 in plan §0 because a red gate
blocks the merge regardless of who caused it.

**The differential needed a PER-CASE diff to be read correctly, exactly as the gate skill warns.** Totals moved
(AGREE_ACCEPT 475→472, WE_REJECT_THEY_ACCEPT 570→574) and looked like regressions. The per-case diff against the
stored report shows **3 flips: 1 fix and 2 AGREE→divergence**, both in `syn_value.at`, and both raising
**COBOLNET1625** — which `git log -S` places in commit `f54c9bd4` (CA34), present at this session's start commit.
**The EC batch introduced zero differential regressions.** The two flips are CA34's deliberate §13.18.63.3 SR2
tightening, which GnuCOBOL's DEFAULT_DIALECT accepts as an extension; whoever revisits them should adjudicate
before "fixing" them. Both baselines are stale and §0 now says so: the numbers quoted in Gates, and the stored
`gnucobol-differential-report.json` (2026-07-22, PRE-CA34).

**⚠ A SEQUENCING MISTAKE OF MINE, recorded so it is not repeated.** I started `guard-fast.sh` while a
`--no-build` Conformance run was still in flight. Guard REBUILDS, and the first Conformance run produced no
verdict line at all as a result — I had to discard it and re-run. Long legs go ONE AT A TIME.

**The clean re-run landed after the session state was written: 3929 passed · 1 failed · 3930 total, 12m 05s**,
the single failure being the pre-existing VCR drift and nothing else. That is the new battery reference and it
supersedes DEVLOG 1006's 3891/3891; §0 carries it. It also sizes the leg honestly for planning — Conformance
alone is ~12 minutes, which is why it cannot be the per-commit gate.

**Where the batch stands:** EC-infra + OO super-batch COMPLETE — 10 landed, CA12 refuted. Queue 42 LANDED ·
1 REFUTED · 3 REMAIN. Every commit pushed; tree clean.

