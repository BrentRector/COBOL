## Entry 1316 — 2026-08-18 10:56 PDT — PB82: every diagnostic names the file, line and column the user edits — the diagnostic cursor and the origin line map

**What landed.** kb/Work **PB82** (MAJOR, usability): a bind-time diagnostic used to be a bare
`error COBOLNET1639: 'UNDEFINED-ITEM' is not defined …` — no file, no line, no column — because the ~160
`EditionContext.Error/Warning` sites pass no location and the string channel accepted none; and every position
that WAS reported (the parser's, `FUNCTION EXCEPTION-LOCATION`'s third part, DEBUG-LINE) was an ordinal of the
RESULTANT text after COPY / REPLACE / continuation processing (§7.2), not a line of the file on disk. Two halves,
one mechanism each, both structural so no report site changes and a new one cannot forget:

1. **The origin line map** (`Frontend/Common/SourceLineMap.cs`). `SourceOrigin(File, Line)`; `MappedText` (text +
   one origin per `Split('\n')` piece — the constructor asserts the invariant, so a stage that breaks it throws
   instead of misattributing every later line); `OriginWriter` (an output piece takes the origin of the FIRST
   content written into it); `SourceLineMap` (resultant line → origin; `Locate`/`SourceOrigin.ToLocation` = the
   ONE conversion to the 0-based `SourceLocation`). Every line-count-changing stage is now MAPPED and its string
   overload is the mapped one's `.Text` (one implementation each): `NormalizeToFreeFormMapped` /
   `ConvertFixedToFree(..., origins)` — a fixed-form continuation join strips the previous output line's newline
   and REOPENS its origin, so the joined line keeps its head line's number (the first cut inferred the join from
   the builder shrinking, which a longer continuation defeats — the join point itself now hands the origin back);
   the CC driver's `Render(MappedText)` / `RenderCopybook`; `CopyProcessor.ExpandCopiesOneLevel(MappedText)` (the
   framing newlines belong to the COPY line; `NormalizeCopybookMapped` carries the copybook's path and physical
   lines; the legacy `ExpandCopyStatements` is now the one-level expander fed with itself — its duplicate loop is
   gone); `ApplyReplaceStatements` / `ApplyReplacements` mapped (a REPLACE statement's lines vanish, kept text keeps
   its origins, a replacement takes the origin of the line its match started on); `StripNistArchiveMarkers` blanks
   a marker line instead of dropping it. `Frontend.Preprocess` returns the `MappedText`, publishes
   `Frontend.LineMap`, asserts every later stage (NIST substitution + the six directive stages) is line-count
   preserving, and hands the map to `CobolErrorListener` (which also passed ANTLR's 1-based line straight into the
   0-based `SourceLocation` — every syntax error was reported one line LATE), to the six directive stages'
   diagnostics (`lineMap.Locate(i + 1, …)`), and via `CompilerDriver` to the binder. The frontend's own reporters
   were swept to the same origin discipline: the fixed-form continuation gates (1-based line into the 0-based
   location — the same off-by-one), the CC `DirectiveDiag` (which reported an INDEX into whatever text was being
   rendered — a copybook directive's line against the main file's name; it now carries the directive line's
   `SourceOrigin`), and `CopyProcessor`'s missing/circular/depth/REPLACING/nested-COPY reports.
2. **The diagnostic cursor.** `IDiagnosticSink.Cursor` (`Cobol.Net.Editions/DiagnosticCursor.cs` — a default
   no-op interface member; a `DiagnosticCursor(Line, Column)` in RESULTANT space) positioned with
   `using var _ = sink.At(…)` (ints in Editions; parse context / token / `DataItem` forms in
   `Binding/DiagnosticCursorAt.cs`; a struct scope, no allocation). `EditionContext` implements it for real:
   `Error`/`Warning` stamp `{file}({line},{col}): ` AUTOMATICALLY through `OriginOf` (the map, else identity on
   `SourceFile`); no cursor ⇒ the bare form (a diagnostic about the unit as a whole — never a fabricated line 1).
   Set points: `StatementBinder.BindStatement` (every statement; a nested statement restores the outer),
   `DataBinder.BindEntries` (every entry) plus the FD/SD, SELECT, I-O-CONTROL, SPECIAL-NAMES, OBJECT-COMPUTER,
   RD / report-group and USING-parameter loops, `ProcedureTableBuilder` (declarative sections, procedure units),
   the post-build model passes at `DataItem.DeclaredAt` (captured from the cursor when the item is bound; TYPE /
   SAME AS clones carry their template's) — REDEFINES/RENAMES resolution, ODO, ANY LENGTH, TYPE/SAME AS
   expansion, the strong-type checks, the index/usage walk — and the two parse-tree passes
   (`VersionConformancePass.ParseArm`, `FlagConformancePass`) through the new
   `Validation/CursorFollowingVisitor` base (the cursor follows the walk and is restored on exit, so a gate fired
   before OR after `VisitChildren` names the visited construct). `EcLocation` (EXCEPTION-LOCATION's third part)
   and the bound tree's DEBUG-LINE `SourceLine` fields (`BinderContext.SourceLine(ctx)`, `_paraLine`) map ONCE at
   production; the `>>TURN` / `>>FLAG` / `>>REF-MOD-ZERO-LENGTH` anchors stay in resultant space by design (their
   event lines are compared with token lines).

**Determination revised** (docs/CONFORMANCE.md §4): EXCEPTION-LOCATION's "implementor-defined identifier of the
source line" (§15.30.3 r2b3) is now the line in the file that PHYSICALLY holds the statement — a bare number for
the main source, `copybook-name(line)` (the diagnostics' own `file(line)` shape) for COPY-incorporated text.
GnuCOBOL prints only a line; the file name is the extra COBOL.NET keeps for the ambiguous case.

**Evidence.** `2023/pb82_exception_location_source_line` — a fixed-form program (kept within column 72 so the
detector reads it as fixed) with a 3-line data COPY, a REPLACE statement and a fixed-form continuation before its
RAISEs and a RAISE inside a procedure copybook: `A=[PB82LOC; ; 35]`, `B=[PB82LOC; ; 40]`,
`P=[PB82LOC; ; pb82loc_proc.cpy(2)]` (the resultant ordinals would have been 37, 41, 46).
`DiagnosticPositionTests` (10: the parse line — was +1; `b.cob(7,12): error COBOLNET1639`; the entry line; COPY
main lines stay main and copied lines name the copybook, for a bind error and a syntax error; REPLACE; the
continuation; the NIST marker; an edition gate at 85; the bare no-cursor form). `SourceLineMapTests` (8: the
`MappedText` invariant, `OriginWriter` splices, the continuation join's head-line origin, the marker strip, COPY
and REPLACE origins through the public driver, the cursor prefix + nested restore + unset-cursor keep, the
identity fallback). Characterization diagnostic snapshots re-baselined for the three `char_neg_*` negatives (the
prefix is the intended change — repo-relative paths, portable). `FlagDirectiveTests`' 22 `StartsWith("warning
COBOLNET162x")` assertions became `Contains` (the flag pass now positions its warnings).

**Found on the way, registered, NOT fixed here.** **PB93** — `01 CPY-B REDEFINES NOPE PIC 9(2).` with no `NOPE`
compiles clean (`ResolveRedefines` reports only the method-scope miss; the program-scope miss leaves
`RedefinesTarget` null against a set `RedefinesTargetName`, so the layout consumers disagree). **PB94** —
`01 V PIC 9 VALUE "abc".` reaches Roslyn (`CS0103: The name 'abcL' does not exist`) and `01 X PIC X(2) VALUE 12.` /
`01 W PIC 99 VALUE "7".` compile silently: §13.18.63.3 SR2/SR4 (literal class vs subject category) are not
enforced for the display categories — the entry-cursor test had to use the SR2 RANGE violation (`PIC 9 VALUE
123`, which IS rejected) instead.

**Friction, honestly.** The Bash tool's heredoc mangles `\\n` inside python patch strings (again — the third
session running); every patch script went through the Write tool. A wave-local gate that included `Corpus` in
its filter ran the whole golden corpus and outlived the tool timeout, which killed it and its testhosts — re-run
detached with `nohup` and a narrower filter (`DisplayName~pb82|…` for the goldens that matter).

**Battery #13 (tree `3d9e04de`, PB92 + PB81 batch, run before this landing): Conformance 4716/4716 · Unit
4221/4221 · Characterization 33/33 · NIST 353/0 audit-clean · differential 1323 cases, 0 flips. ALL GREEN.**
Battery #14 owed for this landing.

