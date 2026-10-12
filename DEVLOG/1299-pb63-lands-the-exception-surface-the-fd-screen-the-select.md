## Entry 1299 — 2026-08-18 01:40 PDT — PB63 lands: the exception surface — the FD screen, the SELECT-spelled name, one access-recording path, the r2b procedure field, and the MODULE-NAME determinations

**All 19 PB63 rows CONFORMS; GAP 4067 → 4048.** A family of small defects clustered on one surface, each with
its own mechanism, landed together:

- **`BindExceptionFileArg`** matched the bare name and stopped — §15.28.3 r1 / §15.29.3 r1 say "the name of a file
  connector that is specified in an FD statement", and an SD or a SELECT with no FD compiled clean and answered
  r2a's two spaces. It now requires `HasFd && !IsSortMerge` and cites the function's OWN clause (the -N twin's
  message said §15.28.3). Three negatives.
- **The SELECT-spelled name.** Both EXCEPTION-FILE forms recovered the display name from the registry KEY by
  `LastIndexOf("::")` — the key is an emit-side namespace (`PROG::`, `::EXT::` + the UPPERCASED external name,
  `Class::INST::` + a per-object `#N`), so `SELECT MixedExtF … FD … IS EXTERNAL` answered `10MIXEDEXTF`. §15.28.4
  r1c/r2b: "the file-name exactly as specified in the SELECT clause". `FileModel.SelectName` (set where the model is
  made, never qualified, never folded) now rides to `FileConnector.SelectName` through a trailing `selectName:`
  argument on every `CobolFile.Register*` (the four emitter sites, `RuntimeApi.FileRegister*`), and both forms read
  it — `FileRegistry.ExceptionFile` directly, `EcFunctions.File` through `CobolFile.SelectNameOf(key)`; both strips
  are deleted. The emitted-C# snapshot `char_seq_file` re-baselined for the one new argument.
- **One access-recording path.** r2a's "never been opened, attempted to be opened, or otherwise attempted to be
  accessed" was a flag written at two hand-picked sites (OPEN, DELETE FILE) — every other verb was wrong by
  default: a CLOSE on a never-opened connector (status 42) and a READ (47) still answered two spaces though the
  FILE STATUS item proved the access. §9.1.13.1 names the statements that set the I-O status; the
  `FileConnector.Status` SETTER now records the access, so all 105 assignment sites are the one path and the two
  hand sites are gone. Measured: `EFC=[42Closed]`, `EFR=[47Readf]`, `EFU=[  ]`.
- **EXCEPTION-LOCATION's three parts.** §15.30.3 r2b: (1) the element "as specified in the FUNCTION-ID,
  METHOD-ID, or PROGRAM-ID paragraph of the … element containing the statement" — a statement inside a method
  named the CLASS-ID; `OoMethodScope.MethodName` (set where the method's scope is made) is now the element
  (`M=[SHOWLOC; M-PARA; 68]`); (2) the procedure field's three shapes — the paragraph-name-OMITTED paragraph
  (§14.4.3) carried a display placeholder `(sentences without a paragraph-name)` where the standard defines an
  EMPTY field, and the section-only case printed the placeholder `OF ONLY-SECT`; `ProcedureTableBuilder` now
  stores the empty name (the dispatch comment supplies its own label) and `EcBinder.EcLocation` builds (a) `"; ; "`,
  (b) `IN-PARA OF ONLY-SECT`, (c) `ONLY-SECT` — the `para.Equals(sec)` heuristic is gone; (3) the line identifier
  is written down as a determination (below).
- **MODULE-NAME.** `ModuleStack.Name`'s TOP-LEVEL arm returned frame 0 unconditionally; r10 says "the runtime
  element that was activated by the operating environment", and a .NET host that drives a COBOL element without
  `RunMain` leaves frame 0 an ordinary CALLed frame — the arm now tests `IsMain` and otherwise answers r3's
  documented single space. That configuration is exactly the one `cobol.exe --run` cannot reach, so it is pinned by
  the new `ModuleStackInvariantTests`, which also carries a 300-character element name through a 62-deep STACK
  untruncated (r2's EC-BOUND-FUNC-RET-VALUE antecedent is structurally unreachable — now a pinned fact rather than
  an unstated one) and pins r1's no-trailing-spaces at depth. The three FIXED-upheld rows (r1 lengths, r5 the
  method-as-activator, r6 the nested-program choice) get their golden, `pb63_module_name_lengths`, and their A.1
  register rows: **133** (length characteristics), **134** (a non-COBOL element: a single space), **136** (the main
  is the element `RunMain` started — the only `IsMain` frame), **137** (ACTIVATING from a nested program: the nested
  program's own name, so r9's STACK chain agrees by construction).
- **The line identifier — a determination, and a defect found through it.** §15.30.3 r2b3's "implementor-defined
  identifier of the source line" was the ANTLR token line of the RESULTANT text after COPY (a 3-line COPY on line
  6 makes a RAISE on source line 9 report 14) — recorded nowhere. It is now a CONFORMANCE.md ⚖ determination (the
  resultant-text line, §7.2; equal to the source line without COPY/REPLACE) — and measuring it exposed something
  larger: **bind-time diagnostics carry NO source position at all** (`error COBOLNET1639: 'UNDEFINED-ITEM' is not
  defined …` — no file, no line), and every reported line after a COPY is a resultant-text ordinal. Registered as
  **`kb/Work/PB82`** (MAJOR usability; `process_only` for the ranker since it changes no program's behavior): the
  fix shape is a statement cursor the binder's `Error/Warning` stamp automatically plus an origin line map through
  the preprocessing chain — one mechanism each, no per-site edits.

**Also:** the LENGTH/BYTE-LENGTH folds no longer cascade a second, misattributed "is a numeric literal" report over
an argument that already failed to bind (`BoundOperandError` passes through). The FMT-15.30.2/31.2/32.2/33.2
rows the sweep found FIXED are pinned in `pb63_exception_location_procedure_field` (keyword-omitted
EXCEPTION-LOCATION, `EXCEPTION-LOCATION-N (1:7)`, WRITE … FROM FUNCTION EXCEPTION-STATEMENT, INSPECT / INITIALIZE
over FUNCTION EXCEPTION-STATUS). (`PF` is the §8.9 report-writer word — a probe of mine tripped over it, not the
compiler.)

**Gates (wave-local).** Solution build · Characterization 33/33 (after the `char_seq_file` re-baseline and a
`RuntimeApiGuardTests` false positive on `.CobolName.` hoisted into a local) · Conformance
`Corpus|Negative|Exception|Ec|Module|File|Io|Intrinsic|Oo|SpecTraceability` **3942/3942** · Unit
`SpecTraceabilityInventory|Ec|File|Module|Diagnostic|Manifest|Intrinsic` **3107/3107** + the 3 new invariant tests ·
CLI probes. Battery owed for the PB62+PB63 batch. Docs: COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md (the location
string and the EXCEPTION-FILE paragraphs), CONFORMANCE.md (A.1 133/134/136/137 + the ⚖ line-identifier row),
kb/Work PB63 → landed, PB82 new, plan §0.

