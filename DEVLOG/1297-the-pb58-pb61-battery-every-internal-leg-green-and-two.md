## Entry 1297 — 2026-08-18 00:49 PDT — The PB58+PB61 battery: every internal leg green, and two differential flips that are the §15.3 screen doing its job

**Tree `9ece51ea`, `scripts/battery.sh`:** Conformance **4616/4616** · Unit **4183/4183** · Characterization
33/33 · guard-fast NIST **353 MATCH / 0 REGRESSIONS**, audit CLEAN · GnuCOBOL differential 1323 cases,
**2 per-case flips**, both `AGREE_ACCEPT → WE_REJECT_THEY_ACCEPT` and both from PB58's argument screen:

- `run_functions:1725` (their "FUNCTION INTEGER-OF-FORMATTED-DATE" case) writes a LITERAL argument-2 —
  `INTEGER-OF-FORMATTED-DATE("YYYY-MM-DD", "2013-12-30")`. §15.48.3 r3: "Argument-2 shall be a data item of the
  same type as argument-1" (D.31.5.8 says it again: "a literal … and a data item whose content is in the specified
  format"), where the sibling SECONDS-FROM-FORMATTED-TIME r3 says only "shall have the same type". PB58 landed the
  data-item half of r3 (COBOLNET1627); GnuCOBOL accepts the literal as a leniency.
- `run_misc:3305` (their "SORT: table (2)") displays `FUNCTION TRIM(TAB1-NR(K))` over a `PIC 99` item. §15.96.3
  r1: "Argument-1 shall be a data item of class alphabetic, alphanumeric, or national" — class numeric is not in
  the list; GnuCOBOL trims the numeric item's character image as an extension.

Both are strict-conformance rejections of nonconforming source, and both are dialect-gated the way DA6 settled it:
under `--permissive` the screen WARNS and the existing coercion runs (`CheckArgumentClasses.Report`). Neither is
implementor-defined latitude, so the owner's follow-GnuCOBOL decision does not apply. The baseline rows are flipped
to `WE_REJECT_THEY_ACCEPT` with this attribution; plan §0's battery line updated. No gate is owed; PB62 is next.

