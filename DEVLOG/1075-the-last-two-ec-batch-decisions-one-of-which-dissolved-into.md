## Entry 1075 — 2026-07-28 13:21 PDT — The last two EC-batch decisions, one of which dissolved into a design correction

No code. Clears the remaining two of the three owner decisions blocking the EC-infra + OO super-batch; the
batch is now unblocked and the 11-step commit plan stands as written.

**CA12 CO-LANDS, ordered last.** Every fatal-EC finding in the batch then inherits the outward-GLOBAL walk
rather than each shipping the same hole. Checked the claim in the emitted code before recommending, and the
asymmetry is plainer than the finding text made it sound: the I/O path ALREADY walks outward —
`ProgramEmitter.cs:280` emits `return __outer.__RunGlobalUse(__f);   // continue outward (§14.9.49.4 GR4b)` —
while the EC dispatch tail emits a bare `return -3;`. So one spec rule has two behaviours today depending on
whether the declarative is I-O, since GR3g directs "the search is repeated as specified in General rule 4" and
the I/O comment cites that very rule. Deferring would also have been a GAP against P14's zero-GAP done-bar,
inherited by six findings and needing all six re-tested later.

**V55's method-side "enabled" literal — the decision mostly dissolved, and what was left was a defect in this
design document.** Three parts:

- The TURN-state source EXISTS. `BoundCompilation.Turn` is the GROUP TurnState and is LINE-KEYED across the
  whole group text; `Enabled(level3, file, statementLine)` folds it at any line, and `OoMethodSymbol` carries
  its `MethodDefinitionContext`. Query at the METHOD-ID header line: the raise sits in the `__CobolInvoke`
  PROLOGUE — arity, per-argument descriptor, RETURNING — before any method statement executes, so the state on
  ENTRY is the only defensible reading of GR7c's "enabled in the activated method". A class- or program-level
  `>>TURN` earlier in the file is picked up for free, because the fold walks every event with `Line < line`.
- ⛔ **But NOT "at emit time", which is what this document said in three places.** Codegen holds no `TurnState`
  ANYWHERE. Every TURN query in the compiler lives in the binder (`ctx.EcState.Turn`); codegen consumes only
  the results — `BoundEcChecked` nodes plus `EcState` gate flags — and `EcState` has no `Turn` member at all.
  The single TurnState mention in a codegen file is a doc comment in `PtrEmitter.cs:148`. Implementing V55 as
  written would have stood up a SECOND mechanism for a job the binder already owns. Corrected: fold at bind
  time beside the other OO method binding, record on `OoMethodSymbol`, read a plain bool in `OoEmitter`. It is
  still a compile-time literal in the generated C#; only where it is computed moves.
- The non-attributing fallback throw gets a NEW runtime type. This was the one genuinely open piece, and it is
  open for a reason worth recording: EVERY runtime exception we have attributes an EC name —
  `CobolFatalException(ecName, detail)`, `CobolCallException(message, ecName = "EC-PROGRAM-IMP")`,
  `CobolSizeError(detail, ecName)` — while GR7c's not-enabled-in-both path must stop WITHOUT naming
  EC-OO-UNIVERSAL. `CobolFatalException` with an empty name is not a workaround, it breaks the statement
  guard's `EcName ==` match. Owner chose a dedicated `CobolImplementorFatalException` carrying no EC name: the
  type itself is the statement that no exception condition was raised, and it gives the §14.6.13.1.1 NOTE 3
  implementor-fatal class a greppable home.

**The pattern across all three decisions.** Two of the three CORRECT recipes already written into the finding
text — CA9's "keep the loud throw at all six raise sites", V55's "at emit time". A session implementing either
finding from its own prose without reading §Risks would get it wrong, so all three resolutions are flagged in
the three places a session actually looks: plan §0 NEXT item 1, the queue's §OWNER-DECIDED, and the deep-dive
§Risks. The verification caveat's own open item is answered in place rather than left standing.

