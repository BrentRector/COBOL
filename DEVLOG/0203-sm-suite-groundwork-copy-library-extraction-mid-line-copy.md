## Entry 203 — 2026-05-28: SM suite groundwork — copy-library extraction + mid-line COPY + copybook normalization

Started the SM (source text manipulation) suite. Its programs were 12/17 COMPILE_FAIL because
the COPY statement could not resolve its library members. Three pieces of groundwork:

1. **Copy-library extraction.** NIST stores copybooks as `*HEADER,CLBRY,<name>` … `*END-OF`
   members inside `newcob.val`; the program extractor skipped them. New
   `tools/extract-nist-copylib.sh` writes the 51 members to `tests/nist/copylib/<name>.cpy`.

2. **Search-path wiring.** Added a `--copy-path`/`-I` CLI flag (repeatable) and, in `--nist`
   mode, auto-discovery of the sibling `copylib/` directory — so the harness needs no change.
   The compile path already plumbed `_copySearchPaths` into `CopyProcessor`.

3. **CopyProcessor correctness.**
   - **Mid-line COPY.** COPY may appear after a level number (`77 COPY K1W03.`), a data-name
     (`01 TST-TEST COPY K101A.`), or inside a statement (`ADD COPY K1P01. TO …`). The old scan
     only matched COPY as the first word of a line. New `FindCopyKeyword` scans anywhere while
     skipping alphanumeric literals (`" COPY - NOT FOR DISTRIBUTION"`) and `*>` comments, with
     word-boundary checks so `COPYSECT-1` is not a false match.
   - **Library qualifier.** Parse and skip `COPY text-name (IN | OF) library-name`.
   - **Copybook normalization.** Library text is itself reference (fixed) format and must be
     normalized before insertion. CCVS members use non-standard column-7 indicator letters
     (`C`, `G`) that the general `IsFixedForm` heuristic rejects, so `NormalizeCopybook` detects
     fixed form from the sequence-number area (cols 1-6 numeric) and converts — otherwise the
     copybook's own `000100` sequence numbers leaked into the program text.

SM COMPILE_FAIL holds at 12 for now (SM106A newly CLEAN, SM207A 2→1 FAIL*) because the copybooks
expand into FD clauses (`LABEL RECORDS`, `VALUE OF`) and NIST `XXXXX###` placeholders that are
not yet handled — those are the next steps. Guard ALL GREEN (1000 / 336 / 137); the mid-line
COPY scanner did not false-match in any guarded program.

