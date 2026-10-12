## Entry 950 — 2026-07-21 14:45 PDT — Wave D (cont.): FLAG flagging Incr 0b — the directive-word edition gates (>>FLAG-14 = 2023 intro; >>FLAG-02 = 2014 intro / 2023 OBSOLETE), through the ONE ConstructRegistry

Added the directive-WORD edition gating for the FLAG directives — closing the gap where `>>FLAG-14` was silently
accepted below 2023 — through the SAME `ConstructRegistry` funnel every other construct uses (the owner's
"uniform handling for all statements as the spec adds/deprecates/removes them"), NOT a bespoke gate.

**An owner spec-fidelity question resolved against the PDF.** The question: "if FLAG-02 was removed in 2023, why
produce a warning?" The answer, confirmed by rendering PDF page 100 (§7.3.14.1) and reading §4.2.13: FLAG-02 is
NOT removed in 2023 — it is **obsolete** ("an obsolete element in this Working Draft … to be deleted from the
**next** edition"). Per §4.2.13 an implementation **shall SUPPORT** obsolete elements (so it must still compile,
not reject) and **shall provide a warning mechanism** to flag their use. So the spec-correct model is
`introducedIn:2014, obsoleteIn:2023` → 0900 (error) below 2014, clean 2014–2022, **COBOLNET0903 obsolete WARNING**
at 2023 — a `removedIn:2023` → 0902 rejection would violate §4.2.13. (F.2, which §4.2.13 says lists obsolete
elements, does NOT name FLAG-02 — an ISO gap; the §7.3.14.1 NOTE is the direct authority. And §4.2.13's "optionally
may be invoked by the user" is honored by our existing all-0903-by-default convention, a separate cross-cutting
question, not FLAG-specific.)

**Implementation.** Two `constructs.json` rows: `flag-14-directive-2023` (a clean `introducedIn:2023`, templated by
`ref-mod-zero-length-2023`) and `flag-02-directive-2014` (`introducedIn:2014` + `obsoleteIn:2023` — the FIRST row
in the matrix to combine introduction and obsolescence; `ConstructRegistry.StatusAt` already handles it:
NotYetIntroduced < 2014, Available 2014–2022, Obsolete ≥ 2023). Regenerated `Constructs.g.cs` /
`ConstructRegistry.g.cs`. `FlagDirectiveProcessor` now edition-gates each directive word via
`ConstructRegistry.Check(EditionInfo.Of(dialectLevel, permissive), BagSink, …)` (the RefModZeroLength precedent),
threaded `DialectLevel`/`Permissive` from `Frontend`.

**Gate.** Build 0W/0E · CLI-probed all four editions (FLAG-02: 85→0900 reject, 2014→clean, 2023→0903 warn-and-
compile; FLAG-14: 2014→0900 reject, 2023→clean) · ConstructRegistry/Diagnostic drift + FlagDirectiveTests 33/33
(registry↔json agree incl. the dual-window) · characterization 33/33 byte-exact · the version-matrix suite
(every construct × 4 editions, incl. the two new rows' Construct_MatchesEditionExpectation + FLAG-02's
ObsoleteConstruct_CompilesEverywhere_WarnsFromObsoleteEdition). NEXT = Incr 1 (the data/VALUE + WRITE + TERMINATE
option detectors).

