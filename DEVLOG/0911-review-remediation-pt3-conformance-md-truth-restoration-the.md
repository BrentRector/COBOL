## Entry 911 — 2026-07-19 13:25 PDT — Review remediation pt3 — CONFORMANCE.md truth restoration + the stale-note sweep (FIX NOW batch complete)

**CONFORMANCE.md (the §4.2.16 record now states CURRENT truth):** the '04' bullet flipped to EMITTED
(§14.9.30 GR14 / §9.1.13.2 item 3; record-sequential READ, fixed + varying legs, line-seq excluded per GR15);
the §1/§4 four-facility present-tense claim reworded (SCREEN's COBOLNET1560 warning is landed; MCS/COMMIT/
ROLLBACK/VALIDATE are today a generic parse error, named recognition = the tracked Wave H code half); the two
dangling "(§2.4)" refs → §4; MCS row reworded (the pre-2002 COMMUNICATION SECTION is not 2023 surface);
statement-§ fixes: DISPLAY row 14.9.13→14.9.11, OPEN rows 14.9.26→14.9.27 (each verified in the spec).

**Stale notes corrected to current state:** the data-model deep-dive's three EC-BOUND-OVERFLOW deferral notes
(the raise is LIVE since P13; EC-BOUND-SET + full var-length-group MOVE/COMPARE remain the follow-ons);
DebugItem's group-width arithmetic text (SubWidth=5 ⇒ 86, not the stale "+4…=83") + the DEBUG-LINE comment
(the CAUSING statement, the Wave F fix — the comment still described the subject-procedure semantics);
ConditionalCompilationProcessor's header (>>EVALUATE/>>WHEN ARE implemented; the real deferrals restated with
their silent-wrong-value defects named, per review C2); DiagnosticCatalog's two 1550-1552 "earmarked
(PHASE-12)" comments (the earmark expired unused — unallocated holes).

Gate: build + drift tests + characterization 33 byte-exact (docs/comments only). The FIX NOW batch of the
review ledger §6 is COMPLETE except the 0849 shared-code reconciliation (routed to the spec-derive wave — it
needs a renumber decision, not a comment).

