# WiseOwl COBOL — Architecture (greenfield COBOL → C# compiler)

> ⛔ **SUPERSEDED FOR DEPTH by `docs/COBOLNET_DESIGN.md`** (the decision-complete SSOT — pipeline/bound-tree, data
> model, numeric, control-flow PC-dispatcher, REDEFINES, strings, files, interprogram, OO, conditions/exceptions,
> intrinsics, project reorg/rename, no-god-class structure, C# 14 usage, the §18 settled decisions, and the G0–G8
> build order). This file remains the brief overview; where it conflicts, `COBOLNET_DESIGN.md` wins — notably the
> §3 numeric table below, which uses native `long`/`Int128`-unscaled (NO `decimal`).

> **Status: LIVE — brief overview (companion to the SSOT `docs/COBOLNET_DESIGN.md`).** The overview of the blank-slate rewrite
> directed by the owner on 2026-06-08: *"Move entirely to .NET representations for COBOL objects … totally
> rewrite it … the best possible COBOL to .NET implementation."* It supersedes the byte-substrate compiler in
> `src/CobolSharp.Compiler` (the *legacy* engine, kept only as a differential oracle until cut-over, task G8) and the
> pre-PIVOT byte-engine "data-model migration" plan (the greenfield is
> born typed-native, no migration). See memory `feedback_typed_native_only`.

## 1. North Star

The best **native** .NET implementation of COBOL. A COBOL record **is** a .NET `record struct`; a COBOL
elementary item **is** a native .NET field (`string` / `long` / `Int128` / `bool` / `float` / `double` / typed array). A
COBOL program is a .NET class; COBOL OO classes are .NET classes. **There is no byte-array storage substrate** —
no `ProgramState`, no `byte[]`-at-offset model, no `(byte[],offset,length)` operations. A byte image exists only
*transiently* at an unavoidable boundary (file I/O / `CODE-SET`, a runtime-API call that needs bytes), built into
a fresh scratch buffer, never persisted.

The compiler translates COBOL to **idiomatic, readable C# source** and compiles it with **Roslyn**. Readable
output is a feature: it is inspectable, debuggable, trustworthy, and optimized by the C# compiler + JIT. Codegen is
behind a **selectable `ICodeGenBackend`** (`--backend roslyn|cil`, default roslyn): a backend-neutral bound tree
feeds either the **Roslyn** backend (idiomatic C# source — the v1 deliverable) or a future **CIL** backend
(typed-native IL via Mono.Cecil — no C#-compile step / no Roslyn dependency, for AOT/direct-IL). See
`COBOLNET_DESIGN.md` §1.1/§18.

**Four compilers in one executable (the mission).** `cobol` targets ISO COBOL **1985 / 2002 / 2014 / 2023**, selected
by `--std` (default COBOL-2023; `--nist` without an explicit `--std` targets 85). Every edition-varying construct
carries TWO co-equal obligations: (1) the complete per-edition ISO-spec behavior in every edition that HAS it; (2) the
correct DIAGNOSTIC in every edition that LACKS it (not-yet-introduced or removed). Tests (NIST etc.) only VERIFY; they
never SCOPE. Framework: `docs/VERSION_CHANGE_REFERENCE.md` (the 130-row edition-change checklist; 2002→2023 deltas
ONLY — derive 85↔2002 gating from the 2002 standard) + `docs/VERSION_TEST_MATRIX_DESIGN.md` (the (construct × edition)
matrix; Phase 0 done). All of it at commercial, decades-sustainable quality — no backward-compatibility constraints;
rewrite anything necessary.

## 2. Pipeline

```
source.cob
  → Front-end (src/Cobol.Net.Frontend — the greenfield front-end project; entry type
        CobolNet.Frontend.Frontend; internal namespaces are CobolNet.Frontend.*):
        Preprocess (reference-format, >>directives, COPY, NIST placeholders) → Lex → Parse → parse tree
  → Bind: typed semantic model — every data item gets a .NET type (no byte offsets)        [task G2]
  → Render: the selected ICodeGenBackend (--backend roslyn|cil) renders the backend-NEUTRAL bound tree —
        NO shared lowered IR                                                                  [tasks G3–G4]
  → Emit C# (CobolNet.CodeGen.CSharpEmitter → CodeWriter)                                    [all tasks]
  → Compile C# (CobolNet.CodeGen.RoslynBackend → Roslyn CSharpCompilation) → assembly + .g.cs
Runtime: CobolNet.Runtime — CobolNum (numeric), CobolString (character), ManagedPointer, format/file helpers
         (reused from the clean, oracle-verified typed substrates; NO byte engine)
```

**Reuse line (deliberate, audited):** only the ANTLR grammar + lexer/parser/preprocessor (a declarative,
ISO-derived *spec* artifact) and the clean typed runtime substrates were carried over — owned by the greenfield
`src/Cobol.Net.Frontend` / `src/Cobol.Net.Runtime` projects. Nothing from the legacy byte
engine — `ProgramState`, `StorageHelpers`, `StorageLayoutComputer`, `PicRuntime`'s byte-window APIs, the byte
`IrLocation`/emit spine — is used. The conformance corpus (`tests/nist/`, `tests/conformance/`) and
`specs/ISO_COBOL.md` are the target + oracle.

## 3. Data model (COBOL → .NET types)

| COBOL | .NET |
|---|---|
| `PIC X(n)` / `A` / `PIC N` (national) | `string` (UTF-16) |
| `PIC 9(n)` / `S9(n)` unsigned/signed **integer** | `long` (PIC-truncated via `CobolNum`) |
| signed/scaled `S9(n)V9(m)`, packed `COMP-3` | native `long` (≤18 digits) / `Int128` (19–38) holding the **unscaled** value; scale is compile-time metadata (NO `decimal`) |
| `COMP-1` / `COMP-2` | `float` / `double` |
| `PIC 1` / `USAGE BIT` | `bool` |
| `01`/group | nested `record struct` |
| fixed `OCCURS n` | `T[]` |
| `OCCURS DEPENDING ON` | `T[]` + a length field |
| `USAGE POINTER` / `BASED` / `ADDRESS OF` | `ManagedPointer` (managed ref; no native heap) |
| `USAGE OBJECT REFERENCE` / OO class | a .NET class reference / class |

Numeric correctness backbone: PIC metadata (digits / scale / sign / usage) is threaded into every `CobolNum`
operation, which applies COBOL truncation, the eight `ROUNDED` modes, and `ON SIZE ERROR` on native
native `long`/`Int128` unscaled values. (`CobolNum`/`CobolString` are oracle-verified against the legacy codec.)

**Unbounded cases** (designed to need no persistent byte State): `REDEFINES`/`RENAMES`
storage overlay, whole-group-as-alphanumeric (materialize struct → fresh scratch at the byte-API boundary), and
file-record serialization (typed record ↔ bytes only on the external medium).

## 4. Control flow (COBOL → C#)

The make-or-break design. COBOL control flow (`PERFORM`/`PERFORM THRU`/`VARYING`/`TIMES`/`UNTIL`, `GO TO`,
`ALTER`, paragraph/section fall-through, `GOBACK`/`STOP RUN`, DECLARATIVES) is **ported from the legacy
compiler's proven PC/dispatch design** (return-address `PERFORM…THRU`, symbol-based control
transfer, exit-bounded `Dispatch(entry,last)`, DECLARATIVES handling) — a design that is *orthogonal* to the byte
data model. Paragraphs/sections become labeled regions driven by a program-counter loop (C# `goto`/labels handle
arbitrary transfers); `PERFORM` ranges use the exit-bounded dispatch. **Correctness over idiomatic for v1**:
arbitrary COBOL control flow is not always structured C#; "pretty" is a later optimization for well-behaved
subsets. (Task G4.)

## 5. Project layout

- `src/Cobol.Net.Frontend/` — preprocessor + ANTLR lexer/parser + parse tree + diagnostics (entry type
  `CobolNet.Frontend.Frontend`; internal namespaces are `CobolNet.Frontend.*`).
- `src/Cobol.Net.Compiler/` — bind → bound tree → `CodeGen/` (`CSharpEmitter`, `CodeWriter`, `RoslynBackend`) behind
  `ICodeGenBackend`.
- `src/Cobol.Net.Runtime/` — the runtime the generated C# calls (`CobolNum`, string ops, file connectors).
- `src/Cobol.Net.Cli/` — the CLI shell (exe `cobol`; `--std`, default COBOL-2023).
- `src/CobolSharp.*` — the legacy byte engine: differential oracle + reference ONLY (never authority — the ISO spec
  is; never a substrate to fall back to). Deleted at G8 cut-over.

## 6. Roadmap (tasks)

- **G1 ✅** Bootstrap + HELLO end-to-end (preprocess→parse→emit C#→Roslyn→run). `DISPLAY` of literals, `STOP RUN`.
- **G0 ✅** Project reorg (SSOT §17): split into `src/Cobol.Net.{Frontend,Compiler,Runtime,Cli}` (exe `cobol`);
  no-god-class emitter decomposition.
- **G2 ✅** Data division → typed C# (elementary fields, groups→record struct, tables→arrays; VALUE init).
- **G3 ✅ (core)** Core verbs (`MOVE`, arithmetic, `IF`/`EVALUATE`, `DISPLAY`/`ACCEPT`, `PERFORM` inline) on typed values.
- **G4 ✅** Control-flow engine (port the PC/dispatch design) — `PERFORM THRU`, `GO TO`, `ALTER`, fall-through.
- **G5 ✅** The NIST corpus drive is COMPLETE: every golden-bearing program locked
  byte-exact (318 across NC/ST/RL/IX/IC/SQ/IF/SM/RW/OBSQ — incl. the full §15 intrinsic catalog, COPY/SM,
  LINAGE, Report Writer, EXTERNAL/GLOBAL FDs, cross-assembly CALL); final census 357/403 GREEN, zero diffs.
- **G6 ✅** Deferred data cases: the full `REDEFINES`/`RENAMES` tier model — Tier C resolved as the zoned
  digit-image codec (§13.18.60 GR4); whole-group `AsImage`/`FromImage` incl. fixed-point BINARY/PACKED leaves;
  variable-length records (§13.18.43) end-to-end.
- **G7 (next)** Post-85 features (OO→.NET classes, UDF, pointers, national/boolean,
  JSON/XML, post-85 intrinsics) gated per `--std` — each with BOTH the per-edition spec behavior AND the
  correct rejection diagnostic in every edition that lacks it
  (`docs/VERSION_CHANGE_REFERENCE.md`, `docs/VERSION_TEST_MATRIX_DESIGN.md`).
- **G8** Cut over: delete the legacy `src/CobolSharp.*` oracle, final
  architecture/doc pass. (Projects already live as `Cobol.Net.*`; internal namespaces are already `CobolNet.*`;
  the exe is already `cobol`.)

## 7. Conventions

- Latest .NET 10 / C# 14 — or later (a .NET 11 upgrade is pre-authorized when its features advance the goals) —
  idioms where they aid clarity (records, primary constructors, collection expressions,
  pattern matching, `using` scopes). Full XML doc comments on public surface + inline rationale on non-obvious code.
- The generated C# is written to `<name>.g.cs` next to the assembly — always inspectable.
- Conformance is the oracle: every feature is driven by (and proven against) `tests/nist/valid/*.txt` or a
  `tests/conformance/<ver>/` `.out`.
