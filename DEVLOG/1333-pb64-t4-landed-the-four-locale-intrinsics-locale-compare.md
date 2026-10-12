## Entry 1333 — 2026-08-19 12:04 PDT — PB64 T4 LANDED: the four LOCALE intrinsics — LOCALE-COMPARE over the ONE locale-collation carrier, LOCALE-DATE / LOCALE-TIME / LOCALE-TIME-FROM-SECONDS over `LocaleFacts` (the ONE place a CultureInfo is read; d_fmt / t_fmt per DETERMINATION L10) — A.4.9 items 2–5 CLAIMED; EC-LOCALE-INVALID live; 34 inventory rows CONFORMS (GAP 3908 → 3874)

**What T4 is** (DESIGN-locale-facility §4.7/§4.8/§8): the functions that READ a locale — `LOCALE-COMPARE ( a b
[ locale-name-1 ] )` (§15.51), `LOCALE-DATE ( yyyymmdd [ locale-name-1 ] )` (§15.52), `LOCALE-TIME ( hhmmss
[ locale-name-1 ] )` (§15.53), `LOCALE-TIME-FROM-SECONDS ( seconds [ locale-name-1 ] )` (§15.54) — and the
runtime snapshot they need, `LocaleFacts`.

**Runtime.** `Globalization/LocaleFacts.cs` — the resolved snapshot of one locale's COBOL categories, cached per
tag: LC_COLLATE is the tag (our own engine, never `CompareInfo`); LC_CTYPE / LC_MONETARY / LC_TIME are .NET culture
data from the tag's nearest predefined culture (the base tag without its `-u-` extension, then its ancestors —
`sr-Latn-RS` → `sr-Latn` → `sr`), with `HasCultureData` false (the invariant culture standing in) when no ancestor
exists or the process is in .NET invariant globalization mode (detected once). DETERMINATION L10: `d_fmt` =
`ShortDatePattern`, `t_fmt` = `LongTimePattern` (the pattern that carries seconds). `Intrinsics/CobolLocale.cs` —
`Compare` (the ONE `LocaleCollation` carrier + the sign map — deliberately NOT a second comparison; §15.51.4 r2's
trimming rides the carrier), `Date` (YYYYMMDD validated per CURRENT-DATE's definition, year 1601–9999 →
EC-ARGUMENT-FUNCTION; `DateTime.ToString(d_fmt)`), `Time` (HHMMSS with THIS clause's own ranges — §15.53.3 r3: hours
00–24, seconds 00–99 — rendered over `t_fmt`'s TOKENS by `FormatTime`, because a `DateTime` cannot hold 24:00:00 or
:99), `TimeFromSeconds` (the ONE standard-numeric-time-form screen, `CobolDate.SecondsOutOfStandardFormFor` — an
internal forwarder over FORMATTED-TIME's; a scaled argument's fraction follows the seconds with the locale's
decimal separator — Annex D.31.4.5's nanosecond note, a determination). Resolution at USE: a named unavailable
locale is EC-LOCALE-MISSING (the root's answer when checking is off); an available locale without culture data for
the category is **EC-LOCALE-INVALID** (§8.2.1 "invalid or incomplete" — a NEW ambient gate: `CheckingFlags.LocaleInvalid`,
`ExceptionState.LocaleInvalidError`, `EcEmitter` + `EcBinder` on intrinsic-bearing statements; the invariant content
stands when checking is off). The design's §8 limit-1 bullet said EC-LOCALE-MISSING for invariant mode; as built it
is EC-LOCALE-INVALID — availability is the ONE known-locale rule, content is the culture data, and §8.2.1 names a
condition for each (the doc is corrected).

**Compiler.** The four catalog rows bind Runtime with `ArgKinds` `"ssl"` / `"sl"` / `"sl"` / `"nl"` — the trailing `'l'` is
§15.3 argument type 8, a LOCALE-NAME (`IntrinsicArgumentRules.NonOperandArgumentKinds['l']`, the PB1-shaped
disposition row; lower-case like every catalog code — the drift test's catalog regex reads `[a-z ]*`, which is how the
first cut's upper-case `'L'` surfaced as "not in the catalog"), `RuntimeMethod` `Compare` / `Date` / `Time` /
`TimeFromSeconds` → `RuntimeApi.LocaleFn` → `CobolLocale.*`. `IntrinsicBinder.BindLocaleFunction`: the operands bind,
the optional last position resolves through `DataBinder.ResolveLocaleName` (COBOLNET1664, citing §15.5x.3's rule) to
the ONE `LocaleRef` on `BoundIntrinsicCall.Locale`; the Verified schemas screen the operands (§15.52.3 r1's 8 and
§15.53.3 r1's 6 positions via `ArgPredicate.ExactWidth` — a 7-position item or the INTEGER literal the T0 probes
wrote is COBOLNET1627). The `IntrinsicBind.Unsupported` arms are gone (no row is Unsupported any more), the D8
edition window is enforced for the four (1502 below 2002 / 2014 — `locale-functions-2002`,
`locale-time-from-seconds-2014` rows; the 2014 edge is kb/Work R28's provisional window), `LocaleUnsupported`
stays for the LOCALE phrase of LOWER/UPPER/NUMVAL-C (T5/T6). `RuntimeDetermined` needed no result-rule member: the
dynamic-length string every §15 string function already returns IS the run-time-determined length (§15.52.4 r3).

**Tests.** Golden `tests/conformance/2014/pb64t4_locale_functions` (2014 because TRIM and LOCALE-TIME-FROM-SECONDS
are 2014: CMP-CUR/ES/SET/EQ/FR, DATE-ROOT/FR/JA + LEN, TIME-ROOT/DE/US/24/99, SEC/SEC-FRAC/SEC-FR, EC-LOCALE-MISSING
observed with the statement interrupted, the EC-ARGUMENT-FUNCTION default); negatives `pb64t4-locale-date-width`
(1627), `pb64t4-locale-function-name-undeclared` (1664), `pb64t4-time-from-seconds-class` (1627); the by-name
refusal negative `locale_functions_a49` deleted; `LocaleDispositionTests` (the four functions compile clean with/
without a locale-name; the integer-argument shape is a class violation, not a refusal) and
`LocaleModuleNonSupportTests` (the suppression LIFTED: 1502 below the window, binding at it; the LOCALE PHRASE
still 1518-only) rewritten; `CobolLocaleTests` (7 facts — the sign map, d_fmt, t_fmt with the r3 ranges, the fraction
+ LEAP-SECOND, the token renderer, MISSING, INVALID through a site-tailored made-up tag "zz-QQ" that is available but
has no culture data). The unit tests pin the ROOT explicitly (`ru.Locale.Set(All, "")`) — they run in-process under
the HOST's user default, which the first run revealed as "8/19/2026" (en-US) where the golden's harness-pinned root
gives "08/19/2026". Inventory: 34 rows CONFORMS (§15.51–§15.54's formats, argument and returned-value rules;
§14.6.6 r7/r8).

**Docs.** CONFORMANCE.md §4 item 5 (items 2–5 claimed, L10, EC-LOCALE-INVALID); DESIGN-locale-facility (§1/§2
re-verdict, §8 rows + the limit-1 correction, §12 T4 ✅); kb/Work PB64 (T5 next); plan §0.

**Gates.** Wave-local GREEN (Conformance 259 on `~Intrinsic|~Locale|~SetLocale|~StandardCompare|~Formatted|pb64t4|
Manifest|VersionMatrix&locale` · Unit 4463/4463 · Char 33/33). **Battery #24 (first run) was NOT GREEN for two
attributed reasons, both fixed before the commit:** (1) Unit 2 red — `CobolLocaleTests`' site-locale fact and
`LocaleManagerTests.SiteTailoringDirectory_IsDiscoveredAndSelectable` both set the process-global
`COBOL_COLLATION_DIR` and xUnit ran the two classes in parallel (the wave-local run had not collided): now one
xUnit collection (`site-tailoring-directory`), rerun ×3 green; (2) the GnuCOBOL differential: **4 FIX flips**
WE_REJECT_THEY_ACCEPT → AGREE_ACCEPT — `run_functions:1883` (LOCALE-COMPARE), `:1913` (LOCALE-DATE), `:1939`
(LOCALE-TIME), `:1965` (LOCALE-TIME-FROM-SECONDS) — the four GnuCOBOL programs now compile AND agree (their
assertions are the sign map and non-blank output); exactly the T-E flips the design predicted; baseline
regenerated with `--write-baseline` (1323 cases: 577/469/207/70). Battery #24b on the corrected tree — §0's
reference. Next:
**T5** — CHARACTER CLASSIFICATION (the G3 OBJECT-COMPUTER grammar work), LC_CTYPE class tests, the UPPER-CASE /
LOWER-CASE LOCALE phrase over `LocaleFacts.TextInfo` (L9), then T6.

