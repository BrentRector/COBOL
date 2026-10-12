## Entry 1242 — 2026-08-08 17:56 PDT — R23: the citation sweep lands and immediately pays for itself — four register rows, one mis-filing corrected

CobolDate cited "implementor item 171" as documented while §7 carried no such row — a documentation
claim nothing checked, the dead-lookup shape in prose. Verified against the standard first: A.1 item
171 IS the SECONDS-PAST-MIDNIGHT precision, required + shall-be-documented — the comment was right and
the REGISTER was missing the row. §7 now carries it (100 ns / 7 fraction digits, tick-count at scale 7,
pinned by CobolDateWindowingTests).

The audit gained the sweep the note demanded: sweep_source_citations — every "implementor item N" /
"A.1 item N" in src/Cobol.Net.* must name a real A.1 item AND a §7 row that discharges it; --self-test
proves both failure reasons fire (7/7). Its FIRST real run caught three more undischarged claims across
six sites — item 22 (CHAR-NATIONAL's ALSO-position choice: the FIRST character defined), item 180
(SMALLEST-ALGEBRAIC rejects float usages under native arithmetic — the increment is exponent-dependent,
not a PICTURE property), item 188 (the ALPHABET UTF-16 correspondence is the BMP identity on the D-N1
substrate) — each now a §7 row written from the code's own pinned determination.

And the audit caught the R26 commit's own mistake from two hours earlier: the "§7 item 216" row squatted
on A.1-216 (USAGE POINTER) — §7 numbers are A.1 CITATIONS, never a sequence. No A.1 item covers the
counter registers' size, so the determination moved intact to §3 (behavior determinations), with the
code and golden comments repointed and R26's note carrying the correction. §7's header now quotes the
fresh audit: 17 of 199 discharged, findings none.

Gate: audit --self-test 7/7 + full audit clean · Unit drift 3169/3169 · the counters golden by name ·
characterization 33/33.

