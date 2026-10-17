## Entry 2128 — 2026-10-16 18:19 PDT — PB3270: a COBOL word spelled like a runtime or System type no longer shadows it in the generated C#

The generated file imports `System` and four runtime namespaces and names their types by simple name:
`CobolString.Store(...)`, `HostLanding.LandText(...)`, `Int128`. The four C# name scopes (`CsNames`) were seeded with
the members the emitter writes, never with those type names, so a data item, group member, formal parameter or
PROGRAM-ID spelled `CobolString`, `CobolNum`, `StorageCell` or `HostLanding` shadowed the type and the module drew
CS0236 / CS0120 / CS1061 / CS0117 on legal COBOL (nothing in the standard reserves those words; ISO section 8.3.2.2
makes them user-defined words).

Re-probe: all four w1093b repros (PB formal, PC record member, PD WORKING-STORAGE, PE PROGRAM-ID) reproduced on a fresh
build with the report's exact errors. A wider probe declaring each of the 206 runtime type names and 30 System names
as items and as group members, plus file, sort, indexed, report, object, TYPEDEF, RENAMES, condition and index-name
positions, failed on main in the same way.

Fix, at the one place the rule belongs: `CsNames.ImportedNamespaces` is the one list of namespaces the using directives
import; `CsNames.ImportedTypeNames` is derived from it by reflection (the runtime assembly's public top-level types of
the four runtime namespaces, the core library's `System` types), never listed by hand; `RecordStruct`, `ProgramClass`,
`HostEntry` and `ModuleTypes` scopes hold it as a shared frozen set beside their own members (no per-scope copy), so
`CsNames.Allocate` gives such a word the next free `_n`. The alternative, rooting every runtime reference with
`global::`, would rewrite thousands of emitter strings and make every generated file noisier; the design doc (section
4.3 Names, section 4.5) records the choice. OO class, interface, method and property names are the upper-case fold of
the word, so they cannot spell an imported type; the one all-capitals type is the OO `BASE`.

`ImportedTypeNameDriftTests` keeps it total: the using directives the emitter writes are exactly `ImportedNamespaces`;
the runtime side equals what Roslyn sees; every `System` type the compiler's strings name by simple name is reserved. It
found one more arm while being written: `StatementEmitter` emitted `Console.Error.WriteLine` unqualified, now
`global::System.Console.Error.WriteLine`.

Golden: `conformance/85/pb3270_runtime_type_names_as_user_words` (a main program whose items, group members, index,
RENAMES, condition and file are named after runtime types, CALLing a program named `HostLanding` with formals
`ProgramRegistry` and a group `CobolRec`). No negative golden: no edition rejects these words, so there is no
rejecting arm to pin. Rows re-verdicted: none (a name-allocation defect with no inventory row).

Self-review (architecture, full code, performance, duplication): one list drives the seed; the seed is shared per scope
kind, so a thousand record structs allocate nothing for it; no second copy of the rule. Gate: `=== BUILD-LOCAL GATE:
LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261017T003242Z-dbf4b3; the golden and the four drift facts passed in
leg 1). Linux gate: the lander's (batched gating).

