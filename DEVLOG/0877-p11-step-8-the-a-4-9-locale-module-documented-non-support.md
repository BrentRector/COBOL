## Entry 877 — 2026-07-17 13:08 PDT — P11 Step 8 — the A.4.9 locale module → documented non-support (COBOLNET1518); intrinsic exit criteria 1/3/4 MET

Ratified decision 3 realized: documented non-support of the OPTIONAL ISO Annex A §A.4.9 locale module —
conformant per ISO §4.2.7 + §A.4.1 (an implementation accepts an optional element's syntax ONLY when
support is claimed; A.4.1 makes even the associated syntax/GRs/ECs optional). This turns the five
`Unsupported` catalog rows (landed in Step 1) into a BIND-TIME reject BY NAME, so "zero Deferred" is now
fully honest — no locale row falls to the renderer's loud backstop — and it closes the LOCALE keyword
variants.

- IntrinsicBinder: ONE `COBOLNET1518` formatter (`LocaleUnsupported`), placed after the D8 window gate,
  before the special-bind dispatch, shared by two surfaces:
  (a) the five locale FUNCTIONS (`Bind == Unsupported`) — LOCALE-COMPARE/-DATE/-TIME/-TIME-FROM-SECONDS +
      STANDARD-COMPARE. Per the scout, STANDARD-COMPARE is A.4.9 item 11 but is ORDERING-TABLE-dependent
      (SPECIAL-NAMES ORDER TABLE §12.3.7), not locale-dependent — so it ALSO cites §A.3 item 25 (the
      implementor need not accept the syntax absent an ISO/IEC 14651:2020 implementation).
  (b) the LOCALE keyword PHRASE of the otherwise-supported LOWER-CASE §15.57 / UPPER-CASE §15.97 /
      NUMVAL-C §15.68 / TEST-NUMVAL-C §15.94, via `HasLocalePhrase`. LOCALE is not a reserved word, so the
      phrase (`arg-1 LOCALE locale-name-1`) parses as extra space-separated arguments; the keyword is found
      at argument position >= 2 (never argument-1, so a data item named LOCALE is not a false positive).
      NUMVAL-C's LOCALE keyword is a spec Annex-A LIST OMISSION (verified by the scout — it appears nowhere
      in Annex A, unlike TEST-NUMVAL-C's which is A.4.9 item 12); disposed identically since §15.94.3 r1
      imports §15.68.3 and the phrase is inoperable without the A.4.9 SPECIAL-NAMES LOCALE machinery.
  The functions WITHOUT a LOCALE phrase bind exactly as before (CLI-proven: LOWER-CASE("AbC") -> "abc",
  NUMVAL-C("$1,234.56") -> 1234.56).

- Tests: negatives `locale_functions_a49` (5 functions -> 5x 1518, no other error) + `locale_keyword_a49`
  (4 keyword phrases -> 1518), both @2023; NEW `LocaleDispositionTests` (11 facts: each function/phrase, the
  STANDARD-COMPARE A.3-item-25 citation, the bare-LOCALE NUMVAL-C form, and a zero-regression proof).

AI misstep logged (feedback_transparency): the first negative golden named its data items RD/RN — RD is the
Report Writer RD reserved word, a parse error before binding; caught by the CLI spot-run (5 parse errors, 0x
1518), renamed WS-REL/WS-DATE/WS-SECS.

**INTRINSIC EXIT CRITERIA MET:** (1) zero Deferred rows; (3) every promotion has a value-exercising golden;
(4) every promotion has a window/disposition negative row. Only exit criterion 2 (Tier-C decided) remains.

Battery on the staged tree: conformance **3515/3515** (+13) · unit **301/301** · legacy 1196+636 ·
guard-fast first pass 352+1 (ST103A COMPILE FAILED, the STRING-suite flake) -> **solo rerun 353 MATCH, 0
regressions, ALL GREEN** (the environmental flake class, 6th occurrence).

