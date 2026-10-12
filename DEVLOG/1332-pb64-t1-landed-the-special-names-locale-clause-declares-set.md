## Entry 1332 — 2026-08-19 11:30 PDT — PB64 T1 LANDED: the SPECIAL-NAMES LOCALE clause declares, SET LOCALE formats 11/12 act on the run unit's ONE locale state, the NAMED `IS LOCALE locale-name-2` alphabet collates, three EC-LOCALE conditions are raised and observed, the SORT/MERGE sequence is snapshotted (§14.6.6 r5) — A.4.9 items 9 and 10 (clause half) CLAIMED; 19 inventory rows CONFORMS (GAP 3916 → 3908)

**What T1 is** (DESIGN-locale-facility §12; the increment after PB108's tooling warm-up): the locale facility's
DECLARATION and STATE — `LOCALE locale-name-1 IS {external-locale-name-1 | literal-4}` (§12.3.7.2), `SET LOCALE
{category… | USER-DEFAULT} TO {identifier-10 | locale-name-1 | USER-DEFAULT | SYSTEM-DEFAULT}` (§14.9.39 format 11)
and `SET identifier-11 TO LOCALE {LC_ALL | USER-DEFAULT}` (format 12), over the run unit's one `LocaleState`; plus
the pieces that were waiting on it: the named alphabet form (T3's remainder), the EC-LOCALE names (PB100's refusal
reverted) with live raise sites, and the SORT snapshot rule owed since PB101.

**Runtime.** `Control/LocaleState.cs` rewritten around a per-CATEGORY value (`LocaleValue` — a tag-identified locale
has one tag in every slot; a SAVED locale is a snapshot whose categories may differ) and a category SET
(`LocaleCategorySet`, the shape the printed format's choice indicators demand — §5.2.6.4): `SetFromLocale` /
`SetFromSaved` / `SetFromUserDefault` / `SetFromSystemDefault` (GR23a/b/c), `SetUserDefaultFromLocale` / `…FromSaved`
(GR22), `Save(userDefault)` → `SavedLocalePointer` (GR26/GR27 — DETERMINATION L4: a typed `ManagedPointer` carrying its
owner and snapshot, numbered monotonically; validity is type + owner, so no handle table is needed), the §14.6.6 r1
COPY at activation (a later user-default change moves nothing until a SET takes it), r3 category independence, r9
run-unit scope. `LocaleIdentification` (L1): `Normalize` (`fr_FR.UTF-8` ≡ `fr_FR` ≡ `fr-FR`; a POSIX `@modifier`
that is a CLDR collation type — `bcp47/collation.xml`'s `co` names and aliases, read from the embedded pack
(`CldrLocaleLoader.IsCollationType`) — becomes `-u-co-type`, any other stays and makes the locale unavailable;
`INVARIANT`/`ROOT`/`C`/`POSIX` → root), `SameLocale`, `IsAvailable` = the ONE known-locale rule. The EC-LOCALE family
joins the ambient statement gates (`CheckingFlags` fields, `ExceptionState.LocaleMissingError` /
`LocaleInvalidPtrError` / `LocaleIncompatibleError`, `EcEmitter.FatalAmbientGates`, `EcBinder` enabling —
INVALID-PTR precisely on `BoundSetLocale`, MISSING and INCOMPATIBLE on any statement in a checking-on region):
EC-LOCALE-MISSING (GR24; and a NAMED `IS LOCALE` sequence whose locale is unavailable, at USE — L1 item 4),
EC-LOCALE-INVALID-PTR (GR21), EC-LOCALE-INCOMPATIBLE (§8.8.4.2.11 — it WAS a raw `ExceptionState.Set` since PB101 that
could neither terminate nor reach a declarative and recorded a status even with checking off, against
§14.6.13.1.1; the PB101 unit test pinned that and is re-pinned to the gated contract). `LocaleCollation` distinguishes
a sequence BOUND to the root ("") from the current-locale form (null) — the first `Snapshot()` probe collapsed ""
to "current" and the snapshot silently followed a mid-sort SET; `CobolCollation.Snapshot()`, `CobolSort.Init(name,
collation)` (the emitter passes the sequence at statement start; `Sort`/`Merge` use the stored one).

**Compiler.** `Binding/Model/LocaleSymbol.cs` — `LocaleSymbol(Name, External, FromLiteral)` with the L1 `Tag`, and
`LocaleRef(Named)` / `LocaleRef.Current` (the ONE "which locale" operand); `LocaleCollatingSpec(LocaleRef)`;
`CollationEmit` renders the named form as `new LocaleCollation("<tag>")`. `DataBinder.Switches`: the `Locales` table
(inherited per §12.3.7.4 GR1), `LocaleBind` (SR10/SR11 through `TryClauseTextLiteral` — the ONE SPECIAL-NAMES
text-literal rule, EXTRACTED from the ORDER TABLE arm so literal-4 and literal-9 are checked by one body; duplicates
COBOLNET1665), `ResolveLocaleName` (COBOLNET1664 — one code, every site, the citing rule named), the named alphabet
arm (SR24). `SetBinder.BindSetLocale` / `BindSaveLocale` → `BoundSetLocale(Categories, SetsUserDefault, Source,
Locale, SavedPointer)` / `BoundSaveLocale(Target, UserDefault)` → `SetEmitter.EmitSetLocale` / `EmitSaveLocale`;
COBOLNET1666 (a duplicate category / a non-category word / USER-DEFAULT beside a category), 1667 (SR25), 1668
(SR27/SR28). Grammar: `SET cobolWord cobolWord+ TO dataReference` (the category set); the predicates:
`localeClauseAhead` is NO LONGER edition-gated — its shape test (the token after LOCALE must be a word, not
IS/ON/OFF) already excludes the '85 switch entry and `LOCALE FR IS "fr_FR"` has no '85 reading, so the clause is
recognized everywhere and `VisitLocaleClause` gates `special-names-locale-2002` (the ORDER TABLE precedent: an
explanatory introduction diagnostic below 2002 instead of a parse error at the clause's literal; the design's §6
bullet is corrected); `setLocaleAhead` scans the category list (the first cut read LT(3)/LT(4) only and the
multi-category form fell through to "'LOCALE' is not defined") and is edition-gated only for the USER-DEFAULT-first
shape (the LC_ words' underscore is not an '85 word character, so `SET LOCALE LC_… TO x` has no '85 reading;
`SET LOCALE USER-DEFAULT TO X` is a legal '85 two-receiver SET); `saveLocaleAhead` un-gated; `VisitSetLocaleStatement`
gates `set-locale-2002` / `set-save-locale-2002`. `EcNameResolution`: the EC-LOCALE refusal deleted.
`constructs.json` +3 rows (registry regenerated); DiagnosticCatalog 1664–1668 (`docs/DIAGNOSTICS.md` regenerated).

**Tests.** Goldens `tests/conformance/2002/pb64t1_locale_declare` (both branches, L1 equivalence witnessed through
three spellings of Spanish, a named Swedish SORT while LC_COLLATE is Spanish), `pb64t1_set_locale_categories`
(r1/r3/GR22/GR23a-c/GR25, the multi-category order-free form), `pb64t1_save_restore_locale` (GR26/GR27/GR23a through
the pointer, the per-category snapshot, EC-LOCALE-INVALID-PTR observed on NULL and on ADDRESS OF — state unchanged),
`pb64t1_ec_locale_missing` (observed at SET and at USE; the interrupted IF keeps VERDICT "?"),
`pb64t1_sort_locale_snapshot` (a SET in the INPUT PROCEDURE: pass 1 root order, pass 2 Spanish),
`pb64t1_locale_scope_and_return` (§12.3.7.4 GR1 into a contained program; §14.6.6 r9 — the callee's SET stands);
eight negatives (1664 ×2, 1665, 1666, 1667, 1668 ×2, 0898 for literal-4); `SetLocaleDispositionTests` rewritten (legal
shapes compile CLEAN with no stray 0901; illegal shapes draw exactly one named rule; below 2002 the introduction
diagnostic, never "'LOCALE' is not defined"; the '85 two-receiver SET still runs); `LocaleStateTests` (9 facts: the
state model, the handle, both conditions with the flags on/off, L1 normalization, availability, the snapshot incl.
`CobolSort.Init`); `SpecialNamesLocaleEditionTests` re-pinned (the clause compiles clean; the locale-NAME slot is still
funneled). The four inverted refusal negatives deleted (`pb25-special-names-locale-a49`,
`pb100-alphabet-locale-phrase`, `pb92-set-locale-format-11`, `pb100-ec-locale-names`); `pb92-set-locale-format-12`
→ `pb64t1-set-locale-pointer-sr28`. T-F: `CompilerUnderTest.RunExit` (the ONE process launcher) pins
`COBOL_USER_LOCALE` / `COBOL_SYSTEM_LOCALE` to INVARIANT for every program the harness runs — a golden that collated
under the default locale would otherwise pass on one regional setting and fail on another. Inventory: 19 rows →
CONFORMS (SR-12.3.7.3-24, GR-12.3.7.4-1/5, SR-14.9.39.3-25..28, GR-14.9.39.4-21..27, GR-14.6.6-1/3/4/5/9; the
`SpecTraceabilityInventoryDriftTests` red on the deleted fixtures' refs was the prompt); GAP 3916 → 3908.

**Docs.** CONFORMANCE.md §4 item 5 (items 9 and 10's clause half + the named alphabet CLAIMED; the determinations;
the EC-LOCALE names legal; three conditions live; the construct rows); DESIGN-locale-facility (§1 re-verdict, §2
rows 1/9/10/11, §4.3 as built, §6 gating correction, §7 codes, §10 T-A/T-F, §12 T1/T3 ✅); kb/Work PB64 (T1 landed;
T4 next); plan §0.

**Gates.** Wave-local GREEN (Conformance 1866 on `~SetLocale|~CorpusRunnerTests&pb64t1|~Manifest|~VersionMatrix&locale|
~Alphabet|~StandardCompare|~Sort|~Ec` · Unit 4452/4452 · Char 33/33); the comprehensive battery (#23) on this tree
follows in the commit message / §0. Next: **T4** — `LOCALE-COMPARE`, `LOCALE-DATE`, `LOCALE-TIME`,
`LOCALE-TIME-FROM-SECONDS` + `LocaleFacts` (design §4.7/§4.8), then T5, T6.

