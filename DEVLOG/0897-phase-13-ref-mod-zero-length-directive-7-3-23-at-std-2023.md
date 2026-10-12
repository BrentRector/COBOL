## Entry 897 — 2026-07-18 18:16 PDT — PHASE-13 — REF-MOD-ZERO-LENGTH directive (§7.3.23) at --std 2023 — the zero-length ref-mod allowance

Landed the `>>REF-MOD-ZERO-LENGTH {ON | OFF}` compiler directive (COBOL-2023, Annex E.3.3 item 23) — the greenfield-only
follow-on to the already-landed EC-BOUND-REF-MOD raise. When ON, a reference-modification result **may** be zero-length
(§8.4.3.3.4 item 5c: "length shall result in a positive nonzero integer, **unless the REF-MOD-ZERO-LENGTH directive is
set to ON, when the result may also be zero**"); the §7.3.23.3 GR1 default (OFF/omitted) keeps raising the fatal
EC-BOUND-REF-MOD on a zero-length result. The directive relaxes **only** the zero-length aspect — an out-of-range
leftmost/length still raises regardless.

**Spec derivation (rule #1).** Read §8.4.3.3.4 item 5b/5c + the trailing GR paragraph (spec :7089) and §7.3.23.1–.3
directly. Confirmed the governing § is **§8.4.3.3.4** — the remaining-waves scout's section-(2) header cites §8.4.2.3
(that id resolves elsewhere); the landed raise commit's §8.4.3.3.4 citation is the correct one, so the code + goldens
cite §8.4.3.3.4. OFF is the underlined default (§7.3.23.2).

**Design (mirrors the >>TURN / >>PROPAGATE directive-stage pattern — the singular pattern).** A new
`RefModZeroLengthDirectiveProcessor` (frontend) runs LAST on the final preprocessed text (line-count preserving, the
>>TURN H3 discipline), collects `RefModZeroLengthEvent(Line, On)` toggles, and edition-gates the directive through the
ONE `ConstructRegistry` (a new `ref-mod-zero-length-2023` constructs.json row → **COBOLNET0900** below 2023, so the
construct enters the version matrix and the message is single-sourced). `ConditionalCompilationProcessor` keeps
REF-MOD-ZERO-LENGTH in `KnownIgnoredDirectives` (legacy still consumes it) + gains a `leaveRefModZeroLengthDirectives`
flag so the greenfield caller lets it survive to the new stage — the exact TURN/PROPAGATE idiom. A `RefModZeroLengthState`
line-fold (mirror of `TurnState`, strict `Line < siteLine`, last-wins, OFF default) threads Frontend →
`BinderDriver.Bind` → `BindSession.RefModZeroLength` → `DataBinder` → `ReferenceResolver`, which sets a new **init-only**
`RefModPlace.AllowZeroLength` from the fold at the ref-mod's own source line. `PlaceRenderer` emits the `allowZeroLength:`
argument to `StrRefMod`/`StrSpliceInto` **only when true**, so `CobolString.RefMod`/`SpliceInto` suppress the
`length == 0` disjunct of the raise test exactly there. Directive concern (is a zero-length result legal?) stays cleanly
separate from EC checking (should a violation raise?).

**Byte-neutrality.** `AllowZeroLength` is init-only (no positional-arity change — every existing `RefModPlace`
construction/deconstruction untouched) and defaults false ⇒ `StrRefMod` emits the pre-existing 3-arg form ⇒ every
existing ref-mod site is byte-identical (characterization **33** byte-exact; the RuntimeApi ratchet accepts the
defaulted optional-param additions). The change is greenfield-only, but it touches the shared-frontend
`ConditionalCompilationProcessor` (a default-false param, legacy path provably unchanged) — full legacy guard run.

**Diagnostics.** Intro gate **COBOLNET0900** (via the registry row). Malformed operand (`>>REF-MOD-ZERO-LENGTH FOO`) →
**COBOLNET1573** — the 087x/088x/089x directive-syntax bands are full, and 1570/1572 are Wave G CLASS A earmarks, 1571
is Wave F; 1573 is the next unreserved. Verified before commit.

**Golden.** `tests/conformance/2023/ref_mod_zero_length` (GreenfieldOnly via the 2023 manifest) proves BOTH the
allowance (ON + `WS-X(3:0)` → no EC, receiver space-filled, status clear) AND the "relaxes only zero-length" property
(a following out-of-range `WS-X(7:2)` STILL raises → USE-F3 declarative → RESUME AT NEXT). CLI-probed all four
quadrants: {ON,OFF}×{zero-length,out-of-range}. Below-edition negative is the matrix row (85/2002/2014 → COBOLNET0900).

**Latent-miss fix (surfaced by the full legacy guard).** The legacy `ConformanceTests.Conformance` (Integration) runs
the SAME `tests/conformance` corpus through the frozen legacy engine, skipping greenfield-only goldens via a
`GreenfieldOnly` set. The new `ref_mod_zero_length` needed an entry — but running the full Integration suite ALSO
surfaced two PRE-EXISTING misses: `ec_bound_ref_mod` (added by `3eebcfd1`) and `ec_bound_overflow` (added by
`e574af41`) both landed WITHOUT their `GreenfieldOnly` entries (neither commit touched `ConformanceTests.cs`), so they
had been red in the legacy Integration ConformanceTests ever since — a [[feedback_legacy_suite_on_shared_corpus]]
violation the earlier wave-local gates missed. All three exclusions are added here (EC/2023 goldens the frozen legacy
can neither compile nor match); the greenfield CorpusRunnerTests still byte-compares all three.

