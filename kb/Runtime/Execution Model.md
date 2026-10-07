---
title: Runtime — Execution Model
area: runtime
status: draft
last_updated: 2026-07-23
related_files:
  - src/Cobol.Net.Runtime/Values/Numeric/CobolNum.cs
  - src/Cobol.Net.Runtime/Verbs/CobolInspect.cs
  - src/Cobol.Net.Runtime/IO/CobolFile.cs
  - src/Cobol.Net.Runtime/Intrinsics/CobolIntrinsics.cs
  - src/Cobol.Net.Runtime/Control/ManagedPointer.cs
  - src/Cobol.Net.Runtime/Exceptions/ExceptionCatalog.cs
  - docs/COBOLNET_NUMERIC_DESIGN.md
  - docs/COBOLNET_FILES_DESIGN.md
  - docs/COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md
tags:
  - cobolsharp
  - runtime
---

# Runtime — Execution Model

The generated C# program is a thin orchestrator that calls into **`Cobol.Net.Runtime`** (`CobolNet.Runtime.*`). The
runtime is **typed-native**: a COBOL record is a .NET `record struct`, an elementary item is a native field
(`long`/`Int128`/`double`/`string`/`bool`), and every verb is a value-in/value-out static helper. There is **no
persistent byte-array `ProgramState` substrate** and no fallback to a byte-array engine — byte images exist only
transiently at the file/CODE-SET/category-mismatch boundary, produced by a compiler-generated per-layout codec.
Semantics live in the binder/bound tree; backends (`--backend roslyn|cil`) only render calls to the same runtime API.
Code is organized under `Values/`, `IO/`, `Intrinsics/`, `Control/` (+ `Signals/`), `Exceptions/`, `Verbs/`. See
[[kb/Architecture/High-Level Design]].

## Numerics (CobolNum)
Fixed-point data is a native integer holding the **unscaled** value; the decimal point is compile-time scale metadata
(`NumProfile.FractionDigits`). `CobolNum` is **Int128-monomorphic**: operands widen `long→Int128` at op entry,
scale-align, compute, and a single **`TryStore`** rescales/rounds/bounds-checks back into the receiver's narrow
storage. `Store` is the silent-truncation branch; `TryStore` returns `bool` (false = ON SIZE ERROR), leaves the
receiver unchanged on overflow, and raises SIZE ERROR for ROUNDED MODE PROHIBITED on an inexact result. The **8
rounding modes** are `CobolRounding`; division uses a guard scale (`DIV_GUARD_DIGITS = 14`). `NumericSign` drives
signed-DISPLAY overpunch images; `CobolEdit` formats numeric-edited fields; `CobolDec` implements ARITHMETIC IS
STANDARD-DECIMAL; `CobolFloat` handles the COMP-1/2 IEEE bypass. See [[kb/IR/Data Flow]].

## Strings (Verbs/CobolInspect, CobolStringOps)
Alphanumeric/national items are UTF-16 `string` at rest; one COBOL character = one UTF-16 code unit (§8.5.1.4).
`CobolInspect` implements INSPECT (TALLYING/REPLACING/CONVERTING, ALL/LEADING/FIRST/CHARACTERS, BEFORE/AFTER,
BACKWARD) as one comparison cycle; `CobolStringOps` implements STRING/UNSTRING (DELIMITED BY, WITH POINTER, ON
OVERFLOW, DELIMITER/COUNT IN). Reference modification lives on `Values/Text/CobolString` (`RefMod` read, `SpliceInto`
write; bounds → EC-BOUND-REF-MOD). All targets resolve through the one universal `Place` lvalue.

## Files & I/O
An FD/SD record is a typed struct; bytes appear only in a generated `IRecordCodec` at the disk edge. `CobolFile` is the
verb facade over `FileConnector` subclasses — `SequentialConnector`, `RelativeConnector`, `IndexedConnector` —
covering all organizations and the OPEN/CLOSE/READ/READ-PREVIOUS/WRITE/REWRITE/DELETE/START state machines
(read-position 43/46, START inclusive FPI, duplicate-arrival 02/26, ascending-WRITE 21). FILE STATUS is a two-char
string (`FileStatusCode`); ordering uses a typed `CobolSort.Key`, shared by indexed files and `CobolSort`.
`FileRegistry` tracks connectors; `CobolReport` and LINAGE ride the sequential print stream. See
[[kb/Semantics/Validation Rules]].

## Intrinsic Functions
`CobolIntrinsics` (partials `.Text`/`.Float`/`.Exact`) plus `CobolDate` implement the full ISO §15 catalog (~70+
functions), typed by §15.2 class: **integer**→`long`/`Int128` (FACTORIAL), **floating math**→`double` (SQRT, trig,
LOG, ANNUITY, PRESENT-VALUE, RANDOM), **exact numeric**→NumX (SUM, MEAN, MOD, INTEGER, NUMVAL) through
`CobolNum.Store`, **alphanumeric/national**→`string` (UPPER-CASE, TRIM, CHAR, NATIONAL-OF), **boolean**→`bool`. The
binder resolves each FUNCTION against a declarative `IntrinsicCatalog` (result category, arity, edition window); an
injectable clock (`IClock`/`SystemClock`) makes CURRENT-DATE/WHEN-COMPILED deterministic for golden output.

## Interprogram (CALL)
One managed-reference carrier — **`ManagedPointer<T>`** (accessor-over-native-field or standalone cell, plus Null) —
serves BY REFERENCE, LINKAGE, POINTER, ADDRESS OF, BASED, ALLOCATE. Two layers: a typed fast path (`_SUB.Run(...)`)
for same-assembly conforming calls, and the opaque ABI `ICobolProgram.Call(CobolArgs)` for dynamic/cross-assembly CALL
via `ProgramRegistry`/`CobolModule`. USING BY REFERENCE/CONTENT/VALUE thread as pass modes; RETURNING → C# return
value. Each program is an instantiable class (singleton for last-used, fresh for INITIAL/RECURSIVE); `ExternalStore`/
`ExternalTable` hold EXTERNAL data across CANCEL; `StopRun`/`ProgramReturn` signals separate run-unit vs called-program
termination. See [[kb/IR/Control Flow]].

## OO Runtime
Every CLASS-ID → a real C# class rooted at **`CobolObject`**; OBJECT WS → instance fields, METHOD WS → statics,
LOCAL-STORAGE → locals, LINKAGE → `ref` params. INVOKE renders as `new`/virtual `obj.M()`/`this.M()`/`base.M()`/
static; genuinely dynamic dispatch uses `__CobolInvoke(name, CobolInvokeArg[], returning)` (reflection-free switch,
AOT-safe). FACTORY is a per-class singleton; `MethodReturn` signals method-only GOBACK. INTERFACE-ID/IMPLEMENTS/
PROPERTY and EC-OO ride the same engine.

## Conditions & Exceptions
Conditions bind to backend-neutral nodes rendered as pure, short-circuiting, fully-parenthesized C# booleans
(NOT>AND>XOR>OR); level-88 → bool properties; class conditions → `CobolClass`. The **EC model** is OFF by default
(zero scaffolding): `ExceptionCatalog` (ISO Table 13 hierarchy + fatality), `ExceptionEngine`/`ExceptionState`
(last-exception register, EXCEPTION-OBJECT, `PerformFrame` stack), `EcFunctions` (EXCEPTION-STATUS/-LOCATION),
`CobolFatalException`, `ResumeSignal`. `>>TURN` folds at compile time; USE declaratives run as bounded pc-ranges
returning a resume action; the Format-3 exception-checking PERFORM is a per-statement interceptor. See
[[kb/Semantics/Validation Rules]].

## Report Writer
`CobolReport` is the RWCS engine using **compose-at-presentation**: each report line is one generated `Func<string>`
invoked after LINE-COUNTER is set. It drives INITIATE/GENERATE/TERMINATE/SUPPRESS, PAGE geometry, LINE-/PAGE-COUNTER,
CONTROL breaks with prior-value CF composition, SUM (accumulate/reset/UPON), GROUP INDICATE, PRESENT WHEN, VARYING, and
USE BEFORE REPORTING hooks. Printable items are synthetic `DataItem`s; physical output goes through the file
connector's print stream.

## Key concepts
- **Typed-native, no byte State** — native fields; bytes only transient at file/CODE-SET/category-mismatch edges.
- **Int128-monomorphic numerics** — unscaled integer + compile-time scale; one `TryStore` funnel for ROUNDED + ON SIZE ERROR.
- **Value-in/value-out helpers** — string/inspect verbs return the new value; emitter assigns once via the `Place` lvalue.
- **Generated codec at the disk edge** — `IRecordCodec` Serialize/Deserialize; typed key ordering shared by files & SORT.
- **One managed-reference carrier** — `ManagedPointer<T>` for BY REFERENCE, POINTER, ADDRESS OF, BASED, ALLOCATE.
- **Backend-neutral runtime contract** — semantics in the binder; both backends call identical runtime entry points.
- **EC checking off by default** — classic ON SIZE ERROR/AT END/INVALID KEY always active; `>>TURN` folds at compile time.
- **Signals as exceptions** — `StopRun`, `ProgramReturn`, `MethodReturn`, `ResumeSignal`, `ExitPerformSignal`.
- **Injectable clock** — deterministic CURRENT-DATE/WHEN-COMPILED for golden-output conformance.

## See also
- [[kb/IR/Data Flow]] — the types the runtime operates on.
- [[kb/IR/Control Flow]] — the `StopRun`/`ProgramReturn` signals.
- [[kb/Architecture/Module Overview]] — the Runtime assembly.
- [[kb/Spec/Language Features]] — the verbs & intrinsics implemented here.

## Backlinks
- [[kb/Runtime/MOC]] · [[kb/Index]] — link here.
- [[kb/Architecture/High-Level Design]] · [[kb/IR/Control Flow]] — reference it.
- Lookup: [[kb/Spec/Lookup/Runtime Mapping]] · [[kb/Diagrams/Runtime Behavior Flow]] — map behaviors here.
