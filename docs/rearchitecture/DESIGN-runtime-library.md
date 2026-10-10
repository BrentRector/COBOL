# DESIGN — Target Runtime Library Organization (`Cobol.Net.Runtime`)

Status: EXECUTED (PHASE-08) — §2's target design IS the as-built runtime; §4's migration steps 1–5 are DONE
(plus the `ExternalSwitches`→`SwitchStore` conversion the §5 hidden-static gate surfaced); step 6 (the G8
RootNamespace flip + real sub-namespaces) is the ONE remaining item, deferred to G8 Cut 3 behind the compiler's
`RuntimeApi` façade. §1 records the pre-P8 problems as the rationale; §6's former open questions are resolved
inline. Scope: the organization + API of the greenfield runtime `src/Cobol.Net.Runtime` that WiseOwl COBOL-generated
C# programs call. Upholds the HARD INVARIANTS (typed-native only; spec-first; battery green throughout; singular
pattern; four-editions-in-one; JSON/XML out of scope).

Cross-dimension dependency: the *compiler-side* references to this runtime (~60 runtime members) are funnelled
through ONE typed `RuntimeApi` façade in the emitter (`CodeGen/Roslyn/RuntimeApi.cs`, a `nameof`-anchored static
class); the raw-string references still in `CodeGen/Emit/*` migrate onto it incrementally. Several renames/namespace
moves below are cheap because that façade exists; where a reference has not yet migrated onto it, the migration keeps
the emitted surface byte-stable (see Migration).

---

## 1. The pre-P8 problems this design fixed (the rationale record; all remedied by PHASE-08)

The pre-P8 runtime was a flat collection of **static facade classes** whose organization had drifted along three
axes (line references are to the pre-P8 tree):

### 1.1 Run-unit state is process-global ambient statics with an INCONSISTENT threading model (the prime smell)
Run-unit-lifetime state is spread across five unrelated static classes, each with its *own* threading assumption:

| State | Home | Threading model (current) |
|---|---|---|
| Program registry (names, instances, containment) | `Control/ProgramRegistry.cs` (static `Dictionary`/`List`) | plain process-global, "single-threaded in-process" assumed |
| Last-exception status, propagation slots, EC ambient gates | `Exceptions/ExceptionState.cs` (all `static … { get; private set; }`) | plain process-global |
| EXTERNAL data store | `ExceptionState.cs` → `ExternalStore` (static `Dictionary`) | plain process-global |
| `FUNCTION MODULE-NAME` call stack | `Control/CobolModule.cs` | **`[ThreadStatic]`** — the ONE type that is thread-local |
| File connector registries + GC-close queue | `IO/CobolFile.cs` (static `Dictionary` + `ConcurrentQueue`) | single-thread registries + a lock-free finalizer hand-off |
| ACCEPT clock / test seam | `IO/AcceptSource.cs` `public static Func<DateTime> Now { get; set; }` | process-global mutable |

`ProgramRegistry.Reset()` (ProgramRegistry.cs:359) is the de-facto run-unit boundary — it clears `ByPath`/`Order`,
`ExternalStore`, and `CobolModule`. But `ExceptionState`, `CobolFile.Init()`, and `AcceptSource.Now` reset through
*separate* entry points, so "start a clean run unit" has no single owner. Consequences: (a) two run units cannot
coexist in one process (forces the test harness to spawn `dotnet out.dll` per program — see the efficiency critique);
(b) the `[ThreadStatic]` vs plain-static split is a latent bug if a host ever thread-hops; (c) `AcceptSource.Now` and
`ExceptionState.ArgumentFunctionChecking`/`DataConversionChecking` are genuine process-global mutable seams.

### 1.2 The three file organizations duplicate the ISO §9.1.13 status machine + position state
`SequentialFile` (438), `RelativeFile` (487), `IndexedFile` (707) each independently re-implement: the two-character
FILE STATUS field + transition rules; the read-position guard pair `_lastReadUnsuccessful` /
`_prevOpWasSuccessfulRead` (verbatim in all three — SequentialFile.cs:31-32, IndexedFile.cs:47-48); `_mode` /
`_optionalAbsent` open-mode tracking; host-path resolution; and record framing (`KeyedFrames` is shared between
relative/indexed, but sequential re-does length-framing separately, SequentialFile.cs:42-48). The `CobolFile` static
facade then dispatches by *trying the sequential registry first and falling through to a parallel `Keyed*` fan-out*
(`CobolFile.cs:103-108,116,169,172,177` → `KeyedOpen/KeyedClose/KeyedStatus/KeyedLastReadLength/KeyedOpenModeOf`,
whose bodies live at the bottom of `IndexedFile.cs:570-707`). That is a second dispatch mechanism layered on the
type split — a singular-pattern violation.

### 1.3 Powers-of-ten recomputed by an identical multiply-loop in FOUR copies (efficiency critique, MEDIUM)
`CobolNum.Pow10`/`Pow10Wide` (CobolNum.cs:404-418), `CobolDec.Pow10` (CobolDec.cs:272), `CobolDate.Pow10`
(CobolDate.cs:154), `CobolIntrinsics.Pow10D`/`Pow10I` (CobolIntrinsics.cs:43-56) — every one is a
`for` loop recomputing a compile-time-constant table on every numeric store/rescale/format.

### 1.4 Value-library folder taxonomy is arbitrary; namespaces are incoherent
`Text/` holds value types (`CobolString`, `CobolBool`, `CobolClass`) while `Strings/` holds *statement runtime*
(`CobolStringOps` = STRING/UNSTRING, `CobolInspect` = INSPECT) — the split is by word, not by role. Namespaces are
mixed: `CobolIntrinsics`, `ProgramRegistry`, `NumProfile`, `CobolModule`, `ManagedPointer`, `ExternalStore` sit in the
**root** `CobolNet.Runtime`; `CobolNum`/`CobolDec` in `.Numeric` reference-only-by-string from the compiler;
`ExceptionState` in `.Exceptions`; files in `.IO`. There is no `.Control` namespace even though a `Control/` folder
exists. And the assembly is `Cobol.Net.Runtime` while the root namespace is still `CobolNet.Runtime` (a deliberate
G0→G7 deferral so generated `using CobolNet.Runtime;` is unaffected — csproj comment).

### 1.5 Minor API-coherence nicks
`NotImplemented`, `StopRun`, `MethodReturn`, `CobolInvokeArg`, `ResumeSignal`, `CobolFatalException` are one-purpose
control/signal types scattered between `Control/` and `Exceptions/`. `FileStatusCode` (the status constants) lives
inside `FileSupport.cs:74` next to the enums but is consumed by all three connectors.

---

## 2. Target design

### 2.0 Guiding principles
- **ONE owner for run-unit-lifetime state**: a `RunUnit` context object. Everything currently process-global that is
  *conceptually per-run-unit* moves onto it. Ambient access via `RunUnit.Current` (see §2.1) so the emitted surface
  need not thread a parameter.
- **ONE canonical mechanism per job** (singular pattern): one file-connector base + polymorphic dispatch (no
  `Keyed*` shim); one `Pow10` table; one clock; one status machine.
- **Role-based folders, not word-based**: *value library* (elementary/aggregate value operations) vs *verb runtime*
  (statement implementations) vs *IO connectors* vs *control/inter-program* vs *exceptions* vs *intrinsics*.
- **Typed-native invariant preserved**: the only character-image (`string`) boundaries remain the on-disk file edge
  (`FileConnector`), the Tier-B/EXTERNAL `StorageCell`, and the CALL-ABI adapters. No new byte substrate.

### 2.1 The `RunUnit` context (NEW) — the state-ownership + threading fix
```csharp
namespace Cobol.Net.Runtime.Control;

/// The single owner of all run-unit-lifetime state (ISO §14.6.1 run unit). Replaces the five independent
/// process-global static stores. One instance per run unit; the ambient current run unit is an AsyncLocal so a
/// host that thread-hops or runs run units concurrently each see their own (uniform threading model — no more
/// [ThreadStatic]-vs-plain-static split).
public sealed class RunUnit
{
    private static readonly AsyncLocal<RunUnit?> _current = new();
    public static RunUnit Current => _current.Value ?? throw new InvalidOperationException("no active run unit");

    public ProgramTable   Programs   { get; }   // was static ProgramRegistry
    public ExceptionState Exceptions { get; }   // was static ExceptionState
    public ExternalStore  External   { get; }   // was static ExternalStore
    public ModuleStack    Modules    { get; }   // was [ThreadStatic] CobolModule
    public FileRegistry   Files      { get; }   // was static CobolFile registries
    public IClock         Clock      { get; set; } = SystemClock.Instance;   // was AcceptSource.Now
    public RandomSequence Random     { get; }   // was static CobolIntrinsics._random (kb/Work PB307, §15.75.3 r4)
    public SortFileTable  SortFiles  { get; }   // was static CobolSort.Files (kb/Work PB1570, §14.6.1 + §14.9.40.4 GR10)

    /// A host's run unit for the duration of `body`; the generated Main begins its own with RunUnit.Begin().
    public static void Run(Action<RunUnit> body)
    {
        var prior = _current.Value;
        var ru = StartAfter(prior);   // a NEW object; only HostConfiguration carries over
        _current.Value = ru;
        try { body(ru); }
        finally { ru.Terminate(); _current.Value = prior; }   // the ONE §14.6.11 epilogue (RunMain calls it too)
    }
}
```
**A new run unit is a new `RunUnit` object — on every path.** `RunUnit.Begin()` (the emitted driver's
`ProgramRegistry.Reset()`) and `RunUnit.Run` both construct one, so every member — program table, EXTERNAL store,
MODULE-NAME stack, files, switches, locale, RANDOM sequence, sort-merge file stores, report flow, exception status, the per-class FACTORY
OBJECTS (`RunUnit.FactoryObject<F>()`, ISO §9.3.14.2: created before the first reference "by a run unit") and the
termination status — is fresh by construction. Only the declared HOST CONFIGURATION (`RunUnit.HostConfiguration`:
the clock seam and the object-time debug switch) is carried from the ambient run unit being replaced. The earlier
shape reset a HAND LIST of members on the one ambient object, and the switch, locale and report-flow state (and the
process-static factory singletons) survived into the next run unit (kb/Work PB1069);
`RunUnitStateDriftTests.EveryMember_IsFreshInTheNextRunUnit` now reflects over every field instead of a list.

**Static storage is ADOPTED by the run unit (kb/Work PB1069).** The one run-unit state a new object cannot make fresh
is STATIC data on a generated type: a RECURSIVE program's or a FUNCTION-ID's WORKING-STORAGE (§13.5.4 GR1) and a
class's METHOD WORKING-STORAGE (OO deep-dive D3). Each such unit emits `__ResetStatics`, and the run unit adopts it
(`RunUnit.AdoptStaticStorage`, once per reset): a program at `ProgramTable.Register`, a class when the run unit
creates its factory object (`RunUnit.FactoryObject` → the generated override of `CobolObject.__AdoptRunUnitStorage`,
which adopts both halves' resets and calls base, so every superclass adopts its own). Adoption runs the reset —
§14.6.2.3.2 1), the initial state at the first activation in a run unit — and `RunUnit.Terminate`, the ONE
termination epilogue `ProgramTable.RunMain` and `RunUnit.Run` both call, closes every file (§14.6.11 2) and runs every
adopted reset again, so a static POINTER, OBJECT REFERENCE or dynamic item releases what it held (§14.6.11 3, 4, 6).

**No run-unit state in a static — enforced.** `RunUnitStateDriftTests.NoWritableStatic_OutsideTheDocumentedProcessStores`
enumerates every writable static field in `Cobol.Net.Runtime` and fails on one not documented there as
PROCESS-lifetime (console encoding, collation-subsystem configuration, the per-thread overwritten-before-use
subscript scratch cell). The RANDOM sequence was the sixth process-global store the consolidation missed (kb/Work
PB307).

**The run unit's thread and the activation resource check (kb/Work PB2659).** Every COBOL activation — a CALL, a
user-defined function reference, an INVOKE or inline method invocation — is a chain of .NET frames on the thread that
runs the run unit, so the depth a RECURSIVE program reaches (§8.6.6) is that thread's stack. `Control/ActivationStack`
owns both halves of the rule:

- **The thread.** `ProgramTable.RunMain` runs the main program and the §14.6.11 epilogue on a thread of its own
  (`ActivationStack.RunOnRunUnitThread`) whose stack is `RunUnitThreadStackBytes` = 384 MiB of reserved address
  space, committed only as deep as the run unit goes. The thread is transparent: the ambient `RunUnit` flows to it
  with the execution context, it takes the caller's culture, and whatever escapes it (the `StopRun` unwind the
  generated `Main` catches, or a defect) is rethrown on the calling thread. Before it, the main program ran on the
  host's default stack (about 1 MB for a Windows main thread) and a self-CALLing program died of a CLR stack overflow at a
  depth of about 250 — no exception condition, no §14.6.11 CLOSE.
- **The check.** §14.9.4.4 GR3c, §8.4.3.2.4 GR6c and §14.9.23.4 GR7b give "the resources necessary to execute the
  program are not available" a defined outcome, and Annex A.1 items 14, 89 and 102 let the implementor say which
  resources are checked. The one checked here is the stack: `ActivationStack.IsAvailable` asks the .NET runtime's own
  margin (`RuntimeHelpers.TryEnsureSufficientExecutionStack`, 128 KB on 64-bit, valid on any thread) and, on the run
  unit's own thread, a larger `ActivationReserveBytes` (1 MiB) measured from the thread's start, so the deepest
  activation keeps room for its own statements and for the handler of the activation that fails. `ProgramTable.CallProgram`
  asks it first (before GR3d–f; a CALL and a function activation raise EC-PROGRAM-RESOURCES through the CALL
  carrier), and the emitted head of every method body asks it through `ActivationStack.RequireForMethod`
  (EC-OO-METHOD, before GR7 d). A PROPERTY clause's synthesized accessor executes no statement and is not checked.
- **No catch-and-rethrow on the activation path.** A catch block runs on top of the stack the exception was thrown
  from, so a `throw;` inside it starts a nested dispatch there. The GR3i boundary mark in `CallProgram` was such a
  rethrow, and a condition crossing N boundaries stacked N dispatches: the EC-PROGRAM-RESOURCES of an exhausted
  recursion overflowed the stack again while unwinding. The mark is an exception FILTER (`MarkBoundaryCrossing`),
  which runs in the dispatch's first pass in the same innermost-first order and declines the condition.

The size is chosen from measurement, not guessed. The target is the depth GnuCOBOL 3.2 reaches on its default 8 MB
stack with a minimal self-CALLing program — 74,687 activations, after which it dies of SIGSEGV with no message —
reached by the same program under its most expensive frame shape here. Measured per activation (deep recursion runs
mostly in the JIT's first-tier frames) and the depth at 384 MiB:

| program | KB per activation | depth at 384 MiB (Windows) | (Linux) |
|---|---|---|---|
| minimal RECURSIVE self-CALL | 1.5 | 256,126 | 143,146 |
| the same under `>>TURN EC-ALL CHECKING ON` | 3.9 | 104,585 | 92,137 |
| twenty statements and LOCAL-STORAGE | 2.5 | 156,190 | 139,121 |
| the same under `>>TURN EC-ALL CHECKING ON` | 12.4 | 32,566 | 66,029 |

(The Linux column ran the IL runtime without ReadyToRun code under WSL; fully optimized code — tiered compilation
off — costs 1.2 to 4.3 KB per activation for the same four programs.) A recursion through PERFORM is not an
activation and is not checked: §14.9.28.4 GR2 makes it undefined, and its NOTE 1 names the stack overflow.

Rationale: `AsyncLocal` (not `ThreadStatic`) because the correct scope is the *logical* run-unit activation, and it
subsumes `CobolModule`'s existing thread-locality while also being correct across `await`/thread-pool hops. Hot
facades cache `RunUnit.Current` in a local at entry to avoid repeated `AsyncLocal` reads.

**Emitted-surface compatibility (battery-green):** the existing static facades stay as *thin delegating shims* over
`RunUnit.Current` during the migration, so generated code (`ProgramRegistry.CallProgram(...)`,
`ExceptionState.Set(...)`, `CobolFile.Open(...)`, `CobolModule.Push(...)`) keeps compiling unchanged:
```csharp
public static class ProgramRegistry {                 // compat shim (deleted at G8 or kept — OPEN QUESTION)
    public static void CallProgram(string n, string c, CobolArg[] a, ManagedPointer? r,
        bool site = false, string ec = "EC-PROGRAM-NOT-FOUND") => RunUnit.Current.Programs.Call(n, c, a, r, site, ec);
    public static void Reset() => /* replaced by RunUnit.Run lifecycle */ ;
}
```

### 2.2 IO — one `FileConnector` base, three organization subclasses, one registry
```csharp
namespace Cobol.Net.Runtime.IO;

/// Shared control logic for every organization (ISO §9.1.13 status machine + §14.9.30/§14.9.35 read-position
/// state + OPEN/CLOSE/mode + host path). Organization-specific record SELECTION is abstract.
public abstract class FileConnector
{
    public string Status { get; protected set; } = FileStatusCode.Success;   // §9.1.13
    protected FileOpenMode Mode;
    protected bool OptionalAbsent, LastReadUnsuccessful, PrevOpWasSuccessfulRead;   // the shared position pair
    protected readonly string HostPath;
    public int  OpenModeView { get; protected set; } = -1;    // §14.9.49 GR6 USE-declarative mode scoping
    public int  LastReadLength { get; protected set; }

    public string Open(FileOpenMode mode);                     // shared preamble → OpenCore; returns the status
    public string Close();                                     // shared → CloseCore
    // The READ preconditions, written ONCE for every organization (kb/Work PB336). Order IS the rule inside
    // SequentialReadGuard: §14.9.30.4 GR2 '47', then GR21's '46' poison, then §9.1.13.4 1c's '10' — which is
    // "first time" only BECAUSE its own arm arms the poison. Random side: §9.1.13.5 3b's '23', never '46'.
    protected string? ReadOpenModeGuard();
    protected string? SequentialReadGuard();
    protected string? RandomReadAbsentOptionalGuard();
    protected abstract string OpenCore(FileOpenMode mode, FilePresence presence);   // takes the ONE presence probe
    //   (Open also answers GR3/'37', GR10/'39' and GR16's write capability/'37' before OpenCore is reached)
    protected abstract bool ReadNext(out string image);        // sequential retrieval
    protected abstract void WriteRecord(string image, int length);
    // Keyed subclasses add: ReadByKey / Start / Delete / Rewrite; sequential adds ADVANCING / LINAGE.
}

public sealed class SequentialConnector : FileConnector { … }  // was SequentialFile (incl. line- & record-seq, LINAGE)
public sealed class RelativeConnector   : FileConnector { … }  // was RelativeFile
public sealed class IndexedConnector    : FileConnector { … }  // was IndexedFile (record-list-as-truth design kept)

/// The per-run-unit connector registry + the GC-finalizer deferred-close queue. ONE lookup keyed by COBOL
/// file-name; dispatch is polymorphic on FileConnector — no Sequential-first/Keyed-fallthrough split.
public sealed class FileRegistry { … Register/Open/Close/Read/Write/Status/CloseAll … }
```
**One presence probe, three answers (`HostFile.Probe` → `FilePresence`, in `IO/FileSupport.cs`).** "Is the
physical file there?" is asked in exactly ONE place in the runtime, and its answer has **three** states —
`Absent` · `Present` · `Unauthorized` — because the standard prices two of them differently: §9.1.13.6 item 5
sets '35' when *"the physical file is not present"*, while §14.9.27.4 GR3 sets '37' when *"the file … is present
and insufficient authority exists to open the file"*. `File.Exists` cannot express that — it swallows every
access error and answers `false` — so it is **banned** everywhere under `Runtime/IO` (drift-guarded by
`HostFileProbeDriftTests`, which also bans `FileInfo.Exists` and the raw `File.GetAttributes` the probe is
built from). `FileConnector.Open` takes the probe once per OPEN, answers GR3 there and hands the result to
`OpenCore(mode, presence)`, so an organization body sees only the two-state Table-18 question it is entitled to
and no two probes in one statement can disagree; `FileRegistry.DeleteFile` takes the same probe for §14.9.10.4
GR14/GR16. GR3's short-circuit deliberately **excludes OUTPUT**: GR18 makes OUTPUT a creation that never
consults presence, and a directory the process may write but not list legitimately accepts a new file, so an
OUTPUT authority failure comes from the creating stream and the shared `catch` instead. (kb/Work PB323 — the
OPEN arm of the sweep PB140 had done only for DELETE FILE.)

**One write-capability probe, asked in the OPEN contract (`HostFile.PermitsWrite`, same file).** Presence is not
capability: a read-only file is `Present` and perfectly observable, and §14.9.27.4 GR16 still requires '37' —
*"If the I-O phrase is specified, the file shall support the input and output statements that are permitted for
the organization of that file when opened in the I-O mode"* — as does §9.1.13.6 item 6 a) 1. for EXTEND and
OUTPUT. **Neither rule names an organization**, and Table 20 is why they cannot: REWRITE sits under the I-O
column for sequential, random *and* dynamic access. So `FileConnector.Open` asks the question ONCE, for
I-O/EXTEND/OUTPUT on a `Present` file, immediately before `OpenCore` — the second half of the same contract
that already owns GR3 and GR10 — and an organization added later inherits the answer instead of re-earning it.
The probe is a real write open (`FileMode.Open` + `FileAccess.Write` + `FileShare.ReadWrite`, nothing written),
because a read-only attribute, a Unix mode bit and a deny-write ACE are three mechanisms with one consequence
and only the host can rank them; opening without writing leaves content and last-write time untouched, which is
what GR25 (*"the file is not affected"*) requires of the open that is about to fail. Only
`UnauthorizedAccessException` is an answer — every other `IOException` propagates to `FileConnector.Open`'s
ONE catch, which asks the host ONE question first: `HostFile.IsSharingRefusal` (Windows
`ERROR_SHARING_VIOLATION`/`ERROR_LOCK_VIOLATION`, Unix `EWOULDBLOCK` from .NET's `flock`). A sharing refusal is
§9.1.13.9 1)'s '61' — another run unit's connector holds the file, and the host said so — and only the residue
is §9.1.13.6 1)'s '30', *"no further information is available"* (kb/Work PB860: every organization answered
'30'). DELETE FILE asks the same question of the host before it deletes (`HostFile.IsHeldByAnother`, an
exclusive read request, inside the RETRY loop) and answers §9.1.13.9 2)'s '62', because a Unix unlink never
consults open handles and would otherwise destroy a file another run unit holds open. INPUT is excluded and the exclusion is load-bearing: a read-only file supports everything
Table 20 permits in the input mode, and item 6 a) 3.'s read-capability question is already answered eagerly by
the sequential reader and the keyed `Attach()`. `HostFileProbeDriftTests` bans `PermitsWrite` outside
`FileConnector` and pins the call count at one. (kb/Work PB328 — the sequential arm asked the question by
accident, since its I-O and EXTEND streams *are* write opens, while the relative and indexed arms only read
their store: the same read-only file answered '37' sequentially and '00' keyed, then '00' for READ and REWRITE,
and the loss surfaced as a '30' at CLOSE on a byte-identical file. The two-arm dispatch again, one arm fixed.)

**One host-path OPEN, four named roles (`HostFile.OpenConnectorStream` / `HostFile.OpenConnectorWriteStream` / `HostFile.OpenConnectorStore` / `HostFile.OpenAuxiliary`, same file).**
The third question `HostFile` owns is *"what may every OTHER handle on this physical file do while this one is
open?"*, and it is not a per-call-site decision: §9.1.15 puts the in-run-unit gate on the file connectors —
*"Before access to a shared physical file is allowed through an OPEN statement, the sharing mode and the open
mode of that OPEN statement shall be allowed by all other file connectors that are currently associated with
the physical file"*, arbitrated by §14.9.27.4's Table 19 — never on the operating environment's handle.
(⛔ That sentence is §9.1.15's, not §14.9.27.4's: this paragraph carried the wrong clause number from the
PB713 landing until PB740's, an INHERITED citation, `cite.py --check`ed on both spellings.) The share mode is
therefore SPENT here and DERIVED in `FileLockPosture` (below).
`OpenConnectorStream(path, mode, access, share, options)` is a sequential connector's own long-lived read or
read-write stream, and `OpenConnectorWriteStream(path, mode, share)` its write stream; BOTH are UNBUFFERED in every
posture, because the connector above keeps exactly one buffer — its `StreamReader`'s or `StreamWriter`'s — and can
invalidate or release only the one it owns (kb/Work PB753, below). **That one buffer IS the connector's input-output
areas** (ISO §12.4.5.14.3 GR1, kb/Work PB643): `FileConnector.InputOutputAreas` (the RESERVE clause's integer-1,
registered by `CobolFile.RegisterReserve`, else `HostFile.ImplementorInputOutputAreas` = 1) × `HostFile.InputOutputAreaBytes`
(4,096), sized in ONE place, `HostFile.InputOutputAreaBuffer`. A connector holding the only writable handle releases
its records to the medium as its areas fill; one whose file lock admits another writer releases at every WRITE /
REWRITE (`SequentialConnector.ReleaseRecord`), so the posture decides WHEN the areas are emptied, never WHETHER they
exist (before PB643 the areas were the host handle's 4,096-byte buffer in one posture and the reader's default buffer
in the other, so RESERVE had no one place to land). `OpenConnectorStore(path, mode, access, share)` is a
RELATIVE or INDEXED connector's long-lived store handle (kb/Work PB771, below), UNBUFFERED: the store is read and
written positionally (kb/Work PB2660), and every byte of it passes through the connector's own areas,
`KeyedConnector._areas`, allocated by the OPEN that takes the handle (kb/Work PB2693 — before it the areas were the
handle's stream buffer, which no store byte passed through). `OpenAuxiliary(path, mode, access)` is a short-lived
bookkeeping handle over a path a connector may already hold — the shared `OPEN EXTEND` write-base measurement,
the §14.9.27.4 GR10 store-header read and the varying framing's parse check — and is **always**
`FileShare.ReadWrite`, because a handle's share mode has to admit the access every outstanding handle already
holds or the host refuses it. Every one of those runs BEFORE the connector's own handle for that OPEN exists,
which is what makes the permissiveness free: this role is never the handle a COBOL statement is served
through and never stands in for a file lock. **A keyed store's whole-file load and persist LEFT this list**
(kb/Work PB771) — they were the only handle those organizations ever took, which is why they held no
§9.1.15 lock; they now travel through the connector's own `OpenConnectorStore` handle. `SharedExtendOpenDriftTests` bans every other host-path open under `Runtime/IO`
and pins both roles to this file. (kb/Work PB713 — a sharing-active `OPEN EXTEND` measured its write-ordinal
base from a SECOND handle on the path it had just opened for WRITE: `File.ReadLines` for the line-sequential
framing, a three-argument `FileStream` for the varying one, both `FileShare.Read`, both refused. The refusal ran
from `FileRegistry.SharedOpenAttempt` — *after* `FileConnector.Open` returned, outside its try — so it left the
run unit as an unhandled `IOException` naming "another process" that was the program itself, where §14.9.27.4
GR1 admits only an I-O status. Fixed the way GR3 and GR16 were: the measurement moved INTO
`SequentialConnector.OpenCore`, before the writer exists, where the '30' mapping already lives, and the
duplicate `HostFile.Probe` it carried against PB323's one-probe rule went with it. The fixed-width framing read
`FileInfo.Length`, took no handle, and passed — one dispatch, three arms, one arm tested.)

**The third role, and the rule it serves: a release is positioned and numbered from the SHARED MEDIUM, at the
moment of the release.** `OpenConnectorWriteStream(path, mode, share)` is a connector's own write handle — `Create`
for `OPEN OUTPUT`, `Append` for `OPEN EXTEND` — and it exists because
.NET has no atomic append: `FileMode.Append` seeks to the end ONCE, at open, and every later write goes at that
stream's own position. For the connector holding the only writable handle there is that is right, and it keeps the
plain handle byte for byte. When the posture admits ANOTHER WRITER it is wrong, and silently: ISO §14.9.51.4 GR19 says of two
connectors extending one shared file that *"the added records follow the records present in the physical file
when it was opened, but are otherwise in an undefined order"* — only their ORDER is undefined — and both
connectors anchored at the same offset, so the later flush wrote over the earlier record while both WRITEs
reported '00'. Such a connector therefore gets `SharedAppendStream`, which repositions at the physical end before
every write, over an UNBUFFERED handle; and `SequentialConnector.ReleaseRecord` — the ONE place a sequential
record is released, reached by all three write arms — flushes it there, because GR12 (*"The successful execution
of a WRITE statement releases a logical record to the operating environment"*) is what makes the '00' a promise
and because a record still in this connector's buffer is one the other connector cannot see, count, or avoid
overwriting. **The predicate is the POSTURE, not a clause** (kb/Work PB740): a handle whose §9.1.15 file lock
admits a writer takes the repositioning stream and flushes each release, which since kb/Work PB322 means a
connector declared `SHARING WITH ALL OTHER` (the only mode Table 19 admits a writing sibling beside). Keying it on
"this SELECT wrote a clause" would have left a pair that shares a file without either having written one —
writing through two independent buffers.

The same sentence settles the record's IDENTITY, which is the half that shows up in §9.1.16 (*"While locked by a
given file connector, a record is not accessible to another file connector in the same or a different run
unit"*). The ordinal is minted from `PhysicalFileTable.State.ReleasedOrdinal` — one mint per physical file,
seeded by a shared `OPEN EXTEND` from the records already there and reset by a shared `OPEN OUTPUT` — rather than
from a per-connector base plus a per-connector count, which had both connectors calling their first appended
record ordinal 2. `FileConnector.Physical` is how a connector reaches that state and EVERY connector holds it —
Table 19 arbitrates every open over it, and since PB740 two clause-less connectors may share one physical file,
so a rule about the MEDIUM cannot be gated on a clause. `SharedPhysical` is the §9.1.16 RECORD-LOCKING view of
it, non-null only for a connector that registered a SHARING or LOCK MODE clause, because the ordinal *is* the
lock identity and §12.4.5.9.4 GR1 b) 2. leaves a clause-less connector's record locking to the implementor
(this compiler's determination: none). Both are written by ONE call, `AssociatePhysical`, immediately before the
OPEN body runs — the boolean that used to sit beside the state is still gone, and the view is derived rather
than stored (kb/Work PB740 removed the conflation with the OS share mode; PB753 split the medium question from
the locking one without adding a second answer).

**The §9.1.15 FILE LOCK is derived, in one place: `FileLockPosture` (`IO/Sharing/`), applied by
`FileRegistry.SharedOpenAttempt`.** §9.1.15 addresses two audiences with one sharing mode and they need two
mechanisms. Inside the run unit — *"Multiple paths of access may exist in the same runtime element, contained
elements, separate runtime elements within the same run unit, or runtime elements in different run units"* — the
gate is Table 19, already run; outside it, *"The successful opening of a file establishes a file lock for the
applicable sharing rules, thereby preventing other run units from opening that file with incompatible sharing
rules"*, and the host's share mode is that lock. It cannot be both, because a share mode names no requester: it
cannot admit this run unit's second connector while refusing a foreign process. So:

- the BASE is §9.1.15's three rules read literally — `NO OTHER` ⇒ `FileShare.None` (*"exclusive access"*),
  `READ ONLY` ⇒ `FileShare.Read` (*"restricts concurrent access … to input mode"*), `ALL OTHER` ⇒
  `FileShare.ReadWrite` (*"allows concurrent access … specifying input, I-O, or extend mode"*);
- a connector with no SHARING specification holds one of those three modes too, given by its open mode
  (`FileRegistry.ImplementorDefaultSharing`, kb/Work PB322, Annex A.1 items 77 and 131): `OPEN INPUT` is `READ
  ONLY` (⇒ `FileShare.Read`) and every other mode is `NO OTHER` (⇒ `FileShare.None`), so a clause-less updater
  protects its physical file from other PROCESSES exactly as `SHARING WITH NO OTHER` does;
- the registry hands the posture down ONCE per OPEN, ahead of the handle (`c.HostSharing = sharing`; `HostShare` is derived from it
  in `SharedOpenAttempt`), and nothing re-derives it while the connector is open. **EVERY organization holds a
  long-lived host handle carrying its posture** — §9.1.15 3) names none, so all three owe the lock (kb/Work
  PB771). `KeyedConnector` is where RELATIVE and INDEXED hold theirs: `TakeFileLock` on each `OpenCore` arm that
  has a physical file and `ReleaseFileLock` in the CLOSE and on an unsuccessful OPEN. ⛔ **It is the SAME handle
  the store travels through** — `KeyedConnector.Load`/`Persist` read and write positionally through it, and `RecordFraming` only composes and decodes the image — because a
  second handle for the load or the persist would ask for access the connector's own `FileShare.None` forbids,
  which is kb/Work PB713's defect re-opened by its own cure. The handle's ACCESS is `FileConnector.HostAccess`,
  not the open mode's floor, because a whole-store organization reads the physical file in every writable mode
  (§14.9.51.4 GR29 a)'s release number is a fact of the records already there).

There is NO widening and NO handle rebuild. Every pair of the three modes that Table 19 admits already has
mutually admissible base postures — `FileLockPostureDriftTests.EveryNormalOpenCellHasMutuallyAdmissiblePostures`
proves it over every *Normal open* cell and every access a handle may take — because a `READ ONLY` connector
admits only input-mode siblings and an `ALL OTHER` one admits every access. (While the implementor default was
UNDETERMINED the base of a clause-less connector was unknown, so its posture was widened to admit whichever sibling
Table 19 admitted and the handle was rebuilt at its logical offset by a virtual `Reposture`; kb/Work PB322
determined the default and deleted that machinery, `SyncHostPostures`, `FileLockPosture.For` and both
`Reposture` overrides with it. History: kb/Work PB740 — one boolean, "did this SELECT write a SHARING or LOCK MODE
clause", was spent on both audiences and answered each backwards: two clause-less connectors Table 19 PERMITTED to
share one file were refused by the host, the second OPEN answering '30' — a status no Table 19 row and no
§9.1.13.9 item produces — and its mirror, measured across processes, had a `SHARING WITH NO OTHER` connector
admit a foreign append to a file the program had declared exclusive.)

**The §9.1.15 FILE LOCK AGAINST OTHER RUN UNITS is Table 19 itself: `RunUnitFileLock` (`IO/Sharing/`), taken by
`FileConnector.Open` (kb/Work PB833).** The share mode above is what a process that takes part in no protocol
meets, and it cannot be the whole of §9.1.15's *"preventing other run units from opening that file with
incompatible sharing rules"*, for two measured reasons: .NET reaches `FileShare` on Unix through ONE advisory
`flock` whose only states are `LOCK_EX` (`FileShare.None`) and `LOCK_SH` (every other value), so rule 2's
*"restricts concurrent access … to input mode"* — admit a reader, refuse a writer — has no expression in it; and
on EVERY host a share mode refuses an open by its ACCESS, so it cannot refuse the truncation of an `OPEN OUTPUT`
when a sibling's posture admits a writer (measured on Windows before PB833: a second run unit's `OPEN OUTPUT`
answered `00` against a file the first held open `SHARING WITH ALL OTHER`, and emptied it, where Table 19 prints
*Unsuccessful open* in every OUTPUT cell). The remedy is not to widen the posture to `FileShare.None`, which
refuses the reader rule 2 admits; it is a second expression of the same table:

- **Publish, then test (`RunUnitFileLock.Take`).** The OPEN publishes its Table 19 COLUMN
  (`Table19.Column(sharing, mode)`) as a SHARED region lock on one byte of the physical file at offset
  2^62 + the column's wire number (the five `ExistingSharingColumn`s; the numbers are a contract between run units
  and are spelled out in the class, not derived from an ordinal), and then asks whether any OTHER description holds
  a column its request ROW refuses (`Table19.Cell(row, column) == UnsuccessfulOpen` over the five columns). A
  refusal is §9.1.13.9 1)'s `61`, the lock is given back, and §14.9.27.4 GR25's *"the file is not affected"*
  holds because the test precedes `OpenCore`'s body on a file that exists. Publishing FIRST is what makes two
  simultaneous OPENs unable to pass each other; it needs a lock the asker's OWN description does not conflict
  with, because the classes that refuse their own kind (NO OTHER, an OUTPUT) would otherwise refuse themselves.
  The table is read, never restated: the cross-run-unit verdict and the in-run-unit verdict are ONE lookup.
- **The host primitive is `OfdRegionLocks` (`IO/Sharing/`): Linux open-file-description locks** (`fcntl`
  `F_OFD_SETLK`/`F_OFD_GETLK`, kernel 3.15; x86-64 and arm64). Classic `fcntl` locks and `FileStream.Lock` are
  keyed on the PROCESS and are dropped when ANY descriptor the process holds on the file closes, which this runtime
  does constantly for its own bookkeeping handles; an OFD lock belongs to the one descriptor that took it, conflicts
  with every other descriptor (this process's or another's) and never reports the asker's own. The descriptor is
  made with `open(2)` directly (`O_CLOEXEC`), not through `FileStream`, because .NET would take a `LOCK_SH` `flock`
  on it that a sibling's own `FileShare.None` handle would then be refused by. The lock is unlocked explicitly
  before the descriptor closes, so a `fork` holding a copy of it until its `exec` cannot make a CLOSEd lock outlive
  the CLOSE.
- **OUTPUT asks the host first where the share modes are the lock (`FileConnector.TakeRunUnitFileLock`,
  `HostFile.ShareModesAreMandatory` — Windows, pinned by the measured `HostCapability.Sharing`).** An OUTPUT open on
  a file that exists first requests `FileShare.None` (`HostFile.IsHeldByAnother`) — refused by every outstanding
  handle there is, which is what Table 19's OUTPUT rows mean — before the body truncates anything. This is what
  closes the Windows gap, where there is no region-lock expression and `FileShare` carries every other cell. A host
  with the region lock does not ask: the lock refuses an OUTPUT like any other cell, and a read open of a FIFO waits
  for a writer that is the program's own next statement.
- **Capability is measured.** `OfdRegionLocks.Open` answers null — and `Take` answers `Unavailable` — for a host
  that cannot hold the lock: an ABI it was not written for (the commands and `struct flock` are the Linux 64-bit
  x86-64/arm64 values), no `libc` (Windows, where `FileShare` is mandatory and per-access already), `EINVAL` from
  the kernel (no OFD locks), `ENOLCK`/`EOPNOTSUPP` from a filesystem without byte-range locks — and a path that is
  not a REGULAR file (read from the descriptor with `statx`, opened `O_NONBLOCK` so a FIFO answers at once): the lock
  lives on the inode, so a device node such as `/dev/null` would arbitrate every run unit on the machine that assigns
  it. An unavailable lock
  leaves the connector the file lock its `FileShare` gives, nothing more.
- **One policy, two hosts (`IColumnLockHost`, kb/Work PB2484).** `RunUnitFileLock.Take` is the POLICY — the column
  an OPEN publishes (`Table19.Column`), the columns its row refuses (`Table19.Cell`), the wire number of each
  (`RunUnitFileLock.WireNumber`) — and speaks to the host of the process through `IColumnLockHost.Establish`,
  chosen ONCE (`RunUnitFileLock.HostOfThisProcess`): `OfdColumnLockHost` where `OfdRegionLocks.Available` (Linux
  x86-64/arm64), no host on Windows (`HostFile.ShareModesAreMandatory`), `SidecarColumnLockHost` everywhere else.
  Both hosts publish BEFORE they test and never count the asker's own publication, which is the whole of the
  race-freedom argument. Adding a host is one class and one line in the choice.
- **The lock-file host (`SidecarColumnLockHost`).** macOS has no host primitive for the Linux bytes: its classic
  `fcntl` locks are process-owned, its open-file-description commands are private, and `fcntl` is variadic, which
  Apple's arm64 calling convention passes differently from a fixed P/Invoke signature — so the class makes NO native
  call. A holder's lock is a FILE — `.wolk-<16 hex digits>/<column>-<guid>` in a directory BESIDE the physical
  file (the digits: SHA-256 of the file's name under `HostFile.PhysicalFileKey`, the case rule that
  `HostFile.PhysicalFileComparer` also reads) — held open through `HostFile.OpenLockHolder` (`FileShare` other than
  `None`, hence a shared `flock`) and tested by `HostFile.IsHeldByAnother` (an exclusive request, refused while the
  holder lives; the kernel drops the lock with the descriptor, so a killed run unit holds nothing). A lock file
  PER HOLDER and not per column, because a request row can refuse its own column and a `flock` cannot say "anybody
  but me" about a file the asker holds: the tester skips only its own file. A holder creates and locks
  `pending-<guid>`, verifies the lock is in force (a host that does not refuse the second request — a network file
  system, `DOTNET_SYSTEM_IO_DISABLEFILELOCKING` — is `Unavailable`) and RENAMES it to its column name, so the testers,
  which read only column names, never see a half-published holder; a column file or pending file they can open freely
  belongs to a dead run unit and is deleted. The CLOSE deletes the file BEFORE closing the descriptor (a `fork` for
  `CALL "SYSTEM"` holds a copy of the description until its `exec`; a name nobody can see cannot refuse a successor)
  and the directory with its last file. A directory the process may not write is `Unavailable`, never an exception.
- **Guards.** `RunUnitFileLockDriftTests` opens a file in one `FileRegistry` and then in a second — a second
  registry shares nothing with the first, neither the `PhysicalFileTable` nor a handle — for every sharing spelling
  × open mode × sharing spelling × open mode × organization (768 pairs) and asserts the second OPEN is `61` exactly
  where `Table19.Conflicts` refuses and a success-family status where it permits; it runs on Windows and Linux,
  through the host of the machine. `SidecarColumnLockHostDriftTests` holds the lock-file host to the same table over
  every (sharing × open mode) pair directly (`RunUnitFileLock.Take(host, …)`), plus the simultaneous-open race, the
  sweep of a dead holder's leftovers, the wire names and the case rule — it is plain managed code, so it runs on
  EVERY host (Windows, Linux and macOS) and needs no Mac to witness the macOS path.

**⛔ DETERMINATION (Annex A.1 item 75, `docs/CONFORMANCE.md` DOC-A.1-75) — WHO IS BOUND.** §9.1.15 binds other RUN
UNITS, and every WiseOwl COBOL run unit is arbitrated by Table 19 on a host that carries the region lock (Linux
x86-64/arm64) or whose `FileShare` carries the table (Windows, with the OUTPUT probe above). A process that takes
part in no protocol meets only the share mode of the connector's own handle, whose strength is the host's:

- **On Windows** the share modes are mandatory and per-access: rules 1–3 are enforced as written against every
  process on the machine.
- **On Linux** the share mode is one advisory `flock`: rule 1 (`FileShare.None` ⇒ `LOCK_EX`) excludes an outside
  reader and writer that also take `flock`, and rule 2's refusal of an outside writer exists only against a run unit
  of this runtime (the region lock). A program that opens the file without locking is neither stopped nor
  detected. `HostCapability.Sharing` (tests/_shared) MEASURES the share-mode semantics and
  `FileLockPostureDriftTests.TheHostsShareModeSemanticsAreTheDocumentedOnes` asserts the measurement against this
  paragraph, so if the host moves, the gate goes red naming this section rather than an assertion being adjusted.
- **On macOS** (and every Unix that is not Linux on x86-64/arm64) the file lock is the LOCK FILE PER HOLDER of
  `SidecarColumnLockHost` (kb/Work PB2484, the bullet "The lock-file host" above), so every WiseOwl COBOL run unit
  is arbitrated by Table 19 there too; an outside program meets the share mode only, as on Linux. The classic
  `fcntl` locks macOS offers are process-owned and cannot be used for the reason above.


**The rule generalizes, and that is why it is written here rather than in the sequential connector.** Each
organization spells it in its own terms and each must read the shared medium at the release, never a base
captured at OPEN:

- sequential — the medium is the file, and the position is its current end (GR19);
- relative — the medium is the shared `RelativeStore`, and the number is `Highest + 1` at the write. GR29 a) says
  so twice: *"one greater than the highest relative record number existing in the physical file"*, and *"If the
  physical file is shared and the open mode is extend, the record numbers are not necessarily consecutive.
  Otherwise, they are consecutive"* — one expression yields consecutive numbers when nothing else is releasing
  and the ascending-with-gaps sequence GR31 describes when something is. `RelativeConnector`'s captured
  `_seqNext` is gone; the store's map is exposed read-only and every mutation goes through `Put`/`Remove`/`Clear`,
  which maintain `Highest`, so the high-water mark has no second way to go stale;
- indexed — the medium is the shared `IndexedStore`, whose `NextOrdinal` was already the shared mint. Its GR38
  high key is per-connector on purpose: the rule says *"when it was opened through that file connector"*.

`SharedExtendWriteDriftTests` measures the whole (organization × framing × spelling) matrix, the exclusive
control, the print-control write arm, the store's high-water invariant and a static ban on `FileMode.Append`
outside `HostFile`; it was proved red on the pre-fix behaviour at 17 of 27. (kb/Work PB739.)

**And the READ side of the same sentence: what a READ delivers is read from the shared medium AT THE READ.**
§14.9.35.4 GR4 gives REWRITE the identical words GR12 gives WRITE — *"The successful execution of the REWRITE
statement releases a logical record to the operating environment"* — and §14.9.30.4 GR21 c)/d) say what the
other connector then owes: the record selected is *"the first existing record **in the physical file** whose
relative key number is greater than the file position indicator if NEXT is specified or implied"*, and that
record *"is made available in the record area associated with file-name-1"*. The physical file, not a snapshot
of what it said when a buffer was filled. The keyed organizations satisfy this by construction — their medium is
the ONE `KeyedStoreTable` store, so a sibling's mutation is visible instantly — and the sequential connector,
whose medium is the file system, needed the rule stating in two places at once:

- `PhysicalFileTable.State.ReleaseGeneration` counts the releases that have REACHED the physical file (both
  verbs; `SequentialConnector.NoteRelease` is the one place it moves, and it advances the releasing connector's
  own watermark in the same breath, because a sequential REWRITE targets a record at or before its own file
  position indicator and cannot invalidate its own read-ahead). `NextFrame` — the ONE physical framing walk —
  compares it before every frame and re-anchors the reader at the logical offset, discarding the read-ahead.
- and the OS handle carries NO buffer of its own when the posture admits another writer, because
  `StreamReader.DiscardBufferedData` reaches only the reader's own characters: with the generation in place and
  a 4096-byte `FileStream` underneath, a sibling's REWRITE was STILL invisible, since `FileStream.Seek` reuses
  its read buffer whenever the target offset lands inside it. One managed buffer between the connector and the
  medium is the most that leaves GR21 c) true.

`SharedReadCoherenceDriftTests` measures the matrix — every framing, a buffer-crossing and a sub-buffer record
count, reader-first and writer-first, the clause-less pair — plus the controls (the sibling APPEND, a lone I-O
connector rewriting its own records, the two keyed organizations) and the two structural facts; each half was
proved red by injection at 25 of 47.

⛔ **A probe of this rule must have SIBLINGS open at once, and only `SHARING WITH ALL OTHER` admits a rewriting or
appending sibling** (kb/Work PB322: a connector with no SHARING specification is `READ ONLY` for `OPEN INPUT` and
`NO OTHER` otherwise, and Table 19 admits neither beside a writer). The matrix therefore runs its sibling shapes
over the `ALL OTHER` clause only, and the clause-less spellings over the shapes with no sibling (the lone I-O
connector rewriting its own records). (Before PB322 a clause-less pair could share, and the probe also had to be
built against the handle REBUILD a widening forced, which discarded a stale read-ahead as a side effect and
rescued a probe that filled its buffer before the sibling's OPEN and read after its CLOSE.) (kb/Work PB753 — a
reader that had taken record 1 went on serving its snapshot after a sibling REWROTE record 3 and reported '00', so a
concurrent read/update pass computed from data the same run unit had already replaced, silently. The invalidation
rule was already written down in `SeekToRecord`'s doc comment — for the connector's OWN seek, never for a sibling's
write.)

**And ACROSS RUN UNITS: a keyed store another run unit may write is a COPY, kept coherent one statement at a time
(kb/Work PB2660).** §9.1.15 3) admits concurrent writers in other run units — *"The sharing with all other mode
allows concurrent access to a physical file through other file connectors specifying input, I-O, or extend mode"*,
and *"Multiple paths of access may exist … or runtime elements in different run units"* — and §9.1.16 binds them to
each other's record locks: *"While locked by a given file connector, a record is not accessible to another file
connector in the same or a different run unit, except by the execution of a READ statement with the IGNORING LOCK
phrase"*, answered by §9.1.13.8 1)'s `51`. The `KeyedStoreTable` store is ONE run unit's, so before PB2660 a second
run unit's `READ … WITH LOCK` of a record the first held answered `00`, its `REWRITE` and `WRITE` answered `00`, and
the first run unit's CLOSE then rewrote the file from the image it had read at its OPEN — measured on both keyed
organizations. Three pieces close it, and each lives in one place:

- **The store GENERATION (`RecordFraming.GenerationOffset`, store format 3; format 4 adds the release mint and each
  indexed record's release ordinals, kb/Work PB2693, so a reload keeps every duplicate order; a format-3 store is
  Foreign to this build and its OPEN answers '39', with no reader or migration kept, CLAUDE.md rule 4).** Eight bytes in the fixed header,
  stamped by every persist with one more than the generation the file carried at that moment. A run unit compares
  them with the generation its store reflects (`KeyedStore.Generation`) and reloads only when they differ, so a
  statement costs one small positional read, not a whole-store load. It is the cross-run-unit twin of
  `PhysicalFileTable.State.ReleaseGeneration` above, which can live in memory only because a sequential sibling is
  in the same run unit.
- **The coherent STATEMENT (`KeyedConnector.BeginStatement` → `StoreStatement`).** A connector whose file lock
  admits another writer (`FileLockPosture.AdmitsAnotherWriter` — only `ALL OTHER`) runs every record statement
  inside the store's cross-run-unit MUTEX (`HostRegionLocks.StoreMutexByte`): it enters (taking the mutex, waiting
  for another run unit's statement to finish, and reloading when the generation moved), the statement runs its
  record-lock check, its operation and its lock actions, and `Complete` persists what THIS statement changed
  before the mutex is given back. `FileRegistry` wraps every keyed record entry point in one, so to every other
  run unit a statement is one step — which is what §12.4.5.9.4 GR8's *"The setting of a record lock is part of the
  operation of an I-O statement"* needs, and why a check-then-act race between two run units cannot exist. A
  connector whose lock admits no other writer cannot meet one (Table 19 refuses that writer's OPEN in every run
  unit, PB833), so it keeps the whole-store model — loaded at the OPEN, persisted at the CLOSE, and the CLOSE now
  persists only when the store holds records the file does not (`KeyedStore.Version` against
  `PersistedVersion`). ⚠ The price of keeping the whole-store FORMAT is paid by the coherent connector alone: each
statement that changes the records persists the whole store, O(store) per mutating statement, so a bulk load
through an `ALL OTHER` connector is quadratic in the records — a record-level in-place format would remove it, and
is the next step if that cost is ever measured to matter. Every load and every persist, coherent or not, runs under
the mutex, so no run unit reads a
  store another is half-way through rewriting. The store's bytes are read and written POSITIONALLY through the
  connector's own handle (`KeyedConnector.Load`/`Persist`, `RandomAccess`), never through the `FileStream`'s
  buffer, which would hand back an image another run unit has replaced; `RecordFraming` only composes
  (`ComposeStore`) and decodes (`DecodeStore`) the image. A persist the medium refuses is the statement's own
  §9.1.13.6 item 1 `30`.
- **Published RECORD LOCKS (`PhysicalFileTable.LockRecord` / `HolderOf`, `HostRegionLocks.RecordLockByte`).** Every
  lock the run unit grants through a connector that has a `RecordLockHandle` (the keyed organizations) is also held
  as a byte-range lock on that handle, on one byte of the record region (SHA-256 of the record identity, 60 bits,
  far beyond any record), and every release gives it back. `HolderOf` answers the conflict question once:
  this run unit's table for its own connectors, the host for every other run unit — and says WHICH, because only a
  holder outside the run unit can release while a statement waits (`RetryAttempt.HolderOutsideRunUnit`,
  kb/Work PB1163). A `RETRY` against such a holder waits OUTSIDE the mutex (`FileConnector.WaitOutsideStatement`),
  since the holder can only release by executing a statement of its own, and re-enters before its next attempt;
  `RETRY FOREVER` against it is therefore not DOC-A.1-109's deadlock. Two distinct records share a byte with
  probability 2^-60, and the only consequence would be an over-cautious `51` across run units, never a missed one.

**The host primitive is `HostRegionLocks` (`IO/Sharing/`), and the byte map is its one table.** Windows
`LockFileEx`/`UnlockFileEx` (per handle, mandatory for the covered bytes, all of which lie at 2^62 or beyond); Linux
open-file-description locks through `OfdRegionLocks` (classic `fcntl` locks are per process). Linux refuses an
exclusive lock through a descriptor not open for writing, so an `OPEN INPUT` connector holds the mutex SHARED (a
reader never changes the store) and, on Linux only, publishes a record lock shared and then TESTS it, giving it back
if another handle holds the byte — publish then test, as `RunUnitFileLock` does. A Windows reader publishes its record
lock EXCLUSIVELY (`HostRegionLocks.ExclusiveNeedsWritableHandle`): `LockFileEx` takes an exclusive lock through a
read-only handle, and its locks are per handle, so a test through the handle that holds the byte shared meets its own
hold and answers held with nobody else holding it. The region map: `RegionBase` = 2^62; the five
Table 19 column bytes at +0..+4 (`RunUnitFileLock`); the store mutex at +0x100; the LOCK-PRESENCE byte at +0x101,
held shared by every handle that holds at least one published record lock, so a statement asks that one byte
(`PhysicalFileTable.AnotherRunUnitHoldsLocks`) before it pays for a record identity, a SHA-256 and a record byte; the
record region at 2^62 + 2^60 for 2^60 bytes. A host without byte-range locks (macOS) answers `Unavailable` for the store mutex, the
presence byte and every record byte, and each run unit keeps only its own record locks and store
(docs/CONFORMANCE.md DOC-A.1-75); the five Table 19 column bytes are the exception, which macOS carries as lock files
(`SidecarColumnLockHost`, kb/Work PB2484) because a column needs no byte-range lock, only a held file. `CrossRunUnitKeyedStoreDriftTests` measures
it with two `FileRegistry` instances (two run units sharing no table and no handle): `51` on `READ WITH LOCK` and
`REWRITE`, `IGNORING LOCK`, a sibling's `WRITE` visible at the next statement, every run unit's update surviving
every CLOSE, and a `RETRY` (n TIMES and FOREVER) that waits outside the mutex.

**The SEQUENTIAL organization publishes its record locks the same way (kb/Work PB2692).** Its `RecordLockHandle` is
the handle beneath its reader or writer (`HostFile.HandleOf`), and three facts make the published lock guard the
right record across run units. (1) A writer that registered a record-locking posture opens its handle READ-WRITE
(`SequentialConnector.HostAccess`): the presence byte is a SHARED hold, which Linux refuses through a write-only
descriptor, and a lock with no presence byte is invisible to every other run unit's hot-path test. (2) A sharing
writer's released record is NUMBERED OFF THE MEDIUM (`ReleaseRecord` → `PhysicalOrdinalOfRelease`): the record lands
where `SharedAppendStream.ReleaseStart` says, and its ordinal is one more than the records starting before that
offset, counted incrementally by the one framing-aware count (`CountRecords`). The in-run-unit mint
(`PhysicalFileTable.State.ReleasedOrdinal`) serves only a writer that is the file's only writer, where it agrees with
the medium. (3) A reader whose file lock admits another writer RE-ANCHORS AT EVERY FRAME
(`EnsureReaderCoherent`): the release generation is one run unit's memory, so another run unit's REWRITE would
otherwise be served from the read-ahead. `CrossRunUnitSequentialLockDriftTests` measures it for the fixed, line and
varying framings.

**Emptying a keyed store is done in ONE place: `KeyedStoreTable.AttachCreated` (kb/Work PB754).** An `OPEN OUTPUT`
creates the file (§14.9.27.4 GR18, *"After the creation of the file, the file contains no records"*), and emptying
a store is a mutation every attached connector sees instantly, so the creation attaches a NEW empty store and
refuses, loudly, to create one another connector of the run unit still holds — Table 19 has already refused that
OPEN (§9.1.13.9 1) e)), so reaching it is an arbitration defect. The absent-OPTIONAL creation arms (§14.9.27.4 GR17)
attach normally: the file was absent, so the store is empty by construction. No connector calls `Clear()` on a store
(`CrossRunUnitKeyedStoreDriftTests.NoConnectorClearsASharedStore`); the organizations' `Fill` — a load or reload,
which every attached connector must see — is the only other emptying.

The static `CobolFile` facade (kept for the emitted surface) becomes a pure delegator to `RunUnit.Current.Files`.
The `Keyed*` static methods at `IndexedFile.cs:570-707` are **deleted**; their callers in `CobolFile.cs` collapse to a
single `Files[name]` polymorphic call. `RecordFraming` (today `KeyedFrames`) becomes the ONE framing helper used by all
three (sequential varying framing folds into it — same 4-byte LE prefix). File **sharing/locking**
(`CobolFile.Locks.cs` + the `FileSharing`/`FileLockMode` enums) becomes `IO/Sharing/` — a `PhysicalFileTable` owned by
`RunUnit` (today the sharing registry is another static in the Locks partial). `CobolSort`, `ReportWriter`,
`AcceptSource` (renamed `IClock`/`SystemClock`) stay in IO.

### 2.3 Values — the value library (numeric + text + tables), role-grouped
```
Values/                         namespace Cobol.Net.Runtime.Values
  Numeric/  CobolNum, CobolDec, CobolFloat, CobolEdit, CobolRounding, CobolSizeError, NumProfile, Pow10 (NEW)
  Text/     CobolString, CobolBool, CobolClass
  Tables/   CobolTable, CobolDynTable
```
`Pow10` (NEW, `Values/Numeric/Pow10.cs`) — the ONE power-of-ten source:
```csharp
internal static class Pow10 {
    private static readonly long[]   L = BuildLong();   // 10^0..10^18
    private static readonly Int128[] W = BuildWide();   // 10^0..10^38
    private static readonly Int128[] F = BuildFive();   // 5^0..5^54 — 10^n's ODD COFACTOR (kb/Work PB623)
    public static long   AsLong(int n) => L[n];         // callers already bound n ≤ 18 / ≤ 38 by design
    public static Int128 AsWide(int n) => W[n];
    public static Int128 FiveAsWide(int n) => F[n];     // the exact binary64 expansion's multiplier
}
```
`CobolNum.Pow10/Pow10Wide`, `CobolDec.Pow10`, `CobolDate.Pow10`, `CobolIntrinsics.Pow10I/Pow10D` all delegate to it,
then are deleted. Pure win, no behavior change (identical values, table-driven).

⛔ **There is deliberately NO `double` view (kb/Work PB623, 2026-09-05).** The 10^n-as-double table existed for
exactly one purpose — scaling a binary64 before landing it in a fixed-point receiver — and that multiply was the
defect: past 2^53 the product is itself rounded, so the landing answered with a value the sender never held, and a
negative scale fell out of the table into a loop that returns 1 (a trailing-P receiver stored 10^|scale| too much).
`CobolFloat.TryExactScaled` now lands the EXACT expansion using 5^scale and a shift, which is why the five table
replaced the double one rather than joining it. A future caller wanting 10^n as a double is either converting a
scaled value — `CobolFloat.ScaledToDouble`, whose own exact-power table stops at 10^22 for the same reason — or
reintroducing PB623.

The numeric value types (`CobolNum` scaled long/Int128 kernel · `CobolDec` decimal128 · `CobolFloat` float/double ·
`CobolEdit` PIC editing) are **coherent as-is** — keep the type set and their internal design; only the folder path and
the `Pow10` dedup change. (The compiler-side "force everything through Int128 even when it fits in `long`" concern is a
*NumericRenderer/emitter* issue, out of scope for this dimension.)

### 2.4 Verbs — the statement runtime (moved out of the mislabeled `Strings/`)
```
Verbs/                          namespace Cobol.Net.Runtime.Verbs
  CobolStringOps  (STRING / UNSTRING)
  CobolInspect    (INSPECT)
```
These implement *statements*, not elementary values — role-grouped away from the `Values/Text/` value types they were
arbitrarily filed beside. (`CobolSort`/`ReportWriter` are verb-runtime too but stay in `IO/` because they are file/
report-connector-bound; noted, not moved.)

### 2.5 Control — run-unit + inter-program
```
Control/                        namespace Cobol.Net.Runtime.Control
  RunUnit (NEW) · ProgramTable (was ProgramRegistry) · ExternalStore · ModuleStack (was CobolModule)
  ManagedPointer / StorageCell / CellPointer · CobolArg / ICobolProgram / CobolArgAdapt
  CobolObject · CobolPtr · ExternalSwitches · CobolInvokeArg
  RuntimeAbi (the runtime version and call ABI a compiled module records and RegisterModule checks; kb/Work PB2097)
  Signals/  StopRun · ProgramReturn · MethodReturn · ResumeSignal · NotImplemented
Repository/                     namespace CobolNet.Runtime.Repository
  CobolRepositoryAttribute (a module's assembly record: schema, runtime version, call ABI, registrar) — the attribute
  family of the external repository's schema (DESIGN-external-repository §6) grows here
```
A module joins a run unit only through its `__CobolModule.EnsureRegistered()` → `ProgramTable.RegisterModule`, the one
writer of the run unit's registration set (DESIGN-external-repository §11.2). The runtime's public surface is recorded
in `PublicAPI.Shipped.txt` (`Microsoft.CodeAnalysis.PublicApiAnalyzers`): a new public member is listed in
`PublicAPI.Unshipped.txt` in the same change, and a removal or incompatible change raises the major version.
The standalone executable's generated `Main` registers its own module INSIDE the run-unit boundary: it calls
`ProgramRegistry.RunModule(__CobolModule.EnsureRegistered, mainPath)` (`ProgramTable.RunModule`, kb/Work PB2846), which
runs the registration and then the main program under the §14.6.12 abnormal-termination surface and the §14.6.11
epilogue, so a registration the run unit refuses (another runtime major or call ABI, or an already-registered
outermost name) ends the run unit through that surface and never escapes as an unhandled .NET exception. A host that
registers its modules itself enters through `ProgramRegistry.RunMain(path)`, the same boundary without the registration.
`ProgramRegistry` → **`ProgramTable`** (an instance owned by `RunUnit`; the name "Registry" is reserved for the
process-level nothing-here). The name-resolution / state-model / CANCEL / sibling-module-probe logic is ported
verbatim onto the instance. `ExternalStore` and `CobolModule`→`ModuleStack` move off statics onto `RunUnit`. The CALL-
ABI types (`ManagedPointer`, `CobolArg`, `ICobolProgram`, `CobolArgAdapt`, `StorageCell`) currently live *inside*
`ProgramRegistry.cs` (700 lines, one file) — split into their own files under `Control/`.

### 2.6 Exceptions — EC engine (instance-owned)
```
Exceptions/                     namespace Cobol.Net.Runtime.Exceptions
  ExceptionState (instance on RunUnit) · ExceptionCatalog (static, immutable table — stays static) · EcFunctions
  Signals moved to Control/Signals/ (CobolFatalException stays here — it is an EC condition carrier)
```
`ExceptionState` becomes an instance (all its `static … { get; private set; }` → instance members; the ambient gates
`ArgumentFunctionChecking`/`DataConversionChecking` become instance flags). `ExceptionCatalog` is an immutable lookup
table — legitimately static, kept. `EcFunctions` reads `RunUnit.Current.Exceptions`.

### 2.7 Intrinsics — keep the family split; only the `Pow10` dedup + clock change
`Intrinsics/CobolIntrinsics.{cs,Exact,Float,Text}` + `CobolDate` are cohesive (the deep-dive's runtime home). No
reorg beyond: `Pow10*` → `Values/Numeric/Pow10`; `CobolDate`'s `AcceptSource.Now` dependency → `RunUnit.Current.Clock`.

**The clock seam carries the offset (R21, 2026-08-08):** `IClock.Now()` returns **`DateTimeOffset`** — the local
wall-clock reading WITH the local time differential factor CURRENT-DATE renders at positions 17–21 (§15.21.3 r1)
and the §15.3.3 offset format fields emit. EVERY now-reading in the greenfield tree goes through the seam —
the ACCEPT temporal sources, CURRENT-DATE, FORMATTED-CURRENT-DATE, SECONDS-PAST-MIDNIGHT and the §15.100
windowing execution-time defaults — so one run unit observes one clock and `COBOLNET_CLOCK` pins all of them
at once (a pin may carry an explicit offset, `2026-06-10T14:30:45.67+02:30`; without one the machine-local
offset is assumed). The only direct `DateTime[Offset].Now` reads are `SystemClock`'s unpinned fallback and
`IntrinsicBinder.CompileClock`'s default (WHEN-COMPILED is the compilation timestamp, §15.99.3 r2);
`ClockSeamDriftTests` is the source-form guard that keeps it that way.

**THE §15.3 INTEGER-ARGUMENT INTAKE IS A CONTRACT WITH THE RENDERER, AND THE BODY'S PARAMETER TYPE IS ITS ONLY
STATEMENT (kb/Work PB22 + PB65 + PB254).** A §15 integer argument reaches a runtime body through exactly one of
two intakes, and which one is not a property of the renderer arm — it is a property of the ARGUMENT's own rule:

- **BOUNDED** — the function definition constrains the argument's VALUE (§15.5.2's integer date form for
  `INTEGER-OF-DATE`/`DAY-OF-INTEGER`/…, §15.23.3 r1's "less than 1 000 000", §15.36.3 r1's "greater than or
  equal to zero" together with a codomain `Int128` cannot hold). A value the body cannot represent then really
  "results in an incorrect value for that argument or for the returned value according to the rules specified in
  the function definition" (§15.3 rule 14), so the argument crosses `CobolIntrinsics.IntegerArg`, the ONE
  narrowing, which RAISES EC-ARGUMENT-FUNCTION instead of wrapping. **The body's parameter is `long`.**
- **TOTAL** — the definition constrains nothing but integer-ness, so the returned-value rule answers for every
  integer and §15.3 rule 14 has nothing to fire on. Putting the narrowing in front of such a body manufactures
  an exception condition the standard does not define: fatal under checking, and with checking off it
  substitutes 0 *for the argument*, which is how `TEST-DATE-YYYYMMDD(1.0E19)` once answered "the date is
  valid" (§15.90.4 r1d) and `FIND-STRING(… START AFTER 9999999999999999999)` once answered a match position
  where §15.37.4 r3 requires zero. **The body's parameter is `Int128`, and the renderer's intake is the wide
  one** (`IntrinsicRenderer.IntArgWide` / `ArgIntWide` → `AsIntWide`, which has no raise point). Members today:
  BOOLEAN-OF-INTEGER argument-1 (§15.13.3 r1), TEST-DATE-YYYYMMDD and TEST-DAY-YYYYDDD argument-1 (§15.90.3 r1
  / §15.91.3 r1), FIND-STRING argument-3 (§15.37.3 r3).

⚠ **The membership is not a maintained list anywhere.** Declaring the body's carrier IS the claim, and
`IntrinsicCarrierAgreementDriftTests.EveryTotalIntegerArgument_TakesTheWideIntake` re-derives the pairing by
reflection over the shipped signatures and fails when an arm and its body disagree — in either direction. A new
total function therefore needs one act, widening its body; forgetting its renderer arm is red, not silent.
A body's `…Real` twin owes the same totality, and owes it WITHOUT a second copy of the rule: it routes through
`TryTotalIntegerArg`, which saturates a magnitude past `Int128` (±∞ included) to the corresponding extreme —
verdict-preserving, because a catch-all arm depends on being outside the window and not on how far — so the
window itself stays written in exactly one body. It must NOT route to `TryIntegerArg`, whose false arm is a
literal verdict; that literal was §15.90.4 r1d, "the date is valid".
⚠ Since kb/Work PB248 a floating-point operand at a §15.3 type-6 position is REFUSED under strict
(COBOLNET1627 — type 6 admits "an integer data item or an always-integral arithmetic expression", and a
floating-point item is neither), so the `…Real` twins are reached only under `--permissive`. That is where
their totality is witnessed: `tests/Cobol.Net.Tests.Conformance/FloatIntegerArgumentPermissiveTests.cs`,
the one home for the coercion lane. The twins are still live code and still owe the rule — the lane
changed, the obligation did not.

⛔ **WHICH bodies owe a `…Real` twin is likewise derived, and FIND-STRING owes none (kb/Work PB635).** The
binary64 lane renders every argument through one `double` conversion, so it is enterable only for a call whose
whole §15.3 argument run is numeric (types 6 and 10) — `IntrinsicArgumentRules.ArgumentRunIsAllNumeric`, the
predicate `IntrinsicRenderer.RenderNum`'s float dispatch now tests. A function with a string, boolean or
reference position renders on its own arm whatever its operands are, so `FindStringReal`, `OrdReal`,
`TestNumvalReal` and `IntegerOfBooleanReal` must not be written; they were all four emitted, and all four failed
Roslyn with CS0117, while the dispatch asked only whether SOME argument was floating.
`IntrinsicRealArgDriftTests` scopes itself on that same predicate rather than on the old
"'s' and 'b' reject a float at bind time" reasoning, which held under strict conformance only —
`CheckArgumentClasses` warns and binds under `--permissive`, which is exactly the lane these bodies live in.

### 2.8 Namespace / assembly rename (G8)
Assembly is already `Cobol.Net.Runtime`. At G8, `RootNamespace` `CobolNet.Runtime` → **`Cobol.Net.Runtime`** and the
sub-namespaces above become real. This is a single coordinated change with the compiler's `RuntimeApi` façade (one
file emits the `using`s), so generated `using CobolNet.Runtime;` flips to `using Cobol.Net.Runtime.*;` in exactly one
place. Until G8 the root namespace stays `CobolNet.Runtime` and the reorg below is **folder-only** (namespaces
unchanged) to keep the emitted surface byte-stable.

---

## 3. Current → target module changes

| Action | From | To | Why |
|---|---|---|---|
| create | — | `Control/RunUnit.cs` | ONE owner of run-unit state; uniform `AsyncLocal` threading; single lifecycle boundary |
| create | — | `Values/Numeric/Pow10.cs` | ONE table-driven power-of-ten; kills 4 recompute-loop copies (efficiency MEDIUM) |
| create | — | `IO/FileConnector.cs` | shared §9.1.13 status machine + read-position state for all three organizations |
| create | — | `IO/FileRegistry.cs` | one polymorphic connector registry; deletes the `Keyed*` fallthrough dispatch |
| split | `Control/ProgramRegistry.cs` (700, holds registry + ManagedPointer + CobolArg + ICobolProgram + CobolArgAdapt + StorageCell + ExternalStore) | `Control/ProgramTable.cs`, `Control/ManagedPointer.cs`, `Control/CallAbi.cs` (CobolArg/ICobolProgram/CobolArgAdapt), `Control/StorageCell.cs`, `Control/ExternalStore.cs` | one concern per file; the ABI types are not "registry" |
| rename+refactor | `ProgramRegistry` (static) | `ProgramTable` (instance on `RunUnit`) | state ownership; enables concurrent run units; a static shim keeps the emitted surface |
| rename+refactor | `Exceptions/ExceptionState` (static) | `ExceptionState` (instance on `RunUnit`) | state ownership; ambient EC gates become run-unit-scoped |
| rename+refactor | `Control/CobolModule` (`[ThreadStatic]`) | `Control/ModuleStack` (instance on `RunUnit`) | remove the lone `[ThreadStatic]`; uniform threading |
| move+refactor | `ExternalStore` (static, in ProgramRegistry.cs) | `Control/ExternalStore.cs` (instance on `RunUnit`) | state ownership; own file |
| rename | `SequentialFile`/`RelativeFile`/`IndexedFile` | `SequentialConnector`/`RelativeConnector`/`IndexedConnector` (: `FileConnector`) | dedup the status/position machinery into the base |
| delete | `IndexedFile.cs:570-707` `Keyed*` static methods; `CobolFile` sequential-first/keyed-fallthrough | (polymorphic `FileRegistry` dispatch) | remove the second dispatch mechanism (singular pattern) |
| rename+move | `IO/AcceptSource` `static Func<DateTime> Now` | `IO/Clock.cs` `IClock`/`SystemClock`, on `RunUnit.Clock` | remove process-global test seam; injectable per run unit |
| move | `IO/CobolFile.Locks.cs` sharing registry (static) | `IO/Sharing/PhysicalFileTable.cs` (on `RunUnit`) | state ownership; cohesive sharing subsystem |
| move | `Strings/CobolStringOps.cs`, `Strings/CobolInspect.cs` | `Verbs/` | role-based: statement runtime ≠ value types; delete empty `Strings/` |
| move | `Numeric/*`, `Text/*`, `Tables/*` | `Values/Numeric/`, `Values/Text/`, `Values/Tables/` | one value-library home (folder-only pre-G8) |
| move | `FileStatusCode` (in `FileSupport.cs`) | `IO/FileStatus.cs` | consumed by all connectors; own file next to the base |
| merge | 4× `Pow10`/`Pow10Wide`/`Pow10I`/`Pow10D` | delegate to `Values/Numeric/Pow10` | one source of truth |
| merge | `KeyedFrames` (relative/indexed) + sequential varying framing | `IO/RecordFraming.cs` | one framing helper across organizations |
| move | `StopRun`, `MethodReturn`, `ProgramReturn`, `ResumeSignal`, `NotImplemented` | `Control/Signals/` | cohesive control-signal group |
| rename (G8) | RootNamespace `CobolNet.Runtime` | `Cobol.Net.Runtime` (+ real sub-namespaces) | assembly/namespace coherence; one coordinated flip with the emitter `RuntimeApi` façade |
| keep | `CobolNum`/`CobolDec`/`CobolFloat`/`CobolEdit`/`NumProfile`, `ExceptionCatalog`, `CobolIntrinsics.*`, `CobolDate` (type designs) | — | already coherent; only path/`Pow10`/clock touched |

---

## 4. Migration (keeping the 3256 conformance + 281 unit + NIST-353 battery green) — steps 1–5 EXECUTED (P8); step 6 = G8

Order the work so each step is independently green and the emitted-code surface is byte-stable until G8.
Steps 1–5 below are DONE (PHASE-08; battery green at every commit; zero compiler-side changes, so the emitted
C# is byte-identical by construction). Step 6 remains for G8. Additions beyond the plan, per the §5 hidden-
static gate: `ExternalSwitches` → instance `SwitchStore` on `RunUnit` (+ static shim) — switch scope is the run
unit (§12.3.7 GR4 NOTE 1); the only statics left are genuinely immutable (`ExceptionCatalog`, `Pow10`,
`SystemClock.Instance`). Naming as landed: the instance types are `ProgramTable`, `ExceptionEngine`,
`ExternalTable`, `ModuleStack`, `SwitchStore`, `FileRegistry` (+ `PhysicalFileTable` under `IO/Sharing/`);
every pre-P8 static name survives as the emitted-surface shim.

1. **`Pow10` dedup (safe, isolated).** Add `Values/Numeric/Pow10.cs`; repoint the 4 copies; delete them. Pure value
   identity — run the numeric unit tests + full conformance. No emitted-surface change.
2. **Folder moves, namespaces unchanged.** Physically move files into `Values/`, `Verbs/`, split
   `ProgramRegistry.cs` into per-type files, group `Control/Signals/`. Namespaces stay `CobolNet.Runtime[.X]` exactly
   as today, so nothing the compiler emits changes. Green by construction (rename/move only).
3. **`FileConnector` base extraction.** Introduce the abstract base + `FileRegistry`; make
   `Sequential/Relative/Indexed` derive from it, hoisting ONLY the provably-identical status/position/mode/host-path
   members. Keep organization-specific rules in overrides. Replace the `Keyed*` fallthrough in `CobolFile` with a
   polymorphic `Files[name]` call; delete the `Keyed*` shims. Guard: the RL/IX/SQ/relative NIST goldens + file unit
   tests are the exact regression net — run after each connector is migrated (one organization at a time).
4. **Introduce `RunUnit`; keep static facades as shims.** Create `RunUnit` owning instance `ProgramTable` /
   `ExceptionState` / `ExternalStore` / `ModuleStack` / `FileRegistry` / `Clock`. Convert the five static classes to
   thin delegators over `RunUnit.Current`. The generated `Main` wrapper's run-unit entry (today
   `ProgramRegistry.Reset(); … ; CobolFile.CloseAll()`) is replaced by `RunUnit.Run(ru => { … })` — a *one-line
   emitter change* in the run-unit driver, or (to defer any emitter change) have the first `ProgramRegistry.Reset()`
   lazily establish an ambient `RunUnit` and `CloseAll()` tear it down. Verify: the exact `Reset()` semantics
   (clears programs + external + module stack + files) must be reproduced by `RunUnit.Run`'s begin/end — assert with
   an inter-program (CALL/CANCEL/EXTERNAL) golden subset first, then full battery.
5. **Clock injection.** Replace `AcceptSource.Now` with `RunUnit.Clock`; the test seam that set `AcceptSource.Now`
   sets `ru.Clock` instead (update the ~handful of date/time conformance fixtures' harness hook). Green when the
   date/time goldens pass.
6. **G8 namespace flip.** Once the compiler routes runtime references through a single `RuntimeApi` façade, flip
   `RootNamespace` to `Cobol.Net.Runtime` and realize the sub-namespaces; update the façade's emitted `using`s in one
   place. Full battery + a from-clean regen. Decide static-shim retirement here (OPEN QUESTION).

Each step is a commit with its own DEVLOG entry and a guard-fast/greenfield-suite run (per the process rules).

---

## 5. Risks

- **`static → RunUnit` ambient is a broad change.** Mitigated by the delegating-shim strategy (emitted surface
  unchanged) and by reproducing `ProgramRegistry.Reset()` semantics *exactly* in `RunUnit.Run`. The
  inter-program/EXTERNAL goldens (IC-series, IC227A EXTERNAL-connector persistence) are the sharp regression edge.
- **`AsyncLocal` per-access cost.** Small but nonzero; hot facades (`CobolFile.*`, `ExceptionState` raise sites) cache
  `RunUnit.Current` in a local. Measure against the 20M-iteration arithmetic-loop benchmark used in the efficiency
  critique — the numeric hot path does not touch `RunUnit`, so the risk is confined to I/O/CALL/raise sites.
- **`FileConnector` base could paper over an intentional per-organization divergence.** The base must be the *true*
  common denominator; anything an organization tunes independently (e.g. sequential REWRITE-requires-prior-READ 43 vs
  indexed key rules) stays an override. Migrate one organization at a time behind its NIST goldens; do not "unify"
  a status transition that the three do differently.
- **Concurrent in-process run units expose latent shared statics** not yet moved (`ExceptionCatalog` is immutable so
  safe; verify no other hidden mutable static remains — a grep gate in CI). Until every run-unit static is on
  `RunUnit`, concurrency stays opt-in/off.
- **Namespace flip breaks generated code if not funnelled.** Strictly gated on the `RuntimeApi` façade existing;
  otherwise it is a corpus-wide emitted-`using` churn. Keep it as the last (G8) step.

---

## 6. Formerly-open questions — RESOLVED (as executed by PHASE-08)

1. **Concurrent in-process run units:** adopted for state-ownership HYGIENE; "one run unit per process" stays
   the supported contract. The `RunUnit`/`AsyncLocal` design enables concurrency later (the harness-throughput
   win), but it is opt-in/untested until every consumer is audited — no capability claim is made.
2. **Ambient mechanism:** `AsyncLocal<RunUnit>` (the recommendation), with a LAZY `Current` so the unchanged
   emitted driver (`ProgramRegistry.Reset(); …`) establishes the ambient unit implicitly. Threading `RunUnit`
   through the `ICobolProgram` ABI was rejected — it would change every generated entry point pre-G8.
3. **Static-facade lifetime — OWNER-RATIFIED (2026-07-16):** kept pre-G8 as the emitted surface (the
   byte-stability proof discipline — NOT back-compat; nothing has shipped); the facades RETIRE at **G8 Cut 3**
   together with the namespace flip, which already forces an emitted-surface change (one re-baseline instead of
   two). G8 design input recorded with the decision: the replacement must resolve the AsyncLocal-read cost —
   the generated run-unit driver should CAPTURE the run unit once (`var ru = RunUnit.Current;`) and route
   statement-level calls through the captured local (or an equivalent cached path), never a per-statement
   `RunUnit.Current` read on hot paths.
4. **Namespace granularity:** deferred to G8 with the flip itself (§2.8's sub-namespaced layout remains the
   plan of record; the folders already mirror it).
5. **`Values/` nesting:** ACCEPTED — `Values/{Numeric,Text,Tables}/` landed.
6. **Sequential connector split:** KEPT UNIFIED (line- + record-sequential + LINAGE in one
   `SequentialConnector`) — the shared position/framing state makes a split net-negative.
