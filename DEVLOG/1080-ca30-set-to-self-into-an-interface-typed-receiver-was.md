## Entry 1080 — 2026-07-28 14:34 PDT — CA30: SET … TO SELF into an interface-typed receiver was unchecked

First commit of the EC-infra + OO super-batch (step 1 of 11), and the first one to land now that all three
owner decisions are settled. Track E — parallel-safe, touches none of the EC hot files.

**The rule.** §14.9.39.3 SR10: "If identifier-4 is specified and the data item referenced by identifier-3 is
described with an interface-name that identifies the interface int-1, the data item referenced by identifier-4
shall be one of the following: … d) the predefined object reference SELF, subject to the following rules:
1. if the SET statement is contained in a method within the FACTORY definition of the class, that factory
definition shall be described with an IMPLEMENTS clause that references int-1, 2. if … within the INSTANCE
definition …, that instance definition shall be described with an IMPLEMENTS clause that references int-1."
Read verbatim from the spec BEFORE the code, per the queue entry's own citation — and it matched.

**The gap.** The SELF loop resolved the receiver's declared name with `host.OoClasses?.Find(tcn)`, which is
CLASS-ONLY. An interface-typed receiver stores the interface name in the same `Pic.ObjectClassName` slot, so
`Find` returned null, the `is { } tcls` pattern failed, and neither arm ran: SR10d was entirely absent. The
emitter then rendered a raw `(I)(this)` — a runtime InvalidCastException for a non-sealed class, or a Roslyn
CS error on GENERATED USER SOURCE for a sealed one, which the G4 no-CS-on-user-source rule forbids. Either
way a non-COBOL error surface where the standard mandates a clean diagnostic.

**Scope check, and it came back clean.** SR10 has five sender alternatives. (a)/(b) are already enforced on the
data-sender path — `OoConformance.ObjectRefWideningMismatch` handles an interface-typed receiver through the
same §11.8.4 GR2 closure — and (e) NULL is unconditionally legal. So SELF really was the only hole, and the
asymmetry between the two paths is what made it survive.

**Sibling swept, and it is a declared gap rather than a bug.** `ObjectRefWideningMismatch` hardcodes
`ImplementsClosure(sc, factory: false)` while SR10b1 speaks of the FACTORY object — but `PicInfo` carries no
FACTORY flag at all, and `USAGE OBJECT REFERENCE FACTORY OF` is rejected at bind with the staged
COBOLNET0899 (`DiagnosticCatalog.OoFactoryObjectReference`). The hardcode is unreachable, not wrong.

**The fix** restructures the loop so the RECEIVER selects the governing rule — SR12c for a class-named
receiver, SR10d for an interface-named one, nothing for a universal receiver (SR8). `FindInterface`,
`ImplementsClosure` and `host.OoInFactory` all already existed and are the same triple the data-sender path
uses; the diagnostic names SR10d1 or SR10d2 by scope.

**⚠ A FALSE GREEN I ALMOST TOOK.** The wave-local filter `FullyQualifiedName~Oo|~Negative` reported 354
passed — but the corpus goldens are `[Theory]` cases whose golden name is a PARAMETER, so it is invisible to
FullyQualifiedName and my filter had matched neither new golden. Caught by asking the filter for the goldens
BY NAME and getting "No test matches". The correct filter is the runner class, `~CorpusRunnerTests` (346
tests). Then proved the harness actually reads them: setting the `.err` to COBOLNET9999 fails exactly
`EnabledNegativeCase_RejectsWithItsDiagnostic(name: "oo-set-self-interface-not-implemented")`, and restoring
it goes green again.

**Goldens, both registered in this commit.** Negative
`oo-set-self-interface-not-implemented` (COBOLNET0867, 2002/2014/2023) and positive control
`2002/oo_set_self_interface` — identical programs differing ONLY by the IMPLEMENTS clause, which is what makes
the pair a test of the RULE rather than of the program. The positive INVOKEs back through the widened
interface view so the widening is proved to reach a working receiver, not merely to bind: expected `PONG`.
Verified the negative compiles SILENTLY CLEAN without the fix — no diagnostic at all — which is the bug.

**Gate (wave-local):** characterization 33/33 · CorpusRunnerTests 346/346 · CLI probe both ways.

