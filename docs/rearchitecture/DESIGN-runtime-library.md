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
`ExceptionState.Set(...)`, `CobolFile.Open(...)`, `CobolModule.Name(...)`) keeps compiling unchanged:
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
    // The guards are PURE. A READ or START entry never assigns Status itself: a failure returns through
    // Unsuccessful, which applies §14.9.30.4 GR18 / §14.9.41.4 GR7 through the virtual InvalidateFilePosition
    // (kb/Work PB359); a success through ReadSucceeded or StartPositioned.
    protected string? ReadOpenModeGuard();
    protected string? SequentialReadGuard();
    protected string? RandomReadAbsentOptionalGuard();
    protected string? StartOpenModeGuard();
    protected string Unsuccessful(string status);             // InvalidateFilePosition, then the status
    protected string ReadSucceeded(string status);            // re-arms the '43' gate, clears the '46' poison
    protected string StartPositioned();                       // '00', clears the '46' poison
    protected virtual void InvalidateFilePosition();          // keyed connectors also clear their FPI validity bit
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
  which is kb/Work PB713's defect re-opened by its own cure. The handle's ACCESS is `FileConnector.HostAccess`
  (every organization opens its handle, `FileConnector.FileLock`, with it and spells none of its own; kb/Work PB2758),
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
- **The lock-file primitive (`SidecarLockFiles`) and its column policy (`SidecarColumnLockHost`).** macOS has no host
  primitive for the Linux bytes: its classic `fcntl` locks are process-owned, its open-file-description commands are
  private, and `fcntl` is variadic, which Apple's arm64 calling convention passes differently from a fixed P/Invoke
  signature — so the class makes NO native call. A holder's lock is a FILE — `.wolk-<16 hex digits>/<column>-<guid>`
  in a directory BESIDE the physical file (the digits: SHA-256 of the file's name under `HostFile.PhysicalFileKey`,
  the case rule that `HostFile.PhysicalFileComparer` also reads) — held open through `HostFile.OpenLockHolder` (an
  EXCLUSIVE `flock`: `FileShare.None` on Unix, `FileShare.Delete` alone on Windows, where share modes are mandatory
  and the open file must still be renamed) and tested by `HostFile.ProbeLockFile` (a read open under a share mode
  that admits every other reader and writer — a SHARED `flock` — refused only while a holder lives; the kernel drops
  the lock with the descriptor, so a killed run unit holds nothing). ⛔ HOLDERS EXCLUSIVE, PROBES SHARED, so two
  testers sweeping one dead file never read each other as a holder (kb/Work PB2917: exclusive probes made the second
  tester's OPEN '61' for a connector that did not exist). The probe answers four states — `Live` (the sharing
  refusal), `Dead` (opened freely: the caller retires it, below), `Gone`, and `Unobservable` (the process may not open it,
  or it is not a file): an unobservable holder is neither free nor swept, and a request row that refuses its column
  is refused. A lock file PER HOLDER and not per column, because a request row can refuse its own column and a
  `flock` cannot say "anybody but me" about a file the asker holds: the tester skips only its own file. A holder
  creates and locks `pending-<guid>`, verifies the lock is in force (a host that does not refuse the probe — a
  network file system, `DOTNET_SYSTEM_IO_DISABLEFILELOCKING` — is `Unavailable`; a pending file a tester reaped in the
  meantime is retried, not `Unavailable`) and RENAMES it to its final name, so the testers, which read only final
  names, never see a half-published holder; a file they can open freely belongs to a dead run unit and is deleted. The
  CLOSE takes the file's name BEFORE closing the descriptor (a `fork` for `CALL "SYSTEM"` holds a copy of the description
  until its `exec`; a name nobody can see cannot refuse a successor) and the directory with its last file. ⛔ NO NAME A PROBER READS IS DELETED IN PLACE (`SidecarLockFiles.Retire`, train 1056b): where share modes are mandatory a file deleted while another handle has it open stays listed, delete-pending, and every open of it is "access denied", the unobservable holder's answer, which no bound on asking again tells apart; so a lock file there is renamed to `gone-<guid>` (a wire value no tester probes) before it is deleted, and "access denied" is always an answer. ⛔ THE SAME HOLDS OF THE LOCK DIRECTORY (`SidecarLockFiles.RetireDirectory`, kb/Work PB3109): the last holder's CLOSE removes the empty directory, and where share modes are mandatory the directory passes through delete-pending under its own name for as long as any other handle on it is open, so a publisher's `CreateNew` inside it and a tester's listing of it were "access denied" — read as a directory this user may not write, a waiting region hold answered `Unavailable` and the store mutex went untaken (11 of 3,000 two-endpoint rounds under CPU load). So there the empty directory is renamed to `.wolk-gone-<guid>` first and only that name is deleted; a directory with any entry, or one the host will not move while a holder's file is open, stays standing, and a publisher that resolved the old name finds it gone and publishes again (the residue of a kill between the rename and the delete is one empty `.wolk-gone-` directory). A directory
  the process may not write is `Unavailable`, never an exception — except that a user who may not WRITE the lock
  directory still LOOKS: a live or unobservable conflicting holder refuses it. A user who may enter the lock directory but not LIST it cannot look: every holder is unobservable to it, so a request row that refuses any column is refused (the train 1056b review: the other user of a `0711` parent, whose mode the lock directory copies). ⛔ THE CROSS-USER MODEL: the lock
  directory is created with the permissions of the physical file's directory (`HostFile.CreateLockDirectory`) and a
  holder's file takes the directory's read and write bits and always its owner's own (`HostFile.OpenLockHolder`: the holder probes its own file before publishing it), not the first holder's umask,
  so a second user of a group-writable shared directory can publish into it and read the first user's files (macOS
  gives a new directory the group of its parent). The determination is `docs/CONFORMANCE.md` DOC-A.1-75.
- **The region locks on the same primitive (`SidecarRegionLocks`, kb/Work PB2936).** The record locks and the store
  mutex of a shared keyed file (below) are held through a `RegionLocks` endpoint made by the host of the process
  (`IRegionLockHost`, `HostRegionLocks.HostOfThisProcess`): `HandleRegionLocks` where the lock belongs to the
  connector's own handle (Windows `LockFileEx`, Linux OFD), `SidecarRegionLocks` everywhere else. A held byte is a
  lock file in the SAME lock directory, `r<byte as 16 hex digits>-<S|X>-<guid>` (⛔ a wire value), published and
  probed exactly as a column file is (`SidecarLockFiles.Publish`, `Observe`): an exclusive hold is refused by any
  other live holder of the byte, a shared one by an exclusive holder, the asker skips only its own file, a waiting
  hold retries after a jittered pause, and an unobservable holder (or a lock directory the asker may enter but not list) is `Held` to a non-waiting hold and `Unavailable`
  to a waiting one (it cannot wait for a release it cannot see). Classic `fcntl` record locks cannot stand in: they
  belong to the process, and any second connector's CLOSE in the same run unit would drop the first's. A witness can
  run any connector on the lock-file host on any machine (`HostRegionLocks.Forcing`), so `RegionLocksDriftTests` and
  the cross-run-unit scenarios hold macOS's expression to the same contract as Windows's and Linux's.
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
  `SidecarColumnLockHost` (kb/Work PB2484, the bullet "The lock-file primitive" above), so every WiseOwl COBOL run unit
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
through an `ALL OTHER` connector is quadratic in the records. ⛔ The owner ruled this whole-store model unacceptable
(2026-10-17). kb/Work PB3330 replaces it with the record-level, page-granular engine designed in §2.2.1 below (one paged
file, B+ trees, a generation ledger for crash consistency); until its groups PB3331-PB3338 land, this paragraph and the
two bullets around it describe the current compiler, and §2.2.1 the target. Every load and every persist, coherent or not, runs under
the mutex, so no run unit reads a
  store another is half-way through rewriting. The store's bytes are read and written POSITIONALLY through the
  connector's own handle (`KeyedConnector.Load`/`Persist`, `RandomAccess`), never through the `FileStream`'s
  buffer, which would hand back an image another run unit has replaced; `RecordFraming` only composes
  (`ComposeStore`) and decodes (`DecodeStore`) the image. A persist the medium refuses is the statement's own
  §9.1.13.6 item 1 `30`.
- **Published RECORD LOCKS (`PhysicalFileTable.LockRecord` / `HolderOf`, `HostRegionLocks.RecordLockByte`).** Every
  lock the run unit grants through a connector that has `RecordLocks` (the keyed organizations) is also held
  as a region lock on that endpoint, on one byte of the record region (SHA-256 of the record identity, 60 bits,
  far beyond any record), and every release gives it back. `HolderOf` answers the conflict question once:
  this run unit's table for its own connectors, the host for every other run unit — and says WHICH, because only a
  holder outside the run unit can release while a statement waits (`RetryAttempt.HolderOutsideRunUnit`,
  kb/Work PB1163). A `RETRY` against such a holder waits OUTSIDE the mutex (`FileConnector.WaitOutsideStatement`),
  since the holder can only release by executing a statement of its own, and re-enters before its next attempt;
  `RETRY FOREVER` against it is therefore not DOC-A.1-109's deadlock. Two distinct records share a byte with
  probability 2^-60, and the only consequence would be an over-cautious `51` across run units, never a missed one.

**The host primitive is the `RegionLocks` endpoint (`IO/Sharing/`), and the byte map in `HostRegionLocks` is its one
table.** A connector holds one endpoint per handle, made by the host of the process (`IRegionLockHost`,
`HostRegionLocks.HostOfThisProcess`, chosen once): `HandleRegionLocks` — Windows `LockFileEx`/`UnlockFileEx` (per
handle, mandatory for the covered bytes, all of which lie at 2^62 or beyond) and Linux open-file-description locks
through `OfdRegionLocks` (classic `fcntl` locks are per process) — or `SidecarRegionLocks` (a lock file per held byte,
the bullet "The region locks on the same primitive" above) on every other host. Linux refuses an
exclusive lock through a descriptor not open for writing, so an `OPEN INPUT` connector holds the mutex SHARED (a
reader never changes the store) and, on Linux only, publishes a record lock shared and then TESTS it, giving it back
if another handle holds the byte — publish then test, as `RunUnitFileLock` does. A Windows reader publishes its record
lock EXCLUSIVELY (`RegionLocks.ExclusiveNeedsWritableHandle`): `LockFileEx` takes an exclusive lock through a
read-only handle, and its locks are per handle, so a test through the handle that holds the byte shared meets its own
hold and answers held with nobody else holding it; a lock file has no such limit and holds exclusively whatever the
connector's access. The region map: `RegionBase` = 2^62; the five
Table 19 column bytes at +0..+4 (`RunUnitFileLock`); the store mutex at +0x100; the LOCK-PRESENCE byte at +0x101,
held shared by every handle that holds at least one published record lock, so a statement asks that one byte
(`PhysicalFileTable.AnotherRunUnitHoldsLocks`) before it pays for a record identity, a SHA-256 and a record byte; the
record region at 2^62 + 2^60 for 2^60 bytes. On macOS the same bytes are lock files (`SidecarRegionLocks`, kb/Work
PB2936; the five Table 19 column bytes likewise, `SidecarColumnLockHost`, kb/Work PB2484): a byte offset cannot be
locked there without a native call whose ABI differs on Apple's arm64, but a held file can be named for the byte, the
mode and the holder. A host that cannot hold the lock at all (a directory the process may not write, a network file
system without `flock`) answers `Unavailable` and the run unit keeps only its own record locks and store
(docs/CONFORMANCE.md DOC-A.1-75). `CrossRunUnitKeyedStoreDriftTests` and `CrossRunUnitSequentialLockDriftTests`
measure it with two `FileRegistry` instances (two run units sharing no table and no handle) on the host of the machine
AND on the lock-file host (`RegionHostKind`): `51` on `READ WITH LOCK` and
`REWRITE`, `IGNORING LOCK`, a sibling's `WRITE` visible at the next statement, every run unit's update surviving
every CLOSE, and a `RETRY` (n TIMES and FOREVER) that waits outside the mutex.

**The record-lock CEILING is the last check of a WRITE or REWRITE (kb/Work PB2848).** §12.4.5.9.4 GR7's `54` / `53`
judge the lock a statement requests, and that lock is the statement's effect on success (§14.9.51.4 GR11, §14.9.35.4
GR12), so the registry no longer asks the ceiling before calling the organization. It hands the connector a
`FileConnector.CommitGate` for the statement (`FileRegistry.LockCeilingRefusal`, only when the statement requests a NEW
lock) and every organization's WRITE and REWRITE asks it once (`RefusedAtCommit()`), after its own rules and before it
first changes the file or its store: the sequential record, line and print arms, `RelativeConnector.Write`/`Replace`,
`IndexedConnector.Write`/`Rewrite`. A new organization or arm inherits the order by calling it at its commit point;
`CommitGateDriftTests` measures every one with a gate that always refuses.

**The SEQUENTIAL organization publishes its record locks the same way (kb/Work PB2692).** Its `RecordLocks` endpoint is made for
the handle beneath its reader or writer (`FileConnector.FileLock`, read through `HostFile.FileOf`), writable exactly
when the host granted that handle write access, and three facts make the published lock guard the
right record across run units. (1) A writer that registered a record-locking posture opens its handle READ-WRITE
(`SequentialConnector.HostAccess`): the presence byte is a SHARED hold, which Linux refuses through a write-only
descriptor, and a lock with no presence byte is invisible to every other run unit's hot-path test. The read half is
a PREFERENCE for OUTPUT and a NEED for EXTEND (kb/Work PB2848): OUTPUT creates the file and has no record to read,
so a host that refuses the read (a write-only file) still grants the write, `OpenWriter` falls back to the mode's
write-only handle, and that connector numbers its records by the in-run-unit mint (its `FileLock` cannot read) and
publishes no presence; EXTEND numbers the records already in the file and answers `37` without the read. (2) A sharing
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

#### 2.2.1 The keyed store engine — a record-level, crash-consistent, page-granular format for RELATIVE and INDEXED files (kb/Work PB3330)

⛔ **STATUS.** This section is the DESIGN the owner ordered on 2026-10-17 ("THis is a completely unacceptable design", of the
whole-store model the paragraphs above still describe). It is the target state; the current compiler is the whole-store model
until the implementation groups kb/Work PB3331 to PB3338 land, each of which rewrites the paragraphs above that it retires and
the two `docs/CONFORMANCE.md` rows it changes (DOC-A.1-107, DOC-A.1-75) in the same change set (CLAUDE.md rule 6). PB3330 lands
when its last group has landed. The bar it meets is PB3330's, line by line: no statement reads or writes the whole file; OPEN
reads the header; a keyed statement touches O(log n) pages; the size limit is the host's; a statement's change reaches the
file when it completes, crash-consistently; `SHARING WITH ALL OTHER` is coherent at page granularity with today's record locks;
every current golden keeps passing plus measured performance witnesses; one engine, no second path and no reader of the old
format; and APPLY COMMIT / COMMIT / ROLLBACK are built on it.

**2.2.1.1 What the surveyed engines do (sources, read 2026-10-17; copied nothing).**

| Engine | Storage shape | Duplicates | Crash model | Source |
|---|---|---|---|---|
| GnuCOBOL 3.2 RELATIVE | one fixed slot per record number: `relsize = record_max + sizeof(size)`, `off = relnum * relsize`, read/written in place with `lseek`; an empty slot is a zero length word (`relative_read` 3048-3092, `relative_write` 3199-3260, `relative_delete` 3300-3334 of `libcob/fileio.c`) | n/a | in-place, no log | the extracted tarball at `scratchpad/gnucobol-src/gnucobol-3.2/libcob/fileio.c` |
| GnuCOBOL 3.2 INDEXED | one Berkeley DB `DB_BTREE` per key (`indexed_open`, 4612-4625); the environment, when `DB_HOME` is set, is `DB_CREATE \| DB_INIT_MPOOL \| DB_INIT_CDB` (3527) — a page cache and concurrent-data-store locking, no `DB_INIT_TXN`/`DB_INIT_LOG` | `DB_DUP` on a key declared WITH DUPLICATES (4612) | Berkeley DB's: none without a transactional environment | same file |
| Berkeley DB Btree | pages of 512 B to 64 KiB, a power of two, defaulting to the filesystem block size capped at 16 KiB; items over `page_size / (minkeys × 2)` go to overflow pages; the finest lock is the page | unsorted duplicates "in the order in which they were added"; `DB_DUPSORT` orders them by a comparator | write-ahead log, checkpoints that write dirty pages so older logs can go, recovery that undoes unfinished and redoes committed transactions; `DB_TXN_WRITE_NOSYNC` keeps ACI and loses only transactions since the last OS flush on a SYSTEM failure | docs.oracle.com/database/bdb181/html/programmer_reference/{general_am_conf,bt_conf,transapp_checkpoint,transapp_recovery}.html, api_reference/C/envset_flags.html |
| IBM VSAM KSDS / RRDS | control intervals (the unit of transfer; e.g. 4 KiB) grouped in control areas; a CI holds records from the low end, a CIDF and one RDF per record from the high end, free space contiguous; an insert that does not fit splits the CI (half the records move to a free CI of the CA), a CA without a free CI splits; a KSDS is a B+ tree (sequence set over the CAs, index set above); an RRDS is a string of fixed-length slots addressed as `slot size × RRN`, an insert into an occupied slot is an error, an RRN never changes; an alternate index is itself a KSDS whose record maps one alternate key to one or more prime keys, maintained by the base only when it is in the upgrade set | non-unique alternate keys: one AIX record, several prime keys | not COBOL's: VSAM RLS takes record-level SHARE (CR) or no (NRI, "dirty read") locks | en.wikipedia.org/wiki/Virtual_Storage_Access_Method; www.ibm.com/docs/SSGMCP_5.5.0/applications/designing/dfhp3mk.html; www.ibm.com/docs/SSLTBW_2.1.0/com.ibm.zos.v2r1.idad400/d4340.htm; www.ibm.com/support/knowledgecenter/SSLTBW_2.1.0/com.ibm.zos.v2r1.idad400/mod9.htm; support.sas.com/documentation/cdl/en/vsamref/61644/HTML/default/a001273101.htm |
| Micro Focus | IDXFORMAT 1 C-ISAM, 2 Level II, 3 (default), 4 (optimized for duplicate keys), 8 (large files), 12 (a separate `.idx` side file holding the key information, otherwise as 8); `rebuild -n` reports the format, `rebuild /q` rebuilds from the `.idx` file | per format | a crashed file is "corrupt" and is REBUILT by the utility | community.rocketsoftware.com/rocket-net-express-server-express-30/how-does-rebuild-report-various-idxformat-settings-19451; the IDXFORMAT and Rebuild pages as summarized by the search engine (the hosts `10252761.microfocus.com` and `cabs.microfocus.com` no longer serve them) |
| ACUCOBOL-GT Vision | block size 512 to 8192 bytes in 512-byte steps (versions 2-4 at most 1024), BLOCK CONTAINS sets it; version 3 keeps keys and data in one file capped at 2 GB; versions 4 and 5 split data and keys into segments of up to 2 GB each; version 6 uses 64-bit offsets | per version | a damaged file answers status 98 and is repaired by `vutil -rebuild` (per key, `-k n`) | community.rocketsoftware.com/rocket-acucobol-29/vision-filesystem-how-do-i-fix-a-corrupt-file-22582; the "Indexed Files - Vision" and "Other Vision Features" pages as summarized by the search engine (every microfocus.com host redirects to an opentext.com 404) |
| Informix C-ISAM | a `.dat` and an `.idx` file "always used together as a single, logical ISAM file"; B+ tree indexes with compressed keys; duplicate and unique keys; transaction management, logging and recovery; a `bcheck` index-integrity utility; 2 GB file limit before 7.25 | yes | a transaction log, replayed by recovery | public.dhe.ibm.com/software/data/informix/pubs/pdfs/cisam.pdf (the technical brief; text extracted locally) |

Three facts of the survey decide the shape below. Every production engine is PAGED (VSAM's CI, Berkeley DB's page, Vision's
block, C-ISAM's node), indexes keys in a B+ tree whose leaves carry the records or point at them, and keeps a relative file as
slots addressed by record number. Every engine that survives a crash without a rebuild utility does it with either a log
(Berkeley DB, C-ISAM) or by never overwriting a page a committed state still references; the ones that overwrite in place
without a log (GnuCOBOL's RELATIVE, Vision, Micro Focus) ship a rebuild utility instead. And duplicate alternate keys are kept
in release order by the ENGINE (`DB_DUP` insertion order; an AIX record's list), never recomputed from a record list — the
per-key ordinal vector kb/Work PB341 and PB2693 established is the same fact, and it stays.

**2.2.1.2 What the host costs (measured 2026-10-17, `scratchpad/proto-pb3330/Program.cs`, Windows 11 26300, .NET 10.0.13, NTFS,
the repository's drive and the temp drive; the numbers decide the fsync policy and the page size).**

| Operation (4 KiB pages) | Per operation |
|---|---|
| positional write handed to the OS, no fsync | 7.4-8.4 µs |
| positional read, random page of a 256 MiB file (mostly OS-cached) | 11.8-15.2 µs |
| positional read, hot page | 1.4-1.8 µs |
| write + `FlushFileBuffers` (`FileStream.Flush(true)`) | 2.9-3.1 ms |
| appends with one fsync per 10 / per 100 statements | 0.28-0.32 / 0.025-0.027 ms per statement |
| the current model: whole-store persist of 64 MiB through the areas (every changing statement under ALL OTHER) | 77-92 ms |
| the current model: whole-store load of 64 MiB (every OPEN; every reload) | 65-68 ms |

An fsync is three hundred times a page write. A design that makes every statement durable against POWER LOSS costs 3 ms per
WRITE (a million-record load: 50 minutes); one that makes every statement durable against PROCESS DEATH costs one page write
per page the statement changed (the same load: well under a minute). The standard asks for the first only at a COMMIT —
§9.1.18.3: *"The COMMIT statement is used to permanently commit any changes made to the files subject to APPLY COMMIT clauses
at that point, so that they will remain in that state even in the event of subsequent abnormal termination."* — and PB3330's
bar asks for the second of every statement ("a process that dies leaves the file in the state after its last completed
statement, and never a torn one"). So the engine hands every statement's pages to the operating environment before the
statement completes, and SYNCS at the points the standard and the file's integrity need: COMMIT, CLOSE, run-unit termination,
and a bounded checkpoint (2.2.1.6). Power loss between two sync points loses at most the statements since the last one and
never tears the file, which is Berkeley DB's `DB_TXN_WRITE_NOSYNC` contract ("transactions exhibit the ACI ... properties, but
not D") made explicit in the format rather than in a flag.

**2.2.1.3 The decisions, in one place.**

1. **ONE paged file, no sidecar, one engine.** The data file IS the store (owner 2026-09-07, kb/Work PB802: "No sidecar of any
   type"). It is a sequence of 4 KiB pages; page 0 and page 1 are the two META pages; every other page is a B+ tree page, an
   overflow page or a free page. RELATIVE and INDEXED are the same engine with different KEY ORDERS and a different number of
   trees; every sharing mode runs the same statement path. There is no small-file path, no in-memory path and no reader of
   store format 4: a format-4 file is `StoreFormat.Foreign` and its OPEN answers '39' exactly as a format-3 file does today
   (CLAUDE.md rule 4; the version byte after the magic moves from 4 to 5, so the existing `Foreign` arm already answers it).
2. **The page size is 4 096 bytes**, the host's I/O block (Berkeley DB's default rule; `HostFile.InputOutputAreaBytes` is
   already 4 096) and the size the measurements were taken at. It is a format constant, not a per-file choice: one size makes
   every page computation static, and a record larger than a page's payload goes to overflow pages (2.2.1.4), so no record
   size needs a bigger page. Page numbers are 32-bit, so the format addresses 2^32 × 4 KiB = 16 TiB (2.2.1.9).
3. **Records live in B+ trees.** INDEXED: one PRIME tree (prime key → the record's frame) and one ALTERNATE tree per alternate
   key ((alternate key, release ordinal) → prime key). RELATIVE: one tree (relative record number → the record's frame).
   The leaf entry's value is the ONE frame shape `RecordFraming.EncodeFrameHead`/`ReadFrame` already define (length word,
   extent table, an indexed record's ordinal vector, payload), so the frame codec of kb/Work PB2754 stays the one codec and the
   store-level shape around it (`ComposeStore`, `DecodeStore`, the gap tag, the composed image) is deleted.
4. **Crash consistency is SHADOW PAGING with a GENERATION LEDGER, not a log** (2.2.1.6): a page the COMMITTED meta references
   is never overwritten; a statement writes its changed pages to fresh pages (or in place when the page is itself fresh since
   the committed generation), then the WORK meta; a sync point promotes the work meta to the committed slot. Rollback is free
   (the committed meta is the rollback target), recovery is a bounded verification at OPEN (no replay, no rebuild utility), and
   the two things a crash can do — lose the statements since the last sync, or nothing — are the only two things it can do.
5. **Coherence across run units stays the PB2660 statement mutex, at PAGE granularity** (2.2.1.7): a coherent statement reads
   ONE page (the work meta) to learn the generation and, when it moved, DROPS its page cache; it then reads only the pages it
   touches. Nothing reloads and nothing persists whole. Record locks, the lock-presence byte, the store mutex and the Table 19
   columns keep their bytes, their hosts and their observable behaviour (2.2.1.8).
6. **The boundary of a relative or indexed file is the host's** (2.2.1.9): a WRITE or REWRITE answers '24' only when the
   record would take the store past the format's 2^32 pages or past a maximum file size the host REPORTS (`EFBIG`,
   `ERROR_FILE_TOO_LARGE`); a medium that is full answers §9.1.13.6 item 1's '30' at the STATEMENT, with the file as the
   previous statement left it, never at a later CLOSE.
7. **APPLY COMMIT, COMMIT and ROLLBACK are the ledger's sync point, discard and adoption rule** (2.2.1.10), plus the lock
   releases §14.9.7.4 GR3 and §14.9.36.4 GR3 name; the data-item half is the compiler's and is its own group (PB3338).

**2.2.1.4 The on-disk format (store format 5).** All integers little-endian, as format 4's were.

```
PAGE (4 096 bytes; every page)
   0   1  kind          M meta · I internal · L leaf · O overflow · F free
   1   1  flags         kind-specific (a leaf's bit 0: rightmost; an overflow's bit 0: last of its chain)
   2   2  entries       entry count (I, L); payload bytes (O)
   4   4  checksum      CRC-32C of bytes 8..4095, the one torn-page detector (2.2.1.6 R6)
   8   8  generation    the generation that last wrote this page (2.2.1.6 R2)
  16   4  self          this page's number (a page read from the wrong offset is a torn read)
  20   4  next          O: the next overflow page of the chain; L: the right sibling (0 = none); I, M: 0
  24   8  reserved      0
  32  ..  body

META (page 0 = the COMMITTED slot, page 1 = the WORK slot; 2.2.1.6)
  32   8  magic "CBNFSTR" + format version 5          (the same first eight bytes as format 4, so a format-4 file is Foreign)
  40   1  organization  'R' relative · 'I' indexed
  41   1  record type   'F' fixed · 'V' varying
  42   4  minimum logical record size · 46 4 maximum logical record size   (§9.1.6, as format 4)
  50   2  key count     0 relative; prime + alternates indexed
  52   8  generation    the coherence stamp (2.2.1.7); one more per statement that changed the file
  60   8  release mint  an indexed store's next release ordinal (kb/Work PB2693); 0 relative
  68   8  page count    pages in the file (the allocation frontier)
  76   4  root[0]       the prime tree (indexed) or the record-number tree (relative)
  80   4  free root     the FREE tree (page number → nothing), the same B+ tree engine
  84   4  ledger root   the LEDGER tree (2.2.1.6): page number → (generation of last write, state)
  88   8  committed generation   the generation the COMMITTED slot carries (the rollback target; equal to `generation` in that slot)
  96   1  flags         bit 0 UNDER COMMIT CONTROL (an active APPLY COMMIT clause covered the last OPEN, 2.2.1.10)
                        bit 1 CLEAN CLOSE (the last writer closed every connector and synced)
  97   ..  per key, exactly format 4's descriptor: offset(4) length(4) flags(1: bit0 DUPLICATES) suppress(Int32 biased
           code-unit count + UTF-16 code units, kb/Work PB1602) collation-length(1) collation(ASCII fingerprint), then
           root[k+1](4), the alternate tree of key k
   ..  the per-key descriptors are bounded by the page: 4 064 bytes hold at least 60 keys of 64-byte descriptors; a
       declaration past the page's capacity is refused at the OPEN with '39' (DOC-A.1-129 names the limit)

INTERNAL (I)   body: entries × { key length(2), key bytes, child page(4) } ascending, plus a leftmost child page(4) at 32
LEAF (L)       body: entries × { key length(2), value length(4), key bytes, value bytes } ascending;
               a value whose length word has bit 31 set is an OVERFLOW REFERENCE: 4 bytes first-overflow-page + 4 bytes total
OVERFLOW (O)   body: up to 4 064 payload bytes, chained by `next`
FREE (F)       body: unused (a free page keeps its header so a stale read is recognizable)
```

- **Keys are BYTES in the file's channel character set** — the same `FileCharacterSet.Medium` bytes the frame payload carries
  (kb/Work PB690; one byte per character position, CODE-SET conversion applied, 2.2.1.5) — and the tree compares them through a
  KEY ORDER chosen once per tree at OPEN (`IKeyOrder`): ORDINAL bytes for a native collating sequence, WEIGHTED (decode, then
  the key's `CobolCollation` as `IndexedConnector.KeyCompare` does today) for a declared one, NUMERIC for a relative record
  number (stored as 8 bytes big-endian, so ordinal byte order IS numeric order and the ordinal comparer serves), and COMPOSITE
  for an alternate tree (the key's order, then the 8-byte big-endian ordinal). A START's partial key (§14.9.41.4 GR17 b)) is a
  seek to the prefix under the same order; the '02' look-ahead of §14.9.30.4 GR27 is the next entry's key prefix.
- **An INDEXED record is n + 1 entries, one per tree.** WRITE inserts the frame under the prime key and, for every alternate
  key whose value is not the key's SUPPRESS WHEN value (the §12.4.5.6.4 GR6 suppression; §14.9.30.4 GR21 c): such a record *"is not
  considered to exist"* under that key), an entry (alternate key, ordinal) → prime key, every ordinal the one fresh mint value (§14.9.51.4 GR40). REWRITE
  replaces the prime entry's value and, for each alternate key whose value or suppression changed (`AltChanged`, the one
  definition today), deletes the old composite entry and inserts the new one with a fresh ordinal — which is §14.9.35.4 GR24
  a)/b) by construction: *"When the value of a specific alternate record key is not changed, the order of retrieval when that
  key is the key of reference remains unchanged"*, and a changed key's record is last among its duplicates because its ordinal
  is the largest. DELETE removes n + 1 entries. A sequential READ under an alternate key walks that tree and takes the record by
  one prime lookup; the record's ordinal vector in the frame is what locates its alternate entries for REWRITE and DELETE, and
  a reload-time renumbering (PB2693's defect) cannot exist because nothing is renumbered.
- **A RELATIVE record is one entry.** The record number is the key; `Highest` (§14.9.51.4 GR29 a), *"one greater than the
  highest relative record number existing in the physical file"*) is the tree's last key; an empty slot costs nothing, so the
  sparse case of kb/Work PB1192 (one record at a key in the billions) is one leaf. START relational operators are seeks;
  READ NEXT / PREVIOUS are cursor steps.
- **A record larger than a leaf can hold inline goes to overflow pages**: a leaf entry keeps at most 1 016 bytes of value inline
  (a quarter of the body, Berkeley DB's `page_size / (minkeys × 2)` with two keys per page) and otherwise an overflow reference;
  the frame is read from the chain. So the maximum record size is the host's, not the page's. Keys are bounded by the leaf: a
  key longer than 1 016 bytes is refused at the OPEN with '39' as a fixed-attribute conflict the format cannot hold (DOC-A.1-129).
- **Free space.** A freed page enters the LEDGER as `freed at generation G` and moves to the FREE tree at the next promotion
  (2.2.1.6 R4); an allocation takes the lowest page of the FREE tree, else the page at `page count` (the file grows). The file
  never shrinks of its own accord; `OPEN OUTPUT` creates a new two-page store (§14.9.27.4 GR18, *"After the creation of the
  file, the file contains no records"*) through the connector's own handle with `RandomAccess.SetLength` to two pages, which
  is the one truncation (today's `Persist` truncation, moved).
- **What the pages prove at a glance.** With 12-byte internal entries (an 8-byte relative key) a page fans out 330 ways; a
  3-level tree covers 36 million relative records, 4 levels 12 billion. With 40-byte prime keys an internal page holds 90
  entries and a leaf 16 records of 200 bytes: 3 levels cover 130 000 records, 4 levels 11 million, 5 levels a billion. Every
  level but the leaf fits a cache of a few hundred pages, which is why a READ is one page of I/O in the steady state and
  O(log n) only cold (2.2.1.11).

**2.2.1.5 The code set and the character set.** The frame payload and the key bytes are the channel bytes of today's
`FileCharacterSet.ToChannel` + `Medium.GetBytes` (one byte per character position; a character with no byte image is refused at
the WRITE with '91', DOC-A.1-110, before any page is touched), and a READ decodes them with `FromChannel` (§13.18.13.4 GR6 a):
the frames come back in the native character set). The meta page, the page headers, the key lengths and the ordinals are the
§9.1.7.2 information *"the implementor may add to the record on the physical storage medium"* and are never converted (the same
rule `ComposeStore` applied to the length word and the header). A declared COLLATING SEQUENCE orders the tree through the
weighted key order; its fingerprint in the meta page is what makes a file created under one sequence refuse an OPEN under
another ('39', §14.9.27.4 GR10 — *"The implementor defines which of the fixed-file attributes are validated during the
execution of the OPEN statement"*), exactly as today, because an index ordered under one sequence cannot be searched under
another.

**2.2.1.6 Crash consistency: the generation ledger.** The whole scheme is six rules; each has a drift test (2.2.1.12).

- **R1 — a page the COMMITTED meta references is never written.** The committed slot (page 0) names a tree whose every page was
  synced to the medium at the promotion that wrote it. A statement that changes such a page allocates a fresh page, writes the
  new content there and re-points the parent, up to the root (path copying; the root's new number goes into the WORK meta).
- **R2 — every page written since the committed generation is in the LEDGER with the generation of its last write and its
  state (`fresh` = allocated since, `freed` = released since).** The ledger is a small B+ tree of the same engine whose root is
  in the work meta; it is rewritten by the statement that changes it, so the work meta and the ledger always agree.
- **R3 — a FRESH page may be rewritten in place.** It is not referenced by the committed meta (R1 is about the committed tree),
  so overwriting it cannot damage the recoverable state; the ledger entry's generation moves with each write. This is what makes
  a sequential bulk load cost ONE leaf write per WRITE: the rightmost leaf is fresh after its first change and stays fresh until
  the next promotion, so the path is copied once per promotion interval, not once per statement.
- **R4 — a FREED page is not reused before the promotion that makes its freeing durable.** Until then the committed meta
  references it, and R1 forbids writing it. At promotion the ledger's `freed` pages move to the FREE tree and its `fresh` pages
  leave the ledger (they are now committed pages, R1 applies).
- **R5 — a SYNC POINT (promotion) is: sync the data file (`FileStream.Flush(true)` on the connector's own handle) → write the
  work meta's content to the committed slot with `committed generation` = its generation → sync again.** Two syncs make the meta
  copy strictly after the pages it names. For a file NOT under commit control a promotion happens at CLOSE, at the run unit's
  implicit CLOSE of every open file (`FileRegistry.CloseAll`) and as a CHECKPOINT whenever the ledger holds more than 1 024
  pages or 4 096 statements have run since the last promotion. For a file UNDER commit control the only promotions are
  COMMIT and the run unit's normal termination (§9.1.18.5 *"When a run unit terminates normally, all file changes are
  permanently committed"*): a CLOSE of such a file writes the work meta and keeps the handle but does NOT promote, because a
  ROLLBACK after that CLOSE must still restore the state at the last COMMIT (§14.9.36.4 GR3), which a promotion at CLOSE
  would have advanced. The checkpoint bounds R6's verification at
  4 MiB of reads (about 20 ms at the measured 12-15 µs a page) and costs, at the measured 3 ms per sync pair, under 2 µs per
  statement amortized; it is never taken for a file under commit control, whose only promotions are the standard's.
- **R6 — the ADOPTION RULE at OPEN decides which meta is the file.** Read page 0 and page 1 (two reads; the whole of what an
  OPEN reads besides the verification below), validate each slot's checksum; then:
  - adopt the WORK slot if its generation is above the committed slot's, its UNDER COMMIT CONTROL bit is clear, and every page
    the ledger lists as `fresh` validates (its checksum, its `self`, and its page generation equals the ledger's) — that is the
    file as the last completed statement left it, which is what a process death leaves (the operating environment keeps what was
    handed to it) and what a power loss leaves when every write reached the medium;
  - otherwise adopt the COMMITTED slot: the `fresh` pages are free (they were never committed), the `freed` pages are live again
    (R4 kept them intact), and the work slot is rewritten from the committed one. A power loss that tore a fresh page or lost a
    write lands here, and so does any file whose last writer ran under commit control and did not COMMIT (2.2.1.10): §9.1.18.5
    *"When a run unit terminates abnormally, all file changes made since the previous commit or, if none, the beginning of the
    run unit are rolled back for all files for which there are active APPLY COMMIT clauses."*
  Neither outcome is torn, because the committed tree's pages were synced before the meta that names them (R5) and nothing wrote
  them since (R1, R4), and a work tree is adopted only when every page it changed is verified present and whole.
- **A statement is atomic by the same rules.** Its page writes land on fresh pages or in place on fresh pages; the work meta is
  written LAST. A host refusal part-way (a full medium, a handle the host revoked) leaves the work meta naming the previous
  statement's tree; the statement drops the cache and answers '30' (§9.1.13.6 item 1, *"A permanent error exists and no further
  information is available concerning the input-output operation"*), and §9.1.13.1's permanent error is in effect from there —
  the behaviour `CompleteStatement` and `Enter` give a refused persist and a refused reload today, now for every connector.
- **Why not a write-ahead log.** A log is a second file (a sidecar, refused by PB802) or a region inside this one that needs
  its own wrap-around, checkpointing and replay; shadow paging needs none of those, gives ROLLBACK for free, and its recovery
  reads only the pages the ledger names. The price is R4's growth between promotions, bounded by the checkpoint for ordinary
  files and by the program's own COMMIT frequency for files under commit control — the price every shadow-paging store pays.

**2.2.1.7 Coherence across run units, at page granularity (the successor of the PB2660 bullets above).** The three pieces keep
their places; what each does changes:

- **The generation lives in the WORK meta** (offset 52), stamped by every statement that changed the file (one more than the
  generation it read). A connector whose file lock admits another writer (`FileLockPosture.AdmitsAnotherWriter`, `SHARING WITH
  ALL OTHER`) still runs every record statement inside the store mutex (`HostRegionLocks.StoreMutexByte`, exclusive through a
  writable handle, shared through a reader's), still enters by reading the generation — now ONE page read of page 1 — and when
  it moved, DROPS the run unit's page cache for the file instead of reloading anything. The statement then reads the pages it
  touches: O(log n), the same count as an unshared statement plus the one meta read. `Complete` writes the statement's dirty
  pages and the work meta before the mutex is given back, so the statement is one step to every other run unit, as before.
- **A connector whose lock admits no other writer** (every other posture; Table 19 refuses a foreign writer's OPEN in every run
  unit, kb/Work PB833) runs the same statement path with no mutex and no generation read: dirty pages are written at the end
  of each statement all the same (the bar: *the file is in the state after its last completed statement*), and the cache is
  never dropped. The whole-store "loaded at OPEN, persisted at CLOSE" branch is gone, so there is ONE statement path.
- **Within a run unit** the successor of `KeyedStoreTable` is a table of OPEN STORES per physical file (`StoreTable`): one
  `PagedStore` — the handle's page I/O, the page cache, the ledger and the trees — attached by every connector of the run unit
  over that path (`Attach`, `AttachCreated` with its PB754 refusal, `Detach` on the last CLOSE), so a sibling's mutation is
  visible instantly because there is one cache and one set of trees, as there was one `KeyedStore`.
- **Published record locks** are unchanged (2.2.1.8).

**2.2.1.8 Where the locks live.** Nowhere new. The region map of `HostRegionLocks` — the five Table 19 column bytes at 2^62,
the store mutex at 2^62 + 0x100, the lock-presence byte at 2^62 + 0x101, the hashed record region at 2^62 + 2^60 — stays, held
through the connector's own handle (`HandleRegionLocks`: Windows `LockFileEx`, Linux OFD locks) or the lock files of
`SidecarRegionLocks` on every other host, and the reason the Windows locks are harmless changes from "beyond
`RecordFraming.MaxStoreBytes`" to "beyond 2^44, the format's largest file" (the comment on `HostRegionLocks` is rewritten with
the constant). `PhysicalFileTable` (the run unit's lock table, `LockRecord`/`HolderOf`/`Unlock`, the ceilings 54 and 53), the
presence-byte shortcut, the publish-then-test discipline for a reader on Linux, `RetryAttempt.HolderOutsideRunUnit` and the wait
outside the mutex (`WaitOutsideStatement`) keep their code and their witnesses: `CrossRunUnitKeyedStoreDriftTests` runs unchanged
in intent over the new engine (its `KeyedStoreReloadFailureDriftTests` sibling becomes the adoption-rule and refused-page-write
witnesses of 2.2.1.12). The §9.1.15 file lock is still the handle the store travels through (kb/Work PB771), and every page
read and write goes through that handle positionally (`RandomAccess`), so no second handle is ever taken.

**2.2.1.9 The size limit, and the new DOC-A.1-107 text (to replace the row when PB3333/PB3334 land).** *"The boundary of a
physical relative or indexed file is the host file system's maximum file size, capped by the format at 2^32 pages of 4 096
bytes (16 TiB). A WRITE or REWRITE whose record would take the store past that boundary is unsuccessful with I-O status '24'
(§14.9.51.4 GR33 b) relative, GR42 d) indexed; the invalid key condition exists, the INVALID KEY phrase runs, no record is
released and the file is unchanged); the host's answer that the file cannot grow further (`EFBIG`, `ERROR_FILE_TOO_LARGE`) is the
same boundary. A medium with no room for the statement's pages (`ENOSPC`, `EDQUOT`, `ERROR_DISK_FULL`, `ERROR_HANDLE_DISK_FULL`,
`ERROR_DISK_QUOTA_EXCEEDED`) makes THAT statement unsuccessful with §9.1.13.6 item 1's '30' and leaves the file as the previous
statement left it; nothing is deferred to the CLOSE. A record's own size is bounded by the host, not the format (overflow pages).
The relative key's own range is separate: a key less than 1 or greater than 2 147 483 647 (the highest relative record number
permitted) answers '34' (§14.9.51.4 GR29 b)), and a sequential-access release whose number has more digits than the relative
key item answers '24' (GR33 c)). Same in every edition."* `RelativeConnector.HighestRelativeRecordNumber` keeps its value: the
tree would hold 2^63 keys, but the number is the §12.4.5.13 key item's domain, a determination that need not change with the
format. The constant `RecordFraming.MaxStoreBytes`, `KeyedConnector.StoreHolds`, `RelativeStore.FramedBytesAfterPut` and
`IndexedStore.RecordBytes` are deleted with the model that needed them. The store header's bytes stay the file's §9.1.6 fixed
file attributes (*"A physical file has several attributes that apply to the file at the time it is created and cannot be changed
throughout the lifetime of the file"*), read by `FixedAttributeConflict` from page 0 through `HostFile.OpenAuxiliary` as today
(the read of one page, not of a variable-length header), and `SequentialConnector`'s "a keyed store opened through a sequential
description is not a conflict" keeps its answer through the same reader.

**2.2.1.10 APPLY COMMIT, COMMIT and ROLLBACK on the ledger.** The facility is declined today at bind
(`DeclinedFacilityPass.VisitApplyCommitClause`, Annex A.4.3 item 2) and COMMIT and ROLLBACK are CONTINUE
(`conformance:2023/pb137_commit_inert`, §14.9.7.4 GR1 / §14.9.36.4 GR1). PB3330's bar builds them on the engine, so the decline
is reversed by its groups: the runtime half (PB3337) and the compiler half (PB3338).

- **UNDER COMMIT CONTROL** is a bit in the meta (offset 96, bit 0), set by an OPEN through a connector covered by an active APPLY
  COMMIT clause (§12.4.6.3.4 GR1: *"This clause specifies that the files and data-items listed are to be subject to the commit
  and rollback facility."*; GR5 defines active) and cleared by the COMMIT (or the normal termination) after which no connector
  under commit control holds the file. While it is set, the only promotions are COMMIT and normal termination (R5: a CLOSE
  writes the work meta, keeps the handle and promotes nothing), and R6 adopts the committed slot at
  the next OPEN whatever the work slot says — which is §9.1.18.5's rollback of an abnormal termination, obtained by writing
  nothing at the termination (there is no process-exit handler to rely on) rather than by undoing anything.
- **COMMIT** (§14.9.7.4 GR3: *"The execution of the COMMIT statement permanently applies any changes made to the files specified
  in all the active APPLY COMMIT clauses and releases all record locks associated with those files."*) = a promotion of every
  open store under commit control, then `PhysicalFileTable.ReleaseAllForConnector` for each such connector; GR4's file locks of
  files closed and not reopened since are released here too, which means a CLOSE of a connector under commit control KEEPS its
  handle (the §9.1.15 file lock: *"except where the file is subject to an active APPLY COMMIT clause ... the file lock is removed
  by the COMMIT statement"*) until the next COMMIT or ROLLBACK and promotes then.
- **ROLLBACK** (§14.9.36.4 GR3: *"All files and data-items referenced in active APPLY COMMIT clauses are restored to the state
  they were in at the execution of the last COMMIT statement or the start of the run unit if no COMMIT statements had previously
  been executed."*) = for every store under commit control: drop the cache, copy the committed slot over the work slot, and treat
  the ledger as R6's second branch does (fresh pages free, freed pages live); then release the record locks. No page of data is
  read or written: the rollback target was never overwritten (R1, R4).
- **Record locking under commit control** is §9.1.16's implicit *"LOCK MODE IS AUTOMATIC WITH LOCK ON MULTIPLE RECORDS"*: the
  registry registers that posture for the connector and refuses the LOCK clause and UNLOCK at bind (PB3338), and the locks are
  released only by COMMIT, ROLLBACK and termination.
- **The compiler half** (PB3338): accept the I-O-CONTROL APPLY COMMIT clause (§12.4.6.3.3 syntax rules 1-11; the period rule
  `ParagraphPeriods` already prints), register each listed file connector and data item with the run unit, emit COMMIT and
  ROLLBACK as registry calls, and save at COMMIT / restore at ROLLBACK the listed data items, the file status items and the
  LINAGE and RECORD clause items (§14.9.7.4 GR5, §14.9.36.4 GR3's second sentence), with EC-FLOW-COMMIT / EC-FLOW-ROLLBACK /
  EC-FLOW-APPLY-COMMIT where §9.1.18.7 raises them (the catalog already lists them). Sequential files under APPLY COMMIT
  (§9.1.18.2 admits *"any type of file on a mass storage device"*) are a further note beyond PB3338: the sequential organization
  has no ledger, and its commit facility is a separate design.

**2.2.1.11 Performance targets, and how a test measures them without a clock.** `PagedStore` counts its page reads, page
writes and syncs per statement (`IoCounters`, internal), and every witness asserts COUNTS, because a wall-clock test is a flake
generator (the repository's rule) and because a count is the thing the bar states. With d = the tree's depth (2.2.1.4 gives the
formulas):

| Statement | Pages read | Pages written | Syncs |
|---|---|---|---|
| OPEN (any mode, file present) | 2 + ledger `fresh` pages (≤ 1 024; 0 after a clean close) | 0 (OUTPUT: 2) | 0 |
| READ by key / by RRN | ≤ d + overflow pages of the record (+ 1 meta read under ALL OTHER) | 0 | 0 |
| READ NEXT / PREVIOUS | ≤ 1 (+ d when the cursor crosses a leaf; + d for an alternate key's prime lookup) | 0 | 0 |
| WRITE / REWRITE / DELETE (indexed, k alternate keys) | ≤ (k + 1) × d | ≤ (k + 1) × d + ledger + 1 meta; ≤ k + 3 in a sequential load (R3) | 0 |
| WRITE / REWRITE / DELETE (relative) | ≤ d | ≤ d + ledger + 1 meta; ≤ 3 in a sequential load (R3) | 0 |
| a promotion (CLOSE or checkpoint of a file not under commit control; COMMIT or normal termination of one under it) | 0 | 1 (the committed slot) | 2 |
| CLOSE of a file under commit control | 0 | 1 (the work meta) | 0 |

The witnesses: (a) **linearity** — a load of N and of 4N records through `NO OTHER` and through `ALL OTHER`, sequential and
random keys, asserts that the pages written per statement differ by at most the depth growth (an integer ≤ 1 between those
sizes) and that no statement wrote more than the table's bound; (b) **OPEN reads the header** — opening a store of 4N records
reads ≤ 2 pages after a clean close; (c) **random READ is O(log n)** — 1 000 random READs on the 4N store read ≤ d + 1 pages
each, with the cache cold (`PagedStore` offers a `DropCache` seam for the witness); (d) **no whole-store I/O exists** — a
tree-wide drift test (`[Trait("Sweep", "tree")]`) asserts that `src/Cobol.Net.Runtime/IO/Store/` is the ONLY code under
`Runtime/IO` that calls `RandomAccess.Read`/`Write`/`GetLength`/`SetLength` on a keyed handle and that it never reads or writes
more than one page per call, and that `ArrayPool<byte>.Shared.Rent` with a file-length argument appears nowhere under
`Runtime/IO` (the shape `ReadImage` has today). For the record, the prototype's wall-clock arithmetic: a million-record
sequential load is about 3 page writes a statement at 8 µs, some 25 s of I/O before the tree's own work; the same load through
today's `ALL OTHER` path is 10^6 × O(file) and does not finish.

**2.2.1.12 The format's drift tests (each invariant pinned by one).** `PagedStore` is written over an `IPageMedium` — the
production medium is `RandomAccess` on the connector's `SafeFileHandle`; the test medium is an in-memory page array that
records which writes were SYNCED and can be asked to *lose every unsynced write* (a power loss) or to *keep them* (a process
death) and to *tear* one named page — so every crash scenario is deterministic and runs on every host without a VM:
- R1: no write ever targets a page the committed meta's tree references (the medium records every write's page number; the
  test walks the committed tree after each statement);
- R2/R3: the ledger lists exactly the pages written since promotion, with their last generation; a fresh page is rewritten in
  place (the sequential-load write count);
- R4: a page freed at generation G is not allocated before the promotion of G;
- R5: the committed slot's pages are all synced before the slot is written (the medium's sync log orders them);
- R6: the adoption matrix — {process death, power loss with every write durable, power loss losing the last k writes, a torn
  fresh page, a torn meta slot} × {not under commit control, under commit control} → {work adopted = last completed statement,
  committed adopted = last promotion}, and the file reads back a consistent tree in every cell;
- the statement's atomicity: a medium that refuses the n-th write of a statement leaves the file at the previous statement
  and the statement at '30';
- the layout: a golden hex dump of an empty relative store and of an empty indexed store with two keys (the meta page byte for
  byte), and the page checksum detecting a single flipped bit in every page kind;
- the engine's tree: a property test of insert/delete/seek/next/prev against a `SortedDictionary` model under every key order,
  including composite keys, overflow values and splits and merges at every level.

**2.2.1.13 What the design deletes and what it keeps (CLAUDE.md rule 4: no wrapper, no shim, no reader of the old format).**
Deleted with their callers: `RecordFraming.ComposeStore`, `DecodeStore`, `DecodeHeader`/`WriteHeader`'s store-level form,
`ReadHeader`'s stream walk (replaced by the one-page meta read), `GapTag`/`GapBytes`/`EncodeGap`, `ImageMedium`,
`MaxStoreBytes`, `HeaderBytes`, `OrdinalSlots`, `FrameBytes(StoredFrame)` and the boundary arithmetic; `KeyedConnector.Load`,
`Persist`, `PersistIfChanged`, `PersistIsReachable`, `ReadImage`, `ReadThroughAreas`, `WriteThroughAreas`, `ReadAt`,
`ReadGeneration`, `Reload`, `Fill`, `PersistFrames`, `ReleaseMint`, `StoreHolds`, `_storeHeaderBytes`, `HostFaultObserver` and
`HostFaultPoint`; `KeyedStore.Version`/`PersistedVersion`/`Generation`/`VersionAtEntry`; `RelativeStore` and `IndexedStore` as
in-memory record maps (their names may survive as the two tree-handle views, their maps may not); `KeyedStoreTable` as the
store of records. Kept, unchanged in contract: the frame codec (`EncodeFrameHead`, `ReadFrame`, `ReadStreamFrame`,
`FrameStarts`, `StreamFramingParses`, `IFrameMedium`, `StoredFrame`, `FrameHead`, the extent-table codec — the sequential
connector and `CobolSort` use them), `FixedFileAttributes` and its comparison, the region map and every lock class,
`PhysicalFileTable`, `StoreStatement`'s shape (`BeginStatement`/`Complete`/`Dispose`), `FileRegistry`'s statement wrapping, every
status the organizations answer and the order they answer it in (DOC-A.1-104). The input-output areas of §12.4.5.14.3 GR1
(*"If the RESERVE clause is specified, the number of input-output areas allocated is equal to the value of integer-1"*) become
the page cache's floor: the cache for a physical file holds at least the attached connectors' areas (one page each) and at
least the engine's working set (d + 3 pages), with the run unit's default of 256 pages (1 MiB) above both; a RESERVE larger than
the default enlarges the cache, and `HostFile.InputOutputAreaBuffer` sizes it in the one place it sizes today.

**2.2.1.14 The implementation, as groups the fix lane lands one mechanism at a time** (the register entries are kb/Work
PB3331-PB3338; this paragraph is a pointer, not a tracker). PB3331 the page layer and the ledger over `IPageMedium`, with the
crash matrix; PB3332 the B+ tree over it, with the model test; PB3333 RELATIVE on the engine (its connector, its tests, the
relative half of DOC-A.1-107); PB3334 INDEXED on the engine (the trees, the ordinals, the collations, the indexed half of
DOC-A.1-107); PB3335 the deletion of the whole-store model and page-granular coherence (the cache drop, `StoreTable`, DOC-A.1-75,
the whole-store ban); PB3336 the performance witnesses of 2.2.1.11; PB3337 COMMIT and ROLLBACK in the runtime; PB3338 the
compiler's APPLY COMMIT. Same-file work is sequenced by `blocked_by`.

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
outermost name) ends the run unit through that surface and never escapes as an unhandled .NET exception. A .NET host
enters through a program's generated host entry (`Cobol.<assembly>.<NAME>.Call`, DESIGN-external-repository §4.3, the
one host surface of §4.4), which registers the program's module and activates it at the outermost boundary;
`ProgramTable.RunMain(path)`, the same boundary as `RunModule` without the registration, is shipped surface no route calls.
**The process's own state belongs to the entry that IS the process (kb/Work PB3163, PB3193).** `RunModule` and the
`cobol` CLI's `Main` are the only writers of the process exit code and the only callers of
`IO.StandardStreams.EnsureUtf8` (UTF-8 standard output and error, and a UTF-8 reader over a redirected standard input,
DOC-A.1-59 and DOC-A.1-1); `RunMain`, `RunUnit.Run`, a host entry's activation and a CALL into a module write neither, so a run unit a host
embeds DISPLAYs through the host's `Console.Out` / `Console.Error` as the host configured them and leaves the host's
exit code and console as it found them. `ProcessStateOwnerDriftTests` (Unit) holds each process-owned write to its
owner; `RunUnitHostExitCodeTests` and `RunUnitHostConsoleTests` (Conformance) are the host-side witnesses.
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
⛔ **A type-6 position has NO `…Real` twin: the renderer takes the operand's INTEGER first (kb/Work PB651).**
Since kb/Work PB248 a floating-point operand at a §15.3 type-6 position is REFUSED under strict (COBOLNET1627 —
type 6 admits "an integer data item or an always-integral arithmetic expression", and a floating-point item is
neither), so it arrives only under `--permissive`, and docs/CONFORMANCE.md DOC-A.1-124 fixes what it then means:
the integer it holds, or the truncated integer with EC-ARGUMENT-FUNCTION checking off. That is a property of the
POSITION, so `IntrinsicRenderer.RawArg` — the one indexed argument entry every intake and lane predicate reads —
takes it there, before a lane is chosen, on every carrier, keeping the VALUE or saying it cannot. An SDIDI operand
STAYS on the SDIDI (`CobolIntrinsics.IntegerArgUnboundedDec`: the integrality test, then `CobolDec.TruncateToInteger`),
because an SDIDI holds 34 significant digits at any exponent and its integer reaches far past `Int128` —
MOD(10 ** 40, 3) and MOD(A ** 3, B) of a 15-digit A are integers the SDIDI body evaluates (kb/Work PB2630, PB69),
and landing them on `Int128` was EC-SIZE-OVERFLOW. A fixed-point operand's integer fits the `Int128` its unscaled
value rides, and a float past it (±1.7e38, ±∞ included) raises EC-ARGUMENT-FUNCTION through
`CobolIntrinsics.IntegerArgWideReal`. ⛔ The position intake never CLAMPS: a clamp
answers for a different integer, so `MOD(1.0E39, 7)` answered 1 where the operand's integer answers 5 (PB651 review
R1-2). Clamping is right only where the function answers the same for every integer past a bound — TEST-DATE-YYYYMMDD
/ TEST-DAY-YYYYDDD argument-1 (the r1a catch-all) and FIND-STRING argument-3 (0 once every match is ignored) — and
those arms alone take `IntrinsicRenderer.IntArgWideSaturating` (`CobolIntrinsics.IntegerArgSaturatingReal`);
`IntrinsicCarrierAgreementDriftTests.OnlyAnArgumentWhoseAnswerIsConstantPastABound_Saturates` keeps the set closed. An integer has no binary64-ness, so a float at a type-6 position never moves a call to the
binary64 lane: the integer-only functions (MOD, FACTORIAL, the date family, the TEST- validators, the windowing
trio) have no `…Real` body, and `IntrinsicRealArgDriftTests.NoRealBody_ServesAFunctionWhoseEveryPositionIsType6`
keeps it so. A mixed row keeps its twin for its type-10 position (`CombinedDatetimeReal`, reached by a float
argument-2). The lane is witnessed over every verified type-6 position by
`IntrinsicIntegerPositionPermissiveDriftTests` and in `FloatIntegerArgumentPermissiveTests`, the coercion lane's home.

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
