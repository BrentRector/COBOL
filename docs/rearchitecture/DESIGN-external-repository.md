# DESIGN — The §8.13 external repository and the .NET host surface

**Status:** DESIGN (adopted shape). Owner decisions, kb/Work PB1099 Q3 and 2026-10-05: **the repository IS ECMA-335
assembly metadata plus COBOL-specific custom attributes** (no bespoke file format); **COBOL-to-COBOL activation keeps
the existing boundary unchanged** — `ICobolProgram.Call(CobolArg[], CobolArg?)` behind `ProgramTable.CallProgram`,
with its instance model, GR3 d)/e)/f) checks, INITIAL/CANCEL and nonfatal dispatcher — and the repository's
signatures are what the COMPILER verifies; **every program and function gets a generated, typed .NET entry** that
packs `CobolArg`s and calls that one boundary, and that entry is the ONE host surface (the untyped host route is
retired when it lands; a trimmed or AOT application composes the programs it calls; every loss of a host value is
refused); and the two standing directives: **COBOL semantics are honored
regardless of cost**, and **otherwise the surface stays as close to .NET as possible** (strongly typed host calls,
ECMA-335 metadata and attributes, no bespoke formats). This is the "R44 design doc" that `docs/CONFORMANCE.md` rows
DOC-A.1-66/-67/-138/-161/-162 defer to; §9 carries the text those rows take.

**Scope:** the compile-time resolution of every REPOSITORY-paragraph specifier against information outside the
compilation group (ISO §8.13, §12.3.8); the metadata that carries that information; the two user mechanisms (update,
check); cross-assembly classes, interfaces, parameterized definitions and expansions; the run-time locator the
registry shares with the binder; the typed .NET host entry; the extension point for COBOL calling plain .NET.
Dimension: **binder repository resolution + backend metadata emission + the run-time locate + the host surface**.
Sibling docs own the data model (`DataItem`/`PicInfo`), the OO mapping (`docs/COBOLNET_OO_DESIGN.md`), the
inter-program runtime (`docs/COBOLNET_INTERPROGRAM_DESIGN.md`) and the backend seam
(`docs/rearchitecture/DESIGN-codegen-backend.md`); where this design changes what they describe, the change is stated
here and those documents are corrected to match it in the slice that makes the change (§20, §21).

**SSOT alignment:** `docs/COBOLNET_DESIGN.md` §1 (typed-native only; one mechanism per job; phases one-directional;
backend-neutral bound tree) and §18. CLAUDE.md rule 4 governs the shape: ONE resolver behind which the compilation
group and the metadata-backed store are two providers; ONE activation boundary, which every COBOL CALL, every
pointer activation, every function activation and every .NET host entry crosses.

**Not in this design:** the BY REFERENCE aliasing of GROUP and REDEFINED formals (§14.2.3 GR8 for a group passed
twice, or read by a callback while the callee is active) is the boundary's own obligation and is owned by
**kb/Work PB2087** (the group is claimed by the existing `StorageCell` forcer and crosses as a cell + offset + width
area, the callee's formal a window over it). PB2087 is the prerequisite for GR8 on group formals; nothing here
re-solves it, and the host entry inherits its carrier through the one argument renderer (§4.3). Because the cell
carries managed slots (PB2087 item 5), the same change carries a strongly-typed group with an object-reference or
pointer leaf across the boundary, which supersedes the leaf-vector fix kb/Work PB1940 proposed: PB1940 is that
symptom of PB2087 and closes when PB2087 lands (§21).

> **Reading guide.** §0 is the decision list. §2 maps every ISO obligation to the design element that satisfies it.
> §3 is the platform evidence, including the experiments. §4 is the activation boundary and the .NET host entry. §5–§11
> are the repository proper. §12 is the exact change to the current code. §17 is the test and drift-test plan, §18
> rejected alternatives, §19 the decisions, §20 the sibling-document facts, §21 the landing-order constraints. The
> work notes are not in this document (CLAUDE.md rule 8: `kb/Work/` is the one register).

---

## 0. TL;DR — the decisions

1. **The repository is the compiled assembly.** Every assembly `cobol` produces carries, for every source unit it
   compiled — program, function, class, interface, program prototype, function prototype, parameterized class or
   interface, expansion — its §8.13 information as ECMA-335 metadata (the CLR type/method/parameter where one exists)
   plus a versioned family of custom attributes in `CobolNet.Runtime.Repository` for every fact the CLR cannot
   express. The assembly-level `[CobolRepository]` attribute IS the per-assembly index; a directory of assemblies IS a
   repository directory (§7). It is read with `System.Reflection.Metadata` and never loaded (§3.1, r6-meta).
2. **The COBOL-to-COBOL boundary is the existing one, unchanged** (owner decision 2026-10-05). A CALL — through a
   prototype from the group or from the repository, by literal, by data-name, through a program pointer — and a
   function activation all render `CobolArg[]` and cross `ProgramTable.CallProgram` (§4.1, §4.2). The repository
   adds COMPILE-TIME information only: a CALL through a repository prototype is bound and checked against the
   recorded signature exactly as a CALL through an in-group prototype is; at run time the program is located by
   name and GR3 d)'s run-time check runs as it does today.
3. **Prototype conformance across separately compiled units is the existing comparator over the recorded
   description tree** (§6.3, §8.2, §9.2): `PrototypeSignatures.Same` — §13.7.3 SR2's "shall match the description",
   in which data-names and intermediate grouping take no part (golden `2002/pb1115_prototype_group_description`).
   Data-names are recorded for diagnostics and the host surface and are never part of signature identity; a
   type-name is (§8.5.3.1, §6.3).
4. **The ONE .NET host surface, generated into the program's own assembly** (§4.3, §4.4; §14.2.3 GR13 and A.1
   item 141 make its restrictions and mechanisms the implementor's): for every outermost program and every
   function — definition or prototype — one `public static class` in `Cobol.<assembly>` whose `Call` method takes
   typed .NET parameters derived from the formal's DESCRIPTION (integral numeric as `long`/`Int128`, scaled as
   `decimal`, character items as `string`, a group as a published record struct with `AsImage`/`FromImage`), and,
   for a program, a `Cancel` method. `Call` lands each host value EXACTLY or refuses it with an
   `ArgumentOutOfRangeException` before anything is activated (owner decision: every loss is refused), renders the
   `CobolArg`s a COBOL CALL renders for arguments of the formals' own descriptions, calls
   `ProgramRegistry.CallProgram`, and decodes the results by ONE validity rule. That method IS the repository anchor
   of the unit's signature (its parameters carry the formal attributes). It performs no activation step: GR3 b)–i)
   are the boundary's. It is a generated CALLER, not a layer over an older API, and the untyped host route
   (`ProgramRegistry.CallProgram` / `ICobolProgram.Call` named as a host API) is retired in the change that lands it
   (owner decision 2026-10-05; §4.4).
5. **Identities, not words.** Every fact in a record that names something outside the unit — a class or interface in
   an object-reference description, a restricted pointer's type or prototype, a dynamic-length structure, a RAISING
   object class, a skeleton's own REPOSITORY resolutions — is recorded by EXTERNALIZED identity (or definition) and
   compared as one (§6.4). Words are per source element: a per-source-element `IdentityScope` maps them (§8.7).
6. **The schema is what the comparators read** (§6.3) — the whole `PicInfo` record by value, REDEFINES subordinates,
   type declarations, structures, referenced prototypes — and a two-sided drift test keeps that true (§17.2).
7. **Attributes, not custom modifiers; Roslyn stays; direct IL stays a free axis** (§3.3, §3.9).
8. **ONE resolver, two providers, documented search order; first match wins; a differing shadowed entry warns**
   (§8.6). Details are taken by EXTERNALIZED NAME, for programs and functions alike (GR10/GR11 a)–c) literally);
   when all three miss, the function WORD is honored — the group's definition, then the repository's recorded word
   (§8.4.6.7); whenever the externalized-name step selects one function and a DIFFERENT function carries the word,
   the externalized name wins, as GnuCOBOL 3.2 does, with warning REPO-13 — in one group or across the repository
   (determination D-R3, §8.3; owner decision 2026-10-05).
9. **Parameterized classes and interfaces cross assemblies, one CLR type per expansion (D3).** A skeleton publishes
   its post-text-manipulation source with its specifier RESOLUTIONS and its compile state; every expansion records
   an `ExpansionKey` — a hash of everything that decides what the expansion MEANS, never of bytes or MVIDs; an
   expander imports an existing expansion of the same name and key, even when that creates an assembly reference
   cycle with its own previous build (the CLR and Roslyn accept one); otherwise it expands, into its own
   deterministic assembly when the expansion's code references no class of the expanding group and into the group's
   assembly when it does. A same-name, same-identities expansion with a DIFFERENT key is stale and is named with
   every assembly that references it (§10.5).
10. **Mechanisms:** `--repository-update on|off` (default `on`), `--repository-check on|off` (default `on`;
    compares the previous build of this output AND every published prototype/definition of the same name in the
    search space), `--repository DIR`, `--reference FILE.dll` (§9).
11. **One locator, one namespace per assembly, one version guard.** `RepositoryLocator` serves the binder and the
    run-time probe (§7.4, §11.2); every emitted type lives in the ONE-SEGMENT namespace `Cobol.<sanitized assembly
    simple name>` and generated code spells every foreign name `global::`-qualified (§4.5); every entry records the
    runtime `AssemblyVersion` major it compiled against (`PublicApiAnalyzers` tie it to the runtime's public surface)
    AND the compiler's call-ABI version (the crossing form and width of every boundary item), and both are refused
    on a mismatch at compile time, at the probe and at module registration (§15.4). Target .NET 10 LTS (§3.8).
12. **AOT and trimming are host-composed** (owner decision 2026-10-05): with the dynamic probe switched off, the
    application's .NET host registers every COBOL assembly it composes through that assembly's one registration
    member, `__CobolModule.EnsureRegistered()`; no compile-time module reference is added (§7.3 stands; §15.5).

---

## 1. Purpose, scope, non-goals

### 1.1 Purpose
ISO §8.13 says the external repository "contains all information required for activating programs, functions, or
methods and for checking conformance", and §12.3.8 makes it the third source every REPOSITORY specifier may resolve
against. Today (kb/Work PB1086) there is none: `REPOSITORY. PROGRAM NOSUCHPG. FUNCTION NOSUCHFN. CLASS NOSUCHCL.`
compiles clean; a program prototype that misses the group resolves with a null signature that
`PrototypeSignatures.Same` treats as conforming; a function that misses it draws COBOLNET1505 at a use; a class that
misses it draws COBOLNET0813/0821/0859 at a use; a property misses silently. And a .NET program can reach a COBOL
program only through the untyped `ProgramRegistry.CallProgram(name, callerPath, CobolArg[])`, which
`docs/CONFORMANCE.md` DOC-A.1-65/-116/-141/-167 document as the host API. This design gives the clause one reading and
every program a typed .NET entry over the one boundary, and makes that entry the only host surface.

### 1.2 Scope
- The REPOSITORY paragraph's five externally-resolvable specifier kinds (§12.3.8.1 "allows specification of program
  prototype names, function prototype names, property-names, class names, and interface names that may be used
  within the scope of this environment division"). The intrinsic specifier resolves against §8.11 and is out of scope.
- The information a compiled unit publishes, its carrier, how a later compilation finds and reads it, and how the
  result feeds the EXISTING conformance checks unchanged.
- The two implementor mechanisms and their user documentation; cross-assembly classes, interfaces, skeletons and
  expansions; the run-time locate.
- The generated .NET host entry per program and function (§4.3).
- The extension point for plain .NET types (§4.6).

### 1.3 Non-goals
- Any change to the COBOL-to-COBOL activation boundary (`ICobolProgram`, `CobolArg`, `ProgramTable.CallProgram`),
  including group-formal aliasing (kb/Work PB2087).
- A direct-IL backend (§3.9 says what it must emit; it is not started here).
- Parametric polymorphism ("Parametric polymorphism is an optional feature", §9.3.5.3 rule 7; A.4.10 item 3, not
  claimed) and multiple inheritance (A.4.10 item 1). Parameterized classes are NOT this and are in scope (§10.5).
- Implementing the plain-.NET provider of §4.6; the resolver and schema admit it, a later slice builds it.
- Incremental builds or a compiled-program cache.

---

## 2. The ISO obligation table

Each quotation was run through `cite.py --check`. **(group)** marks a row the in-group arm already satisfies.

| Clause | Obligation | Satisfied by |
|---|---|---|
| §8.13 ¶1 | "The external repository contains all information required for activating programs, functions, or methods and for checking conformance" | §5, §6; activation information is the externalized name and the index entry (activation is by name, §11.2) |
| §8.13 list | externalized name; type of source unit; "the description of the parameters of the source unit, if any, and the manner of receiving parameters (by reference or by value) and whether they are optional or not"; returning item; exceptions; "the entry convention of the source unit, if any"; object properties; methods; "type declarations required for the description of parameters and returning items"; DECIMAL-POINT IS COMMA; currencies; "any external locale identification for locales associated with formal parameters or returning items of the source unit"; "any other information that the implementor requires" | §5.3 |
| §8.13 ¶2 | "This information about a source unit, excluding the externalized name of the source unit, is called its signature"; "Whether the information is taken from a prototype or a definition, the information stored in the external repository about the signature of a program or a function is the same" | §5.2, §6.3 — one record shape from `RepositoryRecord.Of(BoundUnit)` for both; identity is the description compare, in which data-names and intermediate grouping take no part and type-names do (§8.5.3.1; §17.2) |
| §8.13 ¶3 | "The implementor shall provide a mechanism that allows the user to specify whether to update the external repository when a compilation unit is compiled" | §9.1 |
| §8.13 ¶4 | "whether to flag differences in prototypes and definitions in the compilation group from the information in the external repository" | §9.2, REPO-9 |
| §8.13 ¶5 | "The details on the association of the name of a source unit with information in the external repository are specified in 12.3.8, REPOSITORY paragraph" | §8 |
| §12.3.8.3 SR1 | "all the specifications for that name shall be identical" | (group) `DataBinder.CheckRepositorySpecification` |
| §12.3.8.3 SR2 | literals "shall be alphanumeric literals or national literals and shall be neither figurative constants nor zero-length literals" | (group) `ExternalizedName.Screen` |
| §12.3.8.3 SR3, SR4/SR7 | EXPANDS placement; operands declared in the same REPOSITORY | (group) `OoExpansion`; across assemblies §10.5 |
| §12.3.8.3 SR5/SR8 | "references to object-class-name-1 are to that class definition and this class-specifier is ignored" | (group) `OoRepositoryScope.Build`; the group is asked first |
| §12.3.8.3 SR6 | "If literal-1 is specified, there shall be information in the external repository for the class literal-1"; "If literal-1 is not specified, there shall be information in the external repository for the class object-class-name-1" | §8.4; REPO-1 |
| §12.3.8.3 SR9 | "If literal-2 is specified, there shall be information in the external repository for the interface literal-2" (and the twin) | §8.4; REPO-1 |
| §12.3.8.3 SR10 | the name shall be a prototype in the group, "the name of a function definition specified previously in this compilation group", or "the name of a function for which information exists in the external repository" | §8.3 (legality: all three alternatives honored), REPO-1 |
| §12.3.8.3 SR11 | self-naming function specifier ignored | (group) `BinderDriver.SpecifiesItself` |
| §12.3.8.3 SR14 | the name shall be a prototype in the group, "the name of a program definition specified previously in this compilation group", or "the name of a program for which information exists in the external repository" | §8.2, REPO-1 |
| §12.3.8.3 SR15 | self/containing-program specifier ignored | (group) `ProgramPrototypesOf` |
| §12.3.8.3 SR16 | "there shall be information in the external repository for the property property-name-1 that is part of one of the classes or interfaces that are declared in this REPOSITORY paragraph" (and the literal twin; §19 T1) | §8.5; REPO-1 |
| §12.3.8.4 GR1/GR7 | "If object-class-name-1 is a class described with the USING phrase, object-class-name-1 may be specified only in the REPOSITORY paragraph" | (group) `OoNameResolution`; an imported skeleton likewise (§10.5) |
| §12.3.8.4 GR2 ¶1 | "The implementor shall specify when the AS phrase is required" | §8.1 D-R2 |
| §12.3.8.4 GR2 ¶2 | the property literal "is the externalized name known to the operating environment for a method that implements the named property" | §8.5 D-R5 (§11.7.4 GR1 a): "If the PROPERTY clause is specified, the name is implementor-defined") |
| §12.3.8.4 GR5/GR8 | "a class object-class-name-1 is created from the parameterized class object-class-name-2" | (group) `OoExpansion`; §10.5 |
| §12.3.8.4 GR6 / GR9 | "It is implementor-defined how the information in the class specifier and the external repository are used to determine which class is used" / the interface twin (A.1 161/162) | §8.4, §9.4 |
| §12.3.8.4 GR10 a) | IF "the externalized name of the program prototype is the externalized name of a program definition specified previously in the same compilation group" THEN "the details are taken from that program definition, which is the program that will be called, and the details in the external repository are ignored" | §8.2 step 1 (externalized name AND source order) |
| §12.3.8.4 GR10 b) | "the details are taken from that program prototype definition and the details in the external repository are ignored" | §8.2 step 2 |
| §12.3.8.4 GR10 c) | "the details are taken from the external repository for the program with the same name as the externalized name of the program prototype" | §8.2 step 3 — the record decoded from the entry's host-entry method (§5.3) |
| §12.3.8.4 GR10 NOTE 1 | "Literal-3, if specified, is the externalized name of the program prototype; otherwise, the externalized name is program-prototype-name-1" | `ProgramSpecifier.ExternalizedName` |
| §12.3.8.4 GR11 a)–c) | the function twin: "if the externalized name of the function prototype is the externalized name of a function definition specified previously in the same compilation group" … "the details are taken from the external repository for the function with the same name as the externalized name of the function prototype" | §8.3 (externalized name; SR10's word alternative when all three miss — determination D-R3) |
| §12.3.8.4 GR12–GR14 | intrinsic precedence / keyword omission / ALL | (group) |
| §12.3.8.4 GR15 | "Property-name-1 is the name of an object property that may be used throughout the scope of the containing environment division" | §8.5 |
| §8.4.6.7 | "A user-function-name may be referenced in the REPOSITORY paragraph of any source element that follows that function definition" … "if the external repository is updated, in any subsequently-compiled source unit that specifies that user-function-name as a function-prototype-name in its REPOSITORY paragraph" | §8.3 D-R3 — the record carries the user-function-name beside the externalized name and the provider answers a word lookup (`FunctionByWord`) when GR11 a)–c) miss; REPO-13 when the externalized-name step and the word name different functions |
| §13.7.3 SR2 | "The description of the formal parameters and the returning item that appear in the linkage section of a function prototype or a program prototype shall match the description of the formal parameters and the returning item in the corresponding function definition or program definition" | (group) `PrototypeSignatures.Same` → COBOLNET1513; across units the same comparator over the decoded record (§9.2, REPO-9) |
| §10.6.3 GR1 | "Compilation of a program prototype definition or a function prototype definition generates information required for the external repository" | §7.2 — a prototype unit compiles to its host-entry class carrying the signature, and its index entry |
| §10.6.2 SR2/SR3 | "If a compilation group contains both a program definition and a program prototype definition with the same externalized name, the signatures of these two compilation units shall be the same" (and the function twin) | (group) COBOLNET1513 — within ONE compilation group only; across groups a difference is §8.13 ¶4's "flag differences" (REPO-9, §9.2), never an error |
| §14.9.4.4 GR3 b)–i) | locate ("the runtime system attempts to locate the program being called"), GR3 d) conformance "if checking for it is enabled in both the activated program and activating runtime element", GR3 e) externals, GR3 f) recursion, GR3 i) "If the program was successfully called, after control is returned from the called program the ON EXCEPTION phrase, if specified, is ignored" | the existing boundary, unchanged (§4.1); the host entry crosses it with checking enabled on its side (§4.3) |
| §14.2.3 GR8–GR10 | BY REFERENCE "operates as if the formal parameter occupies the same storage area as the argument"; BY CONTENT/BY VALUE "if the formal parameter is numeric, a COMPUTE statement without the ROUNDED phrase" | the boundary (PB2087 for group formals); the host entry lands host values by the same rule, refuses any landing that would lose a digit, a sign or a character (GR13), and gives one host variable passed twice one storage (§4.3) |
| §14.2.3 GR6 NOTE 1 | "In COBOL, the storage for the returning item is allocated in the activating source unit" | the host entry allocates the RETURNING receiver, as a COBOL activator does (§4.3) |
| §8.3.2.2 | "Within a run unit, all instances of a given name that is externalized to the operating environment shall identify the same kind of entity or item"; "when two or more source elements identify something with the same externalized name, they refer to the same instance"; "The implementor defines the formation and mapping rules of these names" | §8.6; `ExternalizedNames` (DOC-A.1-68); §4.3 (one host class per externalized name per assembly; every class activates the one program by name); §11.2 (a second module carrying an already-registered outermost name is refused) |
| §14.2.3 GR13; A.1 141 | "When either the activating or the activated runtime element is other than a COBOL runtime element, the implementor shall specify the restrictions and mechanisms for all supported language products"; "Procedure division header rules when either the activating or the activated runtime element is not a COBOL element" | §4.3 (the host entry: parameter matching, representation, return of a value, omission, the refusals, the exception contract), §9.4 (DOC-A.1-141's text) |
| §14.2.2 SR2 | BY VALUE data "shall be defined as a data item of class numeric, message-tag, object, or pointer" | §4.3's host-type table (no OPTIONAL BY VALUE; message-tag has no item, §4.3) |
| §13.18.2.3 SR2/SR4 | ANY LENGTH only "in an elementary level 1 entry in the linkage section of a function, of a contained program, or of a method that is not a property method", and in a function "as a formal parameter with the BY REFERENCE phrase" | §4.3's host-type table (no ANY LENGTH RETURNING, ANY LENGTH only on a function's BY REFERENCE formal) |
| §8.8.4.4.4 GR3 n) | the NUMERIC class test: "consists entirely of a valid representation for the usage" (and the display/national arm) | §4.3 — the ONE validity rule for write-back and RETURNING at the host door (`CobolNum.IsNumericImage`) |
| §9.3.12 | "Within a run unit, two classes with the same externalized object-class-name that are created by expanding the same parameterized class with the same actual parameters are the same class instance. If two classes expand a parameterized class with different actual parameters, they are not the same class instance and shall not have the same externalized object-class-name." | §10.5 (D3: one CLR type per (name, triple) by reuse of the expansion of the same `ExpansionKey`; a superseded key is a stale caller REPO-9 names; the "shall not" is reported where the two will necessarily share a run unit) |
| A.1 66 / 67 / 161 / 162 | "External repository (mechanism for specifying whether checking and updating occur)"; "External repository information (other information beyond the required information)"; "how external repository and class-specifier determine which class is used"; "how interface specifier and external repository determine which interface is used" | §9 |

---

## 3. Platform facts and the emission axis

### 3.1 Facts verified (source in parentheses; unverified items marked)
- **Toolchain** (`dotnet --list-sdks/--list-runtimes`): SDKs 10.0.303, **10.0.401**, 11.0.100-preview.7; runtimes 10.0.11,
  10.0.12, 11.0.0-preview.7. The repo targets `net10.0`, `LangVersion 14`, `Nullable enable`, `TreatWarningsAsErrors`
  (`Directory.Build.props`), and sets NO `Version`: every assembly is 1.0.0.0 today — §15.4 sets it from the package
  version and keys the ABI guard on its major. The emitted assembly's simple name is the output file's base name
  (`CompilerDriver.Compile`); `BackendOptions.AssemblyName`'s remark "(the COBOL PROGRAM-ID)" is wrong and is
  corrected (§12).
- **Support policy** (dotnet.microsoft.com): .NET 10 LTS 2025-11-11 → 2028-11-14; .NET 9 STS → 2026-11-10; .NET 8 LTS →
  2026-11-10; .NET 11 RC1 go-live 2026-09-08; "LTS releases get free support and patches for three years. STS releases
  get free support and patches for two years".
- **Measured — assemblies (scratchpad `circ/`, csc 10.0.401, runtime 10.0.12):** circular assembly references compile
  and run (two-pass build); mutual inheritance across assemblies runs; a second `LoadFromAssemblyPath` of one simple
  name in the Default ALC fails ("Assembly with same name is already loaded").
- **Measured — reading without loading (scratchpad `r6/meta/`):** an assembly `T.dll` whose public static class
  carries `[CobolUnit(Program, "P1GQDEF", "P1GQ")]` and whose `Call` method's parameter carries `[CobolFormal(0,
  "D-G")]` and four `[CobolDescription(ordinal, parent, …)]` attributes (the pb1115 definition's `D-G` / `D-S` /
  `D-A` / `D-B` tree), plus `[assembly: CobolRepository(1, "1.0.0", Units = …, Registrar = …)]`, is decoded by
  `PEReader(stream, PrefetchMetadata)` + `MetadataReader` + `CustomAttribute.DecodeValue` with a 20-line
  primitive/string/enum provider: every fixed and named argument is recovered, **with the attribute-defining assembly
  deleted from disk**, `T` and the attribute assembly are not loaded into the reading process, and `T.dll` is renamed
  and restored right after the read (no handle held). The provider cannot see an enum's underlying type without the
  defining assembly, so the schema fixes every enum as `int`-backed (§6.2; a drift test pins it).
- **Measured — the host entry over the boundary (scratchpad `r6/host/`, the worktree's `cobol` build):** `HSUB`
  (`USING LK-AMOUNT PIC S9(7)V99, LK-REC (LK-ID PIC 9(4), LK-NAME PIC X(10)), BY VALUE LK-FLAG PIC S9(4) COMP-5, BY
  REFERENCE OPTIONAL LK-NOTE PIC X(8) RETURNING LK-RESULT PIC S9(9)`) compiled by the current compiler, called from a C#
  host through a hand-written entry of exactly §4.3's shape (`Call(ref decimal, ref LK_REC, long, ref string?)` →
  `CobolArg[]` → `ProgramRegistry.CallProgram(…, callerPath: null, …, siteArgMismatchChecking: true)` → write-back):
  call 1 `amt=20.51 id=42 name='CHANGED   ' note='NOTED   ' result=1` (10.25 × 2 + 0.01 through a scaled `decimal`;
  the group through the record struct's `AsImage`/`FromImage`; the BY VALUE `1` landed by `CobolArgAdapt.LandForFormal`
  into the COMP-5 formal; RETURNING decoded); call 2 with `null` for the OPTIONAL formal prints `HSUB: NOTE OMITTED`
  (the callee's `IS OMITTED` true), `result=2` (one WORKING-STORAGE across two host activations — the boundary's
  instance model), `id=40` (BY VALUE `-2`); `HALIAS.Call(ref x, ref x)` with the callee's `MOVE 5 TO LK-A` / `DISPLAY
  LK-B` prints `LK-B=0005` and the host's `x=5` (one storage for one host variable, `Unsafe.AreSame`); two distinct
  variables print `LK-B=0009`, `y=5 z=9`.
- **Measured — the host door's refusals and validity rule (scratchpad `r7/brk/`, the same build; entries written to
  §4.3's revised shape):** `HRET` (`PROCEDURE DIVISION RETURNING LK-R PIC S9(9)`, `LK-RX REDEFINES LK-R PIC X(9)`,
  `MOVE "ABC" TO LK-RX`) — a COBOL caller's receiver holds `[ABC      ]` and is NOT NUMERIC; the entry's one validity
  rule throws `FormatException` instead of returning a number (a plain decode returns `0`). `HAL2(ref x, ref x)` over formals `PIC
  9(4)` and `PIC 9(8)` is refused with `ArgumentException` before activation and `x` is unchanged (one shared storage
  would let the last write-back win, `x=1234`). Every loss is refused with `ArgumentOutOfRangeException` naming the
  parameter and nothing activated: `12345` into `9(4)` (high order), `-1` into unsigned `9(4)` (sign), `1.005m` into
  `S9(7)V99` (low order), `99999` and `40000` into a BY VALUE `S9(4) COMP-5` (the storage's capacity; the CALL lane's
  `LandForFormal(checking: true)` would have thrown `CobolSizeError` from inside the boundary), an 11-character name
  into `PIC X(10)`. Exact values (`32767`, `-32768`) pass; the entry's `Cancel()` resets WORKING-STORAGE (results
  `1 2`, after `Cancel` `1`).
- **Measured — published names (scratchpad `r7/names/`):** `PROGRAM-ID. CUSTP AS "CUST"` with `01 CUST (05 CUST, 05 F,
  05 F, 05 F-2, 05 G (10 X))` compiles, and a host built `-warnaserror` against the §4.3 naming rule (class `CUST`,
  record `CUST_Record` with `CUST`, `F`, `F_2`, `F_2_2`, `G`, `G_Record`) round-trips it (`MOVE 7 TO F-2` arrives in
  `F_2_2`); a record whose words are `AsImage`, `Equals`, `ToString`, its own name and `CALL` compiles as `AsImage_2`,
  `Equals_2`, `ToString_2`, `R`, `CALL`. The same words broke the INTERNAL record struct (a subordinate named
  `AsImage` or `Equals` drew CS0102 in `_T_0`, the internal allocator not being seeded with the struct's own
  members); the one seeded allocator of §4.5 removed that for the internal struct (kb/Work PB2093) and serves the
  published one.
- **Measured — namespaces (scratchpad `r7/ns/`):** `PAY.dll` (type `Cobol.PAY.V2`) beside `PAY.V2.dll` (namespace
  `Cobol.PAY.V2`) fails a host compile with CS0434 (and a COBOL group's source type with CS0437); with the
  one-segment namespace `Cobol.PAY_u002E_V2` both compile clean under `-warnaserror`.
- **Measured — host composition (scratchpad `r7/compose/`):** `MAINP` does `CALL "LIBP"`; `LIBP` is compiled into
  `Lib.dll`, so the sibling probe (which loads `LIBP.dll`) cannot find it. A host that registers only `MAINP`'s module
  sees `MAINP: LIBP NOT FOUND` (ON EXCEPTION); one that registers both sees `LIBP SET W-N=0042` — registration by the
  host composes the run unit with no probe and no reference from `MAINP` to `Lib`.
- **Measured — Roslyn diagnostics on collisions and legal source (scratchpad `r6/warn/`, csc 10.0.401):** a source
  type or namespace that meets an imported type or namespace of the same full name draws **CS0436** (type vs imported type),
  **CS0435** (namespace vs imported type) and **CS0437** (type vs imported namespace) — each a WARNING by default, and
  each means the compile silently picked one of two declarations; `b == b` draws **CS1718** — the C# a legal COBOL
  `IF B = B` generates.
- **Measured — prototype conformance (scratchpad `r6/pb1115/`, the worktree's `cobol` build):** the golden
  `2002/pb1115_prototype_group_description` (prototype `L-G` / `L-A` / `L-B`, definition `D-G` / `03 D-S` / `D-A` /
  `D-B`) compiles and prints `A=07 B=XY` / `W-A=12 W-B=ZZ`; the negative `pb1115-prototype-group-subordinates` (the
  two fields swapped) draws COBOLNET1513. That comparator, fed the decoded record, is the cross-unit check (§9.2).
- **`MetadataBuilder` / `PersistedAssemblyBuilder` / `UnmanagedCallersOnly` / trimming** (learn.microsoft.com): as cited
  in §3.3–§3.7: `SignatureTypeEncoder.CustomModifiers()` exists; `AddParameter(ParameterAttributes, StringHandle, int)`;
  `ParameterAttributes.Optional/HasDefault`; `PersistedAssemblyBuilder` is .NET 9+ and "require[s] a fully trusted
  environment"; `UnmanagedCallersOnly` methods "Must be marked static", "Must not be called from managed code", "Must
  only have blittable arguments"; the trimming guidance: `IsTrimmable`, `IsAotCompatible`, `[RequiresUnreferencedCode]`,
  `[DynamicallyAccessedMembers]`, `[FeatureSwitchDefinition]` / `[FeatureGuard]` (.NET 9+), "Avoid reflection when
  possible".
- **C# cannot declare arbitrary `modreq`/`modopt`** (csharplang "ref-readonly-parameters"; dotnet/runtime #43088:
  "modreq can't be directly added to a method signature in C#").
- **Not verified on a page:** `MetadataReader`'s trim-safety (reasoned; §17.5 pins it); Micro Focus directive defaults;
  .NET 11 GA date beyond trade press.

### 3.2 The precedent: the legacy compiler's direct emission
`legacy-compiler/CodeGen/CilEmitter.cs` emits IL through Mono.Cecil; its ABI was `public static int
Entry(ManagedPointer[] args)` per program, static LINKAGE fields, alternate `Entry` methods for ENTRY, a name→delegate
registry. Reusable as technique: the by-name registry with factories, deterministic emission, one emitter per concern.
Must not come across: the byte `ProgramState`, static LINKAGE fields, the static entry, and Mono.Cecil (a direct
backend uses the in-box `System.Reflection.Metadata`; `DESIGN-codegen-backend.md` §2.0 is corrected, §20).

### 3.3 Custom modifiers versus attributes
A `modreq` makes a signature DIFFERENT for a binder that does not understand it: a `modreq(CobolOptional)` on a host
entry's parameter would stop a .NET caller that does not know it from calling at all, while OPTIONAL is nullability
and BY VALUE is the parameter's passing convention whatever the caller believes. C# source cannot declare them
(§3.1); emitting them means emitting every public type through direct metadata — the whole type-definition emission of
the backend (price: a direct backend, §3.9). **Decision: attributes for every fact the CLR signature does not carry;
no modifiers.** What the CLR signature of a host entry DOES carry: arity, order, the .NET type and passing convention
(`ref` for BY REFERENCE, by value for BY VALUE), OPTIONAL (nullability), return type, parameter names (display only).

### 3.4 Entry convention
§11.9.7.4 GR2 — COBOL means "the naming convention and mapping of method-names and program-names are as specified in
8.3.2.2, User-defined words; other aspects of the entry convention are implementor-defined"; GR3 — "When
entry-convention-name-1 is specified, the meaning of the entry convention is implementor-defined"; GR4 — "the entry
convention is inherited from the first class specified in the INHERITS clause", otherwise "In all other cases, the
entry convention is COBOL". The CALL statement has no convention clause; `>>CALL-CONVENTION` (§7.3.9.3 GR1 "The default
for the CALL-CONVENTION directive is" COBOL) governs name mapping; the `ENTRY` statement is a vendor extension this
compiler refuses (`StatementBinder`: "ISO/IEC 1989 defines no ENTRY statement") and this design keeps refusing: a
program has one entry. `OptionsBinder.EntryConventionOf` accepts only COBOL (A.1 item 64). The repository records
`EntryConvention = "COBOL"` as a value; an imported base's value feeds GR4 inheritance. `CallConv*`/`UnmanagedCallersOnly`
(native stack conventions, static blittable-only) are rejected as its home.

### 3.5 OPTIONAL
COBOL OPTIONAL means OMITTED is testable (§8.8.4.8); a CLR optional parameter injects a default the callee cannot
distinguish. Rejected. On the host entry OPTIONAL is nullability: OPTIONAL is written only under BY REFERENCE
(§14.2.1's using-phrase), so an OPTIONAL formal is `ref T?`; `null` is OMITTED and the entry passes the boundary's omitted argument
(`ManagedPointer.Null`, §14.9.4.4 GR11), which the callee's omitted-argument condition reads as it does for a COBOL
`OMITTED` (r6-host call 2). `[CobolFormal(Optional)]` records which formals MAY be omitted (§14.9.4.3 SR24 is
checked at bind).

### 3.6 Named arguments
"This correspondence is positional and not by name equivalence" (§14.9.4.4 GR2; INVOKE §14.9.23.4 GR3). Host-entry
parameter names are the sanitized COBOL words of the unit that published the entry (readable to a .NET caller,
display only); the COBOL word itself is in the attribute.

### 3.7 Other facilities (adopt / reject / defer)
| Facility | Decision | Reason |
|---|---|---|
| `System.Reflection.Metadata` reader | **Adopt** (the only reader) | no loading, no reflection (r6-meta) |
| A generated public host-entry class per program and function, in the program's assembly | **Adopt** | §4.3, §4.4 — the typed .NET surface and the repository anchor in one CLR member |
| `ref T` for BY REFERENCE on the host entry | **Adopt** | the .NET idiom for a value the callee may change; one variable passed to two formals of one description gets one storage (§4.3) |
| Feature switch `CobolNet.Runtime.DynamicProbe` | **Adopt** | the dynamic probe is the only reflection; switched off under AOT/trimming the runtime is analyzer-clean (§15.5) |
| `PublicApiAnalyzers` on `Cobol.Net.Runtime` | **Adopt** | ties the ABI major version to the public surface (§15.4) |
| Generic math | Reject | not a signature concern |
| `PersistedAssemblyBuilder` | Reject here; defer to a direct backend | `MetadataBuilder` is the better primitive |
| `MetadataBuilder` custom-attribute encoding | Adopt in the drift test (§17.4) | backend independence |
| Source generators | Reject | one record family + a round-trip test |
| `IsTrimmable` on `Cobol.Net.Runtime` | Adopt | §15.5 |
| .NET 11 / C# 15 | Defer | STS, not GA; nothing needs it |

### 3.8 Target framework policy
Current LTS (`net10.0` until .NET 12 LTS), moving within the next LTS's first support year, never an STS for a shipped
release, no multi-targeting (no user base; the compiler deploys its own runtime).

### 3.9 The emission axis
Roslyn renders the host-entry classes, the record structs and every attribute as C# (`RepositoryAttributeRenderer` is
the one place attribute syntax is written). A direct backend emits the same types and the same `RepositoryRecord`
through `MetadataBuilder` (§17.4 proves equal decoding). The repository requires no direct emission; adopting one for
other reasons costs that backend's slices, not these.

---

## 4. The activation boundary and the .NET host surface

### 4.1 The boundary (unchanged)
A PROGRAM-ID becomes `internal sealed class {ClassName} : ICobolProgram` whose CALL entry is `void Call(CobolArg[]
args, CobolArg? returning)` (`ProgramEmitter.EmitCallMethod`), adopting each positional carrier into a `__lnk{Uid}`
field (`FormalAdopt`; `ManagedPointer<T>`, the accessor carrier of `COBOLNET_INTERPROGRAM_DESIGN` D1). Activation is
`ProgramRegistry.CallProgram(name, callerPath, args, returning, notFoundEc, siteArgMismatchChecking)` →
`ProgramTable.CallProgram`, which performs the whole §14.9.4.4 GR3 boundary around `inst.Call`: locate b) (the §8.4.6.3
scope rules, then the sibling-module probe), recursion f), count/description/RETURNING d) over the registered
`BoundaryItem`s "if checking for it is enabled in both the activated program and activating runtime element",
`DescribeExternals` e), the instance model, `Modules.Push`, the checking-flag scope, the nonfatal dispatcher,
`EndStorage`, INITIAL's implicit CANCEL and the GR3 i) transfer mark. Every COBOL CALL — through an in-group or
repository prototype, by literal, by data-name, through `CallPointer` or `CallFunctionPointer` — and every function
activation cross it. **This design does not change it for a COBOL activator.** Its one host-only arm —
`callerPath: null`, an activator outside every program — gains what `ProgramTable.RunMain` already does for a COBOL
main: it is the outermost boundary, where a STOP RUN or a run-unit-terminating fatal condition ends the run unit
(owner decision R48, §4.3's exception contract). The program class stays `internal`; nothing outside the registry
constructs or calls an instance.

### 4.2 What the repository adds to a COBOL CALL
Only compile-time information. A CALL through a prototype the resolver took from the repository (§8.2 step 3) is
bound exactly as a CALL through an in-group prototype: `CallBinder` checks every argument against the recorded
formal's DESCRIPTION (§14.8.2, `DiagnosticCatalog.CallArgumentConformance`), the BY VALUE landing uses the recorded
formal's description (`CobolArgAdapt.LandForFormal`, §14.2.3 GR10), the RETURNING receiver is checked against the
recorded RETURNING item (§14.8.3), and the site renders the same `CobolArg[]` it renders today. Nothing is referenced
from the callee's assembly: the program is located by name at run time (§11.2), and a callee recompiled with a
different signature after its callers is met by GR3 d) at run time — EC-PROGRAM-ARG-MISMATCH when checking is
enabled in both elements, the lenient positional adoption otherwise — and by REPO-9 at the callee's compile (§9.2).
The DEFAULT is the lenient adoption: the activated program registers its formals' descriptions only when its own
source enables EC-PROGRAM-ARG-MISMATCH checking (`>>TURN`, §7.3.25; `ProgramEmitter.EmitEntryWrapper`), so without
that directive nothing is compared at run time and each argument is read through the callee's description. REPO-9
at the callee's compile is then the only signal; the manual says so (§9.4).

### 4.3 The host entry
§14.2.3 GR13 governs: "When either the activating or the activated runtime element is other than a COBOL runtime
element, the implementor shall specify the restrictions and mechanisms for all supported language products" (A.1
item 141, `docs/CONFORMANCE.md` DOC-A.1-141, whose text is §9.4's). This subclause is that specification for the one
supported non-COBOL language product, .NET. For every outermost program and every function unit with externalized
name `N` — definition or prototype — the compiler generates, in the group's own assembly, one public class:
```csharp
namespace Cobol.ACCT;
/// <summary>PROGRAM-ID ACCT-UPDATE (ISO §11.10). Activates the program through the run unit's activation boundary
/// (§14.9.4.4 GR3): one WORKING-STORAGE per run unit, RECURSIVE/INITIAL honored.</summary>
[global::CobolNet.Runtime.Repository.CobolUnit(RepositoryUnitKind.Program, "ACCT-UPDATE", "ACCT-UPDATE", EntryConvention = "COBOL")]
public static class ACCT_UPDATE
{
    /// <summary>01 LK-REC — the published record of the formal's description.</summary>
    public record struct LK_REC_Record
    {
        /// <summary>05 LK-ID PIC 9(4).</summary>   public long LK_ID { get; set; }
        /// <summary>05 LK-NAME PIC X(10).</summary> public string LK_NAME { get; set; }
        public readonly string AsImage();          // the record's character image; refuses a lossy member (below)
        public static LK_REC_Record FromImage(string image);   // decodes by the validity rule (below)
    }

    [return: CobolReturning, CobolDescription(0, -1, …)]
    public static long Call(
        [CobolFormal(0, "LK-AMOUNT"), CobolDescription(0, -1, …)]                          ref decimal LK_AMOUNT,   // PIC S9(7)V99
        [CobolFormal(1, "LK-REC"), CobolDescription(0, -1, …), CobolDescription(1, 0, …), …] ref LK_REC_Record LK_REC,
        [CobolFormal(2, "LK-FLAG", Mode = PassingMode.Value), CobolDescription(0, -1, …)]   long LK_FLAG,         // PIC S9(4) COMP-5
        [CobolFormal(3, "LK-NOTE", Optional = true), CobolDescription(0, -1, …)]            ref string? LK_NOTE)  // PIC X(8)
    {
        __CobolModule.EnsureRegistered();   // definitions only: this assembly's programs, once per run unit (§11.2)
        // 1. HostLanding: every host value landed EXACTLY into a detached storage of its formal's description, or
        //    ArgumentException — before anything is activated; one storage for one variable passed twice
        // 2. one CobolArg per formal from the CALL lane's argument renderer over that storage
        // 3. global::CobolNet.Runtime.ProgramRegistry.CallProgram("ACCT-UPDATE", callerPath: null, args, returning,
        //        siteArgMismatchChecking: true)
        // 4. finally: every BY REFERENCE variable decoded back by the validity rule; then the RETURNING value decoded
    }

    /// <summary>CANCEL ACCT-UPDATE (ISO §14.9.5) for this run unit.</summary>
    public static void Cancel();   // ProgramRegistry.Cancel("ACCT-UPDATE", callerPath: null)
}
```
- **Names.** The class, the record structs, their members and the `Call` parameters are named by the ONE allocator of
  §4.5 — `DataItem.Sanitize` of the word, then the smallest free `_2`, `_3`, … in source order — over a scope SEEDED
  with that scope's reserved names: the enclosing type's own name (CS0542), the fixed members (`Call` and `Cancel` in
  the class; `AsImage` and `FromImage` in a record struct) and the record struct's synthesized members (`Equals`,
  `GetHashCode`, `ToString`, `Deconstruct`, `PrintMembers`, `EqualityContract`). A group contributes a property and a
  nested record-struct type, named `<word>` and `<word>_Record` in the same scope. So `01 CUST. 05 CUST … 05 F … 05 F
  … 05 F-2` of a program externalized "CUST" publishes class `CUST`, record `CUST_Record` and members `CUST`, `F`,
  `F_2`, `F_2_2` (r7-names): every legal description publishes a compilable surface, and the order rule makes the
  names deterministic. Names are display only; the COBOL word is in the attribute (§3.6).
- **Landing a host value — exact, or refused.** §14.2.3 GR9 lands a BY CONTENT argument for an activator that names
  the program in its REPOSITORY paragraph into "a data item with the same description and the same number of bytes as
  the formal parameter" by "a COMPUTE statement without the ROUNDED phrase" if the formal is numeric, by "a SET
  statement" if it "is of class index, object, or pointer", and "otherwise, a MOVE statement"; GR10 lands BY VALUE by
  the same COMPUTE. The host entry lands every host value — BY REFERENCE initial content and BY VALUE alike — by that
  rule into a detached storage of the formal's description, with ONE restriction GR13 lets it state (owner decision
  2026-10-05): **the landing must lose nothing.** A number is landed only when the COMPUTE would store it exactly — no
  digit truncated at the high-order end, none at the low-order end (a value with more fraction digits than the scale,
  or nonzero digits in P positions), no sign dropped into an unsigned item, nothing beyond a binary item's capacity;
  a string only when it fits the item's character positions (it is space-filled as the MOVE fills it, on the left
  under JUSTIFIED) and holds only characters the item's class can hold (`0`/`1` for boolean); a group only when every
  member lands so and every OCCURS array has its declared length (an OCCURS DEPENDING ON array's length is the DEPENDING
  value and lies within its bounds). Anything else is refused with `ArgumentOutOfRangeException` naming the parameter
  (and the member, for a group) BEFORE anything is activated: a host site has no ON SIZE ERROR, and a silently
  narrowed amount is a wrong answer. One function decides it, `HostLanding.Land(value, description)` in the runtime,
  so the rule is written once (r7-brk: high-order, sign, low-order, COMP-5 capacity and string length each refused).
  An edited formal (numeric-edited, alphanumeric-edited) takes the host string as its character content under the
  same fit rule; a host that holds the number formats it.
- **The arguments are the ones a COBOL CALL renders.** For each formal the entry renders the `CobolArg` that
  `CallEmitter`'s argument renderer produces for an argument of the FORMAL's OWN description passed by CONTENT over
  that detached storage, with the mode set to the formal's (`Reference` / `Value`). One renderer serves both
  (`ArgText`); a change to the CALL lane's carrier for a shape (PB2087's cell areas) is the host entry's change by
  construction. A BY VALUE value, already landed exactly, reaches `CobolArgAdapt.LandForFormal` in range, so that
  function's own size check never fires from a host call.
- **One storage for one variable passed twice.** Two `ref` parameters that refer to the same variable
  (`Unsafe.AreSame`; only parameters of one CLR type can) are given ONE storage when their formals' descriptions are
  the same (`OoConformance.DescriptionMismatch` is null both ways), so `Call(ref a, ref a)` is the host's `CALL …
  USING A A` and §14.2.3 GR8 holds for it (r6-host: `LK-B=0005`). When the descriptions differ — `PIC 9(4)` and
  `PIC 9(8)`, both `ref long` — one storage cannot be both, and the call is refused with `ArgumentException` naming
  both parameters before anything is activated (r7-brk). Each formal has its own record-struct type, so two group
  formals cannot be passed one host variable: a host cannot express that aliasing, and the restriction is
  DOC-A.1-141's. Distinct host variables are distinct storage in .NET; inside the activation every COBOL reference —
  the callee, its contained programs, a COBOL program it calls back — sees the boundary's storage exactly as for a
  COBOL activator.
- **Write-back and RETURNING — one validity rule.** In a `finally`, after the boundary returns or throws, each BY
  REFERENCE variable receives its storage's content decoded by the formal's description; after the try, the RETURNING
  receiver — allocated by the entry as an activating COBOL element allocates it (§14.2.3 GR6 NOTE 1) — is decoded the
  same way and returned (`void` when the unit has none). A numeric content is decoded only when it is a valid
  representation of its description: the class test NUMERIC (§8.8.4.4.4 GR3 n)) is true of it, as the runtime's one
  implementation of that test (`CobolNum.IsNumericImage`) answers — the content a REDEFINES alias, a reference
  modification or a non-conforming caller can leave there (§14.8.2.3.2 rule 1 lets a COBOL activator's argument carry
  it). Content that fails it leaves that variable unchanged (or returns no value), and after every other write-back the
  entry throws `FormatException` naming the formal (or the RETURNING item, or the group member) and quoting the
  characters — the .NET exception for a text that does not parse as a number. The rule is the same function for a
  BY REFERENCE formal, a group member and the RETURNING item (r7-brk: `HRET`'s `[ABC      ]` is refused where a plain
  decode returns `0`). An exception already propagating from the boundary is not replaced. Decoding never
  narrows: the host type of a description holds every value its storage can hold (the table below).
- **Scaled numerics are never presented unscaled.** The host type is a function of the description (below); no member
  anywhere on the host surface exposes a fixed-point item's unscaled integer.
- **CANCEL.** A program's class has `Cancel()`, which performs §14.9.5's CANCEL of that program in the current run unit
  through `ProgramRegistry.Cancel(name, callerPath: null)` — the one boundary's CANCEL (EC-PROGRAM-CANCEL-ACTIVE for
  an active program, §14.9.5 GR5). A function has none: CANCEL names programs. With it the host has no reason to name
  `ProgramRegistry` (r7-brk: `Cancel()` resets WORKING-STORAGE).
- **The boundary is the registry's.** The entry passes `callerPath: null` — an activator outside every program, so
  only §8.4.6.3's outermost-program rule applies — and `siteArgMismatchChecking: true`, so the ACTIVATING half of GR3
  d)'s "enabled in both" is always on. GR3 d)'s comparison then runs when the activated program enables EC-PROGRAM-ARG-
  MISMATCH checking itself (§4.2). That matters only for a PROTOTYPE library's entry, whose program lives in another
  assembly: against a changed definition it is EC-PROGRAM-ARG-MISMATCH when the definition enables the checking, and
  the lenient positional adoption — the definition reads the arguments through its own description — when it does not
  (the default); REPO-9 at the definition's compile is then the signal. A definition's own entry is compiled with the
  program and cannot disagree with it. GR3 b) locate, e) externals, f) recursion, the instance model, INITIAL's
  implicit CANCEL and the GR3 i) mark are `CallProgram`'s; the entry neither creates nor sees an instance. A
  function's entry passes `notFoundEc: "EC-FUNCTION-NOT-FOUND"`, the function arm of the same call (§8.4.6.3's
  program/function discrimination, `ResolveVisible`'s `wantFunction`).
- **The exception contract — what leaves `Call` and why.** Exactly these, and the entry maps none of the boundary's:
  1. `ArgumentException` and its subclasses — `ArgumentOutOfRangeException` for a lossy value, `ArgumentNullException`
     for `null` in a parameter that is not OPTIONAL, `ArgumentException` for one variable given two formals of
     different descriptions or an array of the wrong length — thrown by the entry BEFORE anything is activated.
  2. `CobolCallException` — from the boundary: an activation attempt that failed (GR3 b)/d)/e)/f): EC-PROGRAM-NOT-FOUND,
     -ARG-MISMATCH, -RECURSIVE-CALL, EC-EXTERNAL-*, EC-FUNCTION-NOT-FOUND; `ControlTransferred` false), or such a
     condition propagating out of the activated program's execution (`ControlTransferred` true: `ProgramTable.
     CallProgram` marks every `CobolCallException` that crosses out of `inst.Call`, and only that type). `EcName`
     names it.
  3. The run-unit-ended result of owner decision R48 (DOC-A.1-167): a STOP RUN, or a fatal exception condition that
     terminates the run unit (§14.6.13.1.3 rule 7, "execution of the run unit is terminated abnormally"), performs the
     §14.6.11 actions at the OUTERMOST activation boundary — `CallProgram` with `callerPath: null`, the host's
     counterpart of `ProgramTable.RunMain` — and leaves as the one documented exception carrying the exit status and
     whether the ending was normal or abnormal. `StopRun` and `CobolFatalException` (with its `CobolSizeError`) are
     the runtime's internal signals inside the run unit and never reach the host.
  4. `FormatException` — thrown by the entry after the boundary returned, for content the validity rule refuses.
  Nothing else: a host that catches these four has seen every outcome.
- **What the entry body contains** (the §17.3 drift test asserts it on the emitted corpus): `__CobolModule.
  EnsureRegistered()` (definitions only); per formal one `HostLanding` landing call and the alias grouping; one
  `CobolArg` per formal from the CALL lane's renderer; exactly one `ProgramRegistry.CallProgram`; a `finally` of
  `HostLanding` decodes; the RETURNING decode. No `catch` clause, no `ICobolProgram`, `ProgramTable` or instance
  member, no test or mapping of an EC condition.

**The host type of a description** (`HostSurface.TypeOf(DescriptionRecord)`, one function, used by the emitter and by
the record decoder for display):

| COBOL description | Host type |
|---|---|
| fixed-point numeric, no positions right of the decimal point (`Scale ≤ 0`) | the first of `long`, `Int128` that holds EVERY value the item's storage can hold: for display, national and packed forms the PICTURE's digits with P positions; for a binary form whose truncation is by capacity (COMP-5, BINARY-CHAR … BINARY-DOUBLE) the byte width's range — so an unsigned 8-byte binary item is `Int128` |
| fixed-point numeric with `Scale > 0` | `decimal` when `Digits ≤ 28` and `Scale ≤ 28`; otherwise the runtime's `CobolDec` (exact: a fixed-point item's at most 31 digits fit its 34-digit significand at any exponent) |
| floating-point (COMP-1/COMP-2, FLOAT-*) | the item's own CLR carrier (`float` / `double` / the decimal-float carrier) |
| alphanumeric, national, boolean, alphanumeric-edited, numeric-edited; DYNAMIC LENGTH | `string` |
| ANY LENGTH (a FUNCTION's BY REFERENCE formal only: §13.18.2.3 SR2 allows it in a function, a contained program or a method, and SR4 makes it a function's BY REFERENCE formal) | `string`, whose length is the argument's length (§13.18.2.4 GR1) |
| INDEX data item | `long` |
| POINTER / PROGRAM-POINTER / FUNCTION-POINTER | `ManagedPointer` / `ProgramPointer` / `FunctionPointer` |
| OBJECT REFERENCE | the referenced class's or interface's public CLR type; `CobolObject` when universal |
| message-tag (§14.2.2 SR2 admits it BY VALUE) | none: the A.4 message-control facility is not provided and the DATA DIVISION refuses a message-tag item (`docs/CONFORMANCE.md` §4), so no formal has that class |
| fixed group | the published record struct: one property per named subordinate (FILLER kept privately so `FromImage` → `AsImage` round-trips), nested record structs for subordinate groups, an array property for OCCURS (its length checked at `AsImage`), the DEPENDING ON value as the array's length for OCCURS DEPENDING ON; a REDEFINES subordinate is not a member (the record is the first description; its other views are the image's) |
| strongly-typed group with an object-reference or pointer leaf | the published record struct with those members; packed as the CALL lane's carrier for that shape (PB2087's cell area with managed slots) |

The published record struct is rendered from the DESCRIPTION by `RecordStructEmitter` in its published mode — the
same member/codec walk that renders a program's internal record struct, over the description's host types instead of
the storage form's fields, with `AsImage`/`FromImage` from the one group image codec (`GroupImageCodec`) and the
landing and validity rules of `HostLanding`.

### 4.4 Why the host entry lives in the program's assembly, and why it is not a shim
**Placement.** The entry is generated into the assembly that compiled the unit because (1) the repository IS that
assembly's metadata, and the entry's method is the CLR member whose parameters carry the formal attributes — the
"CLR type/method/parameter where one exists" of §0 item 1 — so ONE unit's typed host surface and its repository record
are one declaration, never two that can disagree; (2) a .NET host references one assembly per COBOL library and gets
IntelliSense from the XML documentation generated from the COBOL descriptions; (3) no further file exists to deploy,
version or lose. Its public surface per externalized program or function is one static class, its nested record
structs, `Call`, and `Cancel` for a program; the program class itself stays `internal`, so a host has no instance to
call around the boundary. A prototype library and the definition's library each publish their own class for one
externalized name, in their own namespaces: two DISPLAY shapes of one signature (pb1115's prototype publishes `ref
L_G_Record`, its definition `ref D_G_Record` with a nested `D_S_Record`: names and grouping differ, the descriptions
are the same, §13.7.3 SR2), which REPO-9 compares (§9.2) and both of which activate the one program by name.

**ONE host surface (owner decision 2026-10-05).** The change that lands the host entry retires every other host
route: `docs/CONFORMANCE.md` DOC-A.1-65, -116, -141 and -167 stop telling a host to activate a program through
`ProgramRegistry.CallProgram` or `ICobolProgram.Call` and describe the entry instead (§9.4, §20);
`NonCobolActivatorReturnTests` — today a host through `ProgramRegistry.CallProgram(…, "", …)` — calls the generated
entries; a module is reached only through `EnsureRegistered()`, its one public registration member (`Register` is
private, §11.2, from §21's first slice on); and `ProgramRegistry`'s members carry `[EditorBrowsable(Never)]` with XML documentation stating that
they are the generated code's ABI, which generated code in other assemblies must be able to call and which carries
no contract for a hand-written caller. A drift test fails when any test, sample or document outside the generated
code and the runtime names `ProgramRegistry.CallProgram`, `ProgramRegistry.Cancel` or `__CobolModule.Register` as a
host route (§17.3).

**It is not a wrapper, shim, forwarder, alias or compatibility layer (CLAUDE.md rule 4).** Rule 4 forbids keeping an
old API alive beside its replacement and forbids a second mechanism that re-does a job another already does. (1) The
host surface is ONE: the entry is the first typed .NET API, and the untyped route it would otherwise sit beside is
retired in the same change, not kept alive for compatibility. (2) It performs no activation step — no locate, no
recursion test, no instance, no checking scope, no mapping of the boundary's EC conditions — every one of which stays
in `ProgramTable.CallProgram`, the ONE boundary. (3) The job it does — converting a caller's values into storage of the
formal's description and back — is the job the generated code of a COBOL CALL statement does for a COBOL caller, done
by the same renderer; its refusals and its validity rule are GR13's restrictions, which exist only at this door, and
one runtime function (`HostLanding`) states them once. It is a generated CALLER, the .NET counterpart of a CALL
statement. (4) `ProgramRegistry.CallProgram` and `CobolArg` stay public only because generated code in other
assemblies calls them: an ABI, not a second host surface. `ProgramRegistry`'s own XML remarks that call it a "shim"
and a "facade" that "forwards" are rewritten to that statement in the same change (§12).

### 4.5 Namespaces and `__CobolModule`
Every emitted type lives in the namespace `Cobol.<S>`, where `<S>` is the assembly simple name made ONE identifier by
`DataItem.IdentifierCharacters` (each character a C# identifier cannot hold, `.` included, written `_uXXXX_`), so
`PAY.V2.dll` is `Cobol.PAY_u002E_V2` and never meets a type `V2` in `Cobol.PAY` (r7-ns: the dotted form is CS0434 in
a host and CS0437 in a group). No emitted type lives directly in `Cobol`, so a namespace never meets a type, and two
COBOL assemblies referenced by one host never collide on a program or class name or on `__CobolModule`; two needed
assemblies whose namespaces coincide are REPO-6. `[assembly: CobolRepository(…, Registrar = "Cobol.ACCT.__CobolModule")]`
tells the run-time probe which type to invoke.
**The one name allocator.** Every C# name generated from a COBOL word — the internal record structs' members, the
host-entry classes, the published record structs and their members, the `Call` parameters, OO classes and interfaces
— comes from ONE function, `CsNames.Allocate(baseName, scope)` (`Binding/Model/CsNames.cs`): the base name (the
`DataItem.Sanitize`d word, or a synthesized base such as `_impliedRecordF`) when it is free, else the smallest free
`_2`, `_3`, … against the scope's claimed set, in source order. Each `CsNameScope` is SEEDED with its reserved names —
the members the emitter itself writes there and the ones C# synthesizes (a record struct: `AsImage`, `FromImage` and
the other image and leaf-vector members, `Equals`, `GetHashCode`, `ToString`, `PrintMembers`, …, in
`CsNames.RecordStructMembers`; a program class: the `ICobolProgram` surface `Call`, `CloseFiles`, `EndStorage`, …, in
`CsNames.ProgramClassMembers`; the published host-entry scopes add `Call`, `Cancel` and the enclosing type's name) —
so a legal word never collides with a generated member. **Landed for the internal scopes (kb/Work PB2093):**
`DataBinder`'s private `Unique`, which left both unseeded (a subordinate named `AsImage` or `Equals` drew CS0102 in
`_T_0`, a record named `CloseFiles` in the program class), is deleted; a group's members are appended only through
`DataItem.AddMember`, which allocates in the group's own record-struct scope, and the class-level roots through the
program-class scope. `CsNameReservationDriftTests` reads every member declaration the code generator writes, the
members C# synthesizes for a record struct and the `ICobolProgram` members, and is red when one is in neither seed.
Within a run unit one externalized name identifies one kind of entity (§8.3.2.2), so a program and a class never
legally share one. Generated code spells every name outside the unit's own namespace `global::`-qualified
(`global::CobolNet.Runtime.ProgramRegistry`): an assembly named `Cobol` or `CobolNet` would otherwise capture the
prefix (CS0234 on legal source). `__CobolModule`'s one public member is `EnsureRegistered()`, which registers this
assembly's programs into the current run unit once (§11.2); its `Register` body is private.

### 4.6 COBOL calling plain .NET (the extension point)
A third provider behind the SAME resolver, `ClrTypeProvider`, answers a CLASS or INTERFACE specifier whose externalized
name is an assembly-qualified CLR type name (`CLASS SB AS "System.Text.StringBuilder, System.Runtime"`) and a PROGRAM
specifier whose name is `"Namespace.Type::Method"` (a public static method), after the metadata provider misses. It
builds an `OoClassSymbol`/`CalleeSignature` with `Origin = Clr` from the type's metadata through the same
`MetadataReader`: methods by name (INVOKE resolves the overload by arity, then by the marshaling table), constructors
as `NEW`, properties as GET/SET, static members on the factory side. Marshaling (COBOL ↔ CLR) is `HostSurface.TypeOf`
read in the other direction (§4.3's table): a group ↔ a `struct` only by `TYPE` declaration of the CLR struct's layout
(a later determination). Identities are CLR full names; `RepositoryUnitKind` reserves `ClrType` and `ClrMethod`. The
schema and resolver admit it; building it is a later slice.

### 4.7 What is checked when
| Fact | Home | Checked |
|---|---|---|
| a formal's description (PICTURE identity, scale, category, sign, usage, clauses, ANY LENGTH, DYNAMIC LENGTH, ALIGNED, locale, currency, DP-comma, strong type, group layout), mode, OPTIONAL; the RETURNING description | `[CobolFormal]` + the `[CobolDescription]` tree on the host-entry parameter / return value (§6) | bind — `PrototypeSignatures.Same` (prototype vs definition), `ParameterConformance` (argument vs formal, §14.8.2), §14.8.3 (RETURNING) — over the decoded record, exactly as for an in-group prototype |
| the signature the callee's previous build or a published prototype/definition carries | the same attributes in that assembly | `--repository-check` at the callee's compile (REPO-9, §9.2) |
| the activated program's formals and RETURNING at run time | `BoundaryItem` registration (unchanged) | GR3 d) when checking is enabled in both elements — every COBOL CALL, and every host entry (which enables it on its side) |
| a host's call | the host-entry method's CLR signature | by the C# compiler when the host compiles; by the CLR (`MissingMethodException`) when a host runs against a rebuilt assembly whose entry changed shape |

---

## 5. The repository model

### 5.1 Unit kinds
`RepositoryUnitKind { Program, Function, Class, Interface, ProgramPrototype, FunctionPrototype, ParameterizedClass,
ParameterizedInterface, Expansion, ClrType, ClrMethod }` (the last two reserved, §4.6). §8.13 ¶2 holds: a prototype and
a definition publish the same record shape from one `RepositoryRecord.Of(BoundUnit)`, and the comparator that decides
"the same signature" reads descriptions, never data-names (§6.3, §17.2).

### 5.2 Per-kind signature contents
| Kind | Contents |
|---|---|
| Program, ProgramPrototype, Function, FunctionPrototype | on the host-entry class and its `Call` method: formals' descriptions, OPTIONAL/mode, returning description, RAISING (identities), entry convention, DP-comma, currencies, locales, referenced type declarations, structures and prototypes; the kind, the user word (program-name / user-function-name) and the externalized name in `[CobolUnit]` |
| Class / Interface | externalized name; FINAL; base and implements identities; rosters (per method: externalized name, formals, returning, RAISING, entry convention, OVERRIDE/FINAL, accessor role); properties |
| Expansion | the class record + `(definition identity, actual identities)`, its `ExpansionKey` and its placement (§10.5) |
| ParameterizedClass / ParameterizedInterface | formal list with kinds; the skeleton's post-text-manipulation source; its specifier RESOLUTIONS (word → identity, for every class/interface its own REPOSITORY declares); its `CompileState` (§6.1) |

### 5.3 The home of each §8.13 item
| §8.13 item | Programs / functions / prototypes | Classes / interfaces |
|---|---|---|
| externalized name, type of source unit | `[CobolUnit(Kind, Name, ExternalizedName)]` on the host-entry class; listed in the assembly's `Units` index | `[CobolUnit]` on the public CLR type (class, `__FACTORY`, interface) |
| parameters (description, mode, optional) | the `Call` method's Params: .NET type + `[CobolFormal]` + `[CobolDescription]` tree | the method's Params, likewise |
| returning item | the return type + `[return: CobolReturning]` + tree | same |
| exceptions | `[CobolUnit(Raising)]` identities | `[CobolMethod(Raising)]` |
| entry convention | `[CobolUnit(EntryConvention)]` | `[CobolUnit]`, `[CobolMethod]` |
| object properties | — | `[CobolProperty(Name, ExternalizedName, Get, Set)]` |
| methods | — | `[CobolMethod]` on each CLR method |
| type declarations | `[CobolTypeDeclaration]` trees on the class | same |
| DECIMAL-POINT IS COMMA, currencies | `[CobolUnit]` + folded into each `PictureClauseIdentity` | same |
| locale identification | `[CobolDescription(LocaleExternal, LocaleSize)]` | same |
| implementor information | §5.4 | §5.4 |

### 5.4 "Any other information that the implementor requires" (A.1 item 67)
1. `SchemaVersion`, the runtime ABI version (`RuntimeVersion`, the `AssemblyVersion` of `Cobol.Net.Runtime` the
   entry was compiled against; its MAJOR is compared) and the call-ABI version (`CallAbi`, the version of the
   compiler's boundary layout — each boundary item's crossing form, carrier type and width — that the entry was
   emitted with; compared exactly) — all three compared: REPO-7, §11.3, §15.4.
2. `CompilerVersion`, `Edition` — named in diagnostics; never compared.
3. The per-assembly index: `Units` (kind-tagged externalized names of every unit the assembly publishes) and
   `Registrar` (the `__CobolModule` full name) — the locator's answers. §17.4 asserts the index equals the record set.
4. Data-names and the publishing unit's words: recorded for diagnostics, for the host surface and for `IdentityScope`
   decoding; no comparator reads them (§6.3).
5. An expansion's triple, key and placement; a skeleton's source, resolutions and compile state (§10.5).
6. Structure definitions and referenced prototypes' signatures (§6.4).

---

## 6. The attribute schema

All attributes live in `CobolNet.Runtime.Repository`, are `sealed`, `Inherited = false`, with ONE constructor taking the
identity arguments and every other fact a named property of primitive / `string` / `string[]` / `int`-backed enum type;
none references a `System.Type`.

### 6.1 The model
```csharp
public sealed record RepositoryRecord(
    RepositoryUnitKind Kind, string Name, string ExternalizedName,                            // the NAME part
    string EntryConvention, bool DecimalPointIsComma, ImmutableArray<CurrencyPair> Currencies, // from here on: the SIGNATURE part (§8.13 ¶2)
    ImmutableArray<RaisingIdentity> Raising, ImmutableArray<FormalRecord> Formals, DescriptionRecord? Returning,
    ImmutableArray<DescriptionRecord> TypeDeclarations, ImmutableArray<StructureRecord> DynamicLengthStructures,
    ImmutableArray<PrototypeRecord> ReferencedPrototypes,
    ClassRecord? Class, SkeletonRecord? Skeleton, ExpansionIdentity? Expansion);
public sealed record FormalRecord(int Position, string Name, PassingMode Mode, bool Optional, DescriptionRecord Description);
public sealed record StructureRecord(int Index, DynamicLengthPrefix Prefix, bool Delimited, string? PhysicalStructureName);
public sealed record PrototypeRecord(int Index, RepositoryUnitKind Kind, string ExternalizedName, ImmutableArray<FormalRecord> Formals, DescriptionRecord? Returning);
public sealed record RaisingIdentity(string Kind /* E | C | I */, string Identity, bool Factory);
public sealed record ExpansionIdentity(string DefinitionIdentity, ImmutableArray<string> ActualIdentities, string Key, bool OwnAssembly);
public sealed record SkeletonRecord(ImmutableArray<string> Formals, ImmutableArray<bool?> FormalIsInterface, string Source,
    ImmutableArray<(string Word, string Kind, string Identity)> Resolutions, CompileState State);
/// EVERY compile-time input that changes what the skeleton's source MEANS (§10.5): the importer re-parses and re-binds under it.
public sealed record CompileState(int Edition, bool Permissive, SignEncoding SignEncoding, bool FlagExtensions,
    string CobolWordsMap, string DirectiveFolds /* TURN / PROPAGATE / REF-MOD-ZERO-LENGTH at the definition */);
public sealed record DescriptionRecord(
    int Level, string? Name, string? RedefinesTarget, int? Occurs, string? OccursDepending, bool OccursDynamic,
    bool Justified, bool BlankWhenZero, bool Synchronized, bool Aligned, bool AnyLength,
    bool DynamicLength, int DynMaxSize, int? DynStructure,
    string? TypeName, bool Strong, bool External, bool IsTypedef, bool DeclaresStrongType, bool IsExternalTypedef, bool ExternalFromType, int? TypeDeclaration,
    GroupUsage GroupUsage, PicRecord? Pic, ImmutableArray<DescriptionRecord> Children);
/// PicInfo BY VALUE — every record member, the three identity members as identities, DigitPositions as its nullable backing.
public sealed record PicRecord(
    PicCategory Category, Usage Usage, int Length, int Digits, int Scale, bool Signed, string SignKind, bool IsRecovery,
    int? RestrictedTypeDeclaration, int? RestrictedPrototype, bool PackedNoSign, bool FloatLittleEndian, bool IsFloatEdited,
    string? EditMask, string? CurrencyString, LiteralClass? CurrencyClass, bool IsAlphabetic, int? DigitPositions,
    string? ClauseCharacterString, string? ClauseCurrencyString, bool? ClauseDecimalPointIsComma, string? ClauseEditing,
    ImmutableArray<EditRuleRecord> EditingRules, LocaleRecord? LocaleEdit,
    ObjectRefKind? ObjectRefKind, string? ObjectRefIdentity, bool ObjectRefFactory, bool ObjectRefOnly);
```
`ClassRecord` carries base/implements identities, properties and both rosters (`MethodRecord(Name, ExternalizedName,
IsFactory, Accessor, PropertyName, Override, Final, EntryConvention, Raising, Formals, Returning, ClrName)`).
`CompileState` is exhaustive by a drift test: every `CompilerDriver.Options` member is either a `CompileState` field or
listed in the test as not affecting bound semantics (output paths, `--copy` directories — the recorded source is
post-text-manipulation — `--run`, `--nist` harness mode), so a new option cannot be forgotten silently (§17.2).

### 6.2 The attribute types
| Attribute | Target | Constructor | Named properties |
|---|---|---|---|
| `CobolRepositoryAttribute` | assembly | `(int schemaVersion, string runtimeVersion, int callAbi)` | `CompilerVersion`, `Edition`, `Units`, `Registrar`, `HasSignatures` |
| `CobolUnitAttribute` | host-entry class (programs, functions, prototypes) / OO class / interface | `(RepositoryUnitKind, string name, string externalizedName)` | `EntryConvention`, `DecimalPointIsComma`, `Currencies`, `Raising`, `Final`, `BaseIdentity`, `Implements`, `FactoryImplements`, `FactoryType`, `ExpandsDefinition`, `ExpandsActuals`, `ExpansionKey`, `ExpansionOwnAssembly` |
| `CobolSkeletonAttribute` | class | `(string source)` | `Formals`, `FormalIsInterface`, `Resolutions`, `Edition`, `Permissive`, `SignEncoding`, `FlagExtensions`, `CobolWordsMap`, `DirectiveFolds` |
| `CobolFormalAttribute` | parameter | `(int position, string name)` | `Mode`, `Optional` |
| `CobolReturningAttribute` | return value | `()` | — |
| `CobolDescriptionAttribute` | parameter / return value, `AllowMultiple` | `(int ordinal, int parent)` | every `DescriptionRecord` + `PicRecord` field |
| `CobolTypeDeclarationAttribute`, `CobolStructureAttribute`, `CobolPrototypeAttribute`, `CobolPrototypeFormalAttribute`, `CobolPrototypeDescriptionAttribute` | the host-entry class / OO class, `AllowMultiple` | indexed as §6.1 | the description fields |
| `CobolMethodAttribute` | method | `(string name, string externalizedName)` | `IsFactory`, `Accessor`, `PropertyName`, `Override`, `Final`, `EntryConvention`, `Raising` |
| `CobolPropertyAttribute` | class, `AllowMultiple` | `(string name, string externalizedName)` | `Get`, `Set` |
One anchoring for programs and methods alike: a formal's attributes sit on its Param of the host entry's `Call` / the
method; the returning tree on the return value; `ordinal` is the preorder index within the tree, `parent` the parent's
(`-1` at the root). The reader has one decoder keyed by `(Param or return, ordinal)`. Every enum in the schema is
`int`-backed: the reader decodes without the defining assembly and cannot learn an underlying type otherwise (r6-meta).

### 6.3 The schema is what the comparators read
Comparators: `PrototypeSignatures.Same` → `OoConformance.DescriptionMismatch` + `StrongTypeModel.SameElementaryLayout`;
`DescriptionMismatch`'s helpers (`StrongTypeMismatch` → `SameType` → `EquivalentTypeDeclarations` →
`SameElementaryLayout` → `SameEssentialCharacteristics` → `DescriptionClauses.AlignedOrDynamicLengthMismatch` +
`SameAnalyzedProfile` — `PicInfo` RECORD equality minus `EditingRules`/`RestrictedTypeDecl`, then `EditingRules`
element-wise; `AsIfElementaryGroupMismatch`; `VariableLengthCompatibility.*` → `Atoms`; `TierCIsland`;
`OoClassTable.LeafCarried`; `CategoryArmMismatch`; `PictureClauseIdentity.*`); `ConformanceDescriptor`;
`BitLayout.*`; `DataItem.ImageWidth`/`ByteWidth`.
| Member read | Read by | Schema field |
|---|---|---|
| `IsGroup`, `Children`, `IsElementary` | all walkers | `Children`, `Level`, `Pic` null-ness |
| `RedefinesTargetName` on subordinates | `ByteWidth`, `ImageWidth`, `Atoms` (§8.5.1.12.1: "any subordinate data items that specify the REDEFINES clause, and all data items subordinate to those data items, are ignored"), `BitLayout` | `RedefinesTarget` |
| `Occurs`, `OccursSpec.DependingName`, `IsDynamicTable` | `ByteWidth`, `Atoms`, `VariableLengthCompatibility` | `Occurs`, `OccursDepending` (word inside the record, else `"<outside>"`), `OccursDynamic` |
| `IsDynamicLength`, `DynMaxSize`, `DynStructure` | `DescriptionClauses`, `Atoms`, `TierCIsland`, `IsImageCapable`, `ConformanceDescriptor` | `DynamicLength`, `DynMaxSize`, `DynStructure` → `StructureRecord` (definition) |
| `IsAnyLength`; `IsAligned`, `Justified`, `BlankWhenZero`, `Synchronized` | `DescriptionMismatch`, `MismatchAtAnyLength`, `ConformanceDescriptor`, `DescriptionClauses`, `CategoryArmMismatch`, `SameEssentialCharacteristics` | the five booleans |
| `StrongType`, `TypeName`, `IsTypedef`, `DeclaresStrongType`, `IsExternalTypedef`, `ExternalFromType`, the TYPE anchor | `StrongTypeModel.*` | the six facts + `TypeDeclaration`; `TypeDeclarations` |
| `GroupUsage`, `IsAsIfElementary`, `AsIfPic` | `AsIfElementaryGroupMismatch`, `ConformanceDescriptor` | `GroupUsage` |
| `HasBitDescendant`, bit positions; `IsImageCapable`, `BoundaryImageCapable`, `CurrentExtentImageCapable` | `ByteWidth`, `SameType`, `SameElementaryLayout`; Tier-C arms, `ConformanceDescriptor`, `LeafCarried` | derived at decode from the recorded tree |
| `Pic` — the WHOLE record, including `ObjectRef` (`Kind, Name, Factory, Only`), `RestrictedTypeName/Decl`, `RestrictedPrototypeName`, `_digitPositions`, `EditingRules`, `LocaleEdit`, `Clause` | `SameAnalyzedProfile` (record equality), `CategoryArmMismatch` arms, `SameDescriptionAs`, `ClrTypeName`, pointer arm, `ConformanceDescriptor` | `PicRecord`: every member; `ObjectRefIdentity`, `RestrictedTypeDeclaration`, `RestrictedPrototype` as identities (§6.4); `DigitPositions` nullable so the backing round-trips |
| `Renames` | `AtomsOf` | not applicable (a formal is level 01/77, §14.2.2 SR1) |
| `CobolName` | diagnostics; REDEFINES/ODO targets inside the record | `Name` |
Not recorded (emitter facts): `CsName`, `Uid`, carrier field names, `StoreAsImage`, `RedefinesClass` tiers, VALUEs,
condition-names, index names. The decoded tree is bound into a codec-owned throw-away forest so derived members answer
as on a bound item (`UsageInheritancePass`, `InheritSignClauses`, group-usage classification, `BitLayout`). Invariant
(§17.2): `Decode(Encode(x))` is indistinguishable from `x` to every comparator and derived quantity, and `PicInfo`
record equality itself holds on the decoded twin. **Data-names and intermediate grouping are recorded and never
compared:** `Name` fields serve diagnostics, the host surface and the REDEFINES/ODO targets inside one record;
`PrototypeSignatures.Same` reads the descriptions (§13.7.3 SR2; pb1115, r6-pb1115). A TYPE-NAME is not a data-name: §8.5.3.1 makes it part of type
identity ("Two type declarations are considered equivalent when they have the same type-name", compared by
`StrongTypeModel.EquivalentTypeDeclarations`), so `TypeName` and the type declarations' names ARE compared, as in
the group.

### 6.4 Identities, not words
- **Object references**: `ObjectRefDescriptor` gains `Target` (the resolved `OoClassSymbol`/`OoInterfaceSymbol`) beside
  the written `Name`; `SameDescriptionAs` compares `Target` identities; `SignatureKey` spells the externalized name;
  `ClrTypeName` is `Target.CsName` with its namespace. ACTIVE-CLASS carries its containing class's identity (excluded
  from equality by §9.3.8.2.3 rule 2 d) as today). `PicInfo` record equality (`SameAnalyzedProfile`) therefore compares
  identities because `ObjectRefDescriptor`'s own equality is redefined over `Target`'s identity.
- **Restricted pointers**: `POINTER TO type` → the type DECLARATION (index into `TypeDeclarations`), compared by
  `EquivalentTypeDeclarations` ("both shall be restricted and of the same type", §14.8.2.3.2); `PROGRAM-POINTER TO proto` /
  `FUNCTION-POINTER TO proto` → the prototype's identity + signature (`ReferencedPrototypes`), compared by identity;
  `PicInfo.RestrictedPrototypeName` is replaced by `RestrictedPrototype` (identity) so record equality compares
  identities; the pointer arm of `CategoryArmMismatch` and `DataPointerKey`/`ProgramPointerKey` follow.
- **Dynamic-length structures**: compared by DEFINITION (`StructureRecord` minus name) in `DescriptionClauses` AND in
  `ConformanceDescriptor`'s key (the definition's canonical text replaces `DynStructure?.Name`), so descriptor equality
  ⇔ `DescriptionMismatch == null` stays an invariant.
- **RAISING, INHERITS, IMPLEMENTS, expansion actuals, skeleton resolutions**: externalized identities;
  `ClassRaisingCovered` walks from the `RaisingTarget`'s resolved symbol.
Identity is the externalized name (the Annex C fold): §8.3.2.2 makes one externalized name one instance within a run
unit; §8.6 keeps the compile-time search space to one entry per identity.

### 6.5 Determinism and size
Fixed emission order (assembly attribute; per unit `[CobolUnit]`, type declarations, structures, referenced
prototypes, formals by position, descriptions by ordinal, returning, methods, properties; arrays sorted ordinally);
deterministic PE (kb/Work PB120) → byte-identical entries from one compiler; §10.5's expansions are identified by
their `ExpansionKey`, never by bytes. A description
is ~45 primitive arguments; a skeleton's source is a string the size of the class. Reader caps: §15.2.

### 6.6 Schema versioning
`SchemaVersion` increments on any constructor or named-property meaning change; the reader implements one version; any
other version is REPO-7, EXCEPT the previous build of this output in §9.2, which is skipped with an informational
REPO-12 ("previous build carries schema v; not compared"). No compatibility layer; the cure is recompiling.

---

## 7. Assemblies and layout

### 7.1 One assembly per compilation group
Every group assembly carries `[assembly: CobolRepository(schema, runtimeVersion, callAbi, Units = [...], Registrar = "…")]` and
every unit's `[CobolUnit]` unconditionally. With `--repository-update on` the description attributes, the class,
interface, skeleton and expansion records are emitted (`HasSignatures = true`); with `off` they are not — the group
still runs, its host entries still work and it is locatable by name (§11.2), but no later compilation can bind
against it (REPO-8).

### 7.2 What each build emits, and the library shape
A group's build writes its assembly `<out>.dll`. Every outermost program or function unit AND every program or
function prototype unit in it contributes its host-entry class (§4.3): a definition's entry registers this assembly's
programs (`EnsureRegistered`) before activating; a PROTOTYPE unit's entry activates by name and registers nothing —
the definition is located at run time (§11.2) — so a prototype library is a compile-time repository entry and a
callable typed surface at once. A PARAMETERIZED definition emits `public static class {Cs}` with
`[CobolUnit(Kind = ParameterizedClass)]` + `[CobolSkeleton]`. A group holding both a prototype and its definition
(§10.6.2 SR2/SR3) emits one host-entry class (the definition's). A group with no main program unit emits
`OutputKind.DynamicallyLinkedLibrary`, no `Main`, no `runtimeconfig.json`. An expansion whose code references no class of the group
emits its own assembly (§10.5).

### 7.3 Who references whom
A unit that resolves a PROGRAM or FUNCTION from the repository takes NO reference to the entry's assembly: the
signature is read from metadata at compile time and the program is activated by name at run time. A unit that
resolves a CLASS, INTERFACE, skeleton or expansion references that assembly (its types appear in the generated code).
A .NET host references the assemblies whose host entries it calls and, in a composed (trimmed or AOT) run unit, every
COBOL assembly of the run unit (§11.4); no COBOL caller ever references its callee's. Mutually calling program groups therefore compile
in any order once each side has a prototype or definition published (§10.6.3 GR1); mutually referencing CLASS groups
compile two-pass (§10.4).

### 7.4 Repository directories and the locator
`RepositoryLocator.Find(directory, kind, externalizedName)`: (1) **the named file** — `<folded name>.dll` in the
directory, read with `PEReader(stream, PEStreamOptions.PrefetchMetadata)` and closed; if its index lists `kind:name` it
is the answer and nothing else is read; (2) otherwise **the directory index** — every `*.dll` (files only, no recursion,
no reparse points, sorted ordinally) read the same way once per process, cached by `(fullPath, length,
lastWriteTimeUtc)` as `(kind, word, externalized name, type handle)` rows; a file that is not a managed PE or carries no
`[CobolRepository]` is remembered as "not an entry". Two DIFFERENT assemblies (different MVIDs) carrying one `kind:name`
found by step 2 are a collision (`Collision` in the answer); identical copies (same MVID) are one entry. The word rows
answer `FunctionByWord` (§8.3). The same locator serves `--repository DIR` at compile time and
`AppContext.BaseDirectory` at run time.

### 7.5 The file protocol
Writing: emit to `{output}.{Guid:N}.tmp`, `File.Move(…, overwrite: true)` with a bounded retry (10 × 50 ms on
`IOException`/`UnauthorizedAccessException`), then loud failure naming the file; a failed emit deletes its temp;
`AssemblyPackager` likewise; an expansion assembly whose target already carries the same `ExpansionKey` is not
rewritten (§10.5).
Reading: never a lingering handle (prefetched metadata, stream closed; r6-meta). A loaded assembly cannot be replaced
from inside its process (circ5): the compiler never loads a repository entry, and the stale-caller tests run the
callee out of process.

### 7.6 Determinism
Identical sources, options and repository → identical answers, diagnostics and bytes (sorted listing, pure decode,
documented order).

---

## 8. Resolution

### 8.0 The resolver and its providers
```csharp
internal interface IRepositoryProvider
{
    CalleeSignature? Program(string externalizedName, SourcePosition before, out RepositoryOrigin origin);
    UserFunctionSignature? Function(string externalizedName, SourcePosition before, out RepositoryOrigin origin);
    UserFunctionSignature? FunctionByWord(string userFunctionName, SourcePosition before, out RepositoryOrigin origin);   // §8.4.6.7: the WORD (§8.3)
    OoClassSymbol? Class(string identity, out RepositoryOrigin origin);
    OoInterfaceSymbol? Interface(string identity, out RepositoryOrigin origin);
    SkeletonSymbol? Skeleton(string identity, out RepositoryOrigin origin);
    OoClassSymbol? Expansion(string identity, ExpansionIdentity triple, out RepositoryOrigin origin);
}
internal sealed class RepositoryResolver(GroupRepositoryProvider group, MetadataRepositoryProvider external, ClrTypeProvider? clr, EditionContext edition) { … }
```
`GroupRepositoryProvider` is today's tables with the §8.2/§8.3 corrections; `MetadataRepositoryProvider` opens nothing
until the first specifier the group cannot answer; `ClrTypeProvider` is §4.6's slot.

### 8.1 D-R2 — when the AS phrase is required
Exactly when the externalized name differs from the declared word (spelling, or a non-COBOL name); without AS the word
IS the externalized name under `ExternalizedNames`' fold (DOC-A.1-68).

### 8.2 Program specifier (SR14, GR10)
1. GR10 a): an outermost program DEFINITION whose EXTERNALIZED name equals the specifier's and whose source element
   PRECEDES the specifier's → its `CalleeSignature`. (`BuildProgramDetailsTable`'s order-blind determination is
   replaced: a later definition is neither a) nor c), because the output being produced is never in the search space
   (§9.3); a specifier naming only a later definition with no prototype is REPO-1 — SR14.) Derivation: SR14 admits
   "the name of a program definition specified previously in this compilation group" and a program "for which
   information exists in the external repository"; a definition that FOLLOWS the specifier is the first only if it
   precedes it, and the second only through a repository entry. The previous build of the output being produced is
   the information this compilation UPDATES (§8.13 ¶3), not information it resolves against, so the legality of a
   source text never depends on whether an earlier build of it exists. The corpus programs that name a later
   definition with no prototype (2002 `pb237_program_prototype`, `pb239_call_address_identifier`,
   `pb549_program_address_identifier`, `pb817_restricted_program_pointer`, `pb1063_restricted_pointer_by_content`,
   `w66g_pb1475_strong_type_locale_identification`; 2014 `pb848_pointer_usage_to_optional`; negatives `pb237-call-
   prototype-argument-count`, `pb817-set-program-pointer-signature`, `pb970-pointer-by-content-prototype-alnum-formal`,
   `pb1063-program-pointer-by-content-sr22`, `w66g-pb1475-strong-type-dpc-differs`) are therefore non-conforming as
   written; kb/Work PB989's slice gives each a program prototype definition of its callee ahead of every other unit
   (§10.6.2 SR1), which keeps each test's subject — a prototype-checked CALL, pointer or argument — under GR10 b),
   with its expectation files re-pinned for the shifted lines (§21 slice 5).
2. GR10 b): a program PROTOTYPE definition in the group (prototypes precede all other units, §10.6.2 SR1).
3. GR10 c): `MetadataRepositoryProvider.Program(externalizedName)` → the first entry of `P:name` in the search order,
   its signature decoded from that entry's host-entry `Call` method (§5.3).
4. None → REPO-1; the prototype is not registered (a CALL through it is COBOLNET1760).
`ProgramPrototype.Signature` is non-nullable. The CALL it binds is the existing one (§4.2).

### 8.3 Function specifier (SR10, GR11) — determination D-R3
GR11 reads literally, keyed by EXTERNALIZED NAME: a) "if the externalized name of the function prototype is the
externalized name of a function definition specified previously in the same compilation group" (NOTE 2 makes a no-AS
prototype's externalized name its word); b) a function prototype definition in the group with that externalized name;
c) "the details are taken from the external repository for the function with the same name as the externalized name
of the function prototype". SR10 states LEGALITY with three alternatives, one of which is the WORD: "the name of a
function definition specified previously in this compilation group". The two can part: `FUNCTION F` with the group
definition `FUNCTION-ID. F AS "G"` is SR10-legal by its second alternative, yet GR11 a) fails ("F" ≠ "G"), b) has no
prototype, and c) has details only if some repository function is externalized "F".
Two ISO clauses then meet at one word. GR11 takes the details from the function EXTERNALIZED "F" — in the group by
a), in the repository by c). §8.4.6.7 lets the user-function-name F be referenced "in the REPOSITORY paragraph of any
source element that follows that function definition" and "in any subsequently-compiled source unit that specifies
that user-function-name as a function-prototype-name in its REPOSITORY paragraph" — the WORD of `F AS "G"`. When both
a function externalized "F" and a different function whose word is F exist — in one compilation group, or one in the
group and one in the repository, or both in the repository — the two clauses name different functions and the
standard does not rank them; that is latitude, settled by the precedence ISO → GnuCOBOL (CLAUDE.md rule 1).
**GnuCOBOL 3.2.0 was probed (cobc, WSL, 2026-10-05):** with `FUNCTION-ID. FX AS "F"` and `FUNCTION-ID. F AS "G"` both
present — in one compilation group, or as separately compiled units linked together — `FUNCTION F` calls the function
externalized "F" (prints 1); with only `F AS "G"` present, in either arrangement, it fails at run time ("user-defined
FUNCTION 'F' not found"): GnuCOBOL resolves by externalized name only and has no repository or word lookup, so on the
conflict it answers, and on the word-only case it has nothing to follow and ISO governs.
**Determination D-R3 (owner decision 2026-10-05):**
1. GR11 a)–c) are applied exactly as written, keyed by externalized name. Whichever of them hits names the function
   (GnuCOBOL's answer and the standard's text).
2. When all three miss and the specifier is legal by SR10's word alternative or by §8.4.6.7, the details are taken
   from the function the WORD names: the group definition whose user-function-name is F (its externalized name "G" is
   what is activated), else the repository entry whose recorded word is F (`FunctionByWord`; every entry carries the
   word beside the externalized name, §5.3) — so `FUNCTION F` in a later-compiled unit never draws REPO-1 on legal
   source.
3. **Whenever step 1 selects a function and a DIFFERENT function whose user-function-name is F exists** — in the
   compilation group (preceding the specifier) or in the repository — the compile warns **REPO-13** naming both
   functions and both clauses ("`FUNCTION F` activates the function externalized "F" (§12.3.8.4 GR11); the function
   `F AS "G"` of … is reachable only through a prototype `F AS "G"` (§8.4.6.7)"). The rule is one rule wherever the
   two functions live; `--repository-check` is not involved (two different functions, not one signature in two
   states).
Legal source is never rejected, so the goldens that write `FUNCTION PB303FN` for `AS "PB303FNX"`
(`2002/pb303_as_externalized_name`, `2014/pb303_as_externalized_name_2014`) and `FUNCTION W59HFN` for a prototype
`AS "Cobol.Net.Runtime"` (`2002/w59h_non_cobol_names_not_found`) keep their source and their expectations; new goldens
pin REPO-13 in one group (`FX AS "F"` and `F AS "G"` both preceding `FUNCTION F`), REPO-13 across the repository (a
repository "F" beside `F AS "G"`), the word reaching a later-compiled unit (`F AS "G"` in a library, `FUNCTION F` in a
program compiled against it), and the group-only word case. REPO-1 for a function specifier cites SR10 only when NONE
of its alternatives and not §8.4.6.7 holds. The group provider keys both tables by externalized name and keeps the
word → definition map for steps 2 and 3 (`BuildUserFunctionTable` keys by word and `UserFunctionsOf` skips a no-AS
specifier: both rewritten). `docs/CONFORMANCE.md` carries this determination under GR-12.3.8.4-11 (§9.4).

### 8.4 Class and interface specifiers (SR6/SR9, GR6/GR9)
1. EXPANDS → §10.5. 2. The group: `Find(word)` / `FindByExternalizedName(lit)`; BASE when no definition claims it.
3. The repository by identity (`lit ?? word`), first match in the documented order (§8.6); a hit imports the symbol
(§10.1) under the importer's word (§8.7). 4. `ClrTypeProvider` (§4.6). 5. None → REPO-1. The group wins a tie.

### 8.5 Property specifier (SR16, GR2 ¶2, GR15) — determination D-R5
An accessor method's externalized name is its property's externalized name (`__GET_<P>`/`__SET_<P>` are CLR member
names, not externalized names). `PROPERTY p [AS lit]` is satisfied when a declared class or interface (group or
imported; base chains and implemented interfaces included) has an accessor pair whose property externalized name is
`lit ?? p`. None → REPO-1.

### 8.6 Search order, shadowing, duplicates, case
Order: `--repository` directories in the order given (within one, §7.4), then `--reference` assemblies in order. **The
first entry of `kind:name` wins.** A later DIFFERENT entry (another MVID) of the same `kind:name` is REPO-2, a warning,
when its record differs (the §9.2 compare). Identical copies (one MVID in two places) are one entry. §8.3.2.2's "same
instance" sentence is run-unit scoped and licenses no compile-time error. Within the group: unchanged. Cross-kind: a
specifier finds only its kinds. Two NEEDED class/interface/expansion assemblies sharing one assembly simple name →
REPO-6 (one assembly per simple name per load context). Comparisons: `ExternalizedNames.Same`; file-system case is
irrelevant (the locator compares folded names over the listing).

### 8.7 Decoding into the importer's word space — the `IdentityScope`
Words are per source element (§8.4.6.4): P1 `CLASS ACCT AS "BankAccount"` and P2 `CLASS ACCT AS "Ledger"` are two
words for two identities. `OoClassTable` therefore stores IMPORTED symbols by IDENTITY only (group-wide, one symbol per
identity), and each source element's `IdentityScope` — `OoRepositoryScope` extended to program prototypes, function
prototypes and dynamic-length structures — maps word → identity and identity → word. Decoding a record for element E
maps every identity through E's scope: the first word whose identity it is becomes the symbol's `Name` IN E (the
`OoClassSymbol` keeps one `CsName`; its word is scope-dependent and `OoNameResolution.Lookup` composes scope + table
as today). An identity E has no word for is imported anonymously (reachable from descriptors, never from `Find(word)`,
spelled `class "<identity>"` in diagnostics). A prototype identity a signature mentions (a restricted program-pointer)
maps through the same scope to E's program-prototype word, else stays anonymous.

### 8.8 Nesting, COMMON and §8.4.6
Unchanged: a REPOSITORY paragraph lives only in an outermost program, a function, a class or an interface
(§12.3.3), and §12.3.4 GR1 makes its entries "apply to each directly or indirectly contained source unit". Resolution
runs once per paragraph in the outermost element's bind; contained units inherit the RESOLVED prototypes. `AS NESTED`
never consults the repository (§14.9.4.3 SR15); the run-time scope rules 1–3 are untouched. Contained programs are not
externalized (§8.3.2.2) and have no host entry.

---

## 9. The two mechanisms and the documented determinations

### 9.1 Update — `--repository-update on|off` (default `on`)
ON emits the description and OO records; OFF emits only the index, the `[CobolUnit]`s, the registrar and the host
entries (`HasSignatures = false`). Why ON: §8.4.6.7's "if the external repository is updated" is the state in which
separately compiled units reference each other. `CompilerDriver.Options.RepositoryUpdate`.

### 9.2 Check — `--repository-check on|off` (default `on`)
Compares every outermost program/function definition and prototype, class and interface of the group by the ONE
compare (`PrototypeSignatures.Same` over the decoded record — descriptions, modes, OPTIONAL, RETURNING; never names
or intermediate grouping; `OoConformance`'s pair checks over rosters + base/implements identities) against (a) the
PREVIOUS BUILD OF THIS OUTPUT (the file callers were compiled against; one prefetched read before the rename) and (b)
every entry in the search space that publishes the same `kind:name` as a prototype or a definition (a definition that
differs from a published prototype, a prototype that differs from a published definition). A difference is **REPO-9,
a warning** naming the entry and the first differing position ("callers compiled against that entry are checked at
run time by §14.9.4.4 GR3 d) when checking is enabled in both, and adopt the arguments positionally otherwise;
recompile them"). An old-schema previous build is REPO-12 (informational) and skipped. Why a warning: a signature
change is a normal event, this compile is the update, and §10.6.2 SR2/SR3's "shall" is scoped to one compilation
group. Cost: one header read per entry the resolver already opened, plus the previous output when it exists.
`CompilerDriver.Options.RepositoryCheck`.

### 9.3 Naming the repository
`--repository DIR` (repeatable, in order; default: the output directory); `--reference FILE.dll` (repeatable, after the
directories). The output being produced is never consulted for RESOLUTION (its previous build only by §9.2). Spelling
precedent: A.1 item 66's own words; Micro Focus `REPOSITORY` directive family (spelling only).

### 9.4 The user documentation and the `docs/CONFORMANCE.md` rows (verbatim)
**Manual, "Separately compiled units and the repository":**
> A WiseOwl COBOL assembly is the external repository entry for every program, function, class, interface, program
> prototype, function prototype, parameterized class or interface and expansion it compiled (ISO §8.13). When your
> REPOSITORY paragraph names a program, function, class, interface or property this compilation group does not
> define, the compiler looks in the repository: first the `--repository DIR` directories, in order (by default the
> output directory), then the `--reference FILE.dll` assemblies, in order. The first entry found is used; a later
> differing entry is a warning (REPO-2); none is an error (REPO-1). A program or function found there is checked at
> compile time exactly as one defined in your compilation group, and is activated at run time by name from the
> assembly in the run unit's application directory that carries it. A class or interface found there is referenced
> directly, and its assembly must be deployed beside the run unit. **Updating:** `--repository-update on` (default)
> records this compilation's signatures in the assembly it produces; `off` produces an assembly other units cannot
> be compiled against (it can still be called by name at run time). **Checking:** `--repository-check on` (default)
> warns (REPO-9) when a unit compiled here differs in signature from the previous build of the same output or from
> a prototype or definition of the same name in the repository; `off` suppresses it. A caller compiled against an
> earlier signature is checked at run time when EC-PROGRAM-ARG-MISMATCH checking is enabled in both programs.

**Manual, "Calling a WiseOwl COBOL program from .NET":** reference the COBOL assembly and `Cobol.Net.Runtime`; deploy
both beside your host. Every program and user-defined function has a class `Cobol.<assembly>.<NAME>` with a method
`Call` whose parameters are its PROCEDURE DIVISION USING formals — a `ref` parameter for BY REFERENCE (the variable
receives what the program stored), a plain parameter for BY VALUE, nullable when OPTIONAL (`null` passes OMITTED) —
and whose return value is its RETURNING item; a program's class also has `Cancel()`. Numbers are `long` or `Int128`
when the PICTURE has no decimal places and `decimal` when it has; text is `string`; a group is a nested record struct
named `<group>_Record`, with `AsImage()` and `FromImage()` for its character image. Run your calls inside
`RunUnit.Run(ru => { … })`, which begins a run unit and ends it (closing its files) when the body returns; a call
outside it joins the ambient run unit of your execution context. Every call crosses the same boundary a COBOL CALL
crosses: the program keeps one WORKING-STORAGE per run unit until you `Cancel()` it, recursion is refused unless the
program is RECURSIVE, and an INITIAL program is re-initialized per call. **What a call can throw:**
`ArgumentException` — before the program runs — when a value would lose anything on the way in (a digit at either
end, a sign, characters beyond the item's length: `ArgumentOutOfRangeException`), when a non-OPTIONAL parameter is
`null`, or when you pass one variable to two `ref` parameters whose COBOL descriptions differ (to two parameters of
one description, the program gets one storage for both); `CobolCallException` with its EC-* name (`EcName`) when the
program cannot be activated or a condition propagates out of it; the run-unit-ended exception when the program
executes STOP RUN or a fatal condition terminates the run unit — the run unit's files are closed and your process
continues; `FormatException` when the program leaves a numeric item holding characters that are not a number of its
description (the variable is left as it was). **Trimmed and AOT applications** have no dynamic loading: your host
references every COBOL assembly the run unit uses and calls each one's `__CobolModule.EnsureRegistered()` before the
first call; a program called by name from COBOL is then found among them. A program compiled against a different
signature than the one it is called with is detected at run time only when it enables EC-PROGRAM-ARG-MISMATCH checking
(`>>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON`); otherwise the arguments are read through its own description.

**`docs/CONFORMANCE.md`, DOC-A.1-141 (the §14.2.3 GR13 determination, verbatim):** "**Supported non-COBOL language
products: any .NET language, and only as the ACTIVATING element.** No non-COBOL element can be activated: a CALL, a
program-pointer or `ADDRESS OF PROGRAM` resolves only WiseOwl COBOL programs. **Mechanism:** every outermost program
and every user-defined function — definition or prototype — has a generated public class `Cobol.<assembly>.<name>`
in the assembly that compiled it, whose static `Call` method activates it through the same boundary a COBOL CALL
crosses (§14.9.4.4 GR3), and, for a program, a static `Cancel` method (§14.9.5). This is the only host interface.
**Parameter matching:** positional (§14.2.3 GR2), one `Call` parameter per USING formal in order; the C# compiler
checks the count and the types; a prototype library's `Call` against a changed definition is EC-PROGRAM-ARG-MISMATCH
when the definition enables that checking. **Data type representation:** a numeric item without decimal places is
`long`, or `Int128` when its storage can hold more than `long` can; one with decimal places is `decimal` (the
runtime's `CobolDec` beyond 28 digits or 28 places); a floating-point item is its own CLR type; an alphanumeric,
national, boolean or edited item is `string`; an index is `long`; pointers are `ManagedPointer`, `ProgramPointer`,
`FunctionPointer`; an object reference is its class's CLR type; a group is a generated record struct with
`AsImage`/`FromImage`. Values are landed by §14.2.3 GR9/GR10's COMPUTE, SET or MOVE, and a value that the landing
would change in any way — a digit lost at either end, a sign dropped, a character beyond the item's length — is
refused with `ArgumentOutOfRangeException` before the program is activated. BY REFERENCE is a C# `ref` parameter:
the program operates on a storage of the formal's description, and the variable receives its content when the call
ends; two `ref` parameters given one variable share one storage when their formals have one description and are
refused with `ArgumentException` otherwise. **Return of a value:** the RETURNING item is the return value; numeric
content that is not a valid representation of its description (the NUMERIC class test, §8.8.4.4.4) — in the
RETURNING item or in a BY REFERENCE formal — is never converted: that variable keeps its value and `FormatException`
is thrown after the other write-backs. GOBACK and EXIT PROGRAM return normally (item 65); STOP RUN and a fatal
condition that terminates the run unit end the run unit as item 167 states. **Omission of parameters:** an OPTIONAL
formal is a nullable `ref` parameter, and `null` passes OMITTED (§14.9.4.4 GR11). Same in every edition."

**`docs/CONFORMANCE.md`, GR-12.3.8.4-11 (the function-name determination D-R3, verbatim):** "GR11 a)–c) are applied as
written, by externalized name. When all three miss, a `FUNCTION F` specifier that is legal by §12.3.8.3 SR10's word
alternative or by §8.4.6.7 takes its details from the function whose user-function-name is F: the earlier definition in
the compilation group, else the repository entry recorded under that word. Whenever GR11 selects a function
externalized "F" and a different function whose word is F exists, in the compilation group or in the repository, the
externalized name is used (GnuCOBOL 3.2.0 does the same) and the compiler warns, naming both functions. Legal source is
never rejected."

**DOC-A.1-66:** the determination above with the four options and defaults; the repository is the compiled assembly's
metadata; witnesses: §17.1. **DOC-A.1-67:** §5.4; schema version and runtime ABI major compared (REPO-7); nothing else.
**DOC-A.1-138** gains: "An expansion is published with its (definition, actuals) identity and a key that hashes what
it means — the skeleton's post-text-manipulation source, its compile state and specifier resolutions, and the published
form of every class it names — and a skeleton with its source and specifier resolutions. A compilation group that
expands a parameterized class first looks for an expansion of the same name and key and, finding one, uses it (one
class instance per run unit, §9.3.12), even when that expansion's assembly references the group's own earlier build.
Otherwise the expansion is compiled into its own deterministic assembly named by its externalized name when its code
names no class of the expanding group, and into the group's assembly when it does. An expansion of the same name and
actual parameters with a different key is out of date: the compiler warns, naming it and every assembly built against
it, which are recompiled as any out-of-date caller is." **DOC-A.1-161:**
"Within the compilation group the specifier names the definition by its declared word, or by its externalized name
when `AS literal-1` is written, and otherwise the standard class BASE; a group definition takes precedence. Outside it,
the class is the first entry in the documented search order whose externalized name equals the specifier's (the
Annex C fold, DOC-A.1-68), imported under the specifier's word with its assembly referenced; a later differing entry
warns (REPO-2); none is an error (REPO-1); an EXPANDS specifier first imports an existing expansion of the same name
and identity and otherwise creates the class." **DOC-A.1-162:** the same for interfaces, with "there is no BASE step".

---

## 10. Classes and interfaces across assemblies

### 10.1 Importing a class
`MetadataRepositoryProvider.Class(identity)` decodes the TypeDef's attributes into a `ClassRecord` and builds an
`OoClassSymbol { Origin = Imported(path, simpleName, fullName) }` (`OoSymbolOrigin { Source, Standard, Imported, Clr }`
replaces the null-`Ctx` test): `CsName` = the TypeDef's full name (namespace included); `FactoryCsName`; `IsFinal`;
`Base`/`Implements` resolved by identity through the same provider (transitively; the standard class → `StandardBase`;
unreachable → REPO-11); one `OoMethodSymbol` per `[CobolMethod]` over decoded `DataItem`s; `OverrideOf` re-linked;
accessors; `Raising`. `OoClassTable.Import(symbol)` registers by IDENTITY (§8.7). Downstream — INVOKE binding,
`ConformanceDescriptor`, `__CobolInvoke` (virtual on `CobolObject`), typed `OBJECT REFERENCE`, IMPLEMENTS and override
checks — reads the symbol as a source one.

### 10.2 References and Roslyn diagnostics
`BackendOptions` gains `ReferenceAssemblies` (every assembly a class, interface, skeleton or expansion was taken from)
and `OutputKind`; `RoslynBackend.Compile` appends their `MetadataReference`s. Imported types are named by full name
under `global::`, so a source `ACCOUNT` and an imported `ACCOUNT` never meet and two imports of one CsName are
distinct. **Warnings.** The driver surfaces only `Error` diagnostics from the backend, because generated C# for legal
COBOL draws warnings no emitter change can remove (CS1718 for `IF B = B`, r6-warn), so a warning-free corpus is not a
property a test can assert. Three warnings are escalated to errors on generated code (`SpecificDiagnosticOptions`):
**CS0435, CS0436, CS0437** — a source namespace or type meeting an imported type or namespace of the same full name
(r6-warn), each meaning the compile silently chose one of two declarations, which in this design is a repository
defect that must stop the compile. Collisions the C# compiler already reports as errors (an ambiguous imported type,
CS0433) stay errors. No `.diag.txt` changes.

### 10.3 What crosses
An inter-assembly INVOKE emits what an in-group one emits (the method convention of `COBOLNET_OO_DESIGN.md`); strong
groups cross as leaf vectors (PB1116).

### 10.4 Bootstrap order for classes
Mutually referencing groups compile two-pass (circ4) — A without the classes that need B, then B, then A — or merge
into one group, or break the cycle through an interface in a third group. The first compile with a specifier the
search space cannot answer is REPO-1 naming the identity; no other diagnostic is involved.

### 10.5 Parameterized classes and interfaces across assemblies — D3
In-group, `OoExpansion.Expand` re-parses a skeleton's token run with each formal replaced by its actual ("An
expansion of a parameterized class is treated in all respects the same as if it were a class that is not a
parameterized class", §9.3.12; DOC-A.1-138/-201). §9.3.12 then fixes identity: "Within a run unit, two classes with
the same externalized object-class-name that are created by expanding the same parameterized class with the same
actual parameters are the same class instance" — ONE CLR type per (externalized name, definition identity, actual
identities) among the groups compiled against one meaning of that triple, by reuse of an existing expansion keyed by
that meaning; a group compiled against a superseded meaning is a stale caller that REPO-9 names (below).
- **A skeleton publishes itself** (§7.2): formals with kinds; the SOURCE (the on-channel token text after text
  manipulation, from `CLASS-ID`/`INTERFACE-ID` to the end marker); its specifier RESOLUTIONS — every class/interface
  word its own REPOSITORY declares, resolved in the DEFINING group to an identity (so `CLASS NODE` inside the skeleton
  means the defining group's NODE wherever it is expanded); its `CompileState` (§6.1): edition, `--permissive`,
  `--sign-encoding`, `--flag-extensions`, the `CobolWordsMap`, the TURN/PROPAGATE/REF-MOD-ZERO-LENGTH folds at the
  definition — every compile-time input that changes what the source means, kept exhaustive by the §6.1 drift test.
  The importer re-parses the recorded source UNDER the recorded state (`FragmentParse.ParseTokens` takes it as an
  argument and the binder's option-dependent arms read it, not the importer's options), substitutes the ACTUALS'
  identities (not the importer's words) and binds the skeleton's own words to the recorded identities through a scope
  seeded from `Resolutions` — so an expansion means the same thing in every group, and the `ExpansionKey` (below) is
  a complete statement of that meaning.
- **An expansion publishes its identity and its key** `[CobolUnit(ExpandsDefinition, ExpandsActuals, ExpansionKey,
  ExpansionOwnAssembly)]` — identities, not words. The `ExpansionKey` is a SHA-256 over the canonical record encoding
  (§6.5) of everything that decides what the expansion MEANS: the definition's identity, the skeleton's recorded
  source, its formals with their kinds, its `CompileState`, its resolutions each with the resolved class's or
  interface's recorded record, and the actuals' identities each with its recorded record. Bytes, MVIDs, file names and
  the compiler version are not in it: two expansions with one key are the same class text over the same published
  surfaces of everything it names, and either serves.
- **`CLASS N [AS lit] EXPANDS P USING A1…An`** — one sequence in every group: (1) P is the group's skeleton, else the
  repository's (REPO-1 if neither); A1…An are in-scope classes/interfaces (group or imported); arity/kind checks as
  in-group (COBOLNET2240). (2) The key K is computed. (3) **Import before expand:** an expansion in the search space
  with externalized name `lit ?? N` and key K is imported (§10.1) and its assembly referenced — also when that assembly
  references this group's own previous build. That is an assembly reference cycle, which the CLR and Roslyn accept
  (circ: circular assembly references compile and run); the existing expansion was compiled against the members this
  group published, and any change to them that matters to it changes K. (4) Otherwise the expansion is made here:
  (a) when its code references no class or interface of THIS group (every actual and every resolution lies outside
  it), as its OWN compilation — the skeleton's recorded source under its recorded state, the referenced assemblies as
  references — into `<externalized name>.dll` beside the group's output, deterministically (§6.5, §7.6); an existing
  file of that name and key is left as it is, whatever its bytes, so two groups that expand one key write one file and
  the CLR loads one assembly of that simple name — one class instance, as §9.3.12 says; (b) when its code references a
  class or interface of this group, in the group's assembly: a separate assembly would need this group's NEW assembly,
  which does not exist while the group compiles. `ExpansionOwnAssembly` records which.
- **Stale expansions.** An entry with name `lit ?? N` and the same (definition, actuals) identities but another key
  was built from a skeleton source, a compile state or a dependency's record that has since changed. It is never
  imported; the expander proceeds to (4) and warns **REPO-9** naming the stale entry, the first differing key
  component (skeleton source, compile state, a resolution's record, an actual's record) and every assembly in the
  search space that references the stale entry (read from each `AssemblyRef` table, nothing loaded). Those groups were
  compiled against the earlier class and are its stale callers, recompiled as any stale caller is (§15.4). A
  placement-(a) target file of the same name with another key is overwritten (this compilation is the update) under
  the same REPO-9; one whose schema, runtime major or call ABI differs is overwritten under REPO-12. Nothing compares
  MVIDs, so a legitimate rebuild — a later compiler, an unchanged key — reuses the file, and REPO-4 has no role here.
- **The sequence that crosses groups.** G1 publishes the skeleton P, whose source names G1's class NODE. G2 writes
  `CLASS PA EXPANDS P USING A3` (A3 in G3): PA's code references NODE and A3, no class of G2, so (4a) writes `PA.dll`
  referencing G1 and G3, key K. G1 later writes the same expansion: (3) computes K from G1's current P and NODE; if
  neither changed it equals `PA.dll`'s and G1 imports it (the G1 ↔ `PA.dll` cycle above) — one CLR type. If G1 changed
  P's source or NODE's published surface, the key is K′: (4b) expands PA in G1 (its code references NODE, a G1 class)
  and REPO-9 names `PA.dll` and G2; G2, recompiled, imports PA from G1's assembly (key K′). Until it is, the run unit
  carries G2 bound to the earlier PA — the stale-caller case the warning names, since §9.3.12's one class instance
  cannot hold between a class and a superseded edition of it.
- **The "shall not" of §9.3.12** ("If two classes expand a parameterized class with different actual parameters, they
  are not the same class instance and shall not have the same externalized object-class-name") is a run-unit rule. It
  is reported as an ERROR (COBOLNET2240) only where the two classes will NECESSARILY share a run unit: the same-name,
  different-triple expansion is in an assembly THIS GROUP REFERENCES (an actual's or the definition's), or it is the
  existing `<name>.dll` that placement (a) would overwrite with a different class. A same-name, different-triple
  expansion that is merely elsewhere in the search space is REPO-2 (a warning), as every other same-name difference
  is (§8.6).
- An imported skeleton is `IsParameterized` in the importer's table (GR1's message applies to a misuse).

### 10.6 Interfaces
`[CobolUnit(Kind = Interface)]` on the public CLR interface; `Prototypes` from `[CobolMethod]`s; `Inherits` from
`Implements`. An interface's literal-1 is externalized for real.

---

## 11. Interaction with the run-time registry

### 11.1 What stays as it is
The whole activation boundary (§4.1): CALL by prototype, literal or data-name, program and function pointers, CANCEL,
the instance model, the §8.4.6.3 scope, EC-PROGRAM-NOT-FOUND, GR3 d)'s run-time check.

### 11.2 The locate step, module registration and the lookup
Outermost nodes live in a per-kind dictionary keyed by `CallName` (`ExternalizedNames.Comparer`) beside `_order`: rule
4, `EntryOf` and `FunctionAddressOf` are O(1). **One module-registration set per run unit, one registration member
per module:** every assembly's `__CobolModule` has exactly one public registration member, `EnsureRegistered()`, whose
body is `ProgramTable.RegisterModule(registrar, runtimeVersion, callAbi, Register)` with this module's compiled-against
versions emitted as constants and its private `Register` as the action. The emitted `Main`, a host entry, a host
composing a run unit (§11.4) and the probe all reach a module through that member, so there is one way in.
`RegisterModule`:
1. returns at once when the registrar's full name is already in the run unit's set (a module a host registered is
   never registered again by a probe, and vice versa — a second `Register` would duplicate every node in `_order`);
2. refuses a module whose runtime major differs from the running runtime's or whose call ABI differs from the
   runtime's (§11.3, §15.4);
3. runs `Register` into a STAGING list and commits it only when none of the module's outermost `kind:name`s is
   already registered by another module: within a run unit one externalized name is one instance (§8.3.2.2), so a
   second module carrying it is refused whole, naming both modules, and nothing of it is registered.
A refusal is a `CobolCallException` with EC-PROGRAM-NOT-FOUND and the reason, the condition the activation that
needed the module raises anyway, so `CarriedNames` is unchanged; reached from the probe it is the probe's failure
reason (below).
```csharp
private bool ProbeSiblingModule(string name, bool wantFunction)
{
    if (!RuntimeFeatures.DynamicProbe)                                   // the feature switch (§15.5)
    { _probeFailure[name] = "the dynamic probe is disabled (trimmed/AOT): the host registers every module it composes"; return false; }
    var kind = wantFunction ? RepositoryUnitKind.Function : RepositoryUnitKind.Program;
    if (!_probedNames.Add($"{(wantFunction ? 'F' : 'P')}:{name}")) return false;
    var hit = RepositoryLocator.Find(AppContext.BaseDirectory, kind, name);   // the named file first; the directory only on a miss
    if (hit is null) return false;
    if (hit.Collision is { } other) { _probeFailure[name] = $"'{hit.Path}' and '{other}' both carry {name}"; return false; }
    if (VersionSkew(hit.RuntimeVersion, hit.CallAbi) is { } skew) { _probeFailure[name] = $"'{hit.Path}': {skew}"; return false; }
    try
    {
        var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(hit.Path);
        var ensure = asm.GetType(hit.Registrar)?.GetMethod("EnsureRegistered", Type.EmptyTypes);
        if (ensure is null) { _probeFailure[name] = $"'{hit.Path}' has no registrar {hit.Registrar}"; return false; }
        ensure.Invoke(null, null);                                       // → RegisterModule, the one way in
        return _byName.ContainsKey(kind, name) || Fail(name, $"'{hit.Path}' did not register {name}");
    }
    catch (Exception e) when (e is FileNotFoundException or FileLoadException or BadImageFormatException
                                  or TargetInvocationException or TypeLoadException)
    { _probeFailure[name] = $"'{hit.Path}': {e.GetBaseException().Message}"; return false; }   // never escapes the CALL (§14.9.4.4 GR3 b))
}
```
The named file is loaded without a directory scan (`PAY.dll` beside `PAY - Copy.dll` keeps working); the index is read
only on a miss, so a program whose assembly is not named after it is found too. `FileNotFoundException` covers a file
removed between the listing and the load; a `RegisterModule` refusal arrives wrapped in `TargetInvocationException`.
`_probeFailure[name]` is appended to the EC-PROGRAM-NOT-FOUND message the CALL raises, so a disabled probe, a
collision, a version skew, a load failure, a duplicate name or a missing registrar is diagnosable instead of silent.
`--repository-update off` assemblies are reached (the index is unconditional). A program class that fails to LOAD
inside the instance creation is EC-PROGRAM-NOT-FOUND with the loader's message — the classification
`FunctionLocateResourceWitnessTests` pins (never EC-PROGRAM-RESOURCES).

### 11.3 Collision and version skew at run time
Step 2 finding two different assemblies carrying one `kind:name` → EC-PROGRAM-NOT-FOUND naming both; an assembly whose
`RuntimeVersion` major differs from the running runtime's, or whose `CallAbi` differs from the runtime's
`RuntimeAbi.CallAbi`, → EC-PROGRAM-NOT-FOUND naming both versions (loading it would fail later, inside the callee, as
a `MissingMethodException`, or read argument images the two sides lay out differently, §15.4). `RegisterModule`
applies the same refusal to every module however it is reached — the emitted `Main`, a host entry, a composing host —
because the versions are its own parameters.

### 11.4 The three resolutions and the deployment contract
(1) Compile time: the locator over the `--repository`/`--reference` space. (2) Run time, programs/functions: the
locator over `AppContext.BaseDirectory` while the probe is on, and in every case the modules already registered — by
the emitted `Main`, by a host entry's own `EnsureRegistered`, or by a host that composes the run unit. (3) Run time,
assemblies referenced statically (classes, interfaces, expansions; a host's COBOL assemblies): the CLR loader by
assembly simple name from the application directory. They agree by the deployment contract the manual states — a
build's output deploys under its compiled file name in the run unit's directory — not by construction; a missing class
assembly fails as any .NET application's missing reference does, a missing program is EC-PROGRAM-NOT-FOUND with the
probe's reason.
**A composed run unit (trimmed or AOT; owner decision 2026-10-05).** With the probe switched off nothing loads an
assembly by path, and a COBOL main's assembly does not reference its callees' (§7.3), so a COBOL main alone cannot
reach another assembly's program: its CALL is EC-PROGRAM-NOT-FOUND with the reason "the dynamic probe is disabled". The
application is then a .NET host that references every COBOL assembly of the run unit, calls each one's
`__CobolModule.EnsureRegistered()` inside `RunUnit.Run`, and activates the main program through its host entry
(`Cobol.APP.MAINP.Call()`); every COBOL CALL inside then finds its callee registered (r7-compose: the CALL fails with
only the main's module registered and succeeds with both). The static references root every program class for the
trimmer, and no reflection is involved.

### 11.5 `argMismatchChecking`
Unchanged in meaning (the activated unit's EC-PROGRAM-ARG-MISMATCH enablement at its PD header). The manual adds: a
callee recompiled with a different signature after its callers is met at run time by EC-PROGRAM-ARG-MISMATCH when
checking is enabled in both, and otherwise — the default, since a unit registers its formals' descriptions only when its
own source enables the checking (§4.2) — by the lenient positional adoption; `--repository-check` warned at its
compile. A host entry always enables the check on its side (§4.3).

---

## 12. Existing code this design changes or deletes

| Today | Change | Callers updated in the same change |
|---|---|---|
| `ProgramEmitter.EmitEntryWrapper` (`public static class __CobolModule` in the global namespace with a public `Register()`; always `Main`) | the one-segment `namespace Cobol.<S>` (§4.5); `[assembly: CobolRepository(schema, runtimeVersion, callAbi, Registrar)]`; `Register` private, `EnsureRegistered()` the one public member (through `RegisterModule`; the emitted `Main` calls it, so no path registers a module twice); `Main` only with a main unit, `OutputKind` library otherwise; `global::` on every foreign name | `ProgramTable.ProbeSiblingModule` (reads `Registrar` from the attribute and invokes `EnsureRegistered` — atomic with the namespace move), `RoslynBackend.Compile`, `AssemblyPackager.Package` |
| `ProgramTable` (`_probedModules` keyed by name; `ProbeSiblingModule` loads `<name>.dll` and calls a global `__CobolModule.Register`; a linear `_order` scan for rule 4; `CallProgram`/`Cancel` take a non-null `callerPath`) | `RegisterModule(registrar, runtimeVersion, callAbi, register)` (one registration set per run unit, the version refusal, staged commit refusing a duplicate outermost name, §11.2); `ProbeSiblingModule` per §11.2 over `RepositoryLocator`; per-kind `CallName` dictionary; `callerPath` is `string?` in `CallProgram` and `Cancel` (null: an activator outside every program), and the null arm is the outermost boundary that turns a STOP RUN or a run-unit-terminating fatal condition into R48's run-unit-ended result after the §14.6.11 actions | `ProgramRegistry` (`CallProgram(string?, …)`, `Cancel(string, string?)`, `RegisterModule`), `RuntimeFeatures` (new), `RunUnit` (the run-unit-ended exception type, `RunUnitEndedException(ExitStatus, Abnormal)`, R48) |
| `ProgramRegistry` (public; XML remarks calling it the "emitted-surface shim", a facade whose members "forward") | `[EditorBrowsable(Never)]` on the type; remarks state that it is the generated code's ABI over the ambient run unit, callable from generated code in other assemblies, with no contract for a hand-written caller (§4.4) | — |
| `ProgramEmitter.Emit` (the outermost-unit loop emits a program class per definition and skips prototypes) | every outermost program, function and prototype → its host-entry class (§4.3) with its record attributes when `RepositoryUpdate`; skeleton → `[CobolSkeleton]` class; expansion → §10.5's steps; the assembly attribute and `[CobolUnit]`s always | `HostEntryEmitter` (new: the entry body over `CallEmitter`'s argument renderer), `RecordStructEmitter` (the published mode), `RepositoryAttributeRenderer` (new), `CompilerDriver` (an expansion assembly is a second emission) |
| `DataBinder.Unique` (private; the internal record struct's member names, unseeded: a subordinate `AsImage` or `Equals` drew CS0102) and `DataItem.Sanitize` | **landed for the internal scopes (kb/Work PB2098):** `CsNames.Allocate(baseName, scope)`: the Sanitized base, then the smallest free `_n`, over a scope seeded with the emitter's own members in that scope (§4.5) — the ONE allocator for internal and published names. `NameAllocator` (the `__`-prefixed temporaries, which no COBOL word can spell) is unchanged | `DataBinder` (every `Unique` caller), `RecordStructEmitter`, `HostEntryEmitter`, `OoEmitter` |
| `CallEmitter.ArgText` (renders a COBOL argument's `CobolArg`) | the one argument renderer is callable for "an argument of description D passed by CONTENT over a detached value" so the host entry renders through it | `HostEntryEmitter` |
| `RecordStructEmitter` (a `private record struct` of the STORAGE form per group) | `+` a published mode: a `public record struct` of the DESCRIPTION's host types (§4.3's table), FILLER private, `AsImage`/`FromImage` from `GroupImageCodec` with `HostLanding`'s landing and validity rules | `HostEntryEmitter` |
| — | `HostLanding` (new, runtime, `[EditorBrowsable(Never)]`): the exact landing that refuses every loss (`ArgumentOutOfRangeException`), the alias grouping (`ArgumentException`), the validity decode (`CobolNum.IsNumericImage`; `FormatException`) — one implementation of §4.3's door rules | `HostEntryEmitter`, `RecordStructEmitter` (published mode) |
| `BindSession.Repository : GroupRepository(…)` | `: RepositoryResolver`; `GroupRepository` DELETED | `BinderDriver.Bind`, `BindUnitProcedure`, `OoDriver` |
| `BuildProgramDetailsTable` (order-blind), `BuildUserFunctionTable`/`UserFunctionsOf` (word-keyed, no-AS skipped) | both keyed by externalized name, source-order aware, with the word → definition map for D-R3's steps 2 and 3; the "previously is not enforced" comment DELETED (§8.2, §8.3) | `GroupRepositoryProvider`, `BindUnitProcedure`, `OoDriver` |
| `ProgramPrototypesOf(…, programDefinitions)`; `ProgramPrototype(…, CalleeSignature? Signature)`; `PrototypeSignatures.Same` null arm and its "the external repository is the RUN UNIT's registry" remark | resolver-driven; `Signature` non-nullable; null arm and remark DELETED | `CallBinder`, `SetBinder`, `PtrBinder`, CANCEL arm, `CheckPrototypeSignaturePairs` |
| `ObjectRefDescriptor(Kind, Name, Factory, Only)` | `+ Target`; equality/`SignatureKey`/`ClrTypeName` over `Target` | `PictureAnalyzer`/`DataBinder` (sets `Target`), `OoEmitter.OoReturnClrType`, `OoConformance`, `SetBinder` Format 5 |
| `PicInfo.RestrictedPrototypeName`; `ConformanceDescriptor` (`DynStructure?.Name`); `CategoryArmMismatch` pointer arm; `ClassRaisingCovered`; `DescriptionClauses` | `RestrictedPrototype` identity; structure DEFINITION key; type-declaration equivalence / prototype identity; `RaisingTarget.Target`; definition compare | `StrongTypeModel`, `OoConformance`, `RaisingTarget` |
| `OoClassTable.Build`; `OoClassSymbol.Ctx`; `OoRepositoryScope` (classes/interfaces) | `+ Import(symbol)` by identity, `IsParameterized` for imported skeletons; `Ctx` → `Origin`; `OoRepositoryScope` → `IdentityScope` (+ prototypes, structures; reverse map) | `OoNameResolution`, `OoEmitter.EmitClassUnit` (skips imported), `ResolveOverrides`, `OoExpansion` |
| `OoExpansion.Expand(tree, classes, interfaces, edition, words)` | resolver-driven; imported skeletons re-parsed under their recorded `CompileState` with recorded resolutions; the `ExpansionKey`; import by key; placement (4a)/(4b); stale detection (REPO-9 with the referencing assemblies); publishes keys and skeleton records (§10.5) | `BinderDriver`, `FragmentParse.ParseTokens` (a `CompileState` parameter), the binder's option-dependent arms, `CompilerDriver`, `RepositoryReader` (`AssemblyRef` listing) |
| `DataBinder.BindDeclarations` REPOSITORY loop (never resolved) | `ResolveRepositorySpecifiers` in `BinderDriver.BindUnitData` for every specifier at its position | `BinderDriver`, `OoDriver.BindClassData` |
| `OoNameResolution.Resolve` message "no … is defined in this compilation group" | adds "and no information exists for it in the external repository" | — |
| `RoslynBackend.Compile(csharp, path, name)` (console only; no extra refs; the driver keeps only `Error` diagnostics) | `(…, outputKind, extraReferences)`; GUID temp + retrying rename; CS0435/CS0436/CS0437 escalated to errors (§10.2) | `RoslynBackend.Emit`, `CompilerDriver` |
| `BackendOptions`; `CompilerDriver.Options` | `+ OutputKind`, `+ ReferenceAssemblies`, `+ RepositoryUpdate`; `AssemblyName` remark corrected; `+ RepositoryPaths`, `+ ReferencePaths`, `+ RepositoryUpdate = true`, `+ RepositoryCheck = true` | `Cobol.Net.Cli BuildParser`, test harnesses |
| `Directory.Build.props` (no `Version`) | `<Version>` from the package version; `RuntimeAbi.Version` reads the runtime's `AssemblyVersion`; `RuntimeAbi.CallAbi`, the call-ABI integer (§15.4); `Microsoft.CodeAnalysis.PublicApiAnalyzers` on `Cobol.Net.Runtime` with `PublicAPI.Shipped.txt` | `CobolRepositoryAttribute`, `RepositoryLocator`, `ProgramTable.RegisterModule`, `EmitEntryWrapper` (emits both versions) |
| `Cobol.Net.Runtime.csproj` | `IsTrimmable`, `EnableTrimAnalyzer`; `RuntimeFeatures.DynamicProbe` (`[FeatureSwitchDefinition("CobolNet.Runtime.DynamicProbe")]`, `[FeatureGuard(typeof(RequiresUnreferencedCodeAttribute))]`) guarding `ProbeSiblingModule` (§15.5) | — |
| 14 characterization snapshots (`tests/Cobol.Net.Tests.Characterization/Snapshots/char_*.g.cs.txt`) | re-baselined for the namespace line, `global::`, the registrar attribute and `EnsureRegistered` (slice 1), and for the host-entry class of each program (slice 2) | — |
| Tests naming the global-namespace registrar: `NonCobolActivatorReturnTests` (`asm.GetType("__CobolModule")…Register`, then `ProgramRegistry.CallProgram(…, "", …)` as a host), `FunctionLocateResourceWitnessTests` (a hand-written fixture declaring a global `__CobolModule` whose `Register` throws, with no `[CobolRepository]`), `InterProgramFileDifferentialTests` (the `XASMS1.dll` sibling-module remarks) | slice 1: the activator test reaches the module through `EnsureRegistered` named by `[CobolRepository(Registrar)]`; the witness fixture carries the assembly attribute and a throwing `EnsureRegistered`, so the probe still selects it and the throwing registrar is still what is tested; the remarks name the locator. Slice 2: the activator test is a host of the generated entries (`Cobol.L1HSTGB.L1HSTGB.Call()` inside `RunUnit.Run`) | — |
| `docs/CONFORMANCE.md` DOC-A.1-65, -116, -141, -167 (the host route through `__CobolModule.Register()`, `ProgramRegistry.CallProgram`, `ICobolProgram.Call`) | rewritten to the host entry (§9.4's DOC-A.1-141 text; the other three name the entry, `EnsureRegistered` and the exception contract) in slice 2 | — |
| Goldens pinning function resolution (§8.3 D-R3) | unchanged source and expectations; new goldens for REPO-13 in one group and across the repository, the word reaching a later-compiled unit, and the group-only word case | — |
| The 2002/2014 goldens and negatives that name a later program definition with no prototype (§8.2) | a program prototype definition of the callee ahead of every other unit; expectation files re-pinned for the shifted lines (kb/Work PB989) | — |
| `OoStandardClasses` comment; XML remarks in `PrototypeSignatures`, `BinderDriver`, `ProgramPrototype`, `ProgramTable` ("probes `<name>.dll`", "invokes its public `__CobolModule.Register()`") describing the registry-as-repository | rewritten to this design | — |
Nothing is wrapped or forwarded: `GroupRepository`, the null-signature state, the order-blind and word-keyed tables,
`DataBinder.Unique`, the public `Register()`, the global-namespace registrar lookup and the documented untyped host
route are deleted in the slice that lands their replacements.

---

## 13. Editions

| Edition | REPOSITORY paragraph | Resolution | Emission |
|---|---|---|---|
| 85 | every specifier kind is refused at recognition with COBOLNET0900 (the `repository-*-2002` rows) | never consulted; a Format-1 CALL is the boundary as always (§14.8.2.3.2 rule 1, "the formal parameter shall be of the same length as the corresponding argument", is its run-time screen) | the host entry and the records ARE emitted (an 85 program is a legal callee of a 2023 prototype and of .NET); `Edition = 85` |
| 2002 / 2014 / 2023 | all five kinds; EXPANDS | §8, §10.5 | all |
No new gate; §17.1's 85 arm proves introduction gating fires before any REPO-* diagnostic.

---

## 14. Diagnostics (placeholders; codes allocated with `alloc.py code N` when the notes are filed)

| Placeholder | Severity | Condition | Clause |
|---|---|---|---|
| REPO-1 | Error | a specifier (or an EXPANDS definition) that neither the group, the repository nor the CLR provider answers — "… names no {kind}: not {group alternatives}, and no information exists for it in the external repository (searched: …)" | §12.3.8.3 SR6/SR9/SR14/SR16; SR10 only when none of its three alternatives nor §8.4.6.7's word holds (§8.3) |
| REPO-2 | Warning | a later different entry of the chosen `kind:name` whose record differs; a same-name different-triple expansion elsewhere in the search space | §8.13 ¶4; §9.3.12 |
| REPO-3 | Error | a named or scanned entry unreadable as a managed PE | §8.13 |
| REPO-4 | Error | a malformed or oversized record / index | §8.13 |
| REPO-5 | Error | a description the codec cannot rebuild | §8.13 |
| REPO-6 | Error | two needed class/interface/expansion assemblies with one assembly simple name, or whose one-segment namespaces coincide (§4.5) | §8.6 |
| REPO-7 | Error | schema version ≠ the reader's, runtime ABI major ≠ this compiler's runtime, or call ABI ≠ this compiler's | §8.13; §5.4 |
| REPO-8 | Error | a specifier resolves to a `HasSignatures = false` entry | §8.13 ¶3 |
| REPO-9 | Warning | `--repository-check`: a unit differs from the previous build's record, or from a prototype/definition of its name in the search space; an expansion of the same name and identities with another `ExpansionKey` is stale (naming it, the first differing key component and every assembly that references it, §10.5) | §8.13 ¶4 |
| REPO-10 | Error | `--repository DIR` does not exist / cannot be enumerated | — |
| REPO-11 | Error | an imported symbol's base, implemented interface or referenced identity is in no entry | §12.3.8.4 GR6 |
| REPO-12 | Info | the previous build of this output, or an own-assembly expansion target of the same name, carries another schema, runtime major or call ABI; not compared / overwritten (§10.5) | §8.13 ¶4 |
| REPO-13 | Warning | a `FUNCTION F` specifier for which GR11 a), b) or c) selects the function externalized "F" while a different function whose user-function-name is F exists, in the compilation group or in the repository; the externalized name is used (D-R3) | §12.3.8.4 GR11; §8.4.6.7 |
Existing codes keep their roles: 1761 (SR1/SR2), 1760, 1505, 0813/0821/0859 (unreachable for a declared name), 1513 (a
definition vs a prototype in ONE compilation group, §10.6.2 SR2/SR3), 2240 (expansion identity reuse; the §9.3.12
"shall not" where the two classes necessarily share a run unit, §10.5), 0900. No exception condition is new and
`CobolCallException.CarriedNames` is unchanged: a stale caller meets GR3 d)'s EC-PROGRAM-ARG-MISMATCH (when checking
is enabled in both); a missing, colliding, duplicate or version-skewed module meets GR3 b)'s EC-PROGRAM-NOT-FOUND with
the probe's or `RegisterModule`'s reason. The host door's own refusals are .NET exceptions, not EC conditions:
`ArgumentException` (and `ArgumentOutOfRangeException`, `ArgumentNullException`) before activation, `FormatException`
for content the validity rule refuses, and R48's run-unit-ended exception at the outermost boundary (§4.3).

---

## 15. Robustness and security

### 15.1 Reading without loading
`PEReader` + `MetadataReader` + `CustomAttribute.DecodeValue` with a primitive/string/`int`-enum-only provider
(anything else is REPO-4). The compiler loads no repository entry (r6-meta).

### 15.2 Limits
Files > 256 MiB skipped (listed in REPO-1's tail); > 65,536 description rows, a skeleton source > 16 MiB or an index >
65,536 units → REPO-4; no recursion, no reparse points; `Path.GetFullPath` canonicalization.

### 15.3 Malformed and hostile metadata
Every decode failure is a diagnostic naming the file; a stray unreadable DLL the scan found is skipped and listed.

### 15.4 Stale entries and version skew
The record describes its own assembly; a CALLER goes stale: (1) `--repository-check` warned at the callee's compile;
(2) GR3 d) at run time when checking is enabled in both (otherwise the lenient adoption, §4.2). **Version skew is
guarded on both sides of the boundary, each by a recorded version and a fixture that makes forgetting it fail a test.**
- **The runtime side — the .NET rule.** `Cobol.Net.Runtime` carries a real `AssemblyVersion` (from the package version;
  `Directory.Build.props`); its MAJOR changes exactly when its public surface or a codec's image layout changes
  incompatibly. `Microsoft.CodeAnalysis.PublicApiAnalyzers` with `PublicAPI.Shipped.txt` makes every surface change a
  deliberate, reviewed edit; a unit test asserts that the recorded major equals `RuntimeAbi.Version.Major`.
- **The compiler side — the call ABI.** The crossing of each boundary item is a COMPILER decision: its crossing arm
  and carrier type (`CallEmitter.CrossingOf`, `ProgramEmitter.FormalCrossing`/`FormalCarrierType`), the
  `NumProfile`/width it registers (`CallEmitter.RegisteredFormal`, `RegisteredReturning`) and the BY VALUE landing
  (`LandForFormal`'s arguments). A compiler-only change there keeps the runtime's hash and major and still makes a new
  caller and an old callee disagree. So the runtime declares `RuntimeAbi.CallAbi`, an integer naming the boundary
  layout it adopts; the compiler emits it into every module's `[CobolRepository(callAbi)]` and `EnsureRegistered`;
  the compiler refuses an entry of another call ABI (REPO-7) and the probe and `RegisterModule` refuse such a module
  (§11.3). The **boundary-layout fixture** (§17.2) is rendered by BOTH halves — for every (usage, picture, group shape)
  row, the compiler's crossing arm, carrier type, registered profile and width, and the runtime codec's image of a
  sample value — and its hash is pinned beside `RuntimeAbi.CallAbi` and the runtime major: a change to either half
  without the matching bump fails the test.
- The compiler refuses another major or call ABI at compile time (REPO-7), the locator and `RegisterModule` refuse it
  at run time (§11.3), and the CLR loader itself refuses a statically referenced assembly OLDER than the reference. A
  rebuild with an unchanged surface, unchanged layouts and an unchanged call ABI strands nothing. No compatibility
  layer.

### 15.5 Trimming and AOT
`Cobol.Net.Runtime` is `IsTrimmable` and analyzer-clean. The dynamic probe is the ONLY reflection
(`LoadFromAssemblyPath` and `Assembly.GetType(string)` are IL2026 sites; `GetMethod` on an unannotated `Type` is
IL2075) and it sits behind the feature switch `CobolNet.Runtime.DynamicProbe` (`RuntimeFeatures.DynamicProbe`, a
`[FeatureSwitchDefinition]` property that is also a `[FeatureGuard(typeof(RequiresUnreferencedCodeAttribute))]`).
With the switch ON (the default for a framework-dependent run unit) the probe runs and a trimmed publish must root the
runtime (`<TrimmerRootAssembly Include="Cobol.Net.Runtime" />`: **a dynamically located callee requires the WHOLE
runtime**, since ILLink would remove members only the callee uses). With the switch OFF (`PublishTrimmed` /
`PublishAot` set it in the runtime's `ILLink.Substitutions.xml`) the probe is removed and the trim analyzer sees no
IL2026 at any CALL; the run unit is then **host-composed** (owner decision 2026-10-05; §11.4): the application is a
.NET host that references every COBOL assembly of the run unit and calls each one's `__CobolModule.EnsureRegistered()`
— the one registration member — before activating the main program through its host entry. No compile-time module
reference is added, so §7.3 stands: a COBOL caller never references its callee's assembly. A COBOL main published
trimmed or AOT by itself reaches only the programs of its own assembly; its CALL to any other is EC-PROGRAM-NOT-FOUND
naming the disabled probe. Host entries are reflection-free.

### 15.6 Cross-platform
Folded-name comparison over the enumerated listing; CI's Linux legs run the multi-unit harness (§17.1).

---

## 16. Performance and efficiency

- **No REPOSITORY paragraph / group answers everything:** no listing, no file open; `--repository-check` reads the
  previous output only when it exists and the entries the resolver already opened (the battery's fresh temp outputs pay
  nothing). Tested (§17.6).
- **First external miss:** one listing + one prefetched header read per DLL, cached per process by `(path, length, mtime)`.
- **COBOL CALL:** unchanged — the repository adds no run-time work; the probe's index is read only when the named file
  misses, once per name per run unit.
- **Host entry:** one detached storage per distinct BY REFERENCE variable, one `CobolArg[]`, the boundary, and the
  decode per BY REFERENCE variable at return — the cost of a COBOL CALL by data-name plus one encode/decode per formal.
- **Backend:** attribute text proportional to the LINKAGE; one host-entry class per outermost program or function;
  per-compilation references only when a class, interface or expansion was imported; an expansion assembly is one
  extra small compilation per expansion whose code references no class of the expanding group, and none when an
  expansion of the same key is imported.

---

## 17. Tests and drift tests

### 17.1 Goldens and the multi-unit harness (per edition 2002/2014/2023; an 85 arm)
- A NEW conformance shape (`tests/conformance/<ed>/repo_*`): a manifest line compiles unit A to a temp directory, then
  unit B with `--repository` naming it, and runs B OUT OF PROCESS (circ5: an in-process reload of one simple name is
  refused); the harness has a Linux leg (`scripts/linux-gate.sh`). Cases: program prototype resolved from a
  prototype-only build (BY VALUE default from the formal, §14.9.4.4 GR9 b); from a definition's build; the pb1115
  shape split across two units (prototype names and grouping differ from the definition's: compiles, runs, no
  REPO-9); a caller's argument that does not conform to the repository formal → the §14.8.2 argument diagnostic;
  function prototype (word and AS; the GR11 c)-hit order); class imported (INVOKE, NEW, INHERITS/IMPLEMENTS, typed
  reference, property via PROPERTY specifier, a different importer word, an anonymous identity in a signature);
  interface by literal-1; a skeleton imported and expanded under its recorded edition/word map; an expansion imported
  instead of re-expanded; two groups expanding one library triple → ONE assembly file, one CLR type observed at run
  time; an in-group expansion reused by a later group; the cross-group sequence of §10.5 (G2 first, then the
  skeleton's own group imports `PA.dll` through the reference cycle; then, after a change to the skeleton, re-expands
  in-group with REPO-9 naming `PA.dll` and G2); a rebuild by a later compiler with an unchanged key reusing the file;
  a prototype-only library; an 85 program
  called through a 2023 prototype; the two-pass build of mutually referencing classes; CALL by data-name and a program
  pointer to a repository program; a program whose assembly is not named after it (found by the index).
- **The host harness** (out of process; Linux leg): a C# host compiled against an emitted assembly calls its entries —
  scaled `decimal` in and out; `long` / `Int128` boundaries; a group through the record struct; BY VALUE landed by GR10;
  an OMITTED OPTIONAL formal (the callee's omitted condition true); RETURNING of each host type; one variable passed
  to two `ref` parameters of one description (one storage) and of two descriptions (`ArgumentException`, nothing
  activated); one WORKING-STORAGE across two host calls and a COBOL CALL; `Cancel()` re-initializing it, and
  EC-PROGRAM-CANCEL-ACTIVE for an active program; INITIAL re-initialized; RECURSIVE refused with
  EC-PROGRAM-RECURSIVE-CALL; every loss → `ArgumentOutOfRangeException` with nothing activated (high order, low order,
  sign, P positions, binary capacity, string length, a group member, an OCCURS array of the wrong length); non-numeric
  content left in a numeric BY REFERENCE formal, in a group member and in the RETURNING item (`HRET`) →
  `FormatException` after the other write-backs, the variable unchanged; STOP RUN and a terminating fatal condition →
  R48's run-unit-ended exception with the files closed; a prototype library's entry against a changed definition →
  EC-PROGRAM-ARG-MISMATCH when the definition enables checking and the lenient adoption when it does not; the
  published names of a colliding description (`CUST`/`F`/`F`/`F-2`/`AsImage`) compile `-warnaserror`; two COBOL
  assemblies `PAY` and `PAY.V2` referenced by one host; a host-composed run unit whose main CALLs a program in an
  assembly not named after it (r7-compose); an 85 program called from .NET (r6-host and r7-brk are the seeds).
- Negative: every REPO-* with its message fragment; 0900 before REPO-1 at 85; `--repository-update off` → REPO-8; a
  shadowed differing entry → REPO-2 and the first used; same-simple-name pair → REPO-6; schema/version skew → REPO-7
  (patched fixtures); `--repository-check` → REPO-9 against the previous build and against a differing published
  prototype / absent with `off` / REPO-12 on an old schema; a specifier naming only a LATER definition → REPO-1; a
  function specifier none of SR10's alternatives satisfies → REPO-1; REPO-13 in one group and across the repository; a
  same-group prototype/definition difference → COBOLNET1513; a same-name different-triple expansion in a referenced
  assembly → COBOLNET2240, elsewhere → REPO-2.
- Run-time (out of process): a stale caller → EC-PROGRAM-ARG-MISMATCH when checking is enabled in both, a lenient
  adoption when not, ON EXCEPTION taking the former; a missing callee → EC-PROGRAM-NOT-FOUND; two different assemblies
  carrying one name → EC-PROGRAM-NOT-FOUND naming both; a runtime-major-skewed and a call-ABI-skewed assembly → naming
  both versions, through the probe and through `EnsureRegistered`; a second module carrying an already-registered
  outermost name → refused whole, naming both modules; `PAY - Copy.dll` beside `PAY.dll` → the named file is used; a
  load failure (and a file removed after the listing) → its message in the EC message; a module registered by a host
  and then reached by a probe is registered once.
- Existing goldens keep their expectations (the three function-resolution goldens included) except the SR14 cases
  kb/Work PB989 owns.

### 17.2 Round-trip and two-sided schema drift tests
- For every LINKAGE root of every conformance golden: `Decode(Encode(x))` satisfies `DescriptionMismatch == null` both
  ways, `SameElementaryLayout`, `SameType`, equality of `ConformanceDescriptor`, `Signature`, `Layout`, `ImageWidth`,
  `ByteWidth`, `ExtentBits`, AND `PicInfo` RECORD EQUALITY (`==`, the synthesized one — so `_digitPositions` and every
  future member are seen) plus `EditingRules` element-wise.
- The converse: every `PicInfo` constructor parameter and init property (reflection over the type, including init-only
  properties with private backing) has a `PicRecord` field; every `DataItem` member in §6.3's table has a
  `DescriptionRecord` field; a Roslyn-analysis enumeration of the members the comparator sources read must be a subset
  of the table.
- A perturbation of each recorded description property — `TypeName` and the type declarations' names included
  (§8.5.3.1) — makes some comparator fail; a perturbation of a DATA-NAME or of intermediate grouping makes none fail
  (§13.7.3 SR2).
- For every §10.6.2 SR2/SR3 pair in the corpus (pb1115 included): `PrototypeSignatures.Same(Decode(Of(prototype)),
  Decode(Of(definition)))` holds, and `Same` against the in-group bound pair agrees.
- **Boundary layout:** a fixture of (usage, picture, group shape) rows covering every byte form, width and crossing
  arm is rendered by BOTH halves of the boundary — the compiler's `CrossingOf` / `FormalCrossing` arm,
  `FormalCarrierType`, `RegisteredFormal`/`RegisteredReturning` profile and width, and the runtime codecs' image of a
  sample value; its hash is pinned beside `RuntimeAbi.CallAbi` and the runtime's major version, and the test fails
  when the hash changes without the matching bump (a compiler-side change → `CallAbi`; a codec change → the major,
  §15.4).
- **`CompileState` is exhaustive:** every public member of `CompilerDriver.Options` is a `CompileState` field or is in
  the test's explicit not-semantic list (§6.1).
- **Every schema enum is `int`-backed** (reflection over the attribute family).

### 17.3 Resolver, host-entry and boundary drift tests
- Every `repositoryEntry` alternative has a `RepositoryResolver.Resolve*` arm.
- Repository metadata is read only through `RepositoryReader`, whose callers are `RepositoryResolver` and the §9.2
  check (a Roslyn-analysis test); every specifier of a bound group is resolved before any body binds.
- `PrototypeSignatures.Same` and `ProgramPrototype.Signature` have no nullable path.
- Identity tests: two descriptions with different words and one identity compare equal through `SameDescriptionAs`,
  the pointer arm, `ClassRaisingCovered`, `DescriptionClauses` and `ConformanceDescriptor`; one word and two identities
  do not.
- **The host entry is a caller:** a Roslyn-analysis test over the emitted corpus asserts that every host-entry `Call`
  body consists of exactly the parts §4.3 lists — `EnsureRegistered` (definitions only), one `HostLanding` landing per
  formal and the alias grouping, one `CobolArg` per formal from the CALL lane's renderer, exactly one
  `ProgramRegistry.CallProgram`, a `finally` of `HostLanding` decodes and the RETURNING decode — with no `catch`
  clause, no `ICobolProgram` or `ProgramTable` member, no instance and no EC-name test; that `Cancel` is one
  `ProgramRegistry.Cancel`; and that its parameter types equal `HostSurface.TypeOf` of the recorded descriptions.
- **One host surface:** a repository scan fails when a test, sample or document outside `src/Cobol.Net.Runtime` and the
  compiler's emitters names `ProgramRegistry.CallProgram`, `ProgramRegistry.Cancel`, `ICobolProgram.Call` or
  `__CobolModule.Register` as a way for a host to activate a program (the runtime's own tests of the boundary are
  listed in the scan as the ABI's tests).
- **The door rules are one function:** every landing, alias and validity decision in emitted code is a `HostLanding`
  call (no inline range test); `HostLanding.Land` refuses every row of a loss fixture (high order, low order, sign, P
  positions, each binary capacity, string length, boolean characters) and accepts the exact boundary values; the
  validity decode agrees with `CobolNum.IsNumericImage` on a fixture of valid and invalid images per byte form.
- **Names:** for every LINKAGE root of the corpus plus a collision fixture (duplicate words, words equal to the
  enclosing type, to `AsImage`/`FromImage`/`Equals`/`ToString`, to `Call`/`Cancel`, `F` beside `F-2`), the internal
  and published record structs and the host class compile with no CS0102/CS0542/CS0111, every name is produced by
  `CsNames.Allocate`, and two compiles give the same names.
- **One renderer:** the `CobolArg` a host entry renders for a formal equals the one `CallEmitter` renders for a BY
  CONTENT argument of the formal's description (a unit test over every LINKAGE root of the corpus).
- **One registration set:** `__CobolModule.EnsureRegistered` is every module's only public registration member,
  `RegisterModule` is the only writer of the registration set, and a module carrying an already-registered outermost
  name, or another runtime major or call ABI, registers nothing.
- **The warning policy is mechanical:** a fixture provoking CS0435, CS0436 and CS0437 asserts each is reported as an
  error; a fixture provoking CS1718 (`IF B = B`, legal source) asserts a clean compile (§10.2).

### 17.4 Backend-independence
A fixture record rendered (a) by `RepositoryAttributeRenderer` through `RoslynBackend` and (b) by `MetadataBuilder` +
`CustomAttributeEncoder` decodes equal; `Units` equals the record set.

### 17.5 Trim and AOT tests
`Cobol.Net.Runtime` builds analyzer-clean. A caller is published trimmed with the probe switch ON and
`TrimmerRootAssembly Cobol.Net.Runtime`, an untrimmed callee deployed beside it, the probe path runs out of process and
the CALL succeeds. A second publish with the switch OFF and no root is a HOST-COMPOSED application (§11.4): a .NET
host referencing the main program's assembly and the callee's, calling both `EnsureRegistered()` and then the main's
host entry; it is asserted analyzer-clean (no IL2026 at any CALL), and the main's COBOL CALL into the other assembly
succeeds; the same host without the callee's `EnsureRegistered()` gets EC-PROGRAM-NOT-FOUND naming the disabled
probe. A trimmed .NET host calling host entries is analyzer-clean.

### 17.6 Zero-cost-when-unused; 17.7 Determinism
As §16: no listing/open when the group answers every specifier; two compiles → byte-identical assemblies (formals, an
imported class, a host entry, a skeleton, an expansion assembly from two different expanding groups).

---

## 18. Rejected alternatives

| Alternative | Why rejected |
|---|---|
| Bespoke JSON/binary repository file; separate signature database | a second artifact that can be missing/stale/mismatched; the CLR already ships a versioned, deterministic container with the assembly (owner decision) |
| The run-unit registry as the repository | no details at compile time; nothing to update or check (PB1086) |
| Documenting a determination only | §8.13's two "shall provide" sentences |
| Loading entries with `Assembly.Load`/reflection | executes module initializers, pins one simple name per ALC (circ5), defeats trim analysis |
| A typed COBOL-to-COBOL activation (typed entry per program, a prototype interface per signature, typed cells as formals) | owner decision 2026-10-05: COBOL-to-COBOL calls keep the existing `CobolArg` boundary; a typed door must re-establish every GR3 obligation and §14.2.3 GR8 for every storage form on a second path beside the one that already has them, and the repository's job — compile-time information and checking — needs none of it |
| The host entry in a separate assembly per program or per signature | a second file to deploy, version and lose; two declarations of one signature (the entry's and the repository record's) that can disagree; the program's own assembly already carries the record |
| A public program class (or a public `ICobolProgram` instance) for hosts | bypasses the boundary: a second WORKING-STORAGE, no GR3 f), no INITIAL cancel, no contained-program construction — the reason program classes stay `internal` |
| Host BY REFERENCE parameters as cells (`ManagedPointer<T>`, `StrongBox<T>`) | not the .NET idiom for an in/out value; an image a host type cannot hold would still need a decision at the same point; `ref` plus `Unsafe.AreSame` gives one storage to the one aliasing a .NET caller can express (a variable passed twice) |
| A scaled numeric as its unscaled integer (`long` with an implied scale) | a host reading `1000` for 10.00 is a wrong answer waiting to happen; `decimal` is the .NET type of a scaled fixed-point value |
| COBOL's COMPUTE truncation at the host door (high-order refused, low-order and sign silently dropped) | owner decision 2026-10-05: every loss is refused; a host site has no ON SIZE ERROR, and a silently narrowed amount is a wrong answer |
| Non-numeric write-back content as `CobolCallException` with EC-DATA-INCOMPATIBLE | the program completed normally, so no activation failed; it would add a name to `CarriedNames` and its drift test; `FormatException` is the .NET exception for a text that does not parse as a number, and one rule serves BY REFERENCE and RETURNING |
| Decoding an invalid RETURNING image to a number | a REDEFINES alias can leave `[ABC      ]` in a numeric RETURNING item; returning `0` for it is a wrong answer (r7-brk) |
| One storage for one variable passed to formals of different descriptions | one storage cannot be both a `9(4)` and a `9(8)`; the last write-back would silently win (r7-brk) |
| A compile-time module reference so a COBOL main composes its callees under AOT | overrides §7.3 (a caller never references its callee's assembly) and binds a CALL by name to an assembly at compile time; owner decision 2026-10-05: AOT is host-composed |
| Keeping `ProgramRegistry.CallProgram` / `ICobolProgram.Call` documented as a host API beside the entry | two host surfaces for one job (CLAUDE.md rule 4); owner decision 2026-10-05 retires it |
| Moving `ProgramRegistry` to a `CompilerServices` namespace to retire it | churns every emitted line and enforces nothing a public type does not already allow; the documentation, `[EditorBrowsable(Never)]` and the one-surface scan (§17.3) retire the route |
| A dotted namespace `Cobol.<simple name>` | `PAY.V2.dll` meets a type `V2` of `PAY.dll` (CS0434/CS0437 on legal source, r7-ns) |
| Including data-names or intermediate grouping in signature identity | §13.7.3 SR2 asks the descriptions to match, and §8.5.3.1's essential characteristics do not include names or grouping; pb1115's prototype and definition differ in both and are one signature |
| Fixing the emitter until the corpus compiles with zero Roslyn warnings | CS1718 is reported on legal `IF B = B` (r6-warn); a warning-free corpus is not reachable by emitter changes alone, and surfacing warnings would change every `.diag.txt` |
| COBOLNET1513 for a definition that differs from a prototype in another group | §10.6.2 SR2/SR3 are scoped to one compilation group; across groups the standard asks for flagging (§8.13 ¶4), which REPO-9 is |
| `modreq`/`modopt`; `CallConv*`/`UnmanagedCallersOnly`; CLR `[Optional]` | §3.3, §3.4, §3.5 |
| Recording the callee's words as identity | §6.4 |
| Refusing parameterized classes across assemblies | §9.3.12 is core OO (DOC-A.1-138) |
| A warning for two independently expanded copies of one triple | ships a known §9.3.12 violation (two CLR types for one class instance); D3 imports an existing expansion of the same key instead |
| Every expansion in its own assembly, in-group references included | the expansion would need the expanding group's NEW assembly, which does not exist while that group compiles |
| Comparing expansion assemblies by MVID or bytes | a rebuild by a later compiler changes both without changing the class; the `ExpansionKey` names what the expansion means |
| Refusing an expansion whose existing home references the expanding group | the CLR and Roslyn accept the reference cycle (circ), so importing it keeps one class instance without refusing legal source |
| A compile-time error for two entries of one name | §8.3.2.2 is run-unit scoped; identical copies are normal |
| Extern aliases for imported types | a per-assembly namespace plus `global::` solves the same collision for .NET hosts too (two `__CobolModule`s) |
| The runtime's deterministic MVID as the version key | any rebuild of the runtime — a comment, a message — would strand every deployed callee; the `AssemblyVersion` major with `PublicApiAnalyzers` is the .NET rule and changes only when the surface or the ABI does |
| A hash of the callee in the caller; reversible file-name mangling; a source generator; emitter facts in the record | second compares, second mappings, a build step for what a test catches, backend dependence |

---

## 19. Decisions

**Decided (recorded so they are not re-asked):**
- **The repository is assembly metadata plus attributes** (owner, PB1099 Q3).
- **COBOL-to-COBOL activation is the existing boundary, unchanged** (owner, 2026-10-05, "option 1"); the repository
  supplies compile-time information and checking only (§4.2). Group-formal aliasing is PB2087's, and PB2087's managed
  slots supersede PB1940's leaf vector.
- **The .NET host surface: one generated public class per outermost program and function, in the program's own
  assembly, whose `Call` method is both the typed host entry and the repository anchor, with `Cancel` for a program**
  (§4.3, §4.4). Cost: one public class, its record structs and two methods per externalized unit; one landing and one
  decode per formal per host call.
- **Retire the untyped host path when the typed host entry lands** (owner, 2026-10-05): ONE host surface. The slice
  that lands the entry rewrites DOC-A.1-65/-116/-141/-167, rewrites `NonCobolActivatorReturnTests` onto the entries,
  makes `__CobolModule.Register` private and marks `ProgramRegistry` as the generated code's ABI, with a drift test
  (§4.4, §17.3). Cost: those rewrites; nothing is kept alive beside the entry.
- **AOT scope: host-composed only** (owner, 2026-10-05): with the probe switched off, the application's .NET host
  registers every COBOL assembly it composes through `__CobolModule.EnsureRegistered()`, the one registration member;
  the probe stays for dynamic CALLs when the switch is on; no compile-time module reference overrides §7.3 (§11.4,
  §15.5). Cost: a COBOL main cannot be published AOT by itself; its application is a .NET host of a few lines.
- **Host value loss: refuse every loss** (owner, 2026-10-05): high-order, low-order, sign, scale/P-position, binary
  capacity and character-length losses are all refused with `ArgumentOutOfRangeException` before activation; no
  silent narrowing at the host door (§4.3, r7-brk). Cost: a host that wants COMPUTE truncation does it itself.
- **D-R3 — the function-name determination** (§8.3; owner decision 2026-10-05): GR11 a)–c) by externalized name; the
  word when they miss (group definition, then the repository's recorded word, §8.4.6.7); whenever the externalized-name
  step and the word name different functions, the externalized name, as GnuCOBOL 3.2.0 does, with REPO-13 — in one
  group or across the repository. Legal source is never rejected.
- **Q4 — AS-literal comparison: the Annex C fold everywhere** (owner, 2026-10-05; DOC-A.1-68; PB661's rows close on
  this document). **Q5 — a Format-1 `CALL "LIT"` does not consult the repository to warn on arity** (owner,
  2026-10-05: no; a vendor extension under §4.2.10). **Q6 — no second typed shape for groups** (owner, 2026-10-05: no;
  the published record struct with `AsImage`/`FromImage` serves the host).
- **R48 — a STOP RUN or terminating fatal condition under a .NET host ends the run unit only** (owner, 2026-09-25;
  DOC-A.1-167): the host entry's exception contract applies it at the outermost boundary (§4.3).
- **D1 — the default search space is the output directory** (§9.3): compile time and run time agree by default (the
  probe's directory). **D1 (data) — groups cross as record images** on the host surface.
- **D3 — one CLR type per expansion meaning** (§10.5): an expansion of the same name and `ExpansionKey` is imported,
  through an assembly reference cycle when it references the expanding group; otherwise a deterministic own assembly
  when its code references no class of the expanding group, the group's assembly when it does; a superseded key is a
  stale caller named by REPO-9. Cost: one small extra compilation and file per own-assembly expansion; the key and
  placement flag in the record.

**Determinations this design makes under the standard's own latitude** (each derived from the cited clause, none an
owner question): the host door's restrictions and its exception contract (§14.2.3 GR13, §4.3); the one validity rule
for write-back and RETURNING, `FormatException` (§8.8.4.4.4 GR3 n), §4.3); one storage only for formals of one
description (§4.3); the forward-reference reading of SR14 — the output being produced is not repository information
(§8.2); the one-segment namespace (§4.5); the call-ABI version (§15.4).

**Open:** none.

**T1 (transcription, for the spec-reconcile lane):** §12.3.8.3 SR16 a) prints "the property literal-5" where the
specifier's literal is literal-4; §12.3.8.4 GR11 c) is run into b)'s paragraph (cite.py reports 11) b)).

---

## 20. The sibling documents this design governs (facts, each corrected in the slice that makes the change)

- `docs/COBOLNET_INTERPROGRAM_DESIGN.md`: the program-prototype resolution paragraph ("this implementation's external
  repository is the run unit's program registry") and the SR20/SR22 note ("no compile-time signature … the screen is
  not emitted") are corrected to §8 here — every prototype is resolved with a signature, from the group or from the
  repository, and checked at compile time; the sibling-module probe paragraph to §11.2 (the index, the registrar
  attribute, the one registration set and `EnsureRegistered`, the version and call-ABI refusal, the host-composed run unit); and the document gains the host entry as a caller of the
  D2 boundary (§4.3). Its D1 (the `ManagedPointer` accessor carrier) and D2 (`ICobolProgram`/`CobolArg` as the
  uniform ABI) stand as written.
- `docs/COBOLNET_OO_DESIGN.md`: an `OoClassSymbol` may be imported (`Origin`); words are per-element (`IdentityScope`);
  descriptors compare identities; every emitted type lives in `Cobol.<assembly>` and foreign names are
  `global::`-qualified; parameterized definitions cross assemblies with D3's key-based reuse and placement rule (§10.5).
- `docs/rearchitecture/DESIGN-codegen-backend.md` §2.0: `RoslynBackend` has `OutputKind`, per-compilation references,
  a temp-and-rename emit, the §10.2 warning policy and a second emission per own-assembly expansion; the
  `CilBackend` line "(Mono.Cecil → IL)" is "(`System.Reflection.Metadata` → IL)"; every backend renders the
  `RepositoryRecord` (§3.9, §17.4).
- `docs/COBOLNET_DESIGN.md` §0.5 (the row for this document) and §9/§10's condensed views (one sentence each pointing
  at §4/§8/§10 here); `docs/DOC_INDEX.md` (the row).
- `docs/CONFORMANCE.md` DOC-A.1-66/-67/-138/-161/-162 and GR-12.3.8.4-11: §9.4's text; DOC-A.1-68 unchanged.
  DOC-A.1-141: §9.4's text, replacing the `ICobolProgram.Call(CobolArg[])` mechanism and its "no argument-count
  check" (kb/Work PB1149's GR13 half). DOC-A.1-65, -116 and -167: the host activates through the generated entry and
  composes a run unit through `EnsureRegistered()`; their mentions of `__CobolModule.Register()`,
  `ProgramRegistry.CallProgram` and `ICobolProgram.Call` go (DOC-A.1-116 is kb/Work PB1253's item-116 row), and
  DOC-A.1-167's "Diverges today" sentence goes with R48's implementation (§4.3).
- `kb/Work/PB1940.md`: blocked by PB2087, its fix replaced by PB2087 item 5 (a strongly-typed group with a pointer or
  object leaf crosses as a cell area with managed slots); its repro becomes one of PB2087's goldens.
- `docs/VERSION_CHANGE_REFERENCE.md`: no row changes; the REPOSITORY rows' witnesses gain §17.1's 85 arm.
- `src` XML remarks listed in §12's last row.

---

## 21. Landing-order constraints (what is atomic, and why)

Architecture-level only — which changes cannot be separated, in dependency order, each slice landable with every gate
green when the slices it depends on have landed, each carrying the sibling-document corrections its change makes true
(CLAUDE.md rule 6). The work items are `kb/Work/` notes filed after approval (CLAUDE.md rule 8), one per slice; their
REPO-n codes are allocated with `alloc.py code N` when the notes are filed. The dependency graph has no cycle: 1 and 2
and 6 stand alone; 3 needs 1, 2 and kb/Work PB2087; 4 needs 3; 5 needs 4; 7 needs 4, 5 and 6; 8 needs 7.

1. **Namespace, registration and the version guard — one atomic change.** The one-segment `namespace Cobol.<S>` and
   `global::` on every emitted type; `[assembly: CobolRepository(schema, runtimeVersion, callAbi, Registrar)]` (the
   attribute type lands here); `__CobolModule.EnsureRegistered()` as the module's one public registration member,
   `Register` private; `ProgramTable.RegisterModule(registrar, runtimeVersion, callAbi, register)` with the version
   refusal and the staged duplicate-name refusal; `ProbeSiblingModule` reading `Registrar` from the loaded assembly's
   attribute and invoking `EnsureRegistered` (a sibling CALL never meets a registrar it cannot find), its catch
   including `FileNotFoundException`; `Directory.Build.props` `<Version>`, `RuntimeAbi.Version`, `RuntimeAbi.CallAbi`,
   `PublicApiAnalyzers` with `PublicAPI.Shipped.txt`, and the boundary-layout fixture test; the 14 snapshot
   re-baselines; the three registrar-naming tests (through the attribute and `EnsureRegistered`). Docs: INTERPROGRAM's
   probe paragraph; the OO design's namespace sentence; DOC-A.1-65/-167's `__CobolModule.Register()` →
   `EnsureRegistered()`. No prerequisite.
2. **The one name allocator — LANDED (kb/Work PB2098, PB2093).** `CsNames.Allocate` with seeded scopes replaced
   `DataBinder.Unique` at every caller (the class-level roots through `CsNameScope.ProgramClass()`, a group's members
   through `DataItem.AddMember`); the collision fixture `tests/conformance/2002/pb2093_member_names_like_generated_members`
   (subordinates named `AsImage`, `Equals`, `ToString`, `F` beside `F` and `F-2`, a record named `CloseFiles`, and a
   TYPE clone of a type with such members) compiles and runs; `CsNameReservationDriftTests` keeps the seeds total.
   The published scopes of slice 3 reuse the allocator with their own seeds.
3. **The host entry and the one host surface.** `HostSurface.TypeOf`; `HostLanding` (exact landing, alias grouping,
   validity decode); `RecordStructEmitter`'s published mode; `HostEntryEmitter` over the CALL lane's renderer with
   `Call` and `Cancel`; `CallProgram`'s and `Cancel`'s nullable `callerPath` and its outermost-boundary arm with R48's
   run-unit-ended exception (the §14.6.11 actions, then the one documented type); `OutputKind` library for a group
   without a main unit; the host harness (§17.1) with its Linux leg; the drift tests of §17.3 for the entry body, the
   one renderer, the door rules, the published names and the one host surface; the snapshots' host-entry classes; the
   retirement of the untyped host route — `ProgramRegistry`'s `[EditorBrowsable(Never)]` and remarks,
   `NonCobolActivatorReturnTests` as a host of the entries, DOC-A.1-65/-116/-141/-167 rewritten. Docs: the manual's
   "Calling a WiseOwl COBOL program from .NET" without its trimmed/AOT paragraph (the switch is slice 5's);
   INTERPROGRAM (the host entry as a caller of D2, the outermost host arm); codegen-backend (`OutputKind`). This slice
   lands kb/Work PB1149's GR13 half and R48's run-unit-ended result, and PB1253's item-116 row. Depends on 1, on 2 (the
   published names), and on kb/Work PB2087 whole — its item 5 carries the strongly-typed groups with pointer or object
   leaves (and closes PB1940), so every program has a complete host entry the day the slice lands.
4. **The schema, the renderer, the reader.** The attribute family and `RepositoryAttributeRenderer` on host entries,
   OO classes and interfaces; `RepositoryRecord` and the codec; `RepositoryReader` (the attributes and the
   `AssemblyRef` table); the round-trip, converse, perturbation (data-names inert, `TypeName` compared), SR2-pair,
   `int`-enum and `CompileState` drift tests; `--repository-update`. Docs: codegen-backend (`RepositoryRecord`, the
   `CilBackend` line). Depends on 3 (a program's record is anchored on its host entry).
5. **The locator and the run-time probe.** `RepositoryLocator` (named file, directory index with word rows,
   collisions); `ProbeSiblingModule` over it; the per-kind dictionary; the feature switch, the disabled-probe reason
   and the trim/AOT tests including the host-composed application; the run-time cases of §17.1 that need no
   compile-time resolution. Docs: INTERPROGRAM's probe paragraph (index-based); the manual's trimmed/AOT paragraph.
   Depends on 4 (the `Units` index) and, through it, 3 (the composed application activates its main through a host
   entry).
6. **The group provider's corrections — kb/Work PB989.** Inside the existing group tables: externalized-name keys and
   source order for GR10/GR11 a)/b), the word → definition map, D-R3 step 2's group arm and step 3's in-group REPO-13,
   with their goldens; and in the same change the corpus programs that name a later program definition with no
   prototype (§8.2's list) gain a prototype of the callee, so no golden depends on the order-blind table this slice
   deletes or on the null-signature arm slice 7 deletes. No prerequisite (no repository is consulted).
7. **The resolver and the metadata provider.** `RepositoryResolver` over the group and metadata providers
   (`GroupRepository` and the null-signature arm deleted in this slice, because with the metadata provider every
   prototype resolves with a signature or draws REPO-1), `FunctionByWord`, the REPO codes, `--repository`/`--reference`/
   `--repository-check` (REPO-9 by `PrototypeSignatures.Same` over the decoded record), the multi-unit harness with its
   Linux leg, the cross-repository REPO-13 and word goldens, the pb1115 split. Docs: INTERPROGRAM's prototype paragraph
   and SR20/SR22 note; CONFORMANCE DOC-A.1-66/-67 and GR-12.3.8.4-11; the manual's "Separately compiled units";
   COBOLNET_DESIGN §0.5/§9/§10 and DOC_INDEX rows. Depends on 4, 5 and 6.
8. **OO across assemblies, in two steps:** `Origin`, `IdentityScope`, class import, the §10.2 warning escalation;
   then interfaces, properties, skeletons with `CompileState`, the `ExpansionKey`, import by key (with the reference
   cycle), the two placements with the second emission, and stale-expansion REPO-9 over the `AssemblyRef` scan. Docs:
   OO design (import, identity, D3); CONFORMANCE DOC-A.1-138/-161/-162; codegen-backend (the expansion emission).
   Depends on 7.
