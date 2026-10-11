## Entry 1686 — 2026-09-24 17:09 PDT — Cloud doc-rows-1 lands: 67 Annex A.1 determination rows written, 68 held rows recorded (GAP 1652 → 1627); PB1537 renumbered PB1546

**What.** Owner decision on PB1535: option (c) — the 126 A.1 rows stay verdict-less until their §7 determination rows
exist — then PB1522 step 3, run by the same credit-billed cloud orchestrator on `claude/adj-doc-rows-1` (cut from
`claude/adj-registrar-1`). Six writer subagents (21 items each) proposed rows to their own files rather than six agents
editing one table; a refuter per batch probed every proposed row against the code (≈ 20 writer overclaims corrected,
e.g. DOC-A.1-42/-187 → PARTIAL); one integrator merged them into `docs/CONFORMANCE.md` §7 and re-verdicted.

**Result.** 67 rows: 60 new, 2 corrected (DOC-A.1-72 was WRONG about what the external-connector ASSIGN check compares —
PB1530 closed as a documentation fix; -128 cited a symbol that no longer exists), 5 `Not provided.` rows for optional
items (7, 106, 143, 193, 196), item 24 re-verified. **68 verdicts recorded** (CONFORMS 51 — 24 closed, 27 test-needed;
PARTIAL 12; DOCUMENTED-NON-SUPPORT 5 — 1 closed, 4 test-needed): **GAP 1652 → 1627**; A.1 audit 125 of 184 obligations
met. **58 A.1 rows stay verdict-less by design:** 45 OMIT — the code is wrong against the spec, and documenting it
would enshrine the defect, so each is claimed by a defect note instead — and 13 awaiting owner answers (PB1536, PB1099,
PB690, PB643). New defects PB1537–PB1545: figurative constants refused where §12.3.7.3 SR11 bans only symbolic
characters · `>>DISPLAY` transfers nothing at compile time (GnuCOBOL prints) · an AS name with edge spaces can never be
called · LINE SEQUENTIAL writes CR LF on every host · a permanent-error I-O status does not persist · CODE-SET writes
unrepresentable characters unchecked and its EBCDIC arm crashes · Unicode spaces split COPY REPLACING words · ORD of a
symbolic character refused · X"…" at `--std 85` to be checked against the 1985 text. Two went to OMIT by the GnuCOBOL
tie-breaker (fatal I-O status continues — PB322; LINE SEQUENTIAL CR LF), so fixing them changes golden
`pb964_ls_plain_write_after_after`. `DerivedVerdictDriftTests`' "optional but undetermined" anchor moved from item 7
(now has a row) to item 67. Comment/doc fixes the writers found ride along; one diagnostic's text changed (COBOLNET0825
now cites §14.9.23.3 SR4 a) or b) by receiver kind, not "SR4d") — no golden asserts the old wording.

**Cost.** The credit went $191 → ~$92 (the orchestrator's reading; the integrator's tail not yet on the meter when
checked) — ~$0.80 per item, twice adjudication's rate because every row was probed by its writer AND its refuter.

**Landing.** Merged locally (the branch carries registrar-1, already on main). One conflict: both orchestrators filed a
PB1537 — this session's `--help` cp1252 note (main, DEVLOG 1685) and the integrator's SR11 figurative-constant defect
(branch, cross-referenced within it). The branch keeps PB1537; this session's note is **renumbered PB1546**. Gate on the
merged tree: solution build; filtered inventory/derived/AnnexA1/work-register/ConformanceRegister drift; the
`OoSpineTests` Conformance class (for the diagnostic-text change); `work.py check` clean. Full matrix in push-main's CI.

