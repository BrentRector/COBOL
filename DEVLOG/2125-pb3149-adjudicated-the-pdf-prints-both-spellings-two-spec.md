## Entry 2125 — 2026-10-16 17:55 PDT — PB3149 adjudicated: the PDF prints both spellings; two spec-tooling leads filed; PB3149 lands: EC-MCS-NO-REQUESTER is the name (Table 13); PB3306 retired into PB779

### PB3149 adjudicated: the PDF prints both spellings; two spec-tooling leads filed

A read-only adjudicator (w1103z) found that the ISO PDF itself spells the MCS exception both EC-MCS-NO-REQUESTER (Table 13 §14.6.13.1.6 and the alphabetic Index) and EC-MCS-NO-REQUESTOR (§E.3.2 3)), that the transcription matches the PDF at every site, and that no general rule raises it (§14.9.31.4 raises INVALID-TAG and REQUESTOR-FAILED only); GnuCOBOL 3.2 registers only NO-REQUESTER. PB3149 records this and corrects its "§G.2 3)" site (really the Index). Two tooling leads from the same report were filed: PB3306 (cite.py attributes the Bibliography and Index to G.2) and PB3307 (some Index links are about 30 PDF pages early). The branch review (R1-1) found PB3306 a duplicate of the open PB779 Part A; the next entry retires it. Notes only; no code changed.

### PB3149 lands: EC-MCS-NO-REQUESTER is the name (Table 13); PB3306 retired into PB779

This branch merges the unlanded notes branch `notes-pb3149-adjudication-20261016` and answers its branch review.

**Review finding R1-1 (duplicate note).** PB3306 filed again the mechanism the open PB779 Part A already owns:
`scripts/spec/cite.py`'s `clauses()` ends a region only at the next NUMBERED heading, so §G.2's region runs to end of
file through the Bibliography, the Index and the Addendum. Re-probed on this tree: `--check G.2 "## Index"`, `--check
G.2 "WITH DUPLICATES phrase"` and `--check G.2 "EC-MCS-NO-REQUESTER exception"` all answer OK with exit 0. PB3306 is
retired in the repository's duplicate form (status `retired`, cluster PB779, `closes_rows_reason` "retired unlanded: a
duplicate of PB779"). Its evidence moved into a new PB779 section, "Seen again": the PB3149 inheritance, the current
line numbers and a `--check G.2 "## Index"` self-test case. PB3149 and the notes branch's pending entry now name PB779.
A sweep of the notes branch's other filing, PB3307 (Index links about 30 PDF pages early), found no open note with
the same mechanism.

**PB3149.** The operator ruled on the question the adjudicator raised (mailbox 20261016-155503, 15:57 PDT): CLAUDE.md
rule 1 settles it. §14.6.13.1.6 Table 13, which "is a list of the exception-names and their attributes", names
EC-MCS-NO-REQUESTER. Annex E is informative, and its §E.3.2 3) spelling EC-MCS-NO-REQUESTOR defines no name. §7.3.25.3
SR2 binds a TURN operand to "the exception names listed in 14.6.13.1". GnuCOBOL 3.2's `libcob/exception.def` (re-read
from the tarball) has 1A05 "EC-MCS-NO-REQUESTER" and no alias. No code changed, because `ExceptionCatalog` already
follows Table 13. Re-probed on this build: the -OR spelling is COBOLNET0711 in `>>TURN` at --std 2002, 2014 and 2023,
and in RAISE; the -ER spelling is accepted at 2023 with the COBOLNET1578 accept-inert warning and is COBOLNET0878 at 2014.
`docs/CONFORMANCE.md` section 3 gains **D-MCS-NR**, which records both spellings, the determination and its
witnesses: `conformance:2023/pb3149_ec_mcs_no_requester_table13` (TURN, USE AFTER EXCEPTION and RAISE of NO-REQUESTER,
and PERFORM ... WHEN of the sibling REQUESTOR-FAILED, both nonfatal) and `conformance:negative/pb3149-ec-mcs-no-requestor-unknown`
(reject-at 2002 2014 2023, COBOLNET0711). PB3149 is landed with no inventory row (none was attached to it).

**Found on the way.** `scripts/spec/where.py` raised UnicodeEncodeError on a cp1252 console as soon as a cited line
carried a non-cp1252 character (a ⛔ in a source comment). It now reconfigures stdout to UTF-8, as cite.py and 47
other `scripts/spec` tools do.

**Self-review.** There is no C# change, so the LSP and type-design lenses have nothing to check. The golden output
follows from §14.9.29.4 GR1 and Table 13's "NF". The doc wording was checked against the RAISE probe. `semgrep/verify.py`
shows every count unchanged. Gate: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run
20261017T002332Z-db1ee1); both added corpus rows passed in leg 1.

