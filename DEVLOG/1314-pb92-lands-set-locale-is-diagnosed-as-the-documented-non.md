## Entry 1314 — 2026-08-18 09:16 PDT — PB92 lands: SET LOCALE is diagnosed as the documented non-support it is

**PB92** (found sweeping PB78's siblings — CONFORMANCE.md §4 item 5 had it on record). `SET LOCALE LC_ALL TO
USER-DEFAULT.` bound as a generic SET of a data item named LOCALE: "'LOCALE' is not defined" plus two false 0901s
about the format's own keywords; `SET X TO LOCALE LC_ALL.` was `unexpected '.'`. §14.9.39 Formats 11 (set-locale)
and 12 (save-locale) are Annex A.4.9 item 9 of the optional locale module COBOL.NET does not provide, and rejecting
an unclaimed optional element is conformant (§4.2.7 / §A.4.1) only when it is DIAGNOSED as such.

- `setLocaleStatement`, listed FIRST in `setStatement`, predicated on the word texts (`setLocaleAhead()` — LOCALE
  + a locale category or USER-DEFAULT + TO; `saveLocaleAhead()` — LOCALE + LC_ALL / USER-DEFAULT after TO), both
  edition-gated 2002+ exactly like `localeClauseAhead` (LOCALE / LC_* / USER-DEFAULT are §8.9 reserved words from
  2002; at '85 they are user words and `SET LOCALE TO 5` over an integer item named LOCALE keeps its Format-1
  reading — pinned). `SetBinder.BindSet` emits ONE COBOLNET1518 naming the format; the two keyword words are exempt
  from the §8.9 funnel (the PB27 shape, as for the LOCALE clause and PB78's CHARACTER CLASSIFICATION).
- **A grammar lesson, learned in one red gate.** The first cut wrote the predicates MID-alternative (`SET
  {setLocaleAhead()}? cobolWord …`, `SET dataReference TO {saveLocaleAhead()}? …`). ANTLR hoists only LEFT-EDGE
  predicates into prediction; a predicate after SET is asserted when the alternative is entered — so prediction
  chose `setLocaleStatement` for `SET IDX-1 TO SUB-1.` (the arms share the SET … TO … prefix with every other SET
  form) and every NIST program with a SET died with "rule setLocaleStatement failed predicate". Both predicates
  are now left-edge (`{setLocaleAhead()}? SET …` looking at LT(2..4); `{saveLocaleAhead()}? SET …` walking to the
  first TO before the period), a false predicate makes the alternative non-viable, and the ordinary forms are
  chosen as before — 3404 conformance tests including NIST green.
- Measured: negatives `pb92-set-locale-format-11` / `-12`; `SetLocaleDispositionTests` (five statement shapes → exactly
  one 1518 each; the '85 control runs). Nine verdicts (SR-14.9.39.3-25/-26, GR-14.9.39.4-21…-27) →
  DOCUMENTED-NON-SUPPORT, GAP 3989 → 3980. CONFORMANCE.md §4 item 5 now says every locale entry point is recognized
  and rejected by name.

**Gates (wave-local).** Solution build (grammar) · Characterization 33/33 · Conformance
`Corpus|Set|Locale|VersionMatrix|Nist|Reserved|Pointer|Ptr|Index|Switch|Oo` **3404/3404** · Unit
`Grammar|Parser|Registry|Manifest|Guard|SpecTraceabilityInventory|ReservedWord|Set` **154/154** · CLI probes. Docs:
CONFORMANCE.md §4 item 5, kb/Work PB92 → landed, plan §0. Battery owed for the PB92 batch with the next landing
(battery #12, tree `7d8a33a8`, was green once its ONE differential flip was attributed: Conformance 4707/4707 · Unit 4221/4221 ·
Characterization 33/33 · NIST 353/0 audit-clean · `configuration:885` WE_REJECT_THEY_ACCEPT → AGREE_ACCEPT — GnuCOBOL's
ebcdic-table case writes `OBJECT-COMPUTER.` + `PROGRAM COLLATING SEQUENCE IS an-ebcdic.`, exactly PB78's form; the
baseline row is flipped in this commit).

