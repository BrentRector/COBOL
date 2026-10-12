## Entry 442 — Full-corpus doc pass: consolidate 179 → 126, one canonical per subsystem, all reference-valid

Owner directive: *"make an edit pass over all 159+ docs and update every one, if necessary, to reflect the current
design/status/plan. I want all docs to be valid reference materials. If a doc represents an abandoned plan, delete it."*
On the dedup question the owner chose: *"consolidate to one canonical each, but extract the non-duplicate current
information from all the duplicates so the canonical contains all information from the parts."*

Executed as a multi-phase agent pipeline, grounded in the current truth (.NET 10 / C# 14; CIL-only via Mono.Cecil, no
custom VM; ManagedPointer single carrier; M001/M003/M004 decompositions DONE; data-model through Stage-4; M1 complete):
1. **Classify (14 read-only agents)** — every doc → KEEP / UPDATE / ANNOTATE / DELETE / CONSOLIDATE with the specific
   stale facts + exact edit. Result: 56 KEEP, 87 ANNOTATE, 16 UPDATE, plus deletion/consolidation candidates.
2. **Cluster (global)** — the per-bucket pass couldn't see cross-bucket dups, so I resolved the duplicate clusters
   against the real on-disk filenames: **21 subsystem clusters covering 67 docs** (4 ExecutionContext, 4 Memory-Model,
   4 File-IO, 5 Debugger-named, 3 Report-Writer, 3 OO, 3 INSPECT, 5 JSON/XML, 6 system-overview, 4 packaging/build,
   3 CALL, etc.).
3. **Merge (21 + 5 agents)** — each cluster → ONE canonical absorbing every member's unique CURRENT content + a status
   banner (real impl status, stack, pointer to MASTER_PLAN) + stale-fact corrections. Where a LIVE SSOT was the natural
   home (grammar→ANTLR4-GRAMMAR-ARCHITECTURE, semantic→SEMANTIC-ANALYSIS, OO→OO_IMPLEMENTATION_DESIGN,
   report-writer→REPORT_WRITER_ROADMAP) the essays were folded in surgically (additive). One agent correctly EXCLUDED a
   mis-clustered member ("Semantic Rules & Edge-Case Behavior Spec" is a distinct behavior constitution, not analyzer
   architecture) and it was kept separate.
4. **Banner singletons (10 agents)** — status banners + stale-fact fixes on the 47 standalone docs (AUDIT_REPORT
   CobolDataPointer→ManagedPointer + C#13→14; USER-GUIDE → COBOL.NET/`cobol` CLI; CONSTRAINTS/GRAMMAR_AUDIT/
   NIST_TEST_REPORT counts; decomposition-status flips; etc.).
5. **Delete** — `git rm` of **53 docs**: 49 merged-into-canonical duplicates + 4 abandoned/broken (2 zero-byte stubs
   `O & Environment-Variable` / `CD, Version Pinning…`; the rejected `Custom VM Instruction Set Specification` since the
   backend is settled CIL-only; and `CobolSharp Debugger Architecture.md` whose content was a byte-identical mislabeled
   copy of `Runtime Library Design`).
6. **Verify** — a ripgrep sweep confirmed **zero stale version/pointer assertions** survive (the earlier lowercase-
   substring scan was almost all false positives — "custom vm" was everywhere the *correcting* "no custom VM"; pointer
   tokens were all "no PointerRegistry / renamed from CobolDataPointer") and **zero broken markdown links** (all
   references to deleted docs are legitimate plain-text "consolidated from" provenance). Updated MASTER_PLAN §9 (the doc
   index, rewritten for the consolidated set) + §10.4 (consolidation now DONE).

**Outcome: docs 179 → 126** (53 removed), ~100 design essays → **21 canonical subsystem design-references**, every
surviving doc now a valid reference (accurate status banner, `.NET 10`/`C# 14`, no superseded claims). Content-preserving
(all unique info merged into canonicals; deletions are duplicates or abandoned/broken). Docs-only change — the guard is
unaffected (the .NET 10 build stays green).

**Transparency (`feedback_transparency`):** the workflow "read element [k] of the config" dispatch proved UNRELIABLE —
agents miscounted indices, so the first merge run double-processed 4 clusters and skipped 5 entirely. Caught it by
reconciling returned canonicals against the cluster map; re-ran the 5 missing clusters with a **one-file-per-agent**
dispatch (robust). Double-processed canonicals were verified dup-free (last-write-wins gave one clean merge). **Lesson:
give each parallel agent its own input file, never "index into a shared array."**

