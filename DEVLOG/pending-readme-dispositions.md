## README: the status section names every declined row by its Annex item, held to CONFORMANCE.md by a test; the test list names the oracle and benchmark projects

PB3134 (docs/readme, MINOR): the owner found the root README outdated the morning after PB3100 landed it. Every claim was
re-verified against `2da05a28a`; the tools, options, emitted files, gate, CI, tree, documents, license and plans all
held. What did not: the status section said four facilities are declined, while `docs/CONFORMANCE.md` §2 and §5 decline
or hold partial 24 rows. The first rewrite listed more of them by hand and was still short: the branch review (R1-1)
found CODE-SET with an alphabet no device encodes (A.3 item 27, COBOLNET1672), a national collating sequence by literals
or LOCALE on a national record key (items 41–42, COBOLNET1584) and CONTINUE AFTER beyond whole seconds (item 8)
missing, and re-reading every row found the refused halves of three facilities whose accepted halves were listed: the
MESSAGE-TAG usage (COBOLNET1943), APPLY COMMIT (COBOLNET1709) and VALIDATE's data-division clauses and exception names
(COBOLNET1708, COBOLNET1710).

So the list is no longer kept by hand. Each bullet names its Annex item (A.3 item N, A.4.N) with its code and severity,
and `ReadmeDeclinedSummaryDriftTests` reads §2 (through `AnnexA3RegisterDriftTests.Register`, the register's one
parser) and §5 and fails when a row that is not Claimed goes unnamed or when the README names a row the tables claim.

The test section names the two projects of the solution that measure rather than test, `tests/Cobol.Net.ArchOracle` and
`tests/Cobol.Net.Benchmarks`, and says what the oracle captures and compares. The branch's first commit named the recorded
oracle baseline and the landing check that refused a stale one (kb/Work PB2885); PB3107 retired both the same afternoon,
the operator flagged it, and the branch merged main and restated the paragraph before its review. No compiler code
changed.
