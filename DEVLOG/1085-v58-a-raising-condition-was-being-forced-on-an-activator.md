## Entry 1085 — 2026-07-28 16:06 PDT — V58: a RAISING condition was being forced on an activator that never asked for it

Step 5 of the EC super-batch, and the second finding this session whose stated scope was wrong in a way that
only showed up when a test was written for it.

**The rule.** §14.9.18.4 GR1b is CONDITIONAL and the condition is the point: "If the RAISING phrase is
specified, an exception condition is raised in the activating runtime element IF CHECKING FOR THAT EXCEPTION
CONDITION IS ENABLED in the activating runtime element, and execution continues in that runtime element as
specified in the rules for the activating statement." GR3 agrees for the main-program half — a GOBACK with no
activator "operates as if executing a STOP statement … A RAISING phrase, if specified, is ignored."

**The fix.** `ProgramTable.ApplyPropagationDefault` threw a `CobolFatalException` for a staged FATAL condition,
citing §14.6.13.1.3 #8. That is a misapplication: #8's implementor latitude governs what may happen once a
fatal condition EXISTS, and GR1b stops it ever coming into existence in an unchecked activator. It now
consumes and drops, matching the already-correct nonfatal arm. The returning element's own last-exception
status still stands (§14.6.13.1.4).

**⚠ THE PATH IS ONLY REACHABLE ACROSS A COMPILATION BOUNDARY, which the finding said and I initially ignored.**
My first golden put both programs in ONE file and it still terminated — with a DIFFERENT message, from
`CallEmitter.EmitPropagationPickup`. The reason: `ecState.Active` is GROUP-level, so any `>>TURN` anywhere in
the group makes every CALL site emit its own pickup and pass `siteHandlesPropagation: true`, and
`ApplyPropagationDefault` is by construction the EC-INACTIVE-caller path. Within one group an
unchecked-activator test cannot be written at all. Two separately compiled assemblies, resolved by
ProgramTable's sibling-module probe (§8.4.6.3 rule 4), is the only shape that reaches it — verified first by
hand at the CLI, both with the fix (IN-SUB / AFTER-CALL, exit 0) and without (abnormal termination, exit 1).

**So the harness grew a capability rather than the test being weakened** (rule 5). `CompilerUnderTest` gained
`CompileAndRunWith(source, params companions)`, which compiles each companion into its OWN assembly beside the
main one. One subtlety cost a cycle and is now commented at the site: the assembly must be NAMED for its
PROGRAM-ID, because the sibling probe looks for `<program-name>.dll` — a generic `companion0.dll` compiles
fine and is then never found, surfacing as EC-PROGRAM-NOT-FOUND rather than as a harness error.

**No corpus golden**, deliberately and not silently: the positive corpus compiles exactly one file per entry,
so it cannot express a two-assembly program. The test is
`ExceptionConditionConformanceTests.GobackRaising_IntoUncheckedActivator_IsNotRaisedThere`, in the class that
already owns the EC-PROGRAM/RAISING family. The single-file golden I had drafted was deleted rather than left
to assert something it could not reach.

**Gate:** characterization 33/33 · Conformance CorpusRunner+ExceptionCondition+RunUnitTermination+InterProgram
416/416.

**Left open, named:** `CallEmitter.EmitPropagationPickup` raises in an EC-ACTIVE activator without consulting
whether THAT activator enabled checking for the specific condition — the same GR1b question one level up. The
finding scopes it out as "the EC-active-activator refinement"; it needs a per-EC membership test at the pickup,
which is a design task rather than a one-liner, and it is not covered by this commit.

