## Entry 284 — SORT/MERGE INPUT/OUTPUT PROCEDURE naming a SECTION now runs the WHOLE section → cleared ~10 ST tests

The "mixed ASC/DESC procedural SORT returns COMPUTED=0" lead from 283 turned out NOT to be about descending
keys at all (SortRuntime already honors them) — it was a SECTION-PERFORM bug in the SORT/MERGE statement.

Trace of ST101A: `InitSortFile` ran but `ReleaseRecord`/`ReturnRecord` were never called — the SORT's INPUT
PROCEDURE released nothing. ST101A declares `INPUT PROCEDURE IS INSORT` where INSORT is a *section* (no THRU);
its RELEASE statements live in paragraphs IN-2…IN-8, past the section's first paragraph IN-1. `BindSort`
resolved the procedure-name with `ResolveProcedureName` (which returns a section's FIRST paragraph) and set
the THRU end only when an explicit THRU was written — so `INPUT PROCEDURE IS INSORT` became `PERFORM IN-1`
(the first, empty paragraph) and the rest of the section never ran → zero records released → RETURN hit
AT-END immediately → every key check read 0. (The OUTPUT PROCEDURE `OUTP1 THRU OUTP3` survived only because
its explicit THRU spanned the whole range.)

Fix: `BindSort`/`BindMerge` now resolve a single-procedure-name INPUT/OUTPUT PROCEDURE through
`ResolveProcedureNameForPerform` (the same resolver a plain `PERFORM section` uses), which returns the
section's (first, last) paragraphs — so a section runs in full, exactly like `PERFORM section`. With an
explicit THRU the two names still bound the range directly; a single PARAGRAPH name still runs alone
(thru == null). One shared helper `ResolveSortMergeProcedure` for both statements and both phrases.

Per ISO §14.9.45/§14.9.24: "INPUT/OUTPUT PROCEDURE IS procedure-name-1 [THRU procedure-name-2]" — when
procedure-name-1 is a section-name, the procedure is that entire section.

Blast radius (re-survey): ST CLEAN 17→23, FAIL* 12→8, COMPILE_FAIL 1→0, timeout 2→1. Newly clean:
ST101A/103A/131A/132A(was a timeout)/134A/135A/136A, and ST109A/122A/133A which had been *vacuous* 000-of-000
passes (their verification sections weren't running). Guard ALL GREEN (276), 0 regressions — the change is
shared by every SORT/MERGE with a procedure phrase, including the 6 already-baselined ST and the SM/NC sort
tests. Baselining the newly-clean ST tests (with producer/consumer chain ordering) follows.

