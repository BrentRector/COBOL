> # ADOPTED DESIGN — the locale facility is being IMPLEMENTED (owner decision, 2026-08-18)
> Drafted 2026-08-09 by the wf_fdd1492c probe-sweep fleet's PB64 design agent; reviewed and adopted by the main
> line on 2026-08-18 when the owner answered the four reserved questions (§15). Registered in
> `docs/COBOLNET_DESIGN.md` §0.5 and `docs/DOC_INDEX.md`. kb/Work/PB64.md owns the item.
>
> **The four owner decisions (2026-08-18), verbatim of record — they supersede council decision 3 (2026-07-03):**
> - **Q1 — IMPLEMENT the A.4.9 locale module.** WiseOwl COBOL claims support for Annex A.4.9 items 1–13 as each
>   increment of §12 lands — and EVERY increment HAS: T6 (2026-08-28) completed the claim, CONFORMANCE.md §5's
>   A.4.9 row reads Claimed, and the COBOLNET1518 by-name refusal is DELETED (the code is never reallocated).
> - **Q2 — the two defaults come from ENVIRONMENT VARIABLES** (DETERMINATION L2 as drafted): `COBOL_USER_LOCALE`
>   (else the process `CultureInfo.CurrentCulture`, else `INVARIANT`) and `COBOL_SYSTEM_LOCALE` (else
>   `CultureInfo.InstalledUICulture`, else `INVARIANT`), read ONCE at run-unit activation (L3).
> - **Q3 — YES, a locale-based collating sequence IS offered for INDEXED file keys** (DETERMINATION L8 as
>   drafted; the key locale is captured at OPEN and the cross-locale key-order caveat is documented).
> - **Q4 — `STANDARD-COMPARE` / the `ORDER TABLE` clause are implemented over Unicode CLDR + UCA data** (.NET's
>   ICU `CompareInfo` root collation, plus tables derived from it — never a hand-vendored ISO 14651 file). The
>   conformance statement WiseOwl COBOL makes, VERBATIM and nowhere reworded:
>   **"Implements collation behavior consistent with ISO/IEC 14651 through derived tables and CLDR/UCA data."**
>   (CONFORMANCE.md §2 A.3 item 25 and §7 carry that sentence.)
>
> **⚙ 2026-08-18, the owner's COLLATION guidance (kb/Work PB101):** the collation realization is WiseOwl COBOL's OWN
> DERIVED CLDR/UCA engine (`src/Cobol.Net.Runtime/Collation/`, README there) — a table generated from the pinned
> CLDR release-48-2 root collation + UCD 17.0.0 data, embedded in the runtime, with its own NFD — and NOT .NET's
> ICU `CompareInfo`, which this design first drafted as the realization. Measured reason: the host's bundled ICU
> lags Unicode (the development host's predates 16.0), so an INDEXED file's key order or a SORT's output would
> depend on the host; a versioned table gives one order everywhere. §4.4/§4.9 below are re-based on it (the
> `CobolCollation` carrier of T2 is LANDED, the LOCALE arm of T3 for the CURRENT-locale phrase is LANDED, T7 lands
> in the same batch); the ICU `CompareInfo` remains a cross-check oracle in the tests only.
> **T0 (posture repair) LANDED before adoption:** rows 6–8 and 14 of §1 as kb/Work PB78 (2026-08-18), rows 9–10
> as PB92, rows 4–5 and 11 plus A.4.9 item 1's exception-names as PB100 — every entry point is refused by name
> today. Under the implement decision those refusals are removed increment by increment (§12), and the PB100
> `EcNameResolution` refusal of the EC-LOCALE / EC-ORDER-NOT-SUPPORTED names is REVERTED at T1 (support is
> claimed, so the names are legal again — the raise sites of §4.10 then make them live).
> **The diagnostic band of §7 is re-based:** codes 1642–1660 were claimed by other work between drafting and
> adoption; the locale band starts at COBOLNET1662 (`scripts/session-probe.ps1` is the authority at each landing).

# DESIGN — The LOCALE Facility (ISO/IEC 1989:2023 Annex A.4.9)

Status: **ADOPTED — IN IMPLEMENTATION (T1 first; §12 sequences the seven increments).** Drafted 2026-08-09,
adopted 2026-08-18. Owns `kb/Work/PB64.md` and its 42 traceability-inventory rows.
Repo path: `docs/rearchitecture/DESIGN-locale-facility.md`; registered in `docs/COBOLNET_DESIGN.md` §0.5 and
`docs/DOC_INDEX.md` (CLAUDE.md rule 6).

Scope: the **whole** locale facility the standard defines — Annex A.4.9 items 1–13 plus the rules the module
reaches into: the SPECIAL-NAMES `LOCALE` clause and the `ALPHABET … IS LOCALE` phrase (§12.3.7), the
OBJECT-COMPUTER `CHARACTER CLASSIFICATION` clause (§12.3.6), the `SET` set-locale/save-locale formats
(§14.9.39 formats 11/12), locale-based comparison (§8.8.4.2.7 / §8.8.4.2.9 / §8.8.4.2.11), the locale-edited
`PICTURE` format 2 (§13.18.40), the four locale intrinsic functions (§15.51–§15.54), the `LOCALE` phrase of
`LOWER-CASE`/`UPPER-CASE`/`NUMVAL-C`/`TEST-NUMVAL-C` (§15.57 / §15.97 / §15.68 / §15.94), `STANDARD-COMPARE`
plus the `ORDER TABLE` clause (§15.85, A.4.9 item 11 + A.3 item 25), the six `EC-LOCALE-*` conditions and
`EC-ORDER-NOT-SUPPORTED` (§14.6.13.1.6), and the locale-identification state machine (§14.6.6).

Companion docs — this one defers to them, never duplicates them:
`DESIGN-frontend-grammar.md` (superset parse / construct-id annotation), `DESIGN-version-conformance-pipeline.md`
(the one edition gate), `DESIGN-edition-framework.md` (`constructs.json`, reserved/context-sensitive words),
`DESIGN-runtime-library.md` (`RunUnit` state ownership), `COBOLNET_DATA_MODEL_DESIGN.md` (PICTURE/category),
`COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` (EC raise/declarative machinery), `COBOLNET_INTRINSICS_DESIGN.md`
(the intrinsic catalog + renderer).

> **Every semantic claim below carries a clause validated with `python scripts/spec/cite.py --check`.** About
> 130 `--check` runs were made while drafting, in six batches; every citation that survives into this document
> passed. Two failures are recorded so they are not re-inherited: (a) six PICTURE locale rules the first pass
> wrote as **§13.18.40.4** FAILED and are in fact **§13.18.40.5 (Editing rules)** — the PICTURE clause has a
> separate *Editing rules* subclause and the locale rules live there, not in *General rules*; (b) the
> figurative-constant locale rules are **§8.3.3.6.4**, not §8.3.3.6. Both are the one-level-short shape this
> project keeps paying for.
> ⚠ The printed **index** disagrees with the body on several locale entries (it points EC-LOCALE-SIZE at
> "13.18.36", which is the LOWLIGHT clause, and EC-LOCALE-INVALID-PTR at "14.9.35.4", which is REWRITE). The
> index was not used as a source anywhere in this document; every clause number here was resolved from the
> body's own headings and then `--check`ed.

---

## 0. Hard invariants this design upholds

1. **Typed-native only.** A locale never introduces a byte substrate. Every locale-touching value is a .NET
   `string` (UTF-16, the D-N1 native repertoire) or a scaled integer. The saved-locale "pointer" of §14.9.39
   format 12 is a **managed handle**, never an address into a byte image (§4.3 below).
2. **Spec-first.** Every rule cites ISO/IEC 1989:2023 and every citation is `--check`ed. Where the standard
   leaves a choice to the implementor, this document makes the choice **explicitly, in one place**, marks it
   `⚖ DETERMINATION`, and names the §4.2.7 documentation duty it discharges.
3. **One mechanism per job.** There is exactly ONE current-locale state (on `RunUnit`), ONE collation
   abstraction (§4.4), ONE LC_MONETARY field model shared by PICTURE editing *and* NUMVAL-C parsing (§4.6),
   ONE locale-resolution path shared by every reference form.
4. **Four editions in one compiler.** The whole facility is COBOL-2002+ (§6). Every new construct gets a
   `constructs.json` row and the mandatory edition-gate sweep; nothing parses differently per `--std`.
5. **The dispatch changes, not the callers.** Locale comparison is an *algorithm*, not a weight table. The
   existing three `CobolString.Compare` overloads (`char pad` / `ushort[] weights` / `NationalCollation`)
   cannot host a fourth arm without becoming a four-way overload set that every call site must re-choose
   between. This design **replaces the carrier with one `CobolCollation` abstraction** (§4.4) — the
   restructuring is the work, not a wrapper (CLAUDE.md rule 5).
6. **Deferral is not a design output.** Where a rule is expensive (the ISO/IEC 14651:2020 ordering table,
   §4.9), the design states the complete implementation *and* names the one owner decision it needs; it does
   not shrink the feature.

---

## 1. Current state — MEASURED, not assumed

Every row below is a run of the prebuilt `E:/COBOL/src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.exe`
(2026-08-09 18:24) on a probe with a unique PROGRAM-ID, `--std 2023` unless stated. Console output is verbatim.

| # | Surface (A.4.9 item) | Probe | Measured result |
|---|---|---|---|
| 1 | The four locale functions + STANDARD-COMPARE (items 2–5, 11) | `PB64FN11` | **Correct named reject.** Five `error COBOLNET1518` lines, one per function; STANDARD-COMPARE's also cites `A.3 item 25 (dependent on an ISO/IEC 14651:2020 implementation)`. `EXIT=70` |
| 2 | `UPPER-CASE`'s LOCALE phrase (item 13) | `PB64UC09` | **Correct named reject.** `error COBOLNET1518: the LOCALE phrase of FUNCTION UPPER-CASE is in the optional locale module …` |
| 3 | SPECIAL-NAMES `LOCALE` clause (item 10) | `PB64SN01` | **Correct named reject** (PB25 landed since the PB64 triage): `error COBOLNET1518: the SPECIAL-NAMES LOCALE clause is in the optional locale module (… A.4.9 item 10) …` |
| 4 | `ALPHABET a IS LOCALE`, alphanumeric branch (item 10) | `PB64AL06` | **Wrong diagnostic.** `error COBOLNET0901: 'LOCALE' is a reserved word in COBOL-2023 and cannot be used as a user-defined word (ISO 8.9)` — but `LOCALE` there is a *required word of the ALPHABET clause* (§12.3.7.2), not a user-defined word. |
| 5 | `ALPHABET a FOR NATIONAL IS LOCALE` (item 10) | `PB64NA07` | **Two wrong diagnostics.** `COBOLNET0898: … not a supported code-name … (ISO 12.3.7.3 SR15 …)` — but Table 6 (§12.3.7.4 GR7) lists `LOCALE` as a *collating sequence*, not a code-name — plus the same spurious `COBOLNET0901`. |
| 6 | OBJECT-COMPUTER `CHARACTER CLASSIFICATION` (item 7) | `PB64CC02` | ⛔ **SILENTLY ACCEPTED and IGNORED.** Compiles clean, `CC-LOWER=[abcd]`, `EXIT=0`. The clause asks for LC_CTYPE case mapping (§12.3.6.4 GR7a) and the program silently gets the invariant fold. |
| 7 | Same, `--std 85` | `PB64CC02` at `--std 85` | ⛔ **Also silent** (`CC-LOWER=[abcd]`, `EXIT=0`) — the clause is COBOL-2002+, so the edition gate is missing too. |
| 8 | `CHARACTER CLASSIFICATION` *after* `PROGRAM COLLATING SEQUENCE` | `PB64OR03` | ⛔ **Rejects legal source.** `error COBOL0307: unexpected 'CHARACTER'. A period may be missing …` ×2. §12.3.6.2's outer bracket carries **choice indicators**, and §5.2.6.4 says the alternatives "may be specified in any order". |
| 9 | `SET LOCALE LC_TIME TO USER-DEFAULT` (item 9, format 11) | `PB64ST04` | ⛔ **Three misleading diagnostics**, none naming A.4.9: `COBOLNET1639: 'LOCALE' is not defined …`, `COBOLNET0901` on `LOCALE`, `COBOLNET0901` on `USER-DEFAULT`. |
| 10 | `SET W-P TO LOCALE LC_ALL` (item 9, format 12) | `PB64SV08` | ⛔ **Raw parse error**: `error COBOL0001: unexpected '.'` |
| 11 | `PICTURE +$9.9 LOCALE SIZE IS 10` (item 8) | `PB64PC05` | ⛔ **Raw, misdirecting parse error**: `error COBOL0307: unexpected 'SIZE'. A period may be missing at the end of the previous sentence.` |
| 12 | `EC-LOCALE-MISSING` in RAISE + USE (item 1) | `PB64EC12` | ✅ **Already conforming.** `abnormal run-unit termination: EC-LOCALE-MISSING (fatal): raised by RAISE with checking not enabled (ISO 14.6.13.1.3 #8 …)` |
| 13 | Same with `>>TURN EC-LOCALE-MISSING CHECKING ON` | `PB64EC13` | ✅ `IN-DECL` then `abnormal run-unit termination: EC-LOCALE-MISSING (fatal): raised by RAISE and not resumed` — the declarative runs. |
| 14 | `OBJECT-COMPUTER.` with computer-name-1 **omitted** (found while designing G3; not a locale construct) | `PB64OC14` | ⛔ **Rejects legal source.** `error COBOL0001: unexpected 'PROGRAM'` for `OBJECT-COMPUTER. PROGRAM COLLATING SEQUENCE IS AL2.` — but §12.3.6.2 brackets `[ computer-name-1 ]`, §12.3.6.4 GR3 provides for its absence ("*When the OBJECT-COMPUTER paragraph is specified, but computer-name-1 is not specified, the object computer is defined by the implementor*", `--check` OK) and §12.3.6.3 SR4 presumes clauses may appear without it (`--check` OK). The grammar has `computerName computerAttributes?` — name **mandatory**. |

**What this measurement changes versus the PB64 triage record.** Rows 3 (now a named 1518) and 4/5 (now loud,
not "silently accepted and silently wrong") have moved since triage; the triage text for those rows is stale
and the notes must be re-verdicted on landing. Rows 6–11 are unchanged and are the live harm.
**T1 (2026-08-19) re-verdict:** rows 3, 4/5 (named form) and 9/10 are IMPLEMENTED — the clause declares, the
named alphabet collates, the SET formats act on the run unit's locale state; rows 6–8 and 11 remain T5/T6.
**T4 (2026-08-19) re-verdict:** row 1's four LOCALE functions are IMPLEMENTED (STANDARD-COMPARE was at T7).
**T5 (2026-08-19) re-verdict:** rows 2 (the UPPER-CASE LOCALE phrase) and 6–8 (CHARACTER CLASSIFICATION — its
semantics, its edition gate, its order) are IMPLEMENTED; only row 11 (PICTURE format 2) remains, T6.

**Reading of rows 6–11 against the standard.** A.4.1 — "*An implementation shall accept the syntax and provide
the functionality for an optional element only when support for that language element is claimed by the
implementor*" (`--check` OK) — makes row 6/7 a conformance failure *under the current non-support posture too*:
the syntax is accepted, no functionality is provided, and nothing is diagnosed. Rows 8–11 are worse than
un-named: they are **misdirecting** — row 8 rejects legal source outright, rows 9/11 name a remedy that does
not exist. §4.2.7 requires the implementor to "*identify in user documentation the optional language elements
for which that implementor claims support*" (`--check` OK); `docs/CONFORMANCE.md` §4 item 5 does that, but it
is now **stale**: it still records the SPECIAL-NAMES `LOCALE` clause as a parse error, which row 3 refutes.

**Also present, and load-bearing for the plan:** the compiler already carries the *names* of all six locale
conditions — `ExceptionCatalog.cs` registers `EC-LOCALE-IMP` (Imp) and `EC-LOCALE-INCOMPATIBLE`,
`EC-LOCALE-INVALID`, `EC-LOCALE-INVALID-PTR`, `EC-LOCALE-MISSING`, `EC-LOCALE-SIZE` (all Fatal). Those
fatalities match §14.6.13.1.6's table exactly (five rows `--check`ed). **Zero raise sites exist**, which is
conforming today under §14.6.13.1.1 — "*The implementor is not required to raise any exception conditions for
level-3 exception-names that are associated with optional language elements … that the implementor has not
implemented*" (`--check` OK) — and stops being conforming the moment support is claimed.

---

## 2. The complete feature, enumerated from the standard

A.4.9 is thirteen items (all `--check`ed). This table is the design's work breakdown; §12 sequences it.

| A.4.9 | Element | Clause | Design § | State |
|---|---|---|---|---|
| 1 | `EC-LOCALE` / `EC-ORDER-NOT-SUPPORTED` in RAISING / USE / PERFORM WHEN / RAISE / `>>TURN` | §14.6.13 | §4.10 | ✅ done (row 12/13); the names legal again since T1 (PB100's refusal reverted), MISSING / INVALID-PTR / INCOMPATIBLE raised and observed |
| 2 | `LOCALE-COMPARE` | §15.51 | §4.8 | ✅ **T4 (2026-08-19)** |
| 3 | `LOCALE-DATE` | §15.52 | §4.7 | ✅ **T4** |
| 4 | `LOCALE-TIME` | §15.53 | §4.7 | ✅ **T4** |
| 5 | `LOCALE-TIME-FROM-SECONDS` | §15.54 | §4.7 | ✅ **T4** |
| 6 | `LOWER-CASE` LOCALE keyword | §15.57 | §4.5 | ✅ **T5 (2026-08-19)** |
| 7 | OBJECT-COMPUTER `CHARACTER CLASSIFICATION` | §12.3.6 | §4.5 | ✅ **T5** |
| 8 | `PICTURE` format 2 (locale) | §13.18.40 | §4.6 | ⛔ parse error |
| 9 | `SET` formats 11 (set-locale) / 12 (save-locale) | §14.9.39 | §4.3 | ✅ **T1 (2026-08-19)** |
| 10 | SPECIAL-NAMES `LOCALE` clause + `LOCALE` phrases of `ALPHABET` | §12.3.7 | §4.1 / §4.4 | ✅ **T1 (clause; named alphabet) + PB101 (bare alphabet)** |
| 11 | `STANDARD-COMPARE` | §15.85 | §4.9 | ✅ PB101 T7 (also A.3 item 25) |
| 12 | `TEST-NUMVAL-C` LOCALE keyword + locale-name-1 | §15.94 | §4.6 | ✗ |
| 13 | `UPPER-CASE` LOCALE keyword | §15.97 | §4.5 | ✅ **T5** |

Two rules outside A.4.9 that the module *reaches into* and that no A.4.9 item names — they are part of the
feature and are designed here:

- **Figurative HIGH-VALUE / LOW-VALUE under a locale-based program collating sequence.** §8.3.3.6.4 GR6:
  "*When locale category LC_COLLATE is in effect for the program collating sequence, HIGH-VALUES is the
  character, or multiple-character combination, that has the highest ordinal position in the collating
  sequence specified by the locale in effect*" (`--check` OK; GR7 is the LOW-VALUES twin). Today
  `CollatingTable.HighValue`/`LowValue` are **compile-time** constants; under a locale they become run-time
  values. §4.4.4.
- **`CHAR` / `ORD` over a locale-based program collating sequence.** §15.15.4 r1 and §15.70.4 r1 define both
  over "*the alphanumeric program collating sequence*" (both `--check` OK), and §15.15.3 r2 bounds the CHAR
  argument by "*the number of positions in the alphanumeric program collating sequence*" (`--check` OK).
  §4.4.5.

---

## 3. Target architecture — the five seams

```
SOURCE                                   COMPILER                                 RUNTIME (RunUnit)
──────                                   ────────                                 ─────────────────
SPECIAL-NAMES. LOCALE fr IS "fr_FR" ─┐
ALPHABET a IS LOCALE [fr]            ├─► LocaleModel (Binding)                    LocaleState
OBJECT-COMPUTER. CHARACTER           │     · LocaleSymbol   (name → spec)           · one slot per LC_* category
   CLASSIFICATION IS fr              │     · LocaleRef      (named | current)       · UserDefault / SystemDefault
PIC +$9.9 LOCALE fr SIZE IS 10      ─┘     · CollationSpec  (native|table|locale)   · SavedLocaleTable (handles)
                                                     │                                        │
SET LOCALE LC_COLLATE TO fr ─────────► BoundSetLocale ┼──────── emits ─────────────► LocaleState.Set(cats, src)
SET p TO LOCALE LC_ALL ──────────────► BoundSaveLocale┘                              LocaleState.Save() → handle
                                                     │
IF a > b  (PCS is locale-based) ─────► BoundRelation ─┴─ carries CollationSpec ────► CobolCollation.Compare
FUNCTION LOCALE-DATE(d fr) ──────────► BoundIntrinsicCall(ResultRule=RuntimeLength)► CobolLocale.Date(d, ref)
PIC … LOCALE … SIZE                 ─► BoundEditedStore ──────────────────────────► CobolLocaleEdit.Format
FUNCTION LOWER-CASE(x LOCALE fr) ────► BoundIntrinsicCall(+LocaleRef) ────────────► CobolLocale.Lower(x, ref)
```

The **five seams**, each singular:

- **S1 · `LocaleSymbol` (compile time).** One symbol table entry per SPECIAL-NAMES `LOCALE` clause, holding the
  *external identification* only. Nothing about the locale is resolved at compile time — §8.1.5: "*When a
  locale is specified, the associated ordering is determined at runtime*" (`--check` OK).
- **S2 · `LocaleRef` (bound tree).** The single "which locale" operand used by every consumer: `Named(symbol)`
  or `Current`. Every rule in §14.6.6 that says "*if a locale-name is specified … otherwise the current
  locale*" is this one type; there is no second spelling.
- **S3 · `LocaleState` (run unit).** The current locale, per category, plus the two defaults and the saved-locale
  table. Owned by `RunUnit` alongside `Exceptions`, `External`, `Switches`, `Files`, `Clock`.
- **S4 · `CobolCollation` (runtime).** ONE comparison abstraction with four implementations; replaces the three
  ad-hoc `CobolString.Compare` overload carriers.
- **S5 · `LocaleFacts` (runtime).** The resolved snapshot of one locale's four COBOL-relevant categories
  (LC_COLLATE / LC_CTYPE / LC_MONETARY / LC_TIME) over .NET `CultureInfo`, cached per culture name. The single
  place where the .NET mapping and its documented limits live (§8).

---

## 4. Target design — detail

### 4.1 The SPECIAL-NAMES `LOCALE` clause and the locale-name (A.4.9 item 10)

**Format** (§12.3.7.2, `--check` OK on `LOCALE locale-name-1 IS`):
`LOCALE locale-name-1 IS { external-locale-name-1 | literal-4 }`, repeatable.

- `locale-name-1` is a **user-defined word** — §8.3.2.2 lists `— locale-name` among the user-defined word types
  (`--check` OK) — and §8.3.2.3.7 makes `external-locale-name` a **system-name**: "*An external-locale-name
  identifies a locale that specifies a set of cultural elements. This locale is provided in the operating
  environment*" (`--check` OK).
- `literal-4` "*shall be alphanumeric or national*" (§12.3.7.3 SR10, `--check` OK) and shall be neither a
  symbolic-character figurative constant nor a zero-length literal (SR11).
- Scope: §12.3.7.4 GR1 — "*All clauses specified in the SPECIAL-NAMES paragraph of a source unit that contains
  other source units apply to each directly or indirectly contained source unit*" and locale-names "*may be
  referenced from any directly or indirectly contained source unit*" (`--check` OK). The existing SPECIAL-NAMES
  symbol inheritance carries this; locale-names join it as one more name kind.
- §12.3.7.3 SR3: in an **interface definition** the ALPHABET, CURRENCY, DECIMAL-POINT and LOCALE clauses are
  the only permitted clauses (`--check` OK) — so the interface-definition SPECIAL-NAMES filter must *admit*
  the LOCALE clause. (Today the whole clause is refused, so this rule is unreachable; it becomes live.)

**⚖ DETERMINATION L1 — what an external identification means.** §12.3.7.4 GR5: "*When the LOCALE clause is
specified, locale-name-1 references a locale identified by external-locale-name-1 or the value of literal-4.
The implementor specifies the allowable external-locale-names and the allowable content of literal-4*"
(`--check` OK). §1 (Scope) confirms this document "*does not specify … The mechanism by which locales are
defined and made available on a processor*" (`--check` OK). WiseOwl COBOL's specification:

1. The external identification is a **culture name** accepted by `CultureInfo.GetCultureInfo(name)` — a BCP-47
   tag (`fr-FR`, `de-CH`, `ja-JP`), or the invariant culture spelled `INVARIANT`.
2. A **POSIX spelling is normalized** before lookup: `ll_CC[.codeset][@modifier]` → `ll-CC`; the `.codeset`
   suffix is *ignored* (the runtime repertoire is UTF-16 by the D-N1 invariant, so a codeset suffix cannot
   change it) and a `@modifier` is passed through as a BCP-47 `-u-` extension when it is one of the CLDR
   collation modifiers, else it makes the locale *unavailable*. `fr_FR`, `fr_FR.UTF-8` and `fr-FR` therefore
   all identify the same locale — which matters because `fr_FR` is a legal COBOL word (§8.3.2.1 admits the
   underscore) and so appears in the `external-locale-name-1` branch, while `"fr_FR.UTF-8"` appears in the
   `literal-4` branch.
3. Both branches produce the same normalized key, so §8.5.3.1 rule 2's "*same external identification*"
   equivalence test (`--check` OK) is a comparison of normalized keys, not of source spellings.
4. **Availability is a run-time property, not a compile-time one.** The compiler never calls
   `GetCultureInfo` — a locale absent at compile time may be present at run time and vice versa. An
   unavailable locale raises `EC-LOCALE-MISSING` at the point of use (§4.10).

*Rejected alternative:* validating the culture name at bind and erroring. It would make compilation depend on
the build machine's installed ICU data, and §8.1.5 puts the resolution at run time.

**Grammar.** `localeClause` already exists (`CobolSpecialNames.g4:47`) as
`{localeClauseAhead()}? cobolWord cobolWord IS? (cobolWord | literal)`, parsed *so it can be diagnosed*. It
needs no shape change — only the binder arm changes (from "emit 1518" to "declare a `LocaleSymbol`"), plus the
two SR checks (SR10 literal class, SR11 no figurative/zero-length) and duplicate-name detection.

### 4.2 `LocaleRef` — the one "which locale" operand

```csharp
// Binding/Model/LocaleRef.cs
public readonly record struct LocaleRef(LocaleSymbol? Named)
{
    public static readonly LocaleRef Current = new((LocaleSymbol?)null);
    public bool IsCurrent => Named is null;
}
```

Every consumer takes a `LocaleRef` and the *category* it needs. §14.6.6 states the selection rule six times,
once per consumer, and every one is "named else current" (all `--check` OK):

| Consumer | Category | §14.6.6 rule |
|---|---|---|
| `PICTURE` format 2 editing / de-editing | LC_MONETARY | r6 |
| `LOCALE-COMPARE` | LC_COLLATE | r7 |
| `LOCALE-DATE` / `LOCALE-TIME` | LC_TIME | r8 |
| `ALPHABET … IS LOCALE` as PCS | LC_COLLATE | r4 (+ §12.3.6.4 GR12) |
| SORT/MERGE `COLLATING SEQUENCE` naming a locale alphabet | LC_COLLATE | r5 |
| `CHARACTER CLASSIFICATION` | LC_CTYPE | r2 |

`LOCALE-TIME-FROM-SECONDS` is not named in r8; its own §15.54.4 r1/r2 supply the same rule and the `t_fmt`
field (`--check` OK), so it routes identically. `NUMVAL-C`/`TEST-NUMVAL-C` take LC_MONETARY from their own
§15.68.3 r5a (`--check` OK); `LOWER-CASE`/`UPPER-CASE` take LC_CTYPE from §15.57.4 r2 / §15.97.4 r2
(`--check` OK).

### 4.3 `LocaleState` — the run-unit carrier, and `SET` formats 11/12 (A.4.9 item 9)

**The state model** is fixed by §8.2.1 and §14.6.6:

- "*At the time a run unit is activated, the current runtime locale is set to the user default locale and
  remains in effect for the run unit until another runtime locale is established*" (§8.2.1, `--check` OK);
  §14.6.6 r1 repeats it per category.
- A `SET` switches only the named categories: "*the new locale becomes the current locale for the run unit for
  the switched categories; the current locale remains unchanged for categories that are not switched*"
  (§14.6.6 r3, `--check` OK), and "*Each locale category specified remains in effect for the duration of the
  run unit or until another SET statement specifying that category is processed*" (§14.9.39.4 GR25,
  `--check` OK).
- ⚠ **A callee's switch is NOT unwound.** §14.6.6 r9: "*Upon return of control from another COBOL runtime
  element, the locale in effect for each locale category at the time of exit from the returning runtime
  element becomes the current locale for that category*" (`--check` OK). So there is no save/restore on CALL —
  the state is genuinely run-unit-scoped, and the responsibility to restore is the callee's (§14.6.6 NOTE).
  This is why `LocaleState` hangs off `RunUnit` and **not** off the module/activation stack.
- "*While there is always a current locale for the entire run unit, it has effect only for compilation units
  using language features that reference a locale*" (§8.2.1, `--check` OK) — so a program with no locale
  feature pays nothing; `LocaleState` is lazily materialized.

```csharp
// Runtime/Control/LocaleState.cs   (a RunUnit-owned property, beside Exceptions/External/Switches/Files/Clock) — AS BUILT (T1)
public sealed record LocaleValue(string Collate, string Ctype, string Messages, string Monetary, string Numeric, string Time);
[Flags] public enum LocaleCategorySet { None, Collate = 1, Ctype = 2, Messages = 4, Monetary = 8, Numeric = 16, Time = 32, All = 63 }
public sealed class LocaleState
{
    public LocaleValue UserDefault { get; private set; }   // §8.2.1 — set only by SET LOCALE USER-DEFAULT TO … (GR22)
    public LocaleValue SystemDefault { get; }              // §8.2.1 — read-only from COBOL
    public LocaleValue CurrentLocale { get; }              // per category; a COPY of the user default at activation (r1)
    public void SetFromLocale(LocaleCategorySet cats, string external);   // GR23a / GR24 EC-LOCALE-MISSING
    public void SetFromSaved(LocaleCategorySet cats, ManagedPointer? p);  // GR23a / GR21 EC-LOCALE-INVALID-PTR
    public void SetFromUserDefault(LocaleCategorySet cats);  public void SetFromSystemDefault(LocaleCategorySet cats);
    public void SetUserDefaultFromLocale(string external);   public void SetUserDefaultFromSaved(ManagedPointer? p);
    public SavedLocalePointer Save(bool userDefault);      // GR26 / GR27 — a typed ManagedPointer, owned by THIS state
}
```
A locale is a value PER CATEGORY (`LocaleValue`): a tag-identified locale has the same tag in every slot; a SAVED
locale is the snapshot of a state whose categories may differ, and restoring LC_TIME from it takes its LC_TIME slot.
The saved-locale handle is a typed `ManagedPointer` subclass carrying its owner and snapshot — no table is needed
for validity (type + owner decide; handles are numbered monotonically and never reused), which keeps L4's guarantees
with less state.

**⚖ DETERMINATION L2 — the two defaults.** §8.2.1 requires: "*The implementor shall specify the manner in which
the user default locale is defined and shall provide at least one user default locale…*" and the same sentence
for the system default (both `--check` OK), and "*The capability of setting the system default locale from
COBOL is not provided*" (`--check` OK). WiseOwl COBOL:

| Concept | Source, in precedence order | Rationale |
|---|---|---|
| user default | `COBOL_USER_LOCALE` env var → `CultureInfo.CurrentCulture.Name` at run-unit activation → `INVARIANT` | .NET already derives `CurrentCulture` from the user's regional settings (Windows) or `LC_ALL`/`LC_CTYPE`/`LANG` (POSIX), which is exactly ISO 9945's user default. The env override exists so a **golden test is reproducible on any host** (§10). |
| system default | `COBOL_SYSTEM_LOCALE` env var → `CultureInfo.InstalledUICulture.Name` → `INVARIANT` | The OS-installed culture is the machine-wide value; COBOL cannot set it, matching the §8.2.1 prohibition. |

§8.2.1 also fixes the non-COBOL interaction: a default switched by a non-COBOL module "*is not utilized by
COBOL unless a SET statement is executed to make it the current runtime locale*", and it is implementor-defined
"*whether, and for which locale categories, a switch of current locale by a non-COBOL runtime module is
utilized by COBOL*" (both `--check` OK). **⚖ DETERMINATION L3:** WiseOwl COBOL *never* observes a foreign switch —
`LocaleState` is authoritative and is read from `CultureInfo` **once**, at run-unit activation. A subsequent
`Thread.CurrentThread.CurrentCulture = …` by a hosting .NET application has no effect on COBOL semantics. This
is the only choice compatible with `RunUnit`'s `AsyncLocal` ambient model and with reproducible goldens.

**SET format 11 (set-locale)** — §14.9.39.2 general format (`--check` OK on the format 12 line;
format 11's figure sits immediately above it). Two syntax rules bind (both `--check` OK): SR25 "*If
USER-DEFAULT is specified as the first operand, identifier-10 or locale-name-1 shall be specified in the TO
phrase*"; SR26 "*Locale-name-1 shall be specified in the LOCALE clause of the SPECIAL-NAMES paragraph*"; SR27
"*Identifier-10 shall reference an elementary data item of category data-pointer*".

⚠ **The category brace carries CHOICE INDICATORS.** The transcription's figure note records the pair of `|`
bars inside the inner LC_ brace, and §5.2.6.4 (`--check` OK on "*in any order*") makes that "one or more of
the alternatives … but any single alternative shall be specified only once … The alternatives may be specified
in any order". So `SET LOCALE LC_NUMERIC LC_TIME TO fr` is legal and the grammar must accept a **set** of
categories, in any order, each at most once. Modelling this as a scalar category would reject legal source —
the `model_the_rule_shape_not_one_case` failure. `BoundSetLocale` therefore carries a
`LocaleCategorySet` (a `[Flags]` enum), never a single category.

General rules (all `--check` OK): GR22 — `LOCALE USER-DEFAULT TO x` sets the *user default* to x; GR23a/b/c —
a category list sets the current locale for those categories from locale-name-1, identifier-10 (a saved
locale), USER-DEFAULT, or SYSTEM-DEFAULT; GR24 — an unavailable locale-name-1 sets `EC-LOCALE-MISSING`; GR25 —
run-unit duration. `LC_ALL` names every category, including "*any other categories included in the locale*"
(§8.2.1 table, `--check` OK on the `LC_COLLATE | Collating sequence` row) — for WiseOwl COBOL the set is exactly
the six named categories, since a `CultureInfo` exposes no others.

**SET format 12 (save-locale).** §14.9.39.4 GR26 (`--check` OK): "*If LC_ALL is specified, the current locale
is saved and a reference to the saved locale is placed into the pointer data item referenced by
identifier-11*"; GR27 is the USER-DEFAULT twin. §14.9.39.3 SR28: identifier-11 is category **data-pointer**.
§14.9.39.4 GR21 (`--check` OK): a pointer that does not "*reference saved locale information*" sets
`EC-LOCALE-INVALID-PTR` and the SET is unsuccessful; GR21's second paragraph defines "saved locale" as *a
locale and its category information as established with a set-locale format of the SET statement*.

**⚖ DETERMINATION L4 — the saved-locale pointer is a managed handle.** Invariant 1 forbids an address into a
byte image. A `SET p TO LOCALE LC_ALL` allocates a `SavedLocale` (an immutable six-slot snapshot) in the run
unit's `_saved` table under a monotonically increasing handle, and stores that handle in the data-pointer.
`SET LOCALE cats TO p` looks the handle up; **any** pointer value that is not a live handle — NULL, a
`SET p TO ADDRESS OF x` pointer, a handle from a different run unit — sets `EC-LOCALE-INVALID-PTR` and leaves
the state unchanged. The handle space is per-run-unit and is not reused, so a stale handle is detectable
rather than aliasing.

**Grammar.** Two new alternatives in `setStatement` (`CobolParserCore.g4:1125`), both listed **before**
`setToValueStatement` because `LOCALE` and the LC_ words are the entry tokens:

```antlr
// SET LOCALE { {LC_ALL|LC_COLLATE|…}… | USER-DEFAULT } TO { identifier | locale-name | USER-DEFAULT | SYSTEM-DEFAULT }
setLocaleStatement : SET LOCALE (localeCategory+ | USER_DEFAULT)
                     TO (USER_DEFAULT | SYSTEM_DEFAULT | dataReference) ;
// SET identifier-11 TO LOCALE { LC_ALL | USER-DEFAULT }
setSaveLocaleStatement : SET dataReference TO LOCALE (LC_ALL | USER_DEFAULT) ;
localeCategory : LC_ALL | LC_COLLATE | LC_CTYPE | LC_MESSAGES | LC_MONETARY | LC_NUMERIC | LC_TIME ;
```

⚠ The `TO` operand of format 11 collapses `locale-name-1` and `identifier-10` into one `dataReference` and
splits them **at bind** (a locale-name resolves in the locale symbol table; anything else must be a
data-pointer per SR27). This is the parse-wide/bind-narrow doctrine; writing two grammar alternatives would
need unbounded lookahead. The `localeCategory+` repetition is licensed by the figure's choice indicators
(§5.2.6.4) — the binder enforces "each at most once" and reports a duplicate, because the *grammar* cannot.

⚠ **The seven LC_ words are CONTEXT-SENSITIVE, not reserved** — §8.9's context-sensitive word table lists
`LC_ALL … LC_TIME` each against the construct "SET statement", so `LC_TIME` must remain usable as a
user-defined word elsewhere. They therefore arrive as ordinary `cobolWord`s and are recognized **by text**
inside `setLocaleStatement`, exactly as the grammar already does for `UCS-4`/`UTF-8`/`UTF-16` in the ALPHABET
clause. They must **not** be added to `reserved-words.json` (where they are correctly absent today);
`>>COBOL-WORDS` handling comes free from the existing context-sensitive machinery. `LOCALE`,
`USER-DEFAULT` and `SYSTEM-DEFAULT` *are* reserved from 2002 and are already in `reserved-words.json`.

### 4.4 Collation — the one abstraction (A.4.9 item 10; §8.8.4.2.7/.9/.11)

#### 4.4.1 Why the carrier must change

§8.8.4.2.7 (`--check` OK): "*Two kinds of comparison are defined: standard comparison and locale-based
comparison. Locale-based comparison is used when the alphanumeric program collating sequence in effect is
locale based; otherwise, standard comparison is used*"; §8.8.4.2.9 is the national twin (`--check` OK).
§8.8.4.2.11 then defines the locale kind (all three sentences `--check` OK):

1. "*When local-based comparisons are specified for purposes of comparison, trailing spaces are truncated from
   the operands except that an operand consisting of all spaces is truncated to a single space.*" — **no space
   padding**, unlike every other COBOL comparison.
2. "*Comparison then proceeds by the algorithm associated with the collating sequence defined by category
   LC_COLLATE from the current locale. This may be a culturally-sensitive comparison, and is not necessarily
   performed character-by-character.*"
3. "*If the locale does not define a collating sequence for all characters of the operands, the
   EC-LOCALE-INCOMPATIBLE exception condition is set to exist.*"

Sentence 1 alone breaks the current API: `CobolString.Compare(l, r, ushort[] weights)` pads the shorter
operand and weighs the pad. Sentence 2 breaks it structurally: there are no per-character weights to hand out.

**The change — ✅ LANDED (PB101, 2026-08-18).** `CobolCollation` (`Runtime/Values/Text/CobolCollation.cs`) is the
single carrier; the arms are its implementations. As landed, the NATIVE sequence has NO carrier object — it is the
two-argument `CobolString.Compare(l, r, pad)` and a `null` in every optional collation slot, so an ordinary program's
generated text is byte-identical to before; `AlphanumericCollation` and `NationalCollation` BECAME the two table arms
(their `Weight`/`CharAt`/`PositionCount` logic verbatim, plus the `Compare` that used to live as `CobolString`
overloads, plus binder-computed HIGH-/LOW-VALUE); `LocaleCollation` is the new arm. `CobolString.Compare` collapsed to
{`char pad`, `CobolCollation`} and `ThruMember` to ONE implementation on the base class; `CobolSort`, the indexed-file
registration (`CobolFile`/`FileRegistry`/`IndexedConnector`), `MaxString`/`MinString`/`OrdMax`/`OrdMin`, `Char`/
`CharNational`/`Ord` all take the carrier. The emitters render every carrier through ONE method,
`CodeGen/Roslyn/CollationEmit.cs` (`__COLLATE`/`__COLLATE_NAT` for the PCS, an inline instance for a statement or
file alphabet that is not the PCS) — and the PCS members themselves are DECLARED by ONE helper for every
runtime-module type, `CodeGen/Roslyn/ObjectComputerEmit.cs` (the program class, and a CLASS-ID's instance and
factory classes — kb/Work PB111: a class with a PCS or a CHARACTER CLASSIFICATION was a CS0103 on its emitted
methods until 2026-08-19; a method's classification is an activation LOCAL, a program's a field assigned in
`__Activate`). The drift test `CobolCollationTests.Drift_TheCarrierIsTheOnlyCollatingParameterType`
asserts the overload set stays collapsed and no public runtime surface takes a raw `ushort[]` or a concrete arm.

```csharp
// Runtime/Values/Text/CobolCollation.cs — as landed
public abstract class CobolCollation
{
    public abstract int  Compare(string? left, string? right);   // each arm applies ITS operand rule (pad / trim)
    public bool ThruMember(string? read, string? lo, string? hi); // §14.7.8, ONE implementation, on the base
    public abstract int  Weight(char c);        // ORD; the locale arm materializes (§4.4.5)
    public abstract int  PositionCount { get; }
    public abstract int  CharAt(long position);
    public abstract char HighValue { get; }     // §8.3.3.6.4 GR6
    public abstract char LowValue  { get; }     // §8.3.3.6.4 GR7
}
sealed class AlphanumericCollation : CobolCollation { … }   // the ALPHABET literal-phrase table (256-entry + GR7 1.3 tail)
sealed class NationalCollation     : CobolCollation { … }   // its FOR NATIONAL twin (sparse)
sealed class LocaleCollation       : CobolCollation { … }   // LOCALE / ordering table — the derived engine
// the NATIVE sequence: no object — CobolString.Compare(l, r, pad) and null in every optional slot
```

Compile side, the twin: `Binding/CollatingModel.cs` — `AlphabetDef(CollatingTable? Table, LocaleCollatingSpec? Locale,
string Phrase)` (the alphanumeric alphabet map's value type and the type of the PCS `DataBinder.Collating`, `BoundSort*.Collating`,
`FileModel.PrimeKeyCollation`/`AlternateKeyCollations`), `NationalAlphabetDef` gaining a `Locale` slot, and
`LocaleCollatingSpec(string? LocaleName)` (null = the current locale at each use). Both defs expose `HighValue`/`LowValue`
so the figurative constants and `ConcatFolder` still fold at compile time (§4.4.4).

⚠ The `: c` weight tail proof recorded in `CobolString.Weight`'s doc comment (order-equivalence of the raw
table tail with the exact GR7 1.3 arithmetic) must be **carried over verbatim** into `TableCollation`, not
silently dropped — it is the standing justification for the fast path.

#### 4.4.2 Where a locale collating sequence comes from

`ALPHABET alphabet-name-1 [FOR ALPHANUMERIC] IS LOCALE [locale-name-2]` (§12.3.7.2 figure) and the
`FOR NATIONAL` twin. Rules:

- Table 6 (§12.3.7.4 GR7) row `LOCALE` has a **`Y` in the collating-sequence column and a blank in the coded
  character set column** (`--check` OK on the table row `| LOCALE |  | Y |`). A locale alphabet is therefore
  **not** a coded character set: naming it in `CODE-SET`, in `SYMBOLIC CHARACTERS … IN`, or in `CLASS … IN` is
  a syntax-rule violation. §12.3.7.3 SR16g and SR17d say so explicitly — "*Alphabet-name-3 shall not reference
  an alphabet specified with the LOCALE phrase*" and the SR17d twin for alphabet-name-4 (both `--check` OK).
  This is exactly the `HasCollatingSequence` flag the existing `NationalAlphabetDef` already carries for
  UTF-8/UTF-16 — **inverted** (LOCALE has a collating sequence but no character set), so the record needs a
  second boolean, `HasCodedCharacterSet`, and the two SR checks read it. One flag pair, two SRs, no new
  mechanism.
- §12.3.7.3 SR24 (`--check` OK): "*Locale-name-2 shall be a locale-name defined by the LOCALE clause*".
  ⛔ **locale-name-2 is a FORWARD reference in every conforming program**: the §12.3.7.2 format prints
  alphabet-name-clause BEFORE the LOCALE clause, and §5.2.1 (`--check` OK) requires "*the sequence given in the
  general format*". So the SPECIAL-NAMES binder resolves it in DEPENDENCY order, not source order: the walk binds
  every clause that references no paragraph name (LOCALE among them), then every ALPHABET clause, then CLASS /
  SYMBOLIC CHARACTERS and the OBJECT-COMPUTER clauses (`DataBinder.SwitchBindSpecialNames`; kb/Work PB1558 —
  it used to bind ALPHABET in source order and refuse the format order with COBOLNET1664).
- §12.3.7.4 GR7e (`--check` OK): "*When the LOCALE phrase is specified, the collating sequence identified is
  defined by the locale referenced by locale-name-2 when specified, otherwise by the locale that is current at
  the time the collating sequence is used at runtime*" — i.e. the alphabet holds a `LocaleRef`, and a
  `LocaleRef.Current` alphabet is re-resolved **at each use**, not once.
- §12.3.6.4 GR11 (`--check` OK): "*When alphabet-name-1 or alphabet-name-2, or both, is associated with a
  locale, locale category LC_COLLATE is used to carry out these comparisons*"; GR12 repeats the
  "specific-locale-else-current-at-use" rule (`--check` OK).

`CollatingModel.cs` gains a third case beside `CollatingTable` and `NationalCollatingTable`:

```csharp
public sealed record LocaleCollatingSpec(LocaleRef Locale, bool National);
public sealed record AlphabetDef(CollatingTable? Table, LocaleCollatingSpec? Locale,
                                 bool HasCollatingSequence, bool HasCodedCharacterSet, string Phrase);
```

**⚖ DETERMINATION L5 — one sequence serves both classes.** §8.8.4.2.7 (`--check` OK): "*If the locale does not
specify a distinct alphanumeric collating sequence, class alphanumeric and alphabetic operands are mapped to
their corresponding representation in the national character set for purposes of comparison; the correspondence
between alphanumeric characters and national characters is defined by the implementor*", and §8.2.1 requires
such a locale to define a national sequence covering every display character (`--check` OK). On the D-N1
substrate the alphanumeric and national repertoires are the *same* 65,536 UTF-16 code units, so the
implementor correspondence is the **identity**, and a `CultureInfo.CompareInfo` is a single sequence serving
both classes. This is recorded as the implementor-defined correspondence §8.8.4.2.7 asks for; nothing else in
the design branches on it.

#### 4.4.3 The comparison itself

```csharp
// Runtime/Values/Text/LocaleCollation.cs — as landed (PB101)
sealed class LocaleCollation(string? localeTag) : CobolCollation     // null = the current locale at each use (GR7e)
{
    public static LocaleCollation Current { get; }                    // the `IS LOCALE` phrase without a locale-name
    public Collator Resolve() =>
        CollationEngine.ForLocale(localeTag ?? RunUnit.Current.Locale.Current(LocaleCategory.Collate));
    public override int Compare(string? l, string? r2)
    {
        string a = TrimForLocale(l), b = TrimForLocale(r2);            // §8.8.4.2.11 sentence 1 — no padding
        if (!Collator.IsWellFormed(a) || !Collator.IsWellFormed(b))
            ExceptionState.Set("EC-LOCALE-INCOMPATIBLE", fatal: true);   // L6, re-derived below
        return Resolve().Compare(a, b);                                   // sentence 2 — the derived CLDR/UCA engine
    }
}
```

`RunUnit.Locale` is a `LocaleState` (`Runtime/Control/LocaleState.cs`) — the T1 seam in its smallest form: the two
L2 defaults read once at activation and the current locale per category (`Set` is where SET LOCALE lands).

- "*Two zero-length operands are equal*" (§8.8.4.2.11 sentence 2's tail) falls out: both trim to `""`.
- ⚠ Note the **all-spaces ⇒ one space** clause is not the same as `TrimEnd`: `"    "` becomes `" "`, not `""`.
  A test pins the three cases (`""`, `"   "`, `"a  "`) because this is the sentence most likely to be
  "simplified" into a plain trim.
- **⚖ DETERMINATION L6 — `EC-LOCALE-INCOMPATIBLE` (§8.8.4.2.11 sentence 3) — RE-DERIVED over the derived table
  (PB101).** The table assigns an explicit or UTS #10 Table 16 implicit weight to EVERY well-formed code point —
  assigned, unassigned and noncharacter alike (U+FFFE/U+FFFF carry CLDR's explicit minimum/maximum) — so the one
  operand "the locale does not define a collating sequence for" is ill-formed UTF-16: an **unpaired surrogate**.
  That sets the fatal EC-LOCALE-INCOMPATIBLE; the comparison still returns a deterministic order (the lone unit is
  walked as its own code point). Documented under §4.2.7 and pinned by
  `CobolCollationTests.LocaleArm_UnpairedSurrogate_SetsEcLocaleIncompatible`, which fires it.
- **⚖ DETERMINATION L11 — the algorithm "associated with LC_COLLATE" (§8.8.4.2.11 sentence 2).** The locale's
  CLDR collation at its CLDR defaults: the locale's tailoring over the root table at TERTIARY strength with
  NON-IGNORABLE variables (case and accents distinguish; punctuation weighs at level 1) — what ICU/CLDR give a
  locale's default `strcoll`-style comparison. The four-level ISO/IEC 14651 default ordering (variables shifted to
  level 4) is what STANDARD-COMPARE provides (§4.9). Documented under §4.2.7; pinned by the `pb101_alphabet_locale_pcs`
  golden and `CobolCollationTests`.

#### 4.4.4 HIGH-VALUE / LOW-VALUE under a locale-based sequence

§8.3.3.6.4 GR6/GR7 (both `--check` OK) make these **run-time** values under a locale-based PCS. As landed,
`LocaleCollation.HighValue`/`LowValue` are read from the materialized order vector of §4.4.5 (the highest-coded
member of the last rank, the lowest-coded of the first) — and under EVERY CLDR/UCA table they are U+FFFF (CLDR gives
it the maximum primary) and U+0000 (completely ignorable), which is why the compile-time twin `AlphabetDef.HighValue`
/`LowValue` can still fold the figurative constants for a locale PCS (`FigurativeConstants`, `ConcatFolder`) without a
run-time call. A tailoring cannot outrank U+FFFF (a tailored primary is bounded by the engine's weight domain).

⚠ Two honest limits, both documented:
- The rules say "*the character, or multiple-character combination*". A multiple-character combination cannot
  be the value of a one-character figurative constant in this compiler's typed model; WiseOwl COBOL returns a
  single character. Recorded as an implementor determination with the §4.2.7 duty.
- Ties (characters at equal primary/secondary/tertiary weight) are broken by **code unit**, ascending, so the
  result is stable — §15.15.4 r2 imposes exactly that stability duty on the sibling CHAR function: "*for a
  given implementation, collating sequence, and ordinal position, every invocation of the CHAR function shall
  return the same character*" (`--check` OK).

#### 4.4.5 `CHAR` / `ORD` over a locale-based sequence

§15.15.4 r1 and §15.70.4 r1 define both over the alphanumeric **program collating sequence** (both
`--check` OK), and §15.15.3 r2 bounds CHAR's argument by "*the number of positions*" in it (`--check` OK).
A locale collation is an algorithm, not a position table — so the positions must be *materialized*.

**⚖ DETERMINATION L7 — as landed.** `LocaleCollation` lazily builds, once per resolved `Collator` (process-wide
cache), a 65,536-entry order vector: every native code unit's `CollationKey` under the collator, sorted (ties by
code unit, per §4.4.4). `ORD` returns the rank + 1; `CHAR` inverts it; `PositionCount` is the number of **distinct**
ranks — characters the locale collates equally share a position, which is precisely the case §15.15.4 r2
anticipates, and the "first character defined" tie rule resolves to the lowest-coded member of the equal-weight
group (the only "definition order" a locale has). ~65 k key builds + one sort, executed at most once per collator,
and only for a program that references ORD/CHAR (or HIGH-/LOW-VALUE at run time) under a locale PCS.

#### 4.4.6 The other collation consumers

- **SORT / MERGE `COLLATING SEQUENCE`** naming a locale alphabet: §14.6.6 r5 (`--check` OK) — LC_COLLATE of the
  associated locale, and "*A locale switch during execution of a SORT or MERGE statement has no effect on the
  processing of that SORT or MERGE statement*". As landed, `BoundSort`/`BoundMerge`/`BoundTableSort.Collating` carry
  a `SortCollation` PAIR — §14.9.40.4 GR5 / §14.9.24.4 GR5 determine an ALPHANUMERIC and a NATIONAL sequence
  separately, and each KEY takes the one its class names (kb/Work PB678) — and the emitter passes the matching
  carrier (`__COLLATE` / `__COLLATE_NAT`, or an inline `LocaleCollation`/table for a statement alphabet). A key of
  class numeric or boolean takes NEITHER carrier: GR5 names no sequence for those classes. Both sequences are
  snapshotted at statement start by `CobolSort.Init(name, collation, national)` (T1's `CobolCollation.Snapshot`),
  so a SET LOCALE issued from an INPUT/OUTPUT PROCEDURE cannot move either sequence mid-sort; the store records
  that a snapshot was TAKEN rather than inferring it from a non-null sequence, because the native order is a
  legitimate snapshot value.
- **Indexed-file keys.** §A.3 item 41 (`--check` OK) makes a locale-defined collating sequence for indexed
  primary/alternate keys **processor-dependent**: "*The capability of specifying a collating sequence for
  primary and alternate keys of an indexed file where the alphabet specified in the COLLATING SEQUENCE clause
  is defined in the SPECIAL-NAMES paragraph with the LOCALE phrase or with literals is dependent on the
  capabilities of the processor*". ⚖ **DETERMINATION L8:** WiseOwl COBOL *does* provide it — the index is an
  in-process ordered structure keyed by `CobolCollation`, so a locale key ordering costs nothing extra — and
  documents the one consequence: **an indexed file written under one locale and read under another is not
  guaranteed to be in key order**, which is why the resolved locale key is captured at OPEN and a mid-file
  `SET LOCALE LC_COLLATE` does not re-order an open file.

### 4.5 Character classification (A.4.9 items 6, 7, 13)

**The clause.** §12.3.6.2's `CHARACTER CLASSIFICATION` clause takes `locale-phrase-1` (alphanumeric) and
`locale-phrase-2` (national), each one of `locale-name-n | LOCALE | SYSTEM-DEFAULT | USER-DEFAULT`, optionally
under `FOR ALPHANUMERIC` / `FOR NATIONAL`. §12.3.6.3 SR3 (`--check` OK): "*Locale-name-1 and locale-name-2
shall be locale names defined in the SPECIAL-NAMES paragraph*".

§12.3.6.4 GR5 gives the eight initial-classification cases (a/b `--check` OK as representatives): a named
locale, `LOCALE` (= the current locale), `SYSTEM-DEFAULT`, `USER-DEFAULT`, or absent (= the coded character
set). GR6 (`--check` OK) supplies the inherited/absent default; GR7 names the two consumers — "*the uppercase
and lowercase mappings of characters for the UPPER-CASE and LOWER-CASE intrinsic functions*" and "*the
classification of characters for class tests ALPHABETIC, ALPHABETIC-LOWER, ALPHABETIC-UPPER, and for the class
test specifying an alphabet-name that is associated with a locale…*" (both `--check` OK); GR8 (`--check` OK)
routes it to category **LC_CTYPE**. §14.6.6 r2 (`--check` OK) restates it at runtime-element activation.

**⛔ Two defects to fix, both measured (§1 rows 6–8).**

1. `computerAttributes : ~(DOT | PROGRAM)+` (`CobolParserCore.g4:397`) is a raw token **sink** that swallows
   the clause. It is replaced by real productions:
   ```antlr
   objectComputerParagraph
       : OBJECT_COMPUTER DOT (computerName? objectComputerClause* DOT)? ;
   objectComputerClause
       : memorySizeClause | segmentLimitClause
       | characterClassificationClause | programCollatingSequenceClause ;
   characterClassificationClause
       : CHARACTER CLASSIFICATION (localePhrase localePhrase?
                                  | (FOR (ALPHANUMERIC|NATIONAL) IS localePhrase)+ ) ;
   localePhrase : IS? (USER_DEFAULT | SYSTEM_DEFAULT | LOCALE | cobolWord) ;
   sourceComputerParagraph
       : SOURCE_COMPUTER DOT (computerName? debuggingModeClause? DOT)? ;
   ```
   The `objectComputerClause*` loop is what fixes row 8: §12.3.6.2's outer bracket **encloses choice
   indicators**, and §5.2.6.4 (`--check` OK) makes the alternatives specifiable "*in any order*", each at most
   once. The binder enforces at-most-once and diagnoses a repeat; the grammar cannot, and must not try.
   `computerName?` fixes row 14 (§12.3.6.4 GR3, `--check` OK).
   ⚠ `CLASSIFICATION` is a §8.9 **context-sensitive** word ("OBJECT-COMPUTER paragraph"), so it stays out of
   `reserved-words.json` and is matched by text, like the LC_ words.
2. ⛔ **Deleting the sink silently deletes three live edition gates unless they are moved first.**
   `VersionConformancePass.VisitComputerAttributes` (`:506`) is a **token-TEXT scan over the sink** and is the
   only enforcement of `MemorySizeRemoved2002`, `SegmentLimitRemoved2002` and `DebuggingModeRemoved2014` — the
   last of which also sets `_debuggingModeDeclared`, which drives the `--std 85` USE FOR DEBUGGING posture
   (VCR row 7.17). Note the sink is shared by **both** computer paragraphs (`sourceComputerParagraph` uses it
   too), and `WITH DEBUGGING MODE` is a SOURCE-COMPUTER clause — hence the `sourceComputerParagraph` rewrite
   above. G3 therefore lands as: add the typed productions **and** re-home all three gates onto typed visits in
   the same commit, with the existing edition-matrix cases for those three constructs as the proof. This is the
   two-arm-dispatch hazard in its exact classic form — the arm being fixed is CHARACTER CLASSIFICATION, and the
   arm that must not be dropped is the obsolete-clause trio.
3. The `character-classification-2002` construct row then gates the new clause (§6).

**The three consumers.**

- `UPPER-CASE` / `LOWER-CASE` **with** a LOCALE phrase (§15.57.4 r2 / §15.97.4 r2, both `--check` OK) → LC_CTYPE
  of the named locale.
- The same functions **without** one (§15.57.4 r3, `--check` OK) → LC_CTYPE of the classification locale
  established by `CHARACTER CLASSIFICATION`, if any; else §15.57.4 r4 (`--check` OK) — "*When a locale is not
  in effect, the implementor defines the correspondence*", which is today's `ToLowerInvariant` and is already
  documented in `CONFORMANCE.md`. Closing item 7 turns that unconditional call into the **else-arm it is
  supposed to be**, which is exactly what inventory row `RV-15.57.4-4` (verdict PARTIAL) records as missing.
- The **class tests** `ALPHABETIC`, `ALPHABETIC-LOWER`, `ALPHABETIC-UPPER` (§12.3.6.4 GR7b) become
  classification-aware: `char.IsLetter`/`IsUpper`/`IsLower` are Unicode-wide and locale-independent, so under a
  classification locale they route to the culture's `TextInfo` round-trip test (`c` is upper iff
  `ToLower(c) != c`), matching POSIX `isupper` semantics for the tailored letters (Turkish dotted/dotless I,
  Azeri).

**⚖ DETERMINATION L9 — case mapping is SIMPLE (per-code-unit), and that is conforming.** §15.57.4 r5
(`--check` OK) admits a returned string "*longer or shorter than argument-1*" when the correspondence is not
one-to-one. .NET's `TextInfo.ToUpper`/`ToLower` are **simple** maps: `ß` is unchanged, never expanded to `SS`.
That is not a shortfall: §8.2.1 requires locale categories to be "*as specified in ISO/IEC 9945:2009, Clause
7*" with implementations free to differ "*provided that logically-equivalent functionality is supported*"
(`--check` OK), and ISO 9945's LC_CTYPE `toupper`/`tolower` are themselves strictly per-character maps. So
under WiseOwl COBOL's LC_CTYPE the correspondence is always one-to-one and §15.57.4 r5's second sentence is
vacuously satisfied — documented under §4.2.7, and pinned by a Turkish-I golden that proves the *tailoring*
is live (`LOWER-CASE("I" LOCALE tr)` → `ı`, U+0131, witnessed by `FUNCTION ORD`, never by the console echo).

**As built — ✅ T5 (2026-08-19, kb/Work PB64).** The clause binds to a `ClassificationSpec(LocalePhrase?
Alphanumeric, LocalePhrase? National)` on the `DataBinder` (`ResolveClassification` after the SPECIAL-NAMES walk —
the locale-names resolve through the ONE undeclared-locale-name diagnostic, COBOLNET1664; inherited by contained
units through the configuration inheritance, §12.3.6.4 GR1); `LocalePhrase(LocalePhraseKind Kind, LocaleSymbol?
Symbol)` carries `Named | Current | SystemDefault | UserDefault` (`Runtime/Globalization/LocalePhraseKind`). **The
classification is RESOLVED AT EACH ACTIVATION of the module** (§12.3.6.4 GR8 "effective with the initial state of
the runtime modules"; §14.6.6 r2 "on activation of a runtime element"): the `__Activate` prologue
(`DispatchEmitter`) assigns the per-module field `__CLASSIFY = CharacterClassification.Resolve(kind, tag, kind, tag)`
(`Runtime/Globalization/CharacterClassification.cs` — a pair of `LocaleFacts?`, `None` when the unit has no clause),
so `LOCALE` is the locale current at THAT activation and a later `SET LOCALE LC_CTYPE` does not move it (golden
`pb64t5_case_locale_phrase`: INNER vs OUTER-2). The consumers: `IntrinsicRenderer` renders `UPPER-CASE` /
`LOWER-CASE` as `CobolLocale.UpperCase(s, "tag")` with a LOCALE phrase (r2 — `BoundIntrinsicCall.Locale`, bound by
`BindCaseFunctionWithLocale`, the 2002 construct row `case-function-locale-phrase-2002`), as `CobolLocale.UpperCase(s,
__CLASSIFY.For(national))` in a module with a classification (r3), and as the plain invariant
`CobolIntrinsics.UpperCase` otherwise (r4 — unchanged generated text for every program without the clause);
`ConditionRenderer` appends `, __CLASSIFY.For(national)` to the three class tests, whose `CobolClass.IsAlphabetic(s,
LocaleFacts?)` overloads classify a Unicode LETTER per LC_CTYPE (POSIX `alpha` — **no space**: §8.8.4.4.4 GR3 b1 names
only LC_CTYPE's alphabetic characters where b2 names space explicitly; documented in CONFORMANCE.md §4 item 5) and the
case round-trip for `-UPPER` / `-LOWER`. **The ONE §8.2.1 gate is `LocaleFacts.Require(category, operation, rule)`**
— every consumer asks it AT USE: an UNAVAILABLE locale (a declared name no environment provides — L1 makes that a
run-time fact) raises EC-LOCALE-MISSING and the coded character set's behavior stands when checking is off; an
available locale without culture data raises EC-LOCALE-INVALID; the LOCALE functions (`CobolLocale.Facts`) ride the
same gate, each citing its own rule. `EC-LOCALE-INVALID` checking therefore rides EVERY statement of a module with a
classification (a class test is not an intrinsic-bearing statement — `EcBinder`). §8.8.4.4.3 SR2: a class condition
naming a LOCALE alphabet of either class is COBOLNET1669 (`DataBinder.IsLocaleAlphabet`, the one predicate — §12.3.7.3
SR16g / SR17d and CODE-SET's §13.18.13.3 SR1/SR2 await those phrases binding, kb/Work PB110; the class condition
with a CODED-character-set alphabet-name, GR3 a, is kb/Work PB109). Goldens `2002/pb64t5_character_classification`,
`2002/pb64t5_case_locale_phrase`, `2002/pb64t5_classification_unavailable`; negatives `pb64t5-*` (four); construct
rows `character-classification-2002` / `case-function-locale-phrase-2002`.

### 4.6 Monetary editing — `PICTURE` format 2 and the NUMVAL-C family (A.4.9 items 8, 12)

**Format** (§13.18.40.2, `--check` OK):
`{PICTURE|PIC} IS character-string-1 LOCALE [ IS locale-name-1 ] SIZE IS integer-1`.

Syntax rules, all `--check` OK: §13.18.40.3 SR32 — not in / under a `CONSTANT RECORD` item; SR33 —
character-string-1 shall contain at least one `Z` or `9`; SR34 — each of `+`, `.`, the currency symbol at most
once; SR35 — 1..31 digit positions; SR36 — the currency symbol and `+` only left of the decimal point
position; SR37 — "*Locale-name-1 shall be specified in the LOCALE clause in the SPECIAL-NAMES paragraph*".
Plus the two SIGN-clause prohibitions, one per data-description context: §13.16.3 SR19 and §13.17.3 SR9, both
"*If the LOCALE phrase of the PICTURE clause is specified, the SIGN clause shall not be specified*"
(both `--check` OK).

Editing rules (**§13.18.40.5**, all `--check` OK — this is the subclause the first drafting pass got wrong):
GR9 LC_MONETARY supplies the currency symbol's position/length/characters; GR10 `BLANK WHEN ZERO` takes
precedence over locale editing; GR11 named-else-current locale; GR12 separators and group sizes from
LC_MONETARY; GR13 `+` ⇒ the sign representation comes from the locale; GR14 the **hypothetical data item**
algorithm — align on the decimal point, apply grouping and separators, then move into the SIZE-declared item:
larger ⇒ right-justified with space fill; smaller ⇒ **truncate on the left**, and "*If any truncated character
is neither a zero nor a space caused by a suppressed zero, the EC-LOCALE-SIZE exception condition is set to
exist*"; GR15 `Z` suppression with space replacement.

**Data model.** A format-2 item's **size in character positions is `integer-1`**, not the picture's width —
"*the picture character string is not an indication of the field size needed to hold the edited item*"
(Annex D's programmer guidance). So `DataItem` gains `LocaleEdit: (LocaleRef Locale, int Size)?`, the item's
length is `Size`, and its category is numeric-edited. Type equivalence follows §8.5.3.1 rule 2 (`--check` OK)
and §14.8.2.3.2 rule 2 (`--check` OK): two such items match only if they "*specify the same SIZE phrase*" and
either both omit locale-name or both name the **same external identification** — i.e. the normalized key of
DETERMINATION L1, which is why L1's normalization has to be a single function reachable from the
type-equivalence check.

**Runtime.** `CobolLocaleEdit.Format(Int128 unscaled, int scale, string picture, LocaleFacts f, int size)`
implements GR14 exactly, and `DeEdit` inverts it. It sits **beside** `CobolEdit`, not inside it: `CobolEdit`'s
mask is a 1:1 character map, and locale editing has no fixed mask (the currency string can be multi-character
and can precede or follow). One shared helper — the LC_MONETARY field snapshot — serves both this and the
NUMVAL-C family, so the mapping table (§8) is written once.

**NUMVAL-C / TEST-NUMVAL-C with LOCALE** (§15.68.3 r5, `--check` OK on r5a and on r5b.2 "*Usage national
representation of locale fields is used for purposes of matching argument-1*"; §15.94.3 r1 imports the whole
rule set, `--check` OK) are the **inverse** of the same table: recognize a currency string matching
`currency_symbol` or the first three characters of `int_curr_symbol` per `p_cs_precedes`/`n_cs_precedes`;
recognize signs per `positive_sign`/`negative_sign` and `p_sign_posn`/`n_sign_posn`; separators per
`mon_decimal_point`/`mon_thousands_sep`/`mon_grouping` — the group SIZES are enforced (r5b.6, `--check` OK:
"*in accordance with locale fields mon_thousands_sep and mon_grouping*"; kb/Work PB835): no separator, or one at
EVERY `mon_grouping` position, checked as a PREFIX at each character so TEST-NUMVAL-C's position is the first
character no grouped completion admits. The size list has ONE reader, `MonetaryFacts.GroupSize` (the POSIX
last-size repetition and trailing-0 stop), with `GroupingAdmits` (the scan: prefix or whole) and `GroupingIsExact`
(the format-2 de-edit: every position, exactly what the edit writes) over it; the edit's own separator placement
walks the same `GroupSize`, and `MonetaryFactsTests` pins all three against .NET's independent `N0` group-size
rendering for every host culture. §15.68.4 r3 (`--check` OK) fixes the sign of the
result under the LOCALE keyword. `ANYCASE` under LOCALE folds through the **LC_CTYPE** correspondence of the
same locale (§15.68.3 r5b.3's second paragraph), which is why §4.5's case mapping must be reachable from here
— one mechanism, two callers.

### 4.7 Locale date and time (A.4.9 items 3, 4, 5)

Formats (§15.52.2 / §15.53.2 / §15.54.2, all `--check` OK): `FUNCTION LOCALE-DATE ( argument-1
[ locale-name-1 ] )`, and the two time twins.

| Rule | LOCALE-DATE | LOCALE-TIME | LOCALE-TIME-FROM-SECONDS |
|---|---|---|---|
| argument-1 class/length | alphanumeric or national, **8** positions (§15.52.3 r1) | alphanumeric or national, **6** positions (§15.53.3 r1) | numeric, **standard numeric time form** (§15.54.3 r1) |
| argument-1 value | CURRENT-DATE positions 1–8, valid per that function (§15.52.3 r2) | CURRENT-DATE positions 9–14 (§15.53.3 r2) with hours **00–24** and seconds **00–99** (§15.53.3 r3a/b) | "*a numeric value representing seconds past midnight*" (§15.5.5) |
| locale-name-1 | §15.52.3 r3 | §15.53.3 r4 | §15.54.3 r2 |
| result | `d_fmt` (§15.52.4 r2) | `t_fmt` (§15.53.4 r2) | `t_fmt` (§15.54.4 r2) |
| length | "*depends on the format indicated in the locale*" (§15.52.4 r3) | §15.53.4 r3 | §15.54.4 r3 |

(Every cell's clause `--check`ed.)

Three consequences the design must carry:

1. **Argument-1 of the two string functions is a STRING, not an integer.** §15.6's Table 21 row confirms
   (`| LOCALE-COMPARE | Alph1 or Anum1 or Nat1, … | Anum |`, `--check` OK). The catalog's `ArgKinds` for
   LOCALE-DATE/-TIME is already `"ss"` in the current tree (it was `"is"` at PB64 triage — an inherited
   Annex-D reading, since corrected). ⚠ **The existing conformance tests write integer literals**
   (`FUNCTION LOCALE-TIME(120000)`), which §15.53.3 r1 forbids; those cases must be **rewritten**, not
   extended, when the module is claimed.
2. **The 00–24 / 00–99 ranges are this clause's own** — wider than CURRENT-DATE's — so a shared CURRENT-DATE
   validator cannot be reused; LOCALE-TIME needs its own range pair. The existing
   `CobolDate.SecondsOutOfStandardForm` (an exact `Int128` comparison against 86 400, citing §7.3.17) is the
   right screen for LOCALE-TIME-FROM-SECONDS and is reused as-is.
3. **The result length is run-time-determined.** The catalog already models result-length rules declaratively
   (`IntrinsicResultRule.Fixed | FollowsArgument1 | FollowsUniformArguments | IntegerFollowsAllArguments |
   IntegerFollowsArgument1`), so this is one new member — `RuntimeDetermined` — not a schema rewrite. The
   receiving context governs: a MOVE truncates/pads per §14.9.25, and a reference-modification of the result
   is bounded at run time. This is the constraint inventory rows `RV-15.52.4-3`, `RV-15.53.4-3` and
   `RV-15.54.4-3` each recorded independently; it is satisfied once, here.
4. **A rejected argument-1 substitutes ONE SPACE, not a zero-length value** (kb/Work PB470). The three
   functions' argument rules can fail at run time (an invalid date, a malformed or out-of-range time, a
   value outside standard numeric time form), and §15.3 rule 14 hands the result of the reference to
   the implementor when EC-ARGUMENT-FUNCTION checking is off. **The determination is not made here** —
   it is `docs/CONFORMANCE.md` row `DOC-A.1-90`, which settles the class by asking what determines the
   returned LENGTH. Consequence 3 above is exactly why these three are NOT in that row's zero-length
   class: the length depends on the LOCALE (§15.52.4 / §15.53.4 / §15.54.4 r3), which the
   rejection of argument-1 leaves intact, so the row's general "spaces" clause governs — one space,
   on §15.30.3 r1's answer for an alphanumeric function whose length is content-derived and whose
   content is absent, since L10's culture patterns fix no width once the value is gone. The four guards
   read `CobolLocale.LocaleSubstitutePositions` and raise-and-substitute in one expression
   (`ExceptionState.ArgumentErrorSpaces`); `2014/pb470_locale_argument_substitute` and its 2002 twin are
   the witnesses, and they read the reference DIRECTLY because §14.6.8.5 makes a MOVE unable to tell
   the two answers apart — which is how the wrong one survived from T4's landing to 2026-09-04.

**⚖ DETERMINATION L10 — `d_fmt` and `t_fmt`.** ISO 9945's `d_fmt` is the culture's short date and `t_fmt` its
`%H:%M:%S` time. WiseOwl COBOL maps `d_fmt` → `CultureInfo.DateTimeFormat.ShortDatePattern` and `t_fmt` →
`LongTimePattern` (the pattern that carries seconds; `ShortTimePattern` omits them, and §15.53.4 r2 requires
"*hours, minutes, and seconds*"). Documented under §4.2.7. `LOCALE-TIME-FROM-SECONDS` additionally honours
D.31.4.5's NOTE that it "*recognizes and processes argument values representing precision to the nanosecond*"
by carrying the fractional part into the formatted value when the argument's scale is nonzero — Annex D is
informative, so this is a determination, not a rule. **L12/L13 (kb/Work PB2764):** every string the rendering emits
(separators, AM/PM designators, era, day and month names, the fraction's decimal point) is normalized by the one
`NormalizeLocaleText`, not only the pattern, and the year, month and day rendered are the GREGORIAN date argument-1
names, never the culture's default calendar; both are `TimeFacts`' job (CONFORMANCE.md §4 item 5).

### 4.8 `LOCALE-COMPARE` (A.4.9 item 2)

Format `FUNCTION LOCALE-COMPARE ( argument-1 argument-2 [ locale-name-1 ] )` (§15.51.2, `--check` OK).
Argument rules (`--check` OK): r1/r2 class alphabetic, alphanumeric or national; **r3 the two may differ**;
r4 locale-name-1 associated with a locale in SPECIAL-NAMES. Returned-value rules (`--check` OK): r1 mixed
classes with one national ⇒ the other is converted to national; r2 the trailing-space truncation (identical to
§8.8.4.2.11 sentence 1); r3 named-else-current locale, `EC-LOCALE-MISSING` if unavailable; r4 cultural
ordering; r5 the result is `'='`, `'<'` or `'>'`; r6 length 1.

The whole function is therefore **`LocaleCollation.Compare` plus a sign-to-character map** — the same code
path as a locale-based relation condition. If it is written as a second comparison implementation, the two
will drift; the design requires it to call the §4.4.3 method.

⚠ **The third argument is a NAME, not an operand.** `IntrinsicArgumentRules`' class screen is driven from
`Verified` and its kinds describe data classes; a locale-name has no class. The catalog's `ArgKinds` gains a
`'L'` (locale-name) code whose screen resolves the word in the locale symbol table instead of binding an
expression — the same treatment `'k'`-style keyword arguments already get. The existing
`IntrinsicArgumentClassDriftTests` does **not** compare `ArgKinds` against `Verified`; adding the `'L'` code
without extending that drift test would reproduce the PB1 trap, so the extension is part of this work.

### 4.9 `STANDARD-COMPARE` and the `ORDER TABLE` clause (A.4.9 item 11 / A.3 item 25)

Format `FUNCTION STANDARD-COMPARE ( argument-1 argument-2 [ ordering-name-1 ] [ argument-4 ] )` (§15.85.2).
This is **not** locale-driven — §15.85.3 r5 (`--check` OK): ordering-name-1 "*shall be associated with a
cultural ordering table in the ORDER TABLE clause of the SPECIAL-NAMES paragraph … If ordering-name-1 is not
specified, the default ordering table 'ISO 14651_2020_TABLE1' described in Annex A of ISO/IEC 14651:2020 shall
be used*". §15.85.4 r2 (`--check` OK) sets `EC-ORDER-NOT-SUPPORTED` when the table or level is unavailable.
§A.3 item 25 (`--check` OK) makes the whole trio "*dependent upon an implementation of ISO/IEC 14651:2020*"
and says "*The implementor need not accept the syntax or set the EC-ORDER-NOT-SUPPORTED exception condition to
exist when support for ISO/IEC 14651:2020 is not provided*".

It shares exactly two things with the locale feature: the result-character contract (r6/r7, identical to
§15.51.4 r5/r6) and the trailing-space truncation (r4). Everything else is a separate table facility. The
design is small and complete:

- `ORDER TABLE ordering-name-1 IS literal-9` joins `specialNameEntry` (§12.3.7.2 figure); literal-9 is
  alphanumeric or national (SR10) and ordering-name-1 "*may be specified only in the STANDARD-COMPARE
  intrinsic function*" (§12.3.7.3 SR9).
- argument-4 is the ordering **level**; absent ⇒ "*the highest level defined in the ordering table*"
  (§15.85.4 r1).
- **⚖ OWNER DECISION (Q4, 2026-08-18) — Unicode CLDR + UCA as the base implementation, realized (PB101, the
  owner's collation guidance) by WiseOwl COBOL's OWN derived engine.** The default ordering table `ISO 14651_2020_TABLE1`
  is `CollationEngine.Standard`: the derived root table (generated from the CLDR release-48-2 root collation, UCA
  17.0.0 — ISO/IEC 14651's Common Template Table is kept synchronized with the same Unicode data) under the
  ISO/IEC 14651 DEFAULT treatment — four levels, variable characters (space, punctuation, symbols) ignored through
  level 3 and weighted at level 4 (UTS #10 "shifted"). The ordering LEVEL (argument-4: 1 primary, 2 secondary, 3
  tertiary, 4 the full four-level ordering) is `CollationEngine.StandardAtLevel(n)` — a `Collator` at that
  strength; absent, level 4 (§15.85.4 r1: "the highest level defined in the ordering table"); 0 or > 4 is
  `EC-ORDER-NOT-SUPPORTED` (r2). `ORDER TABLE ordering-name-1 IS literal-9` (§12.3.7.2 — ONE clause, the last of the
  paragraph; §12.3.7.4 GR17: "*The implementor specifies the allowable content of literal-9*") is resolved by
  `CollationEngine.TryGetOrderingTable`: `"ISO 14651_2020_TABLE1"` in either spelling (§15.85.3 r5 writes a space,
  §12.3.7.4 NOTE 5 an underscore — case-insensitive, the two interchangeable) → the default; as an implementor
  extension a CLDR locale tag → that locale's tailoring over the root table (a locale .NET recognizes but no
  tailoring file → the root order); anything else → `EC-ORDER-NOT-SUPPORTED` at the reference (§15.85.4 r2), the
  binder warning at compile time that the literal will never resolve. Trailing spaces truncate per r4 (the same
  `TrimForLocale` as §4.4.3); the national conversion of r3 is the identity on the D-N1 substrate; the result is
  `"<"`, `"="`, `">"` (r6/r7). Every derived table stays a Unicode-licensed derivation — never a hand-vendored ISO
  file. The conformance statement WiseOwl COBOL makes, VERBATIM:
  **"Implements collation behavior consistent with ISO/IEC 14651 through derived tables and CLDR/UCA data."**

### 4.10 Exceptions

Six conditions, all already registered with the fatalities §14.6.13.1.6's table gives (five rows `--check`ed
individually). This design supplies the **raise sites**, which is what turns the §14.6.13.1.1 exemption off:

| Condition | Raised from | Clause |
|---|---|---|
| `EC-LOCALE-MISSING` | locale resolution fails, at every consumer | §8.2.1; §14.9.39.4 GR24; §15.51.4 r3; §15.52.4 r1; §15.53.4 r1; §15.54.4 r1; §15.68.3 r5a |
| `EC-LOCALE-INVALID` | resolved culture lacks the category data the operation needs | §8.2.1 |
| `EC-LOCALE-INVALID-PTR` | `SET LOCALE cats TO p` where p is not a live saved-locale handle | §14.9.39.4 GR21 |
| `EC-LOCALE-INCOMPATIBLE` | locale comparison over an operand the collation does not order (DETERMINATION L6) | §8.8.4.2.11 |
| `EC-LOCALE-SIZE` | format-2 editing truncates a significant character | §13.18.40.5 GR14b |
| `EC-LOCALE-IMP` | reserved; no WiseOwl COBOL use — documented as such | §14.6.13.1.6 |

Each is a one-line `ExceptionState.Set(name, fatal: true)` at the site, using the existing engine. Every one
gets a golden that **observes** it (a `>>TURN … CHECKING ON` + declarative program, the shape probe
`PB64EC13` already proves works) — a registered-but-never-raised condition is exactly the
zero-fan-out result the project has been bitten by before.

---

## 5. Grammar changes (all pre-authorized; superset parse, bind-narrow)

| # | File | Change |
|---|---|---|
| G1 | `Core/CobolSpecialNames.g4` | `localeClause` — no shape change; the binder arm changes. Add `orderTableClause` (§4.9). |
| G2 | `Core/CobolSpecialNames.g4` | `alphabetDefinition` gains a `LOCALE cobolWord?` alternative (both the ALPHANUMERIC and the FOR NATIONAL branch). Removes the spurious `COBOLNET0901`/`COBOLNET0898` of §1 rows 4–5. |
| G3 | `CobolParserCore.g4` | Delete the `computerAttributes : ~(DOT\|PROGRAM)+` sink; add `objectComputerClause*` + `characterClassificationClause` + `localePhrase`, make `computerName` optional in **both** computer paragraphs, and add `debuggingModeClause` to SOURCE-COMPUTER (§4.5). **Order-free** per §5.2.6.4. ⛔ Re-home the three sink-scanned edition gates in the same commit. |
| G4 | `CobolParserCore.g4` | `setLocaleStatement`, `setSaveLocaleStatement`, `localeCategory` (§4.3), listed ahead of `setToValueStatement`. |
| G5 | `Core/CobolData.g4` | `pictureClause` gains the format-2 tail `LOCALE (IS? cobolWord)? SIZE IS? integerLiteral` (§4.6). ⚠ `SIZE` is already a reserved token, so the tail is unambiguous; the PICMODE lexer mode must terminate at `LOCALE`, exactly as it already does for `SYMBOL` in the CURRENCY clause. |

⚠ **Repetition arity check (the PB45 discipline).** Three loops are introduced and each points at a printed
`…` or at choice indicators: `objectComputerClause*` ← §12.3.6.2's choice-indicator bracket; `localeCategory+`
← §14.9.39.2 format 11's choice-indicator brace; the LOCALE clause's own repetition ← the `…` printed after
the clause in §12.3.7.2. No loop is added for convenience.

Every version-gated rule carries its committed-match **construct-id annotation** per
`DESIGN-frontend-grammar.md`; version numbers stay in `constructs.json`.

---

## 6. Edition gating

The whole facility is **COBOL-2002+**. Evidence, and its limits, stated honestly:

- `LOCALE`, `USER-DEFAULT`, `SYSTEM-DEFAULT` are reserved from 2002 in `reserved-words.json`
  (`r85:false, r2002:true, …`, provenance "added 2002"), so at `--std 85` the clause words are ordinary user
  words and `SPECIAL-NAMES. LOCALE IS FOO.` must keep parsing as an implementor-switch entry. **Correction at
  T1 (the ORDER TABLE precedent):** that '85 reading is excluded by the predicate's SHAPE test (the token after
  LOCALE must be a word — not IS / ON / OFF), and `LOCALE FR IS "fr_FR"` has NO '85 reading at all, so
  `localeClauseAhead()` is no longer edition-gated: the clause is recognized at every edition and the ONE
  construct gate (`special-names-locale-2002`, `VisitLocaleClause`) answers below 2002 with the explanatory
  introduction diagnostic instead of a parse error at the clause's literal. Likewise `SET LOCALE LC_… TO x` (the
  LC_ words carry an underscore, not an '85 word character) and `SET p TO LOCALE …` are recognized everywhere;
  only `SET LOCALE USER-DEFAULT TO x` — a legal '85 Format-1 SET of two receivers — keeps its edition gate.
- The catalog currently windows `LOCALE-COMPARE`/`-DATE`/`-TIME` at 2002 and `LOCALE-TIME-FROM-SECONDS` at
  2014 (the latter per `kb/Work R28`, from WG4 CD 1.2 Annex D.2). ⚠ Those windows are **provisional** under
  ratified decision #1 (no further standards acquisition): the 2023 text proves only presence-in-2014 (Annex E
  lists no locale addition) and says nothing about 2002. The design does not re-litigate them; it inherits
  them and marks them provisional in `constructs.json`, as the neighbouring rows already do.

New `constructs.json` rows (ids follow the existing `<feature>-<edition>` convention):

| id | introducedIn | citation |
|---|---|---|
| `special-names-locale-clause-2002` | 2002 | ISO §12.3.7.2 / GR5 |
| `alphabet-locale-phrase-2002` | 2002 | ISO §12.3.7.4 GR7 e |
| `character-classification-2002` | 2002 | ISO §12.3.6.2 / GR5 |
| `picture-locale-format2-2002` | 2002 | ISO §13.18.40.2 format 2 |
| `set-locale-2002` | 2002 | ISO §14.9.39.2 format 11 |
| `set-save-locale-2002` | 2002 | ISO §14.9.39.2 format 12 |
| `order-table-clause-2002` | 2002 | ISO §12.3.7.2 (STANDARD-COMPARE support) |

⚠ **The mandatory edition-gate sweep applies** (`feedback_edition_gate_sweep`): gating a construct breaks every
corpus program that compiles it below that edition. Each row lands with its four-`--std` matrix case, and the
negative corpus entries asserting `COBOLNET1518` **invert** as their element lands. ✅ DONE, with a stale-list
correction (T6): `locale_functions_a49` was deleted at T4 and `pb25-special-names-locale-a49` at T1;
`locale_keyword_a49` inverted to COBOLNET1664 at T6 (its LOC1 is undeclared — no SPECIAL-NAMES paragraph), and
`pb100-picture-locale-format-2` SPLIT: its P1 (`+$9.9 LOCALE SIZE IS 10`) is LEGAL format 2 and moved into the
positive golden `pb64t6_picture_locale_smoke`; its P2 (`+$$9.99`) stays the SR34 negative at COBOLNET1673.

---

## 7. Diagnostics

`COBOLNET1518` is the single A.4.9 non-support diagnostic (after T0 it has one raise site per entry point:
`IntrinsicBinder.LocaleUnsupported`, the SPECIAL-NAMES / ALPHABET / CHARACTER CLASSIFICATION / SET LOCALE /
PICTURE arms, and `EcNameResolution`). Under the implement decision each arm is **deleted as its increment lands**
(never left dormant — a dead diagnostic is a lie in the user documentation); after T7 the descriptor itself goes.

New codes are needed for the syntax rules that only become reachable once the syntax is accepted. ⚠ The band
drafted here as 1642–1650 was CLAIMED by other work between drafting and adoption (the catalog's maximum was
COBOLNET1661 at adoption); the locale band therefore starts at **COBOLNET1662**, allocated increment by increment
from `scripts/session-probe.ps1` — the rules below keep their letters (a–i) so the design's references survive
renumbering:

| Rule | Reachable from |
|---|---|
| (a) locale-name not declared in a SPECIAL-NAMES LOCALE clause (§12.3.6.3 SR3 / §12.3.7.3 SR24 / §13.18.40.3 SR37 / §14.9.39.3 SR26 / §15.51.3 r4 …) — **one code, every site**, with the citing site named in the message | ✅ T1 — **COBOLNET1664** `locale-name-undeclared` (`DataBinder.ResolveLocaleName`) |
| (b) duplicate locale-name in SPECIAL-NAMES | ✅ T1 — **COBOLNET1665** `locale-name-duplicate` |
| (c) `SET LOCALE` names a category more than once (§5.2.6.4 "each at most once"), a non-category word, or USER-DEFAULT beside a category | ✅ T1 — **COBOLNET1666** `set-locale-categories` |
| (d) `SET LOCALE USER-DEFAULT TO SYSTEM-DEFAULT`/`USER-DEFAULT` — SR25 requires identifier-10 or locale-name-1 | ✅ T1 — **COBOLNET1667** `set-locale-user-default-source` |
| (e) identifier-10/-11 is not category data-pointer (§14.9.39.3 SR27/SR28) | ✅ T1 — **COBOLNET1668** `set-locale-pointer-category` |
| — the LOCALE clause's literal-4 violating SR10/SR11 | ✅ T1 — the ONE SPECIAL-NAMES text-literal rule (COBOLNET0898), shared with ORDER TABLE's literal-9 |
| (f) format-2 PICTURE violates SR32–SR36 / Table 11 / EDITING-beside-LOCALE (one code, sub-rule named) | ✅ T6 — **COBOLNET1673** `picture-locale-format2` (`PictureAnalyzer.AnalyzeLocaleEdited`; SR32's two halves in `DataBinder`) |
| (g) `SIGN` clause with a LOCALE PICTURE phrase (§13.16.3 SR19; §13.17.3 SR9 rides the A.4.2 SCREEN posture — the whole section is COBOLNET1560-recognized, so the screen twin is subsumed by that documented non-support; §13.15.3 has NO report-writer twin, a deliberate non-arm with a positive golden) | ✅ T6 — **COBOLNET1674** `sign-clause-with-locale-picture` |
| (h) an alphabet defined `IS LOCALE` used where a **coded character set** is required (§8.8.4.4.3 SR2; §12.3.7.3 SR16g/SR17d; §13.18.13.3 SR1/SR2; Table 6) | ✅ T5 — **COBOLNET1669** `locale-alphabet-not-a-charset` at the class condition (`DataBinder.IsLocaleAlphabet`, both alphabet classes); the SYMBOLIC CHARACTERS / CLASS `IN` phrases and CODE-SET bind nothing yet — kb/Work PB110 |
| (i) `CHARACTER CLASSIFICATION` / `PROGRAM COLLATING SEQUENCE` specified twice in one OBJECT-COMPUTER paragraph — ✅ landed with PB78 (COBOLNET1652 `object-computer-duplicate-clause`) | done |

⚠ Under the **keep-non-support** decision (§15 Q1 answer "no"), a different, smaller set is required — one
named diagnostic per un-named entry point (rows 6–11 of §1) reusing `COBOLNET1518` with a per-element
`element` string, exactly as the SPECIAL-NAMES arm already does. That work is **not optional under either
answer**; see §12 track T0.

---

## 8. The .NET mapping and its documented limits (§4.2.7 deliverable)

`LocaleFacts` is the ONE place a `CultureInfo` is read. Every row is an implementor determination and every row
lands in `docs/CONFORMANCE.md`.

| ISO 9945 category / field | COBOL use | .NET carrier | Documented limit |
|---|---|---|---|
| LC_COLLATE | §8.8.4.2.11, LOCALE-COMPARE, PCS, SORT/MERGE, ORD/CHAR | `CultureInfo.CompareInfo` | See the three globalization-mode limits below |
| LC_CTYPE | class tests, UPPER-/LOWER-CASE | `CultureInfo.TextInfo` | **Simple (1:1) mapping only** — DETERMINATION L9 — ✅ T5 (`LocaleFacts.TextInfo`; the class tests are POSIX `alpha`/`upper`/`lower` over Unicode letters, space excluded) |
| LC_MONETARY `currency_symbol` | PICTURE fmt-2, NUMVAL-C | `NumberFormatInfo.CurrencySymbol` | — |
| … `int_curr_symbol` | NUMVAL-C matching | `RegionInfo.ISOCurrencySymbol` | .NET has no separator character for the international form; WiseOwl COBOL uses the 3-letter code plus one space, and §15.68.3 r5b.3 only ever matches "*the first three characters*" |
| … `mon_decimal_point` / `mon_thousands_sep` | separators | `CurrencyDecimalSeparator` / `CurrencyGroupSeparator` | — |
| … `mon_grouping` | group sizes | `CurrencyGroupSizes` | — |
| … `frac_digits` | fraction digits | `CurrencyDecimalDigits` | — |
| … `int_frac_digits` | international form | **no .NET carrier** | WiseOwl COBOL uses `CurrencyDecimalDigits` for both; a documented limit |
| … `positive_sign` / `negative_sign` | sign strings | `PositiveSign` / `NegativeSign` | — |
| … `p_cs_precedes`, `n_cs_precedes`, `p_sign_posn`, `n_sign_posn` | placement | derived from `CurrencyPositivePattern` (4 values) and `CurrencyNegativePattern` (16 values) | A **generated** pattern→triple table, never hand-maintained (see below) |
| LC_TIME `d_fmt` / `t_fmt` | LOCALE-DATE / -TIME | `ShortDatePattern` / `LongTimePattern` | DETERMINATIONS L10, L12, L13 — ✅ T4 + PB2764 (`TimeFacts`, the LC_TIME snapshot beside `MonetaryFacts`: `TimeFacts.FormatDate` / `FormatTime` render `d_fmt` / `t_fmt` over their tokens, since hour 24 / seconds 99 / a fraction exceed a `DateTime`, and every separator, designator, name, era and decimal point they emit has passed `LocaleFacts.NormalizeLocaleText`; the date is the Gregorian date argument-1 names) |
| LC_MESSAGES, LC_NUMERIC | settable and queryable only | stored slots | §8.2.1: "*not used directly by COBOL; however, the ability to set and query these locale categories is provided*" (`--check` OK) |

**The pattern→placement tables are DERIVED AT RUNTIME, not written and not build-time-generated** (a T6
correction to this section's own draft, rule 5: a build-time source generator bakes the BUILD host's
`NumberFormatInfo` semantics into the assembly and can never fail on the RUN host, defeating the stated goal).
`MonetaryPlacement`'s static initializer PROBES every pattern value the running .NET accepts — the range is
DISCOVERED, never hard-coded: this runtime accepts SEVENTEEN `CurrencyNegativePattern` values (0–16; the
documented 0–15 is false — luy-KE uses 16) — and derives each pattern's
(`cs_precedes`, `sep_by_space`, `sign_posn`) convention by a round-trip against the ONE POSIX renderer,
THROWING when no convention reproduces a layout. `sep_by_space` is load-bearing (9 of the 17 negative
patterns need it nonzero; `$ -n` and `n- $` exist only at sep 2); the POSITIVE pattern carries NO sign slot,
so `p_sign_posn` = 1 is an explicit determination (never mirrored — the invariant's `n_sign_posn` is 0).
The **drift test** (`MonetaryFactsTests`) asserts, for every specific culture on the host (≥400 asserted
examined), that WiseOwl COBOL's format-2 edit produces the same *placement shape* as `value.ToString("C",
culture)`, and that the derived range equals the runtime's accepted maximum — the assertion that would have
caught pattern 16 and will catch a 17. ⚠ The test builds its picture FROM the culture's `frac_digits` so the
two digit strings are comparable — a TEST-CONSTRUCTION choice, not a rule: §13.18.40.5 r12 hands the locale
only the separators and group sizes, and NO editing rule reads `frac_digits`.

**Three globalization-mode limits, all detected rather than assumed:**

1. **Invariant globalization mode** (`InvariantGlobalization=true`, or the equivalent runtime switch) collapses
   every culture to the invariant one. LC_COLLATE is unaffected — it is WiseOwl COBOL's own engine (PB101), never
   `CompareInfo` — but LC_CTYPE / LC_MONETARY / LC_TIME are .NET culture data: `LocaleFacts` probes for the mode
   once (`LocaleFacts.InvariantMode`) and, if set, every non-root locale's culture data is INCOMPLETE
   (`HasCultureData` false) ⇒ **`EC-LOCALE-INVALID`** at an operation that needs it (§8.2.1 "invalid or
   incomplete"), the invariant content standing when checking is off — the honest answer §8.2.1 provides for.
   (As built at T4; the draft said EC-LOCALE-MISSING — availability is the ONE known-locale rule, content is
   the culture data, and §8.2.1 names a condition for each.)
2. **NLS vs ICU on Windows** (`System.Globalization.UseNls`) changes collation results. Detected and reported
   in the compiler's `--version` banner and in `CONFORMANCE.md`; results are guaranteed reproducible only
   within one mode.
3. **ICU version skew** across hosts changes tailorings. `LocaleFacts` records
   `CultureInfo.CompareInfo.Version` (the sort version) and the goldens pin **relational outcomes**
   (`"côte" < "coter"` in `fr-FR`) rather than absolute weights, since relational outcomes are what the
   standard specifies and what survives an ICU upgrade.

---

## 9. GnuCOBOL survey, per latitude point

**Method.** `cobc` is **not installed on this machine** (`where cobc` → nothing), so nothing here is a fresh
run of GnuCOBOL. Two in-repo sources are used and are labelled: **[M]** = measured — the harvested GnuCOBOL
testsuite (`tests/external/gnucobol/tests/testsuite.src/run_functions.at`) and the recorded differential
verdicts (`tests/external/gnucobol/last-differential-report.json`); **[U]** = unverified vendor claim, which
must be re-derived against GnuCOBOL's own documentation before it is relied on for a latitude decision.

| Latitude point | GnuCOBOL evidence | Bearing on this design |
|---|---|---|
| `LOCALE-COMPARE` | **[M]** Accepted (`verdict: WE_REJECT_THEY_ACCEPT`, case `run_functions:1883`) and its testsuite pins the **one-character** results: `("A","B") = "<"`, `("B","A") = ">"`, `("A","A") = "="`. | Confirms §15.51.4 r5's contract as the interoperable one. Adopt. Note the test uses **comma-separated** arguments and never the locale-name arm. |
| `LOCALE-DATE` | **[M]** Accepted (`run_functions:1913`); the test asserts only `X NOT = SPACES` for `"19630302"` into `PIC X(32)`. | Argument-1 is a **string** in the interoperable reading too. No format is pinned, so GnuCOBOL supplies no evidence on `d_fmt` mapping — DETERMINATION L10 stands on its own. |
| `LOCALE-TIME` | **[M]** Accepted (`run_functions:1939`), `"233012"`, same non-blank assertion. | Same. |
| `LOCALE-TIME-FROM-SECONDS` | **[M]** Accepted (`run_functions:1965`), integer `33012`. | Confirms the numeric argument-1 of §15.54.3 r1. |
| `STANDARD-COMPARE` | **[M]** *No case exists* in the harvested corpus. | No evidence either way. Do not infer non-support from absence. |
| SPECIAL-NAMES `LOCALE` clause | **[M]** No case in the harvested corpus (a `LOCALE` grep over `tests/external/gnucobol/` hits only `run_functions.at`). **[U]** GnuCOBOL is generally understood to accept a SPECIAL-NAMES LOCALE clause and to implement the locale intrinsics over the C library's `setlocale`/`strcoll`/`strftime`. | The **[U]** claim, if true, would make the external identification a **C library locale name** (`fr_FR.UTF-8`), which is exactly why DETERMINATION L1 normalizes the POSIX spelling. Verify against GnuCOBOL's manual before citing it as precedent. |
| `SET LOCALE` formats 11/12 | **[M]** No case in the corpus. **[U]** unknown. | No latitude guidance; the design follows the standard alone. |
| `PICTURE` format 2 (locale) | **[M]** No case in the corpus. **[U]** unknown. | Same. |
| `CHARACTER CLASSIFICATION` | **[M]** No case in the corpus. **[U]** unknown. | Same. |
| ANYCASE / LOCALE on NUMVAL-C | **[M]** No locale-bearing case. | Same. |

⛔ Two disciplines this survey enforces. (1) **GPL** — GnuCOBOL's sources are never read or copied; only its
published testsuite behaviour, which is already vendored in this repo, and its documentation. (2) The four
**[M]** rows are the exact four differential cases that will **flip** when this design lands: their
`WE_REJECT_THEY_ACCEPT` verdicts become accepts, and the differential baseline must be regenerated with each
flip attributed by name (§10).

---

## 10. Test plan

**Bar:** a test that cannot fail for the reason it exists is worthless. Each item below names what it would
catch.

**T-A · Spec-derived goldens (`tests/conformance/`, manifest-registered, one per rule).**
- ✅ `pb64t1_locale_declare` (was `locale_special_names_declare`) — a LOCALE clause with both branches (`external-locale-name` and `literal-4`)
  and a program that uses each; catches the DETERMINATION L1 normalization (`fr_FR` ≡ `"fr_FR.UTF-8"` ≡
  `fr-FR`).
- `locale_compare_ordering` — `FUNCTION LOCALE-COMPARE` over a pair whose order **differs** between the
  invariant and a tailored locale (`"côte"` vs `"coter"` under `fr-FR`); catches a stub that returns ordinal.
  Result witnessed by `FUNCTION ORD` of the returned character, never by the console echo.
- `locale_compare_trailing_spaces` — `""`, `"   "`, `"a  "` (the three §15.51.4 r2 / §8.8.4.2.11 cases);
  catches the plain-`TrimEnd` simplification.
- `locale_relation_pcs` — `ALPHABET a IS LOCALE fr` + `PROGRAM COLLATING SEQUENCE IS a` + a relation whose
  truth flips versus native; catches a PCS that silently stays native.
- `locale_pcs_current` — the same with `ALPHABET a IS LOCALE` (no locale-name) and a `SET LOCALE LC_COLLATE`
  **between two comparisons**; catches a one-time resolution where §12.3.7.4 GR7e requires per-use.
- ✅ `pb64t1_set_locale_categories` (+ negative `pb64t1-set-locale-duplicate-category`) — `SET LOCALE LC_NUMERIC LC_TIME TO fr` (the multi-category, order-free form) and a
  duplicate-category negative; catches a scalar-category model.
- ✅ `pb64t1_save_restore_locale` (+ `pb64t1_ec_locale_missing`, `pb64t1_sort_locale_snapshot`) — format 12 then format 11 through the pointer, plus an `EC-LOCALE-INVALID-PTR`
  negative on a NULL and on an `ADDRESS OF` pointer.
- `locale_picture_edit_size` — the §13.18.40.5 GR14 three cases (larger, exact, smaller-with-EC-LOCALE-SIZE)
  under two locales; plus `BLANK WHEN ZERO` precedence (GR10) and a `SIGN`-clause negative (SR19/SR9).
- `locale_date_time` — `LOCALE-DATE`/`LOCALE-TIME`/`LOCALE-TIME-FROM-SECONDS` under a pinned locale, with the
  result length witnessed by `FUNCTION LENGTH`; catches the `RuntimeDetermined` result rule collapsing to a
  fixed length. Also the §15.53.3 r3 boundary values `24` hours and `99` seconds.
- ✅ `pb64t5_case_locale_phrase` (was `locale_case_turkish`) — `LOWER-CASE("I" LOCALE TR)` and `UPPER-CASE("i" LOCALE TR)`, witnessed by
  `FUNCTION ORD` (U+0131 / U+0130); catches an invariant fold wearing a locale argument — plus `CLASSIFICATION IS
  LOCALE` resolved at the container's vs the CALLed contained program's activation, and the PLAIN program's r4 arm.
- ✅ `pb64t5_character_classification` (was `locale_classification`) — `CHARACTER CLASSIFICATION IS TR` **without** a LOCALE phrase on the function, and
  the class tests (the dotless ı alphabetic; space NOT alphabetic under a classification; -UPPER / -LOWER);
  this is the golden that would have caught §1 row 6 for the last year.
- ✅ `pb64t5_classification_unavailable` — a DECLARED, UNAVAILABLE classification locale: checking off → the
  coded character set's behavior; `>>TURN EC-LOCALE-MISSING CHECKING ON` → the class test and the case
  function each raise at use, the statement interrupted; catches a classification that silently falls back.
- ✅ `locale_classification_order` — landed as the PB78 OBJECT-COMPUTER rewrite (`objectComputerClause*`); the
  negative `pb64t5-classification-twice` pins the at-most-once rule (COBOLNET1652).
- `object_computer_no_name` — `OBJECT-COMPUTER. PROGRAM COLLATING SEQUENCE IS a.` with computer-name-1 omitted
  (§1 row 14); **not a locale golden** — it belongs with the OBJECT-COMPUTER rules and ships with G3.
- `object_computer_obsolete_gates` — MEMORY SIZE / SEGMENT-LIMIT at `--std 2002` and SOURCE-COMPUTER
  `WITH DEBUGGING MODE` at `--std 85` and `2002`; these already exist in the edition matrix and must be run
  **against the rewritten grammar** as the proof that G3 re-homed all three gates rather than dropping them.
- ✅ `pb64t5-class-condition-locale-alphabet` (negative) — `IF X IS a` where `a IS LOCALE` (either class; §8.8.4.4.3
  SR2) is COBOLNET1669; catches Table 6's blank coded-character-set column being ignored. The `SYMBOLIC CHARACTERS /
  CLASS … IN a` and CODE-SET siblings (§12.3.7.3 SR16g / SR17d; §13.18.13.3 SR1/SR2) land with kb/Work PB110.
- Six EC goldens, one per `EC-LOCALE-*`, each `>>TURN … CHECKING ON` with a declarative that observes it.

**T-B · Unit tests.** `LocaleFacts` mapping (each POSIX field → its .NET carrier); the pattern→placement table
generator; `LocaleState` category independence (setting LC_TIME leaves LC_COLLATE alone — §14.6.6 r3);
saved-locale handle lifetime; `CobolCollation` conformance across all four implementations against a shared
behavioural test list.

**T-C · Drift tests (the "automatic next case" guarantee).**
1. `CobolString.Compare` has exactly ONE public overload taking a `CobolCollation` — fails if a fifth
   comparison kind re-forks the API.
2. Every `IntrinsicSig` whose `ArgKinds` contains `'L'` has a locale-name screen registered — the PB1 trap
   closed for the new code.
3. The pattern→placement table covers every `CurrencyPositivePattern`/`CurrencyNegativePattern` value the
   running .NET exposes.
4. Every `EC-LOCALE-*` name registered in `ExceptionCatalog` has **at least one raise site** — the standing
   answer to "registered but never raised".

**T-D · Edition matrix.** All seven `constructs.json` rows × four `--std` values; plus the sweep that every
currently-green corpus program still compiles at every edition.

**T-E · Differential.** The four GnuCOBOL cases in §9 flip from `WE_REJECT_THEY_ACCEPT` to accepted; the
baseline is regenerated and each flip attributed by name in the DEVLOG. ⚠ GnuCOBOL's `LOCALE-DATE`/`-TIME`
tests assert only non-blankness, so agreement there is weak evidence — the *format* must be pinned by T-A, not
by the differential.

**T-F · Determinism — ✅ T1:** `CompilerUnderTest.RunExit` (the ONE process launcher) pins both variables to `INVARIANT`
for every program it runs; a test that wants another default passes it in `env`. The whole conformance harness exports `COBOL_USER_LOCALE=INVARIANT` and
`COBOL_SYSTEM_LOCALE=INVARIANT` by default, and each locale golden sets its own. Without this the goldens pass
on the author's machine and fail in CI — a failure mode this repo has already paid for once with a BOM.

**T-G · NIST.** The CCVS corpus contains no locale coverage; the NIST leg is a **guard** here, proving the
`computerAttributes` grammar replacement (G3) and the `CobolString.Compare` collapse (§4.4.1) regress nothing.

---

## 11. Current → target module changes

| Module | Change |
|---|---|
| `Cobol.Net.Frontend/Grammar/**` | G1–G5 (§5); delete the `computerAttributes` sink |
| `Cobol.Net.Frontend/Parsing/CobolParserCoreBase.cs` | `localeClauseAhead()` stays; add the analogous OBJECT-COMPUTER predicate if needed |
| `Compiler/Binding/DataBinder.Switches.cs` | LOCALE clause arm: declare instead of reject; ALPHABET `IS LOCALE`; ORDER TABLE |
| `Compiler/Binding/CollatingModel.cs` | `LocaleCollatingSpec`; `AlphabetDef.HasCodedCharacterSet` |
| `Compiler/Binding/Model/LocaleRef.cs`, `LocaleSymbol.cs` | NEW |
| `Compiler/Binding/IntrinsicCatalog.cs` | five rows lose `IntrinsicBind.Unsupported`; `'L'` ArgKind; `IntrinsicResultRule.RuntimeDetermined` |
| `Compiler/Binding/Procedure/Verbs/IntrinsicBinder.cs` | delete `LocaleUnsupported`; bind the LOCALE phrase instead of rejecting it |
| `Compiler/Binding/Procedure/Verbs/SetBinder.cs` | formats 11/12 |
| `Compiler/Binding/DataBinder.cs` (PICTURE) | format 2: `LocaleEdit`, size, SR32–37, SIGN prohibition |
| `Compiler/Validation/VersionConformancePass.cs` | typed OBJECT-COMPUTER visit; the seven new construct gates |
| `Compiler/CodeGen/Emit/**` | emit `CobolCollation` instead of the three carriers; locale intrinsic arms; locale-edited store/fetch |
| `Runtime/Control/RunUnit.cs` | `LocaleState` property |
| `Runtime/Collation/*` (PB101 ✅) | NEW — the derived CLDR/UCA engine: `CollationTable` (+ `Data/root-collation.bin`, generated by `scripts/collation/generate-collation-table.py` over `data/unicode/`; format 2 — reordering groups + case bits, PB105), `CollationElementIterator`, `Normalizer`, `Collator` (+ `CollationOptions`: strength, alternate/maxVariable, caseFirst, backwards secondaries), `CollationKey`, `CollationEngine` (+ `ResolveLocale` → `ResolvedLocaleCollation`), `TailoringRules` (+ `Tailoring/*.tailor` — the site-override layer) |
| `Runtime/Collation/CLDR/*` (PB105 ✅) | NEW — the CLDR locale loader: every CLDR release-48-2 collation file embedded (`Data/cldr-collation.zip`, `scripts/collation/pack-cldr-collation.py`), `CldrParser` (LDML/JSON + the rule syntax), `CldrLocaleLoader` (parent chain, `-u-` keys, `ResolveCollation`), `CldrTailoringBuilder` (rules → a locale's table + settings). **The locale universe a `LOCALE` clause / the named `IS LOCALE` form (T1) names is now every CLDR locale, not the four `.tailor` files.** |
| `Runtime/Collation/Cache/*` (PB106 ✅) | NEW — `CollationKeyCache` per collator; `CobolCollation.SupportsKeys` / `KeyOf`; SORT/MERGE key columns and the indexed-file LOCALE key comparison key through it |
| `Runtime/Collation/CollationRuntime.cs` (✅) | NEW — `Initialize()` (every `RunUnit`, cheap) / `Warmup()` / `Status` |
| `Runtime/Unicode/Segmentation/*` (PB104 ✅) | NEW — UAX #29 grapheme clusters from a derived table (`Data/grapheme-break.bin`, `scripts/unicode/generate-grapheme-table.py`) |
| `Runtime/Control/LocaleState.cs` (PB101 ✅, minimal), `Runtime/Values/Text/CobolCollation.cs` (✅), `Runtime/Values/Text/LocaleCollation.cs` (✅), `Runtime/Intrinsics/CobolLocale.cs`, `Runtime/Values/Numeric/CobolLocaleEdit.cs`, `Runtime/Globalization/LocaleFacts.cs` | NEW (the last three with T4–T6) |
| `Runtime/Values/Text/CobolString.cs` (✅) | three `Compare` + three `ThruMember` overloads → the `char pad` native form + ONE `CobolCollation` form |
| `CodeGen/Roslyn/CollationEmit.cs` (✅) | NEW — the ONE renderer of a carrier from an `AlphabetDef` / `NationalAlphabetDef` |
| `Editions/Diagnostics/DiagnosticCatalog.cs` | 1642–1650; delete 1518 (implement) or extend it (non-support) |
| `tests/version-matrix/constructs.json` | seven rows |
| `docs/CONFORMANCE.md` | §4 item 5 rewritten; the §8 determination table added; the stale "parse error" claim corrected **regardless of the decision** |

---

## 12. Migration plan — seven increments, each independently battery-green

**T0 · Posture repair — ✅ LANDED (PB78 2026-08-18: rows 6–8 and 14; PB92: rows 9–10; PB100: rows 4–5, 11 and
A.4.9 item 1's exception-names).** As drafted: fix §1 rows 4–11 so every
locale entry point is *named*: the ALPHABET LOCALE phrase, `CHARACTER CLASSIFICATION` (including the ordering
defect — the grammar work of G3 is needed either way), `SET LOCALE`/save-locale, and `PICTURE` format 2 all
parse and then draw the cited A.4.9 diagnostic. Row 14 (the mandatory computer-name) rides G3 and is **not a
locale defect at all** — it needs its own `kb/Work/` note, filed before it becomes a paragraph here, because
CLAUDE.md rule 8 forbids this document being the place a defect lives. Correct `CONFORMANCE.md`'s stale
SPECIAL-NAMES sentence. **This closes the A.4.1 violation that exists today** and is the only part of this
design that is unconditionally owed.

Then, if Q1 is answered "implement":

| # | Increment | Closes |
|---|---|---|
| T1 | ✅ **LANDED 2026-08-19 (PB64 T1)** — `LocaleSymbol` + `LocaleRef` (`Binding/Model/LocaleSymbol.cs`; `LocaleCollatingSpec(LocaleRef)`); `LocaleState` on `RunUnit` in its full form (`LocaleValue` per category, `LocaleCategorySet`, the saved-locale `SavedLocalePointer` handle, `SetFrom*` / `SetUserDefaultFrom*` / `Save`); `LocaleIdentification.Normalize` (L1); the SPECIAL-NAMES LOCALE clause declares (`DataBinder.LocaleBind`, SR10/SR11 through the ONE text-literal rule shared with ORDER TABLE, COBOLNET1665 duplicates, §12.3.7.4 GR1 inheritance); `SET` formats 11/12 (`SetBinder.BindSetLocale` / `BindSaveLocale` → `BoundSetLocale` / `BoundSaveLocale` → `SetEmitter`); the NAMED `IS LOCALE locale-name-2` alphabet (T3's remainder); the EC-LOCALE ambient gates (MISSING / INVALID-PTR / INCOMPATIBLE — `CheckingFlags`, `ExceptionState.Locale*Error`, `EcBinder`, `EcEmitter.FatalAmbientGates`; the EC-LOCALE names legal again); the SORT/MERGE snapshot (§14.6.6 r5 — `CobolCollation.Snapshot`, `CobolSort.Init(name, collation, national)` — BOTH GR5 sequences since kb/Work PB678); COBOLNET1664–1668; construct rows `special-names-locale-2002` / `set-locale-2002` / `set-save-locale-2002`; goldens `tests/conformance/2002/pb64t1_*` + eight negatives; the harness pins `COBOL_USER_LOCALE` / `COBOL_SYSTEM_LOCALE` to the root (T-F). `LocaleFacts` (S5) is NOT part of T1 — it is the LC_CTYPE / LC_MONETARY / LC_TIME snapshot the T4–T6 consumers need and lands with T4. | item 9, 10 (clause half) |
| T2 | ✅ **LANDED 2026-08-18 (PB101)** — `CobolCollation` collapse; corpus/unit batteries green, ordinary programs' generated text unchanged | (enables T3) |
| T3 | ✅ **LANDED (PB101 the current-locale form; PB64 T1 the NAMED form and the SORT snapshot)** — `LocaleCollation` + `ALPHABET … IS LOCALE [locale-name-2]` + PCS + SORT/MERGE + indexed keys + MAX/MIN + HIGH/LOW-VALUE + ORD/CHAR; the named form is the sequence of THAT locale (its L1-normalized tag in the carrier; EC-LOCALE-MISSING at use when unavailable); the SORT/MERGE sequence is snapshotted at statement start (§14.6.6 r5) | items 10 (alphabet half), §8.8.4.2.11 |
| T4 | ✅ **LANDED 2026-08-19 (PB64 T4)** — `Runtime/Intrinsics/CobolLocale.cs` (`Compare` over the ONE `LocaleCollation` carrier + the sign map; `Date` / `Time` / `TimeFromSeconds` over `LocaleFacts` — `Runtime/Globalization/LocaleFacts.cs`, the ONE place a `CultureInfo` is read, L10: `d_fmt` = ShortDatePattern, `t_fmt` = LongTimePattern, rendered over the pattern's tokens so §15.53.3 r3's hour 24 / seconds 99 and a scaled argument's fraction render); the catalog rows bind Runtime with the `'l'` locale-name kind (`IntrinsicArgumentRules.NonOperandArgumentKinds['l']`, `IntrinsicBinder.BindLocaleFunction`, `BoundIntrinsicCall.Locale` — the ONE `LocaleRef`), the Verified schemas screen the operands (the 8/6-position widths); EC-LOCALE-MISSING at use, **EC-LOCALE-INVALID** (§8.2.1 — no culture data; a new ambient gate) ; `RuntimeDetermined` needed no enum member: the dynamic-length string every §15 string function already returns IS the run-time-determined length | items 2–5 |
| T5 | ✅ **LANDED 2026-08-19 (PB64 T5)** — `ClassificationSpec` / `LocalePhrase` (`Binding/Model/LocaleSymbol.cs`; `DataBinder.ResolveClassification`), `Runtime/Globalization/CharacterClassification.cs` (`Resolve` at each activation — the `__CLASSIFY` field, `DispatchEmitter`'s `__Activate` prologue; `LocalePhraseKind`), `CobolLocale.UpperCase / LowerCase` (the LOCALE phrase — `BindCaseFunctionWithLocale`, `BoundIntrinsicCall.Locale`; the classification — `__CLASSIFY.For(national)`), `CobolClass.IsAlphabetic* (s, LocaleFacts?)` (GR3 b1/c1/d1 — letters per LC_CTYPE, no space; the case round-trip), **`LocaleFacts.Require` — the ONE §8.2.1 gate** (MISSING / INVALID at use, the LOCALE functions moved onto it), `DataBinder.IsLocaleAlphabet` + COBOLNET1669 (§8.8.4.4.3 SR2), EC-LOCALE-INVALID enabled on every statement of a module with a classification; construct rows `character-classification-2002` / `case-function-locale-phrase-2002`; goldens `2002/pb64t5_*` (three) + four negatives; `locale_keyword_a49` shrunk to the T6 functions. Registered beside it: PB109 (the GR3 a coded-character-set class condition is a loud staged value) and PB110 (the SYMBOLIC CHARACTERS / CLASS `IN` phrases and CODE-SET bind nothing). | items 6, 7, 13 |
| T6 | ✅ **LANDED 2026-08-28 (PB64 T6)** — `Runtime/Globalization/MonetaryFacts.cs` (the ONE LC_MONETARY snapshot, L12-normalized) + `MonetaryPlacement.cs` (the ONE POSIX renderer + the RUNTIME-derived pattern tables, range discovered, round-trip-proved) + `Runtime/Values/Numeric/CobolLocaleEdit.cs` (§13.18.40.5 r9–r15 editing/de-editing; **EC-LOCALE-SIZE's first raise site**, character-based per r14 b; DeEdit = the §14.6.13.2 r4 image-inverse, EC-DATA-INCOMPATIBLE) + the NUMVAL-C LOCALE scan (`CobolIntrinsics.NumvalLocale.cs` — templates from the snapshot, r4 NOT inherited, ANYCASE folds only the currency comparison via the locale's LC_CTYPE, §15.94.4 positions ordinal); `PicInfo.LocaleEdit` (`LocaleEditSpec` — Length = SIZE, EditMask NULL, the doc that licenses every mask deref REWRITTEN), `AnalyzeLocaleEdited` (SR33–36 + Table 11's closed form `[+] [cs] Z… 9… [. Z…\|9…]`, DECIMAL-POINT IS COMMA inert per §12.3.7.4 GR14), the DataBinder arms (SR32 both halves, SR19 → 1674, SR37 → 1664, ValidateNumericValue's locale legs), the REPORT-WRITER arm (PB113 — one analyzer, three callers), the emit dispatch INSIDE `RuntimeApi.EditFormatFor`/`EditTryFormatLocale`/`ReceiverScaleOf` (the ONE receiver-scale rule replacing two drifted copies) + `NumericRenderer`'s de-edit arm + the three VALUE-image producers (`LocaleEditCompose` — runtime images, no compile-time image exists) + `ConditionRenderer`'s level-88 arm + `OoConformance`'s locale-identity axis + `HIGHEST-/LOWEST-ALGEBRAIC`'s locale arm; `BindNumvalCFamily`'s r5 arm (`BoundIntrinsicCall.LocaleWritten` — bare LOCALE ≠ no phrase) + the three renderer arms (native/Test/Dec); grammar `(IS? cobolWord)?` (PB114) + the edition-free predicate (the ORDER TABLE precedent); rows `picture-locale-format2-2002` / `numval-c-locale-phrase-2002`; COBOLNET1673/1674; COBOLNET1518 + `LocaleUnsupported` + `IntrinsicBind.Unsupported` DELETED; goldens `2002/pb64t6_*` (6) + three negatives + the pb100/locale_keyword inversions; +60 Unit (the drift oracle over every host culture green first run) | items 8, 12 — **the module claim completes** |
| T7 | ✅ **LANDED 2026-08-18 (PB101)** — `ORDER TABLE ordering-name IS literal-9` (§12.3.7.2, one clause; SR10/SR11; GR17 — a literal the engine cannot resolve warns COBOLNET1662 and sets EC-ORDER-NOT-SUPPORTED at every reference) + `STANDARD-COMPARE` (§15.85: `BindStandardCompare`, ArgKinds `ssoi` with `'o'` = §15.3 item 12, COBOLNET1663 for r5/r6 violations; runtime `CobolIntrinsics.StandardCompare` over `CollationEngine.Standard` / `StandardAtLevel`, r4 trim, r6/r7 result; EC-ORDER-NOT-SUPPORTED through the ambient-gate machinery, `"="` when checking is off) | item 11 |

T2 is deliberately a **behaviour-free** commit: a refactor that changes results is indistinguishable from a
feature that breaks them.

---

## 13. Risks

| # | Risk | Mitigation |
|---|---|---|
| R1 | ICU version/mode skew makes goldens host-dependent | T-F env pinning; relational (not absolute) assertions; the sort-version recorded in `LocaleFacts` |
| R2 | The `CobolCollation` collapse regresses SORT/indexed/relation paths | T2 is behaviour-free and gated on a full battery + NIST; the `: c` order-equivalence proof is carried over verbatim |
| R3 | Deleting `COBOLNET1518` breaks the negative corpus cases asserting it | ✅ Retired at T6 — TWO remained by then (§6's corrected list); both inverted/split in the deleting commit |
| R4 | The ORD/CHAR order vector (65 k comparisons) is a startup cost | Lazy, per-culture, per-run-unit, and only for programs referencing ORD/CHAR under a locale PCS |
| R5 | `EC-LOCALE-INCOMPATIBLE` is unreachable under ICU (DETERMINATION L6 could be wrong) | The drift test requires every registered EC to have a raise site **and** a golden that fires it |
| R6 | The 2002 edition windows are provisional | Inherited, marked provisional, unchanged by this work; re-derivable only with the 2002/2014 texts (decision #1) |
| R7 | Locale editing quietly disagrees with .NET's own currency formatting | The generated placement table + the `ToString("C")` shape oracle (§8) |
| R8 | **Deleting the `computerAttributes` sink silently drops the MEMORY SIZE / SEGMENT-LIMIT / WITH DEBUGGING MODE edition gates** and the `_debuggingModeDeclared` flag behind VCR row 7.17 | §4.5 item 2: re-home all three onto typed visits in the same commit; `object_computer_obsolete_gates` (T-A) is the failing-first proof |

---

## 14. What explicitly does NOT change

- The typed-native invariant; no byte substrate anywhere in the facility.
- The legacy runtime locale bodies (`LocaleCompare` returning `1m/-1m/0m` from
  `String.Compare(CurrentCulture)`, `LocaleDate` treating argument-1 as an integer date, `LocaleTime`
  substituting `ToString("T")` for `t_fmt`) are **not** ported and are **not** an oracle: each is
  independently non-conforming, so no row here may ever be closed on the legacy differential. They die at P15.
- `EC-LOCALE-*` names, fatalities, and the `>>TURN`/USE/RAISE machinery (already conforming, §1 rows 12–13).
- The four `--std` compilers remain one grammar with one gate; no parse-time edition predicate is added.

---

## 15. Open questions

**Owner-reserved (this design cannot answer them):**

- **Q1 — Does WiseOwl COBOL claim A.4.9 support?** ✅ **ANSWERED 2026-08-18: YES — implement.** Council decision 3
  (2026-07-03, documented non-support) is superseded. T0 landed first (PB78 / PB92 / PB100).
- **Q2 — the default-locale mechanism.** ✅ **ANSWERED 2026-08-18: environment variables** — `COBOL_USER_LOCALE`
  / `COBOL_SYSTEM_LOCALE` with the .NET culture fallbacks (DETERMINATION L2 as drafted; no compiler option, no
  configuration file).
- **Q3 — locale-based collating for INDEXED file keys.** ✅ **ANSWERED 2026-08-18: YES** (DETERMINATION L8 as
  drafted, with the cross-locale key-order caveat documented).
- **Q4 — `STANDARD-COMPARE` / ISO/IEC 14651:2020.** ✅ **ANSWERED 2026-08-18: Unicode CLDR + UCA as the base
  implementation** (§4.9); the conformance statement is fixed verbatim: "Implements collation behavior
  consistent with ISO/IEC 14651 through derived tables and CLDR/UCA data."

**Design questions this document answers, recorded so they are not re-opened silently:** what an external
locale identification is (L1), the two defaults (L2), foreign-switch visibility (L3), the saved-locale handle
(L4), one sequence for both classes (L5), when `EC-LOCALE-INCOMPATIBLE` fires (L6), how ORD/CHAR get positions
under a locale (L7), indexed keys (L8), simple case mapping (L9), `d_fmt`/`t_fmt` (L10).

**Open, but answerable by measurement rather than by the owner:**

- The GnuCOBOL **[U]** rows in §9 — whether GnuCOBOL accepts the SPECIAL-NAMES LOCALE clause, `SET LOCALE`,
  `PICTURE` format 2 and `CHARACTER CLASSIFICATION`. `cobc` is not installed here; the harvested corpus is
  silent on all four. These must be re-derived from GnuCOBOL's documentation (never its GPL sources) before
  any latitude decision leans on them.
- Whether §8.3.3.6.4's "*multiple-character combination*" for HIGH-VALUE/LOW-VALUE has any realizable meaning
  in a typed-native model, or is (as §4.4.4 assumes) an artefact of byte-oriented implementations. A spec
  reading, not an owner decision — but it should be settled before T3 rather than after.
