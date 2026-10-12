## Entry 779 — 2026-07-11 13:00 PDT — P5.10+12+13: ClrType deleted; Storage single-writer-sealed; the equivalence scaffolding retired; the apostrophe-VALUE goldens land PROVEN failing-first

**Step 10 (as re-scoped by the recorded deviations):** `DataItem.ClrType` DELETED (zero readers — the topology
audit's grep). `Storage` gets an INTERNAL setter + the single-writer doc (StorageFormPass is the sole writer; the
design's init-only shape is not expressible with the pass-assignment pattern — deviation recorded). The doc's
10.1 "delete StoreAsImage entirely" is DEVIATED: ~35 readers all mean exactly "is this numeric leaf
image-stored", so the NAMED projection is the one definition (singular-pattern) — kept. 10.2's cached image
facts are DEVIATED to P7: the recompute-on-read HAZARD (sensitivity to the last flag flip) died with the flag;
what remains is O(subtree) perf, safer done with the P7 DataItem slimming (fill-timing interacts with the Tier-B
SignKind rewrite and TYPE expansion).

**Step 12:** identities #1 (Storage↔flag) and #5 (RecordLayout↔OdoModel width) are retired with their subjects
(comments in Verify say why); #2/#3/#4 stay — #3 (RecordLayout↔DataItem.ImageWidth) genuinely guards two live
computations. The DAG was finalized at P6 (GroupTail incl. the terminal conformance pass).

**Step 13 (exit criterion #6):** `ApostropheValueDifferentialTests` ×3 — the '-delimited program byte-equals its
"-delimited twin (elementary + group VALUEs; ALL literal; MOVE ALL; INITIALIZE REPLACING BY), plus the doubled-
opening-delimiter decode PER FORM (the contents legitimately differ across forms — 'A''B' is A' B not A"B; the
first draft equal-compared them, a test bug caught immediately). PROVEN FAILING-FIRST: with `IsStringLiteral`
temporarily reverted to the '"'-only guard, 1 of 3 fails; restored, 3/3 green — the §8.3.1.2 net bites. Battery:
conformance 3166 (+3) · unit 281 · characterization 32 byte-identical · guard 353 MATCH · P5.8+9 CI green.

