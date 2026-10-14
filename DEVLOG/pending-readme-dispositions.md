## README: the status section names every declined element, the test list names the oracle and benchmark projects

PB3134 (docs/readme, MINOR): the owner found the root README outdated the morning after PB3100 landed it. Every claim was
re-verified against `2da05a28a`; the tools, options, emitted files, gate, CI, tree, documents, license and plans all
held. What did not: the status section said four facilities are declined, while `docs/CONFORMANCE.md` §1, §2 and §5 also
decline FORMAT and SELECT WHEN (COBOLNET1705), WRITE FILE and REWRITE FILE (COBOLNET1706), STANDARD-BINARY arithmetic
(COBOLNET0806), FLOAT-BINARY-128 and the standard decimal floating-point usages (COBOLNET1564, the FLOAT-DECIMAL clause
warned by COBOLNET2424), the RECORD DELIMITER clause (warned, COBOLNET1778), a record key's SOURCE phrase (COBOLNET1954),
class multiple inheritance (COBOLNET0849) and method overloading (COBOLNET0822), and hold the report writer and
dynamic-length items partial. The section now points at §2, §4 and §5 and lists each with its code and severity, read
from the catalog and the binders rather than from the conformance prose alone.

The test section names the two projects of the solution that measure rather than test, `tests/Cobol.Net.ArchOracle` and
`tests/Cobol.Net.Benchmarks`, and says what the oracle captures and compares. The branch's first commit named the recorded
oracle baseline and the landing check that refused a stale one (kb/Work PB2885); PB3107 retired both the same afternoon,
the operator flagged it, and the branch merged main and restated the paragraph before its review. No code changed.
