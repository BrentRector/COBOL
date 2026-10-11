## Entry 1334 — 2026-08-19 14:20 PDT — PB64 T5 LANDED: the OBJECT-COMPUTER CHARACTER CLASSIFICATION clause (resolved at each ACTIVATION into the module's LC_CTYPE classification), the UPPER-CASE / LOWER-CASE LOCALE phrase, and the classification-aware class tests — A.4.9 items 6, 7, 13 CLAIMED; `LocaleFacts.Require` is the ONE §8.2.1 gate (EC-LOCALE-MISSING / -INVALID at use); §8.8.4.4.3 SR2 → COBOLNET1669; 24 inventory rows (GAP 3874 → 3857); PB109 / PB110 registered

**What T5 is** (DESIGN-locale-facility §4.5): the locale category LC_CTYPE and its two consumers — `OBJECT-COMPUTER.
CHARACTER CLASSIFICATION [IS] locale-phrase-1 [locale-phrase-2] | FOR ALPHANUMERIC IS … | FOR NATIONAL IS …`
(§12.3.6.2; each phrase `locale-name | LOCALE | SYSTEM-DEFAULT | USER-DEFAULT`), the `LOCALE locale-name-1` phrase of
`UPPER-CASE` / `LOWER-CASE` (§15.97.2 / §15.57.2), and the class tests `ALPHABETIC` / `ALPHABETIC-LOWER` /
`ALPHABETIC-UPPER` under a classification (§8.8.4.4.4 GR3 b1 / c1 / d1).

**The one design point: the classification is resolved at ACTIVATION, not at bind and not at use.** §12.3.6.4 GR8 —
"effective with the initial state of the runtime modules to which they apply" — and §14.6.6 r2 — "On activation of
a runtime element, if the CHARACTER CLASSIFICATION clause is specified …, category LC_CTYPE in the specified locale
is used" — make `LOCALE` (GR5 b: "the current locale") the locale current when THIS module is activated: the binder
records WHICH phrase (`ClassificationSpec(LocalePhrase? Alphanumeric, LocalePhrase? National)`, `LocalePhraseKind`
`Named | Current | SystemDefault | UserDefault`), `DispatchEmitter`'s `__Activate` prologue assigns the per-module field
`__CLASSIFY = CharacterClassification.Resolve(kind, tag, kind, tag)` (`Runtime/Globalization/CharacterClassification.cs`
— a pair of `LocaleFacts?`, `None` for a unit without the clause), and a later `SET LOCALE LC_CTYPE` leaves it where
it is while a CALLed contained program — which inherits the clause (GR1) — resolves ITS classification at ITS
activation. Golden `pb64t5_case_locale_phrase` shows exactly that: OUTER-1 = 74 (the container resolved under the
harness's pinned root), INNER = 305 (U+0130 — the contained program activated after `SET LOCALE LC_CTYPE TO TR`),
OUTER-2 = 74 (unchanged), PLAIN = 74 (a second top-level unit with no clause: the implementor's map, r4, even while
LC_CTYPE is Turkish).

**Consumers.** `IntrinsicRenderer` has three arms for `UPPER-CASE` / `LOWER-CASE`: the LOCALE phrase →
`CobolLocale.UpperCase(s, "tag")` (r2 — `BindCaseFunctionWithLocale`: exactly `LOCALE` + one word, COBOLNET1504
otherwise; the word a SPECIAL-NAMES locale-name, COBOLNET1664 otherwise; the 2002 construct gate
`case-function-locale-phrase-2002` — the phrase is 2002, the function 1985; `BoundIntrinsicCall.Locale`, the ONE
`LocaleRef`); a module with a classification → `CobolLocale.UpperCase(s, __CLASSIFY.For(national))` (r3); else the
plain `CobolIntrinsics.UpperCase` (r4 — the generated text of every program without the clause is unchanged).
`ConditionRenderer` appends `, __CLASSIFY.For(national)` to the three class tests; `CobolClass.IsAlphabetic(s,
LocaleFacts?)` and siblings classify a Unicode LETTER per LC_CTYPE (POSIX `alpha` — `Rune.IsLetter`) and, for
`-UPPER` / `-LOWER`, a letter that IS upper/lower or that the locale's case mapping lowers/uppers to something else
(the round-trip — Turkish dotted/dotless I classify as the locale says). **Determination, documented (CONFORMANCE.md
§4 item 5):** SPACE IS NOT ALPHABETIC UNDER A CLASSIFICATION LOCALE — §8.8.4.4.4 GR3 b2 (no locale) lists "space"
explicitly, b1 (a locale in effect) names only the characters LC_CTYPE identifies as alphabetic, and POSIX `alpha`
excludes space; `"ab cd" IS ALPHABETIC` is TRUE without a classification and FALSE under one (golden
`pb64t5_character_classification`, whose UP-I / LO-I / LEN-UP / LEN-LO / UP-A / ALPHA-TR / ALPHA-SP / UPPER-TR /
LOWER-TR / UPPER-LO lines are each witnessed by `FUNCTION ORD` or a yes/no, never a console echo of U+0130). L9 (simple
1:1 mapping — `TextInfo.ToUpper/ToLower`, which ISO 9945's `toupper`/`tolower` also are) makes §15.97.4 r5's "may be
longer or shorter" a permission not exercised (`ß` stays `ß`) and r6's unchanged-letter rule hold by the map being
total — both pinned in `CobolLocaleTests`.

**The ONE §8.2.1 gate.** "If the locale is not found during an operation requiring a locale, the EC-LOCALE-MISSING
exception condition is set to exist … If the locale content is invalid or incomplete during an operation using a
locale, the EC-LOCALE-INVALID exception condition is set to exist" — `LocaleFacts.Require(category, operation, rule)`
(+ `LocaleFacts.IsAvailable`): an UNAVAILABLE locale raises MISSING and returns null (the caller's coded-character-set
arm stands when checking is off — §14.6.13.1.3 #8), an available locale without culture data raises INVALID and
returns itself (the invariant stand-in). Every consumer asks it AT USE — the class tests, the case functions under a
classification or with a phrase, and the four LOCALE functions, whose `CobolLocale.Facts` moved onto it (and now
cites each function's OWN rule: the T4 message printed `§15.54.4 r1` for every function — UPPER-CASE inherited the
wrong clause through the T4 ternary). Consequence for the binder: `EC-LOCALE-INVALID` checking rides EVERY statement
of a module with a classification (a class test is not an intrinsic-bearing statement — `EcBinder`). Golden
`pb64t5_classification_unavailable`: `CHARACTER CLASSIFICATION IS XX` with `LOCALE XX IS "xx-NOWHERE"` COMPILES (L1 —
availability is a run-time property); checking off → OFF-ALPHA=yes / OFF-UP=74 (the coded character set's behavior,
silently); after `>>TURN EC-LOCALE-MISSING CHECKING ON` the class test and the case function each raise, the
statement is interrupted (VERDICT stays "?", S stays "????") and the declarative's RESUME continues.

**§8.8.4.4.3 SR2 — and its siblings registered, not swept under.** A class condition's alphabet-name-1 naming an
`ALPHABET … IS LOCALE` of EITHER class is COBOLNET1669 `locale-alphabet-not-a-charset` (`DataBinder.IsLocaleAlphabet`
— the one predicate over `Alphabets` AND `NationalAlphabets`; the first cut read the alphanumeric dictionary alone and
the FOR NATIONAL sibling slipped — caught by the rule-4 sweep, the negative carries both). The same rule family at
the SYMBOLIC CHARACTERS / CLASS `IN` phrases (§12.3.7.3 SR16g / SR17d) and CODE-SET (§13.18.13.3 SR1/SR2) cannot fire
because those references bind NOTHING today — `CLASS BYORD IS 66 THRU 68 IN REV` builds the class from the NATIVE
ordinals (measured, a silent wrong answer), SYMBOLIC CHARACTERS is accepted-inert, `codeSetClause` is the COBOL-85
one-word form and no binder reads it — registered as **kb/Work PB110** (wrong answer, silent; ranks first now), and
the class condition with a CODED-character-set alphabet-name (GR3 a — `IF X IS STD` for `STD IS STANDARD-1`) is a LOUD
staged value (`NotImplementedCobolFeatureException`, never a wrong answer) — **kb/Work PB109**. The 1669 descriptor
names the rule family and the site that raises it today, without claiming the others.

**Tests.** Goldens `2002/pb64t5_character_classification`, `2002/pb64t5_case_locale_phrase`,
`2002/pb64t5_classification_unavailable`; negatives `pb64t5-classification-name-undeclared` (1664),
`pb64t5-classification-twice` (1652), `pb64t5-case-phrase-name-undeclared` (1664), `pb64t5-class-condition-locale-
alphabet` (1669, both classes); the pb78 refusal negative `pb78-object-computer-character-classification` DELETED
(the clause is live) and `locale_keyword_a49` shrunk to the NUMVAL-C / TEST-NUMVAL-C lines (T6);
`LocaleDispositionTests` (the case-function phrase is LIVE — a declared name compiles, an undeclared one is 1664,
never 1518), `LocaleModuleNonSupportTests` (the suppression lifted a SECOND time: the phrase is 0900 below 2002 and
binds at 2002+; the non-support arm now probes NUMVAL-C), `CobolLocaleTests` (+3: the Turkish/German/invariant
case maps with r5/r6, the activation-time resolution of all four phrase kinds + the class tests, the MISSING gate at
use; the INVALID arm rides the existing zz-QQ site-tailoring fact). Inventory: 24 rows — §12.3.6.3 SR3, §12.3.6.4
GR5/6/7/8, §14.6.6 r2, §8.8.4.4.3 SR2, §15.57.4 r2–r6, §15.97.4 r1–r6, AR-15.97.3-1, FMT-15.57.2 / 15.97.2 / 12.3.6.2
CONFORMS; GR-8.8.4.4.4-3 PARTIAL on item a alone (adjudicated on a–d only, said so) — GAP 3874 → 3857.

**Docs.** CONFORMANCE.md §3 (the case-mapping bullet is the else-arm now) · §4 intro · §4 item 5 (the header names only
NUMVAL-C / TEST-NUMVAL-C / PICTURE format 2 as still refused; the T5 block with determinations a–d) · §5 A.4.9 row
(items 1–7, 9, 10, 11, 13 claimed; 8 and 12 await T6) · row 127; DESIGN-locale-facility §1/§2/§4.5 as-built/§7 (h)/§8
LC_CTYPE/§10/§12 T5 ✅; DIAGNOSTICS.md (1669); kb/Work PB64 (T6 next), PB109, PB110 (new); plan §0.

**Gates.** Wave-local on the first cut (filter `~Locale|~Intrinsic|~CorpusRunner|~VersionMatrix|~Condition|~Class|
~Disposition|~Special`): Conformance 3241/3242 (the one red the stale pb78 refusal negative — deleted), Unit 4467/4469
(the two reds the inventory test-ref rename and the 1669 DIAGNOSTICS.md row — both regenerated), Char 33/33. **Battery
#25 (the T5 tree, `/tmp/battery-pb64t5`): `=== BATTERY: ALL GREEN ===` — FULL Conformance 4887/4887 (11 m 54 s) · Unit 4470/4470 · Char 33/33 · guard-fast ALL GREEN (NIST 353 MATCH / 0 REGRESSION, audit CLEAN) · GnuCOBOL differential 0 PER-CASE FLIPS (1323 cases: 577/469/207/70). The plan §0 battery reference is updated (and #24b's stamp corrected to 12:57).**

**Missteps, honestly.** (1) Three Bash heredocs in a row mangled `\n` inside Python string literals (a `\\n` became a
newline; a `\)` regex lost its escape) — the memory says write patch scripts with the Write tool, and each lapse cost
a re-run; every patch after the third went through the Write tool. (2) The first SR2 arm read only `Alphabets`; the
FOR NATIONAL sibling was found by asking "which arm did you fix". (3) The T4 MISSING message cited §15.54.4 r1 for
every function through a ternary that only knew three names; T5's UPPER-CASE would have inherited the wrong clause
— replaced by an explicit per-function rule map on the shared gate.

