## Entry 1339 — 2026-08-28 20:55 PDT — PB64 T6 wave 1: the LC_MONETARY runtime — MonetaryFacts (the ONE snapshot), MonetaryPlacement (the POSIX renderer + the RUNTIME-derived .NET pattern tables, range DISCOVERED — 17 negative patterns, not the documented 16), CobolLocaleEdit (§13.18.40.5 r9–r15; EC-LOCALE-SIZE's FIRST raise site), the NUMVAL-C LOCALE scan; DETERMINATION L12

The first of the T6 landing waves (DESIGN-locale-facility §4.6/§12 T6; the plan hardened by a 7-agent read-only
workflow + adversarial critic — 12 contradictions in the draft corrected, 37 gaps closed, zero owner questions).
Runtime only; the compiler still rejects the syntax (waves 2–3 flip the two COBOLNET1518 arms).

- `Runtime/Globalization/MonetaryPlacement.cs` — ONE POSIX layout renderer (`Render` over (cs_precedes,
  sep_by_space, sign_posn); posn 0 = parentheses ARE the sign; posn 3/4 glue the sign to the currency string,
  which is what separates .NET pattern 16 `$- n` from 12 `$ -n`). The pattern→convention tables are DERIVED AT
  RUNTIME in the static initializer — design §8 said build-time source generator; that bakes the BUILD host's
  semantics in and can never fail on the RUN host, so the design doc is being corrected (rule 5). The accepted
  range is PROBED, never hard-coded: this runtime accepts CurrencyNegativePattern 0..16 (luy-KE uses 16; the
  documented range 0..15 is false), and an unreproducible layout THROWS naming the pattern.
- `Runtime/Globalization/MonetaryFacts.cs` — the ONE LC_MONETARY model (§8.2.2 fields + §8.2.1's ISO 9945
  import), shared by editing and recognition. Determinations documented: int_curr_symbol =
  RegionInfo.ISOCurrencySymbol + one space, NULL for neutral/invariant cultures; int_frac_digits = frac_digits;
  positive_sign = .NET's numeric "+"; p_sign_posn = 1 flat (.NET's positive pattern has NO sign slot — never
  mirrored from n_sign_posn, whose invariant value is 0/parentheses).
- **DETERMINATION L12** (`LocaleFacts.NormalizeLocaleText`, generalizing PB112's L10 addendum — ONE mechanism):
  strip Unicode Cf, map U+00A0/U+202F/U+2009 to the plain space; applied to the LC_TIME patterns AND every
  LC_MONETARY string. Monetary strings consume CHARACTER POSITIONS of a fixed-width format-2 item, so a
  host-varying byte would change the §13.18.40.5 r14 b) truncation arithmetic itself (measured: ICU 72 moved
  fr-FR's mon_thousands_sep U+00A0→U+202F; every ar-* currency symbol ends in Cf U+200F). U+2212 kept.
- `Runtime/Values/Numeric/CobolLocaleEdit.cs` — Format/DeEdit BESIDE CobolEdit (no fixed mask). Format is the
  rules in their own order: r10 BWZ short-circuit (before any locale consult) → §8.2.1 resolution AT USE (r11
  named-else-current, never cached) → GR18 sign (no '+' ⇒ |v|, unsigned, NO exception) → r14 alignment
  (silent) → the FULL-width hypothetical item with per-position roles → r15 suppression (all-Z-zero blanks the
  whole ITEM — no separator, no currency, no sign; the window stops at min(zRun, digitsLeft); a grouping
  separator inside the suppressed run becomes a space — sj < k, correcting the critic's own off-by-one) →
  r14 a/b right-justify-or-LEFT-truncate with **EC-LOCALE-SIZE's first-ever raise site** (character-based per
  r14 b's text, not Table 13's narrower "digits" gloss; checking off stores the truncated remainder). DeEdit is
  the INVERSE of Format under §14.6.13.2 r4 (EC-DATA-INCOMPATIBLE for non-images; all-spaces legal only for
  all-Z or BWZ; a leading space run = the suppressed positions and contributes NOTHING — the hardening
  derivation's own pseudo-code counted suppressed separator spaces as zeros, found by hand-derivation of the
  round-trip test), never the NUMVAL-C scanner (r5b is deliberately laxer).
- `Runtime/Intrinsics/CobolIntrinsics.NumvalLocale.cs` — NumvalCLocale / NumvalCLocaleDec / TestNumvalCLocale
  over ONE template scan generated from the snapshot (§15.68.3 r5; r4 NOT inherited: no commaMode, CR/DB inert,
  the fixed formats gone). Negative convention first (an empty positive sign matches only by absence); both
  r5b.3 currency candidates, longer first; ANYCASE folds ONLY at the currency comparison through the locale's
  LC_CTYPE (L9 1:1, positions stay ordinal; LC_CTYPE joins the §8.2.1 gate); §15.94.4 positions = 1 + the max
  consumed prefix (r1 b.1's "0 1"→3 verbatim), LENGTH+1 when the scan ran off the end, the (cap+1)-th digit's
  ordinal for r6/r7. Permissive determinations (each a CONFORMANCE row in wave 5): group SIZES unvalidated
  (§15.68.1 names the separator, not mon_grouping; strict would reject "1,23,4" — legal in arm A/B), fraction
  width unconstrained by frac_digits, sp* at every adjacency (sep_by_space superset), L12 spacing equivalence
  in matching.
- EC-LOCALE-SIZE: CheckingFlags.LocaleSize + ExceptionState.LocaleSizeChecking/LocaleSizeError (Fatal).
- Tests (+60 Unit): CobolLocaleEditTests (hand-derived invariant-locale rows, the EC arms both ways, the I2
  length invariant, round-trips, fr-FR separator pin), MonetaryFactsTests (T-C 3: the range==accepted-max
  assertion that would have caught pattern 16, canonical-order pins, the round-trip proof re-run, and the
  DRIFT ORACLE — every specific host culture's format-2 edit vs value.ToString("C") placement shape, ≥400
  cultures asserted examined, positive control included — green on first run over 605 cultures),
  NumvalCLocaleScanTests (the parentheses-negative DEFAULT path, r1b/r1c legs, cap ordinals, ANYCASE scope,
  int_curr_symbol first-3, L12 equivalence), EcRaiseSiteDriftTests (T-C 4, empty exemption list — EC-LOCALE-IMP's
  RAISE-only carve-out stated in the test, not in a side list).
- kb/Work: PB113 (report-writer format-2 SILENT wrong answer — pre-existing, found by the sweep; fixed in
  wave 2) and PB114 (the grammar's required IS before locale-name-1 rejects legal source; wave 2) registered.

Gate (wave-local): solution build · Conformance ~Locale|~Numval|~Intrinsic|~Corpus 978/978 · FULL Unit
4535/4535 · characterization 33/33 — GREEN. One test expectation corrected on first run (my hand-derived
error position, not the code). The T4 LOCALE-DATE/TIME goldens survived L12 unchanged.

