## Entry 1923 — 2026-10-07 16:36 PDT — Nineteen open adjudications decided by an adjudicator-plus-refuter fleet: 12 defects, 7 closed; 21 notes filed (PB2499–PB2521)

The owner's account-2 week (claude@wiseowl.com) is to be spent before Saturday 10:00, and with the implementer gate slot
saturated, the operator filled the session with READ-ONLY work that needs no gate: the nineteen open `kind:
adjudication` notes in `kb/Work` (all MINOR, none blocked). A two-stage workflow (`E:\COBOL-coord\scratch\
wf_note_adjudicate.js`, run `wf_a974b389-830`, 38 agents, eight notes at a time) gave each note a `cobol-adjudicator`
that derived the expected behaviour from ISO/IEC 1989:2023 first (every citation through `cite.py --check`), then read
the code and probed a pinned, built tree (`pin-97006f3`, main `97006f310`; GnuCOBOL 3.2.0 in WSL where the spec leaves
latitude), and then an independent `cobol-refuter` told to overturn the decision. The refuters amended or overturned
eight of the nineteen: PB1935's "conforms" became an under-reject defect, PB1923's "conforms" stood but surfaced a
silently dropped reference modifier on the SORT Format-2 subject (`SortBinder.cs#SortBindTable`), PB1664's line-end
rule was re-cut three ways (LF for a record-sequential ADVANCING file, the host newline for LINAGE and report files,
a bare CR for ADVANCING 0 per GR25 c)), and PB259 split (the two COMMIT/ROLLBACK rows documented non-support).

Outcome, recorded by a `cobol-clerk` from the decision and refute files: TWELVE defects — PB780, PB845, PB850, PB1027
(MAJOR: the record that triggers a LINAGE overflow lands on the wrong page), PB1643 (DECIMAL-POINT IS COMMA ignored by
DISPLAY of a non-integer literal), PB1664, PB1927, PB1934 (silent), PB1935, PB1943, PB1954, PB2071; FIVE closed as
conforming (PB865, PB1923, PB1924, PB1926, PB2021); TWO conforming but held open until the registrar records their
rows (PB867, PB259's remainder: `DefectiveRowCoverageDriftTests` refuses a landed note that claims a defective row).
Twenty-one new notes, PB2499–PB2521, carry the siblings the fleet found (e.g. NUMVAL/MAX function results ignoring
DECIMAL-POINT IS COMMA, a report-writer under-reject in `DataBinder.Reports.cs#MinimumLastLine`, a used STRONG type
escaping §13.18.63.3 SR1). PB1027 carries an operator caveat: its decision rests on GnuCOBOL's NON-DEFAULT
`-fwrite-after`; default cobc and §14.9.51.4 GR25 are to be re-derived before any plain-WRITE output changes.

Gate: implementer GREEN (Conformance 10,914 · Unit 32,507 · Characterization 35) after one red on a section-sign
citation in PB1927 that the clerk fixed; `work.py check` ✓ 1972, `audit_doc_citations --check` 0/0. Reports
`E:\COBOL-coord\scratch\reports\w1033-adj-clerk-report.md`; decisions `E:\COBOL-coord\scratch\w1033-adj\`.

