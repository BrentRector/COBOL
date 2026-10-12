## Entry 1096 — 2026-07-28 23:14 PDT — PHASE-14 merges, and a principle that changed a plan I had already called finished

`main` is `c056f1f4`. 195 commits, 96 DEVLOG entries, 2026-07-22 to 2026-07-28.

**The gate, run in full and read off the verdict lines rather than inferred.** Conformance 4113/4113 ·
greenfield Unit 580/580 · characterization 33/33 byte-identical · `guard-fast.sh` printing `=== ALL GREEN ===`
with NIST 353 MATCH / 0 REGRESSION · legacy Unit 1203/1203, Integration 503/504 (1 skipped) · GnuCOBOL
differential 0 regressions.

The differential is worth a sentence because it is the leg where a false green is cheapest to accept. The
per-case diff showed three flips, and §0 already carried an attribution for them (CA34). I did not take it: all
three carry `COBOLNET1625`, and `git merge-base --is-ancestor f54c9bd4 56aa9a39` proves that commit predates the
branch tail. Attribution verified, not inherited — which is the same discipline this whole phase was about.

**One correction the merge forced.** §0 told the next session to "refresh the stored
`tests/external/gnucobol-differential-report.json` so the next per-case diff has a truthful before". That file is
GITIGNORED. It is a per-machine local artifact: a fresh clone has none, and the first run there can only
ESTABLISH a baseline, never diff against one. The durable committed baseline is the numbers in §0, which is now
what §0 says. Committing the report would make per-case diffs portable and is GPL-clean under §0's own rule — it
carries only their titles and keywords — but that is a `.gitignore` policy question, not something to slip inside
a merge.

**Then the owner stated a standing principle, and it re-opened a plan I had just closed:** *"we always bias
towards usability, understanding, support, maintenance, production quality."*

I had recorded V59's corrected scope an hour earlier and considered it handed over cleanly — spec-derived,
verified, costed at 129 references across 30 files. Re-read against those five, it was short on three, and all
three are about people rather than code:

- **Support.** V59 changes the ON-DISK RECORD LAYOUT. Every data file written by an earlier build becomes
  unreadable, and a fixed-length sequential file carries no self-description, so the failure presents as silent
  garbage on a customer's machine. I had not written that down at all. Partial detection is genuinely available —
  a fixed-length file whose byte length is not a multiple of the declared record length is provably wrong — and
  that is now part of the work rather than an afterthought, along with a stated migration note.
- **Usability / understanding.** The on-disk form becomes a user-facing guarantee. I had it filed as the §4.2.16
  implementor-documentation obligation, which is true and is the wrong framing: the audience is a COBOL
  developer, not an auditor. Width table, radix, and a worked byte example per usage.
- **Maintenance.** The prize is not fixing the instance. `LENGTH` and `BYTE-LENGTH` disagreed for two phases and
  nothing stops them diverging again; a drift test asserting they agree for every non-national, non-boolean item
  retires the CLASS. And the goldens must pin actual BYTES, because today nothing tests them at all — which is
  the whole reason this survived.

The lesson I want to keep: a plan that is complete on correctness and gates can still be unfinished. "It passes
and it is architecturally right" is not the bar; supportable, documented and self-defending is. Re-reading a
finished plan against those five takes minutes and it changed three things here.

PHASE-14's substance is in Entries 1000–1095; what it leaves is a queue at 44 landed / 1 refuted / 1 remaining
(V59, analysed and specified), a transcription that is published, de-paged and citable, and citations that are
now mechanically enforced in both directions.

