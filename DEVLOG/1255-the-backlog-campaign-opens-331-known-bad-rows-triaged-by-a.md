## Entry 1255 — 2026-08-09 10:00 PDT — The backlog campaign opens: 331 known-bad rows triaged by a probe+refute fleet — 114 verified fixed, 217 live and clustered, and the register is the fix queue again

The owner's directive pivoted the session: fix the KNOWN backlog to zero, no further adjudication
exploration (batch 6's three in-flight subjects were recorded and exploration closed: SIN 3 CONFORMS,
SECONDS-PAST-MIDNIGHT 4 CONFORMS + 1 PARTIAL, SIGN 2 CONFORMS + 1 PARTIAL — 5 rows closed on existing
spec-derived tests). The known backlog was the 331 inventory rows carrying DIVERGES/PARTIAL/
NOT-IMPLEMENTED — every one verdicted BEFORE the PB/R fix waves landed, so the honest first move was
re-measurement, not fixing from stale notes.

THE TRIAGE: a 24-agent workflow (14 probe groups by function family, each feeding an independent
adversarial refuter; all agents re-EXECUTED probes via the CLI, deriving expected values from the rule
text, never trusting the recorded note). Zero agent errors. Result: 114 FIXED (confirmed by refuters
with additional variation axes — the fix waves had quietly repaired a third of the backlog), 170
STILL-LIVE, 47 NOT-IMPLEMENTED-CONFIRMED, 20 prober claims overturned by refuters. The 111 FIXED rows
with code-locations are re-verdicted CONFORMS (untested — they left the defect backlog; closing to OK
comes with each cluster's goldens); 3 need code-locations first. The drift gate then caught exactly
one bad record — a batch-6 code-location naming a symbol that does not exist (BindFunctionCall for
BindIntrinsicCore) — fixed and re-gated 10/10.

MEANWHILE, INLINE (the fleet froze the tree, so the window's fixes were doc/register-side):
- A.1 item 112 (LEAP-SECOND ≥86,400 determination) DISCHARGED in CONFORMANCE.md §7 — the PARTIAL's
  only open half — closing RV-15.80.3-4 (19/199 obligations discharged, audit-verified).
- The RANDOM unseeded-seed NEEDS-OWNER-DECISION resolved WITHOUT a question under the owner's standing
  follow-GnuCOBOL-on-latitude rule, SURVEYED not assumed: GnuCOBOL 3.x seeds its first unseeded
  reference from seconds-past-midnight × module-pointer bits (libcob/intrinsic.c, read from source) —
  per-process entropy, the same choice as .NET's parameterless Random. Recorded as the VOLUNTARY §7
  item-144 row; AR-15.75.3-4 → CONFORMS. Decision queue: 3, all genuinely owner-reserved.
- PB55 DECIDED by the owner (rule-is-the-unit) — implementation queued.
- PB56 filed: the SDIDI landing truncates at working scale 6, so sub-microscale Dec operands reach
  exact-family intrinsics as ZERO (SIGN(1e-9−0)=0) — the prose-ledgered Dec-carrier end state D3
  records is now a register item with its design drafted.
- PB57 filed and CLOSED STALE the same hour, on a probe: CURRENT-DATE already reads the R21 clock seam
  (CD pinned to 1999-12-31 returns the pin; the §15.100.3 r5 identity holds). The filing inherited a
  pre-R21 path-read — the premise-failure pattern's 15th instance, mine, caught by my own probe.
- CONFORMANCE.md's stale "186 obligations remain" corrected to defer to its owning script.

THE CLUSTER MAP (the 217 live rows collapse to ~9 root causes; notes follow this entry): the
argument-screen table shape (~20 rows — width/integrality/value-domain/cross-arg predicates a class
table cannot express); PB56's standard-mode/Dec routing family (~18); CONVERT/BASECONVERT/Repertoire
(~20 — Latin-1 hardcode, Trim shapes, missing screens); NUMVAL-C's remove-then-parse value path (~12);
LENGTH/BYTE-LENGTH binder folds (~14); TryExpandAll bind-time ALL/ODO (~9); the exception/module
family (~19, several doc-only); the LOCALE feature (38, NOT-IMPLEMENTED, the one true feature block);
and single-site chokepoints (the unchecked CobolNum.Rescale widening multiply — a silent wrap shared
by three rows — YearToYyyy's in-band omitted-ness, MinScaled value-vs-index selection).

Subagent economics, owner-corrected mid-campaign: subagents had inherited Fable pricing; all agents
now run Opus 5 (CLAUDE_CODE_SUBAGENT_MODEL persistent + per-call overrides), and the interrupted
launches' four surviving probe files were recovered rather than re-run.

