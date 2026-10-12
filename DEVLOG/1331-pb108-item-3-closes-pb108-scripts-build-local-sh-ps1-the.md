## Entry 1331 — 2026-08-19 10:45 PDT — PB108 item 3 (CLOSES PB108): `scripts/build-local.{sh,ps1}` — the wave-local gate as one command: build the SOLUTION, then Conformance on a REQUIRED filter, full Unit, Characterization; a bare `~X` filter term silently matches nothing in vstest (exit 0) — the wrapper expands the shorthand and is RED on a missing verdict line

**What landed.** Two thin twins (bash + pwsh): `dotnet build CobolSharp.sln` (Debug — the binaries `battery.sh`
PHASE 0 builds; never a `--no-build` leg on an unbuilt tree, `feedback_fresh_build_before_no_build_test`) →
`dotnet test tests/Cobol.Net.Tests.Conformance --no-build --filter "<filter>"` → full Unit → Characterization, and
a GREEN/RED verdict line. The filter is REQUIRED (bash: usage + exit 2; pwsh: `[Parameter(Mandatory)]`) because the
wave-local gate is a filter chosen from what the change TOUCHES (plan §0 Gates; the PB36 `~Corpus`-only lesson) — a
default would re-create that mistake. NOT `battery.sh`, NOT `guard-fast.sh`, NOT Release.

**Two traps found while firing it.** (1) vstest's filter grammar needs a property on EVERY term:
`--filter "~CollationKeyCache|~GraphemeBreaker"` and even `"FullyQualifiedName~A|~B"` report "No test matches" —
and (2) `dotnet test` **exits 0 on zero matches**, so a raw shorthand filter is a silent green. The wrappers expand
each property-less term (`(^|[|&(])(!=|=|~)` → `FullyQualifiedName…`) and treat a leg with NO `Passed!/Failed!`
verdict line as RED ("the filter matched no test — a run must assert its population",
`feedback_verdict_evidence_invariant`). Plan §0 Gates and §9 now name the wrappers and carry the ⛔ on the bare
shorthand (the §0 prose example `~Corpus|~ModuleName` is shorthand the wrapper accepts, not a raw filter).

**Fired, both twins:** no filter → usage + non-zero; `~NoSuchTestAnywhere` → `conformance: NO VERDICT LINE` + RED
(exit 1) while Unit 4431/4431 and Characterization 33/33 still ran; the batch's real filter
`~Collation|~Locale|~StandardCompare` → Conformance 17/17 · Unit 4431/4431 · Characterization 33/33 → GREEN in
2 m 32 s. **PB108 CLOSED** (`status: landed`; items `469fd12b`, `7ab0b2a5`, this). The battery reference stays #22:
PB108 is tooling plus two behavior-neutral, unit-covered constant extractions; it rides wave-local into the T1
batch. Next: T1 — `SET LOCALE` Format 11 / the save-locale Format 12, `LocaleSymbol`/`LocaleRef`/`LocaleFacts` over
the ONE `LocaleState`, per `DESIGN-locale-facility.md` §12.

