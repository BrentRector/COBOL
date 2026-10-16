## Entry 2069 — 2026-10-16 06:46 PDT — PB160 landed: the Annex A.1 documentation register is complete; GR-8.3.3.5.4-5 closes on its pinned determination

Wave 1091 group G (the PB2911 GAP campaign, Opus fix lane). PB160 had been open since batch 8 (2026-08-29) as the
note that the Annex A.1 documentation register was 169 obligations behind. Re-measured on this tree,
`scripts/spec/audit_annex_a1.py` reports 182 of 182 required obligations documented in `docs/CONFORMANCE.md` §7,
none remaining and no findings: every item the note named over its life (1, 5, 18, 19, 39, 56–58, 97, 122, 177, 178,
210–217) now has its determination. Three of its four claimed inventory rows were already `OK`, closed by the
doc-rows and golden lanes without a back-link here: `DOC-A.1-97` (`bc1756abb`), `DOC-A.1-122` (`6563edae7`) and
`DOC-A.1-217` (`1de01e454`).

The fourth, `GR-8.3.3.5.4-5`, was PARTIAL only because the determination was not written down and no test pinned it.
§8.3.3.5.4 GR5 leaves the result of a hex-character-sequence with no corresponding character to the implementor
(`cite.py --check 8.3.3.5.4 "The implementor defines the result of specifying a hex-character-sequence-1 for which no
corresponding character in that coded character set exists."` → OK §8.3.3.5.4 5). `CobolLiteral.DecodeHexGroups`
stores each four-digit group verbatim as its UTF-16 code unit, so a surrogate half or noncharacter takes one
character position with no diagnostic and no exception condition. DOC-A.1-97 documents this, and the golden
`2002/l1c12_hex_national_no_character` pins it. Re-probed on this build at `--std 2002`, the golden's output matched
its `.out` exactly. Batch `w1091g-PB160-hex-national-no-character` re-verdicted the row PARTIAL → CONFORMS at
2002/2014/2023 (COBOLNET0900 at 85, negative `r03-hex-national-literal-at-85`): GAP 98 → 97.

Found and fixed on the way: the witness golden compared two literals, `IF NX"FFFF" > NX"D800"`. §8.8.4.2.1 forbids
that (`cite.py --check 8.8.4.2.1 "A relation condition shall contain at least one reference to an operand that is
not a literal"` → OK §8.8.4.2.1 13), and the compiler warned COBOLNET3146. The golden now uses the stored data item
`WS-N(2:1)` as the subject, and its output is unchanged. The `DecodeHexGroups` comment cited only GR4 (the mapping);
it now also cites GR5 and DOC-A.1-97, so `where.py 8.3.3.5.4 5` finds the site. Eleven other conformance goldens (besides the deliberate `85/pb1470_relation_of_two_literals_runs`) still
compare two literals; they are reported as a lead, not fixed here.

