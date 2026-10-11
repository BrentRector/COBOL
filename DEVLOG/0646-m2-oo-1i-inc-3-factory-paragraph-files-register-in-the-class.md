## Entry 646 — 2026-07-06 21:22 PDT — M2-OO-1i inc 3: FACTORY-paragraph files register in the class singleton (+ a class-file `using` root-cause fix)

Increment 3: an OBJECT/FACTORY paragraph may own an INPUT-OUTPUT SECTION + FILE SECTION (§12.4.3 SR1 / §13.4.3 SR1);
this lands the FACTORY half. The factory FILE gate (`CSharpEmitter.Oo.cs`, was COBOLNET0899 "not yet implemented")
is removed; factory file connectors now qualify + register + are usable from factory method bodies.

- **`OoQualifyClassFiles(cls)`** — the OO analogue of the per-program connector-namespace qualification
  (Call.cs:138-142), called for every class after it. A factory file (the class singleton, §9.3.14.2) keys
  `Class::FACT::name`; an EXTERNAL class file keys `::EXT::name` (§13.18.22.4 GR4a — inc 5). (Instance branch: inc 4.)
- **`OoEmitFileMembers(csName, data, w)`** — emitted in `OoEmitTypeHalf` right after `fields.Emit()`: a class that
  declares host files gets an emitted parameterless constructor whose body is the reused `EmitFileRegistration`
  (over `_ctx.Data`, each file addressed by inc-2's `FileKeyExpr`). A FACTORY file registers once in the singleton's
  ctor (`__Instance = new()`); zero host files ⇒ NO ctor emitted (byte-identical to a file-less class). FD records
  already emit as fields (factory-type fields, D11) and method bodies resolve the file via `cls.FactoryData.FilesByName`
  (§11.7.4 GR5), so no other binder/emit change was needed.
- **GLOBAL guard (COBOLNET1520)** — a factory `FD … IS GLOBAL` is rejected: §13.18.27.3 SR4 bars GLOBAL in a
  factory/instance/method definition (GLOBAL is nested-PROGRAM containment; program↔class sharing is EXTERNAL only,
  §9.1.5). Object-half guard lands with inc 4.

**Root-cause fix (caught by an incremental compile-and-run, not the design):** the `anyFiles` flag that gates both
the `using CobolNet.Runtime.IO;` import and the entry wrapper's `CobolFile.Init`/`CloseAll` scaffolding counted
PROGRAM-unit files only — a class-only-file program emitted `CobolFile.Register`/OPEN with no `CobolFile` in scope
(CS0103). `anyFiles` now also counts `classes.Any(c => c.Data.Files.Count > 0 || c.FactoryData.Files.Count > 0)`, so
a class-file program gets the import and the run-unit `CloseAll` backstop (§9.1.4). The recon design had not flagged
this — the standing lesson that emit only proves itself when you run it (feedback_verify_demo_output).

Golden `oo_factory_file` (a FACTORY FD round-tripped by a factory method → `FGOT=FACTORY DATA`; GreenfieldOnly —
legacy can't parse OO) + the `FactoryFile_Global_1520` OoSpineTests fact. Battery: **1965 conformance (+2) · 216 unit
· legacy integration 76 GREEN** (the GreenfieldOnly exclusion verified — legacy skips the OO golden), zero regressions.

