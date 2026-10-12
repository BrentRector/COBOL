## Entry 1091 — 2026-07-28 20:41 PDT — CA12 is REFUTED: a Format-3 USE cannot be GLOBAL, so the walk it asks for is unreachable

Step 11 was to be the last of the EC super-batch. It is instead the first REFUTED finding in the queue, and the
refutation also corrects claims I made earlier in this same session.

**What the finding asks for.** `__EcDispatch` returns -3 when no local Format-3 declarative matches; CA12 says
it should first continue the search OUTWARD into a containing program's GLOBAL declaratives, citing
§14.9.49.4 GR3g → GR4b. I implemented it — an EcState gate, a `__RunGlobalUseEc` twin of the I-O
`__RunGlobalUse`, the tier predicates restricted to GLOBAL entries — and then could not write the golden.

**`USE GLOBAL AFTER EXCEPTION CONDITION` does not parse. And the grammar is RIGHT.** §14.9.49.2's general
formats give `[ GLOBAL ]` to Format 1 (`USE [GLOBAL] AFTER STANDARD … PROCEDURE ON …`) and Format 2
(`USE [GLOBAL] BEFORE REPORTING …`) and to NO other. Format 3 is
`USE AFTER { EXCEPTION CONDITION | EC } { exception-name-1 | … } …`. I did not take that from the
transcription — a general format was load-bearing, so I RENDERED printed page 804 and looked at it. There is no
`[ GLOBAL ]`. Across the whole of §14.9.49 the word occurs five times: Formats 1 and 2, Format 1's figure note,
and GR3g/GR4b's references to it. The syntax rules never admit it for Format 3.

**So GR4b can never select a Format-3 declarative** — it selects "a qualifying declarative WITH THE GLOBAL
ATTRIBUTE", and none can have one. The outward walk for a non-I-O condition is vacuous, and `return -3` is
correct. What GR3g's tail describes is real, but the declaratives it can reach are the Format-1 GLOBAL ones,
which is exactly the walk `__RunGlobalUse` already performs.

**⚠ THIS CORRECTS MY OWN EARLIER CLAIMS, in three places.** When the owner decided CA12 should co-land I wrote
— in the plan, the queue and the design doc — that the I-O path walking outward while the EC dispatch returned
-3 was "ONE spec rule with TWO behaviours depending on whether the declarative is I-O", and I offered the
emitted code as evidence. The code observation was accurate; the CONCLUSION was wrong. It is not one rule with
two behaviours: it is the correct consequence of only Formats 1 and 2 admitting GLOBAL. I reached it by reading
the finding's premise and confirming the symptom, without ever checking whether a Format-3 USE can BE global —
the same inheritance failure the owner corrected me on for CA10's clause numbers, one level up: I validated the
rule TEXT and never validated the PREMISE. All three documents now carry the correction rather than the claim.

**The owner's co-land decision is therefore moot**, not overridden. It was a sound decision on the evidence it
was given; the evidence was wrong.

**Reverted**, because unreachable machinery is worse than none: the EcState flag, the `__RunGlobalUseEc`
emitter, the ProgramEmitter gate and the golden are all gone. Nothing about the batch's other ten commits
depends on them.

**Gate after the revert:** characterization 33/33 · Conformance CorpusRunner+ExceptionCondition 409/409.

**Where this leaves the batch:** ten of eleven steps landed, CA12 refuted, so the EC super-batch is COMPLETE.
The pre-merge comprehensive gate — full Conformance, NIST, and the GnuCOBOL differential diffed per-case before
and after — was to be co-located with CA12 and is now owed on its own.

