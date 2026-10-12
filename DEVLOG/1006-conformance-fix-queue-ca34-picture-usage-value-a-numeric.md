## Entry 1006 — 2026-07-23 02:41 PDT — Conformance fix-queue: CA34 (picture-usage-value) — a numeric VALUE literal must be in the PICTURE range / correctly signed (ISO §13.18.63.3 SR2/SR3), rejected loud not silently mis-stored

**CA34 (CONFORMANCE-FIX-QUEUE, MAJOR/M) landed.** ISO §13.18.63.3 SR2 requires a numeric VALUE literal to be a
permissible value in the range the PICTURE indicates, "representable exactly in the subject of the entry, without
truncation of leading or trailing nonzero digits"; SR3 requires a signed numeric literal to have a signed (or
sign-bearing numeric-edited) subject. Both are `shall` SYNTAX rules, so the only conforming response to a violation
is a compile-time diagnostic — the legacy lenient truncating / sign-dropping store is a non-conformant extension.

**The bug.** For a category-numeric item the initializer is `ValueInitializer.cs:169 EmitText.UnscaledAtScale(raw,
pic.Scale)`, which only rescales the fractional part — NO high-order modulo, NO unsigned magnitude. So `01 A PIC 99
VALUE 12345` silently seeded `12345L` into the native `long` (so `IF A = 12345` was TRUE), and `01 U PIC 99 VALUE
-5` seeded `-5L` into an unsigned field. The sole VALUE validator (`DataBinder.ValidateValueCategory`) covered only
National/Boolean category+size — a grep confirmed NO §13.18.63 SR2/SR3 range/sign check existed for category-numeric.
This was the highest-impact confirmed picture-usage-value finding because it produced a silently-WRONG runtime value,
not merely a missing strictness diagnostic.

**The fix.** New `DataBinder.ValidateNumericValue(pic, raw, where)`, called alongside `ValidateValueCategory` at the
item-VALUE site (DataBinder.cs ~2076) for `pic is { Category: Numeric, IsFloat: false }`. It parses the plain numeric
literal (figurative / quoted / national are handled on their own paths and returned early), then:
- **SR3:** a leading `-` into an unsigned subject → `COBOLNET1625`. Scoped to the NEGATIVE sign (the sign-losing
  case); a leading `+` into an unsigned item is the common harmless idiom (`+5 == 5`) and is not rejected.
- **SR2:** model the subject's stored ('9') digit positions by their power-of-ten exponents. UNIFORMLY across an
  implied point (V), leading-P (Scale > Digits), and trailing-P (Scale < 0) pictures — given `Digits` and the signed
  `Scale` — the lowest stored exponent is `-Scale` and the highest is `Digits - Scale - 1`. The literal is
  representable iff every NONZERO digit lands in `[low, high]`; a nonzero digit above `high` is leading-nonzero
  truncation, one below `low` trailing-nonzero truncation. Value-zero literals and picture-less numerics (INDEX,
  Digits 0) are skipped. The exponent model was derived directly against `PictureAnalyzer`'s scale convention
  (trailing P → `-trailingP`; leading P → `leadingP + digitPositions`), NOT assumed.

**Diagnostic allocation.** The RESUME-AT scouting suggested COBOLNET0803 in an "unallocated 0803-0807 value/picture
band" — WRONG: a grep proved the ENTIRE 0801–0899 band is allocated (0803 = rounded-mode-is-2014). Confirmed the
mechanical next-free via `session-probe` = **COBOLNET1625** (registered a `DiagnosticCatalog` descriptor
`value-numeric-out-of-range`; regenerated `docs/DIAGNOSTICS.md`; the drift/registry tests stay green). Lesson
reinforced: never trust a scouted code number — verify against the registry (§0 itself flagged this).

**Strictness sweep (the CA39 lesson).** This rejects previously-accepted source, so a 6-agent corpus sweep
(`wf_ebe10542-4e0`) scanned 806 corpus files → 94 candidates, **0 genuine at-risk cases**: every one of the 92 NIST
candidates is a P-scaled picture whose value FITS once scale is applied (the sweep's decisive caveat — the SR2 check
MUST be scale-aware, not raw-digit-length — is exactly the property the exponent model has; hand-verified the sweep's
#1 canary `NC114M S9P(17) VALUE -100000000000000000` → stored unscaled −1, FITS). The 2 flagged rejects are the new
negative fixtures themselves.

**Gate (comprehensive — a common-construct strictness change).** Full greenfield Conformance **3891/3891 0 fail**
(incl. `NistDifferentialTests` = all NIST P-scaled VALUEs compile) · characterization **33/33** byte-identical (no
emit change for valid programs) · diagnostic drift/registry **28/28** · corpus sweep 0 regressions. Goldens: two
negative fixtures `conformance/negative/ca34_value_range_over` (SR2) + `ca34_value_negative_unsigned` (SR3),
registered in the negative manifest, edition-invariant (reject-at 85 2002 2014 2023). **16 landed / 30 fix-ready
remain.** NEXT = CA35 (USAGE BINARY/COMP/PACKED with a non-numeric picture, §13.18.60.3 SR3).

