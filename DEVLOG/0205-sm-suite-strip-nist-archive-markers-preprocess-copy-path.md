## Entry 205 — 2026-05-28: SM suite — strip NIST archive markers + preprocess copy-path (CLEAN 5→7)

SM102A/SM104A/SM204A failed with a stray `,SM102A` at column 1. Root cause: 70 of the 459
extracted NIST programs still carry a trailing `*END-OF,<name>` archive marker. The `*` sits in
column 1 (sequence area), not column 7, so reference-format normalization read column 7 (`F`)
as a normal indicator and emitted the source area (`,SM102A`) as code — an unexpected comma.

Rather than mutate 70 test inputs, added `ReferenceFormatProcessor.StripNistArchiveMarkers`,
which drops any line beginning with `*HEADER,` or `*END-OF,` (unambiguous CCVS member
delimiters, never valid COBOL). It runs on the raw text before normalization in both the compile
path (`Compilation.Preprocess`) and the `preprocess` CLI command. Also taught the `preprocess`
command the `--copy-path`/`-I` flag and sibling-`copylib/` auto-discovery, so preprocessed output
can be inspected with copybooks expanded (which is how the stray comma was traced).

SM CLEAN 5→7 (SM102A/SM204A clean; SM104A now runs → 1 FAIL*). COMPILE_FAIL 9→6. Guard ALL
GREEN (1000 / 336 / 137) — the marker strip touches every NIST program with no regression.

