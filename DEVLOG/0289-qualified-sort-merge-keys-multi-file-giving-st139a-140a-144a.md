## Entry 289 — Qualified SORT/MERGE keys + multi-file GIVING → ST139A/140A/144A/147A + ST107A/126A (NIST 289→295)

Two SORT/MERGE bugs, surfaced by the "MERGE custom-alphabet" family, that turned out not to be about
collating at all (MY-FAVORITE-ALPHABET is just STANDARD-1):

**(1) Qualified sort/merge keys silently dropped.** `BindSortKeys`/`BindMergeKeys` resolved each key via
`ResolveData(dataRef.GetText())` — for a qualified key like `A-KEY OF SORT-KEY`, `GetText()` concatenates to
"A-KEYOFSORT-KEY", which resolves to nothing, so the key was dropped. With every key dropped, the SORT/MERGE
ran with ZERO keys → records came back in input order, not sorted/merged order (ST139A merged 102 records but
unsorted; the count/order checks failed). Same `GetText()` trap as IX215A. Fix: a `ResolveKeyDataReference`
helper extracts the base name + OF/IN qualifiers and resolves via `SemanticModel.ResolveQualifiedData` (the
qualifier-aware resolver added in 281); unqualified keys are unaffected.

**(2) Multi-file GIVING wrote only the first file.** A `SORT/MERGE … GIVING file-a file-b file-c` writes the
ENTIRE result to EACH output file (ISO §14.9.24/§14.9.45). `EmitSortGivingFile` consumes the sort's return
cursor (`ReturnIndex`), so the first GIVING file got all records and the rest got none — ST147A's SQ-FS4/
SQ-FS5 came out wrong ("EOF NOT FOUND"). Fix: new `IrSortRewind` → `SortRuntime.RewindReturn` resets the
return cursor at the start of each GIVING file's write loop, so every output file receives the full result.

Impact (re-survey): ST CLEAN 25→29. Baselined the 4 MERGE tests (ST139A 10/10, ST140A 11/11, ST144A 11/11,
ST147A 26/26) plus two chain consumers the same fixes unblocked: ST107A (6/6, ← ST106A) and ST126A (18/18,
← ST125A, a 3-file consumer). Guard ALL GREEN: 1000 unit / 347 integration / **295 NIST** (… + 40 IX + 25
ST), 0 regressions — both fixes are shared by every SORT and MERGE, and all 25 ST + the SM/NC sorts stay
green. Remaining ST: ST117A (1 FAIL* even after its ST116A builder — a genuine per-test bug), ST121A (fails
after ST120A — needs the right producer), ST146A (1 FAIL*), ST137A (rc=127 crash), ST115A (timeout), the
NO_OUTPUT builders, ST301M (flagging).

