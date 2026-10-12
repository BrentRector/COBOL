## Entry 949 — 2026-07-21 14:20 PDT — Wave D (cont.): FLAG flagging Incr 0 — the core pipeline + the two syntactic detectors (READ-PREVIOUS, CLOSE NO REWIND/UNIT); COBOLNET1620/1621/1622

Implemented Increment 0 of the FLAG-02/FLAG-14 migration-flagging subsystem from the design SSOT
(`DESIGN-flag-directives.md`) — the full shared pipeline plus the two purely-syntactic detectors, proving both
directives and both warning codes end-to-end.

**A design correction found during implementation (folded into the doc, same change set).** The census had the
bound detectors as a bound-tree walk. But `BoundStatement` carries NO uniform source line (only a few nodes hold a
`SourceLine` for DEBUG-LINE; `BoundTerminate` has none), and the flag fold is line-sensitive (GR2 — a flag applies
to the text FOLLOWING its directive). The parse tree, by contrast, has `ctx.Start.Line` on every node, anchored to
the same final-text lines as the collected `FlagEvent`s. So `FlagConformancePass` is a PARSE-TREE visitor (the same
generated ANTLR base visitor VCP's ParseArm uses), not a bound walk — D2 rewritten. A bonus: FLAG-02 c I-O-STATUS-07
is parse-visible (`closeOption` = UNIT or NO REWIND), so it needed NO `BoundClose` model change and moved from the
planned Incr 3 up to Incr 0.

**The pipeline (three seams, each on a proven template).** (1) COLLECT — `FlagDirective.cs` (enums + the ONE
`FlagOptions` catalog of all 17 options across both directives + the `FlagDirectiveLine` operand parser + the
`FlagEvent` record) and `FlagDirectiveProcessor` (a clone of `RefModZeroLengthDirectiveProcessor`: scans the FINAL
text, parses each `>>FLAG-nn {ALL|opt…}{ON|OFF}` into a line-anchored event, reports a malformed operand, blanks the
line — H3 line-count-preserving). Wired via a `leaveFlagDirectives:true` arm in `ConditionalCompilationProcessor`
(FLAG-02/14 stay in `KnownIgnoredDirectives` so legacy callers still consume them) + a new stage in
`Frontend.Preprocess`, exposing `Frontend.FlagEvents`. (2) FOLD — `FlagState` (a per-option clone of
`RefModZeroLengthState`): `IsOnAt(line, option)` folds "last toggle strictly before the site wins, default OFF",
with ALL fan-out (empty option list) and ALL-OFF reset, isolated per directive. Threaded `FlagEvents` →
`CompilerDriver` → `CSharpEmitter.Bind` → `BinderDriver`. (3) EMIT — `FlagConformancePass.Run(group, flagState,
sink)` invoked right after the `GroupTail` loop (a sibling to VersionConformancePass, NOT a manifest pass — flagging
is directive-driven + always-Warning + fires regardless of `--std`); `VisitReadStatement`
(`readDirection().PREVIOUS()`) and `VisitCloseStatement` (`closeOption` UNIT / NO REWIND) emit per-option Warnings
gated by `flagState.IsOnAt(ctx.Start.Line, option)`. Zero-overhead: no `>>FLAG` line ⇒ `FlagState.Empty` ⇒ no walk.

**Diagnostics.** COBOLNET1620 flag-02-incompatibility (Warning), 1621 flag-14-incompatibility (Warning) — the
per-directive channels carrying per-option ConstructId/Message/Citation — and 1622 flag-directive-malformed (Error).
DIAGNOSTICS.md regenerated.

**Gate.** Build 0W/0E · 16 new `FlagDirectiveTests` (parser, fold incl. ALL/ALL-OFF/cross-directive-isolation, and
end-to-end compile: ON emits, absent/OFF do not) all green · characterization 33/33 byte-exact (the threading
changed no existing output) · diagnostics/construct/VCR drift 17/17 · CLI-probed (both warnings fire; absent and
OFF are clean). NEXT = Incr 0b (the directive-word edition gates: >>FLAG-14 = 2023 introduction, >>FLAG-02 = 2014
intro / 2023 obsolete, via ConstructRegistry + constructs.json rows).

