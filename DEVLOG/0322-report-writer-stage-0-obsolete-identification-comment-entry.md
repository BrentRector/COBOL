## Entry 322 — Report Writer Stage 0: obsolete IDENTIFICATION comment-entry paragraphs unblock RW (and DB/SG/CM) header parsing

Began the Report Writer workstream (the one LIVE COBOL-85 module still unimplemented — ISO 2023 §A.4.11,
optional element; plan in `docs/COBOL85_COMPLIANCE_PLAN.md`, build stages in `docs/REPORT_WRITER_ROADMAP.md`).
Recon on RW101A surfaced a surprise: **RW is NOT blocked by Report-Writer grammar.** The RW lexer tokens (RD,
SUM, DETAIL, …) and a partial `reportSection` already exist in `CobolReportWriter.g4`. RW101A actually died at
line 11 — `error COBOL0001: unexpected '5203'` — inside the obsolete **`INSTALLATION`** paragraph's free-text
address block ("5203 LEESBURG PIKE…"), BEFORE any REPORT SECTION.

Root cause: the obsolete IDENTIFICATION comment-entry paragraphs (`AUTHOR`/`INSTALLATION`/`DATE-WRITTEN`/
`DATE-COMPILED`/`SECURITY`/`REMARKS`; ISO 1989:1985 §III, obsolete/removed in 2002) were parsed as
`AUTHOR DOT authorContent? DOT` with `authorContent : ~DOT+` (CobolParserCore.g4). A comment-entry is free-form
commentary — *any* characters across one or more lines until the next Area-A header — so `~DOT+` stops at the
FIRST embedded period (`…SERVICE.`), and a multi-line entry with embedded periods, a number-starting line
(`5203 LEESBURG PIKE`) or a quoted line (`" HIGH "`) breaks. This is fundamentally **un-parseable by a token
grammar**: RW101A's INSTALLATION contains "…AUTOMATED **DATA** AND TELECOMMUNICATION…", so any keyword-stop or
negative-set rule trips over the reserved word `DATA` embedded in the address text. Only **column-awareness**
(Area A vs Area B) disambiguates the comment-entry boundary. (NC-vintage programs dodged this entirely — they
put their header info in `*`-indicator comment lines, never the obsolete paragraphs, so `~DOT+` was rarely
exercised; the older RW/DB/SG/CM suites use the real paragraphs.)

Fix (column-aware, in `ReferenceFormatProcessor.ConvertFixedToFree` — the fixed→free pass that already knows
columns): when an ordinary code line begins in Area A (cols 8-11) with one of the six comment-entry keywords,
enter a `inCommentEntry` state and comment out (`*>`) the header and its body until the next Area-A header
(division/section/paragraph). Back-to-back comment paragraphs stay swallowed (keyword check precedes the
exit check); the next *non*-comment Area-A header (ENVIRONMENT/DATA/PROCEDURE/…) ends the state and is processed
normally. Because these paragraphs are optional (`identificationParagraph*`), the parser simply never sees them
— **no grammar change**, and every embedded-period / terminating-period / reserved-word-in-text edge case is
sidestepped. New helper `FirstAreaAWord` returns the Area-A leading word (or null for Area-B/blank lines).

Result: RW101A now parses PAST the header and reaches the genuine Report Writer constructs — `REPORT IS` on the
FD (line 96, the Stage-1 `reportClause`) and `INITIATE` (line 346, a Stage-4 verb). The fix also unblocks the
header of the other old-vintage suites (DB/SG/CM) for the WS-DIALECT parse+flag work. Guard **ALL GREEN** —
1040 unit / 347 integration / 360 NIST baselines, **0 regressions** (the change touches every program's ID
division, so this was the gate). Roadmap updated with Stage 0. Next: Stage 1 — add `reportClause : REPORT IS?
reportName` to the FD/SD clauses and gap-fill the existing `reportSection` grammar against the design.

