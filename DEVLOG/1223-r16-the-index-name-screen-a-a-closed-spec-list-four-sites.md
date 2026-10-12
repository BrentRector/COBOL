## Entry 1223 â 2026-08-08 11:58 PDT â R16: the index-name screen â a closed spec list, four sites, one code, and one band deliberately left for adjudication

DISPLAY IX compiled clean and aborted at run time. The governing rule turned out sharper than F11's
Â§8.4.3.1.2 framing: Â§13.18.38.3 r7 is a CLOSED LIST â an index-name may be specified only as a subscript,
in PERFORM/SEARCH VARYING, in SET, or as a relation-condition operand. Probing the siblings split the
defect into three bands, and the split IS the design:

- The CRASH band (DISPLAY Â· MOVE-to-string Â· STRING â runtime bombs) and the SILENT band (MOVE IX TO a
  numeric item computed 0002 with no diagnostic) both draw the new COBOLNET1637, screened at four sites:
  DISPLAY's operand walk, MOVE's sending operand â deliberately beside COBOLNET0809, which the W2 review
  already lands unconditionally for class-index DATA ITEMS, so the index-NAME judgment matches the landed
  index-ITEM one â the ONE StrUnstrSender funnel (all four STRING/UNSTRING sending positions, one place),
  and INSPECT identifier-1 (compile diagnostic replacing a runtime stage). IF IX = 2 still binds.
- The ARITHMETIC band (COMPUTE N = IX + 1 â 3, silent) went to a NEW note R29 instead of a blind fix: its
  screen would live in RefExpr's index arm, which also serves the r7-LEGAL numeric consumers, so it needs a
  context audit and the strict/--permissive survey (index-name arithmetic is the classic GnuCOBOL
  extension). Corpus 776/776 confirms no golden relied on any of it.

One process trip worth its line: I hand-added the DIAGNOSTICS.md row and the drift test failed in the same
minute â the file is GENERATED (gen-diagnostics-doc.ps1). The guard caught the fork before it could rot.

Fixtures: negative display-index-name + move-index-name (reject-at all four editions). Gate: wave-local
776/776 Â· Diagnostic drift 30/30 Â· characterization 33/33. Next: R12 (the CALL-boundary ABI, promoted back
to the head now that its two small neighbours are done) Â· R17 Â· R19 â 10 actionable.

