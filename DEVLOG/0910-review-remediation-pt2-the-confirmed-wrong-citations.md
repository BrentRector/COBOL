## Entry 910 — 2026-07-19 13:12 PDT — Review remediation pt2 — the confirmed wrong-§ citations corrected (spec-verified); review resume killed by the MONTHLY spend limit

**Citations (each § re-read in the spec BEFORE editing — the review's own discipline):**
- **'04' READ length rule = §14.9.30 GR14** (READ), not §14.9.35 (REWRITE): SequentialConnector, FileStatus,
  the `io_status_04` golden header, the GreenfieldOnly row comment.
- **The two annex items are DIFFERENT rules and the code had one wrong label for both:** VCR 63's introduction
  proof is **Annex E.3.3 item 20** ("External data items may now be strongly typed") — fixed in
  VersionConformancePass + constructs.json (+ `gen-constructs.ps1` regen); VCR 16's flip proof is **Annex E.2
  item 10** (CONSTANT RECORD now strong-only; line 49148 sits under E.2, which starts at 49022) — fixed in
  DataBinder. This also resolves the review's apparent C9-verifier/C22 conflict: both were half-right.
- **External CONSTANT-RECORD initial-state rule = §11.9.10.4 GR7** ("External items in the Working-storage
  section are not initialized … except for those with the CONSTANT RECORD clause"), not "§13.6.2 GR7" (§13.6.2
  is the LOCAL-STORAGE general format, no GRs): DataBinder.Linkage, OoEmitter, DataBinder VCR-16 comment, the
  2014 `external_constant_record` golden header.
Gate: build + all drift tests + characterization 33 byte-exact (comment/citation-only — no semantics change).

**Review completion status:** the resume of workflow `wf_099b899f-7eb` was killed by the MONTHLY spend limit
(107/119 agent calls failed). Before dying it landed 5 finder re-runs (23 raw findings — folded into the ledger
§7 per the owner's fold-as-they-complete directive; notable: a B2 claim that RANDOM sequence state is a
process-wide static contradicting the P8 RunUnit-owner claim, and a C3 claim that WriteFill drops
AllowZeroLength — a spurious fatal EC-BOUND-REF-MOD under `>>REF-MOD-ZERO-LENGTH ON`) + 3 high-confidence
verdicts confirming the >>DEFINE-AS-PARAMETER, >>SOURCE-FORMAT-first-wins, and CC-in-COPY findings. The
ledger §5 carries the resume protocol for finishing once the limit is raised.

