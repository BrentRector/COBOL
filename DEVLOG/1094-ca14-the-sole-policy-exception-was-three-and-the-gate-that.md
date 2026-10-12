## Entry 1094 — 2026-07-28 22:45 PDT — CA14: the "sole policy exception" was three, and the gate that proved it

CA14 was the smallest item left in the queue: one call site, effort S, an owner decision already taken. Replace a
manual `Edition.Removed` emission in `DataBinder.ResolveIndexItems` with the canonical
`ConstructRegistry.Check` funnel so SYNCHRONIZED-on-a-group-item — a 2023 introduction (§E.3.2 item 6, "This
clause may now be specified for a group level data item") — is a hard error at both `--std <2023` and
`--std <2023 --permissive`, like every other introduction. The adjudication called it "the SOLE site routing an
Introduction code through the Removed severity seam".

It was not sole. It was three, and I only know that because the fix shipped with a gate.

**Validate the citations first, and three of CA14's were wrong.** Before reading any code I ran `cite.py --check`
over the entry's §s, which is CLAUDE.md rule 1. "Standard extension" is **§4.2.9**, not the §4.2 the entry
claimed; the warning-mechanism requirement is **§4.2.2** (Acceptance of standard language elements), not §4.2.1
(which is just "General"); and the entry's "Annex E.3.2 item 6, line 48926" points at an EMPTY LINE even in the
spec revision it was written against — item 6 was at 49438 there. The adjudication's REASONING survives all
three intact, which is exactly why this failure mode is dangerous: nothing downstream of a wrong clause number
looks wrong. Corrected in the queue with a note saying so, rather than silently.

**The fix, and then the gate that mattered more.** Swapping the call site is four lines. The question worth
asking is why a policy the compiler documents in three places — `EditionSeverityPolicy.For(NotYetIntroduced) =>
Error`, the `EditionContext.Permissive` summary, the registry row's own `introducedIn` — could be contradicted
by a site for two whole phases with a green battery. The answer: `VersionMatrixTests` tested the permissive axis
ONLY for rows with a `removedIn` (`RemovedConstruct_CompilesPermissive_WithWarning`). An INTRODUCTION row's
permissive behaviour was never asserted anywhere. So I added `IntroducedConstruct_IsRejectedUnderPermissive` —
every active row × every edition below its `introducedIn`, compiled `--permissive`, must be rejected. 182 cells.

It went red on its first run, and not on the site I had just fixed.

**`receive-as-user-word` and `end-receive-as-user-word` at COBOL-85.** Both compiled under `--permissive` with a
warning. The §8.9 reserved-word arm in `VersionConformancePass` emitted its severity as
`EditionSeverityPolicy.For(ConstructAvailability.Removed, edition)` — the verdict ASSERTED as a constant rather
than computed. For most reserved words that constant is right: COMMIT was user-definable until 2023 reserved it,
so a 2014 program using COMMIT as a data-name compiled at `--std 2023` is precisely the migration case
`--permissive` exists for. But RECEIVE and END-RECEIVE are RE-reserved words: reserved by the '85 communication
module, user-definable at 2002/2014, reserved again at 2023. At `--std 85` they were never user words, so no
conforming '85 program can contain one and there is nothing to migrate — a not-yet-introduced construct
receiving the migration mode's leniency, the identical defect to CA14's, reached by a different mechanism.

**The cure computes what was asserted.** `ReservedWordSet.UserWordVerdictAt(word, edition)` returns the real
availability of "this spelling used as a user-defined word": walk the editions BEFORE the target, and if the
word was user-definable at any of them the edition TOOK the spelling away (`Removed`, migration applies);
otherwise it was reserved at the target and everywhere before it (`NotYetIntroduced`, error on both axes). The
data was already there — `ReservedWordEntry` carries R85/R2002/R2014/R2023 — nobody had asked it the question.
`EditionInfo.All`/`Before` became the one edition list, so `Validate` and the interval walk cannot disagree.

Verified at the CLI, all three cases, because a severity change is exactly the sort of thing a unit test can
agree with while the compiler does something else:

    RECEIVE as a data-name, --std 85   --permissive  ->  error   COBOLNET0901   (was: warning, compiled)
    RECEIVE as a data-name, --std 2014 --permissive  ->  clean                  (user-definable at 2014)
    RECEIVE as a data-name, --std 2023 --permissive  ->  warning, compiles      (the true migration case)
    01 G SYNCHRONIZED. (group), --std 2014 [--permissive] -> error COBOLNET0900 on BOTH axes

**Bookkeeping the sweep exposed.** The queue's `CA11 · [MAJOR/L] · oo` entry was landed by DEVLOG 1088 with only
its heading left unflipped — invisible because there are TWO CA11 entries, one exceptions-ec and one oo, and the
LANDED list names "CA11 ✅" once. Flipped. VCR row 43 still carried `<!-- todo -->` ("not yet a constructs.json
row") while `sync-on-group-2023` had been active since P3 step 10; flipped to `gate:sync-on-group-2023` and the
status index regenerated. With CA14 landed and CA11 counted, **every CONFIRMED finding of the 46-item audit is
closed** — V59 alone remains, effort-L with an approved interim.

Gates: FULL Conformance **4113/4113**, zero failures (3931 before, +182 from the new theory) · greenfield Unit
580/580 · VCR drift 4/4 after regenerating the index. The battery stays entirely green.

**The lesson is about the shape of the fix, not the fix.** CA14 as written was a four-line diff that would have
passed every gate and left two identical defects in place. What found them was refusing to land a policy without
a test that ENFORCES the policy for every row, present and future. A new introduction row joins that theory the
moment it enters `constructs.json`.

