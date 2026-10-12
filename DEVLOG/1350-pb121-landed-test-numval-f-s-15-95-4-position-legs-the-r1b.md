## Entry 1350 — 2026-08-29 00:06 PDT — PB121 LANDED: TEST-NUMVAL-F's §15.95.4 position legs — the r1b/r1c misdispatch and the absent r1b.6 capacity leg

Two defects in NvfScan, both from batch 7. (1) The no-significand-digit return was `n + 1` UNCONDITIONALLY,
so a scan that broke on a real character in error ("ABC", "--1", "$1.5", "+.A") was reported on leg c's
LENGTH+1 where leg b requires that character's own position — the SIBLING NvScan in the same file has carried
the correct discrimination since 813d9bd4 and NvfScan (written later, PB60) never inherited it: the
two-arm-dispatch family's seventh audited arm. (2) No capacity check existed at all: under standard-decimal
arithmetic r1b.6 requires the position of the exponent's FIRST digit when the magnitude exceeds the SDIDI's
±9.999…E+6144 (§8.8.1.5.2 NOTE 2), and NvfScan bounded only the significand digit count and the exponent
DIGIT COUNT, so TEST-NUMVAL-F("1E+9999") returned 0 — "conforms".

The capacity flag rides a NEW NvfParse.CapPos field rather than ErrPos, because the ONE-SCAN twins project it
differently: TEST-NUMVAL-F reports it, while NUMVAL-F's ARGUMENT rules (§15.69.3) say nothing about magnitude
— the value twin stays argument-conforming and its own range check disposes (NumvalFDec → CobolDec.FromParsed
→ EC-SIZE-OVERFLOW). Underflow is NOT "exceeds the capacity" (1E-9999 → 0). Native arithmetic has no r1b.6
leg (the rule names only the standard modes) — pinned from the native side in the 2002 golden. The format's
own "n is one, two, three, or four digits" makes the 4-digit exponent cap mode-INDEPENDENT, so that leg was
already right. Standard-binary (cap 35 / SBIDI) is unreachable — COBOLNET0806 rejects ARITHMETIC IS
STANDARD-BINARY as obsolete — and the map is documented at the CapPos field for the wave that lands it.
Sweep: NvScanLocale already discriminates broke-vs-ran-off (bestConsumed >= n → n+1 else bestConsumed+1).
Goldens 2002/pb121_test_numval_f_positions + 2014/pb121_test_numval_f_capacity (hand-derived positions);
24 unit pins in TestNumvalFPositionTests — my first draft of the .001E+6148 case expected position 4 and the
CODE said 7; re-deriving from the rule text ("the position of the first digit of the exponent") proved the
test wrong, not the scan. RV-15.95.4-L2.6 → CONFORMS, RV-15.95.4-1 → CONFORMS.

