## Entry 1277 — 2026-08-09 16:54 PDT — Correction: the map's 4100 was RIGHT — r4 pads the source's bits, and I mis-derived the residue I had just registered

Entry 1276 (four minutes old, already pushed) closed by calling the mechanism map's predicted
`CONVERT(WS-A ANUM NAT HEX)` golden of 4100 "wrong twice over (byte order AND channel)" and asserting
the rule derives 0041. Re-reading §15.19.4 r4 before starting 5b refuted MY claim, not the map's: the
HEX legs hex THE SOURCE'S OWN BITS — "if the number of bits in argument-1 is not a multiple of those
needed for a single national character, the TRAILING portion … is padded with zero bits" — so an 8-bit
ANUM source 'A' pads trailing to 16 bits, 0x41 0x00, hex 4100; the destination keyword (ANUM vs NAT)
only picks the DIGIT repertoire (display vs national characters), never a re-encoding of the value.
0041 would be the UTF-16BE of a NATIONAL 'A' — the exact confusion r2/r4's padding units exist to
prevent, and the same trap as reading "in national format" as "of the national representation". The
derivation cross-checks against the inventory's own B"101" analysis (r2 → A0, r4 → A000 — 16-bit
trailing pad), which I had read hours earlier and failed to apply. Corrected in the same tree: §0, the
PB59 note's 5b block, the apply-plan correction block, and AR-15.19.3-7's inventory notes (via
record_verdicts, gate re-run 10/10). The lesson is the one the memory index already carries — a
citation can be right and the DERIVATION still wrong; deriving the expected VALUE from the rule text is
part of the citation discipline, and "the map is wrong" is a claim that needs the same evidence bar as
"the compiler is wrong". The 5b landing pins 4100 as the golden.

