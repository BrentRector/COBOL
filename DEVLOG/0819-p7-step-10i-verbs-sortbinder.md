## Entry 819 — 2026-07-11 20:16 PDT — P7 Step 10i: `Verbs/SortBinder`

**What (the AS-BUILT PLAN's 10i batch).** SORT (file + table F2) / MERGE / RELEASE / RETURN as one class
over `(ctx, host, seqIo)`: `ResolveProcedure` stays a HOST edge called at the SAME bind point
(INPUT/OUTPUT PROCEDURE resolution is position-dependent — never snapshot early; internal now);
WriteSource/FileOfRecord flip from the 10h host-accessor interim to the ctor-injected
`SequentialIoBinder`; `host.BindBlocks` rides the RETURN AT-END Split lambda. The 0870/0871/0872 gates
moved VERBATIM with their exact per-site control flow (report-and-continue at table-SORT/RELEASE vs
report+BoundUnsupported at alphabet-name-2). The 8 bound types → records-only `BoundSort.cs`.

**Verify.** Sln Debug clean; 33 characterization (32 snapshots byte-exact) · 281 unit · 3166 conformance —
verdicts read as separate actions.

