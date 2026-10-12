## Entry 1232 — 2026-08-08 14:20 PDT — R30: a typo is never a feature gap — COBOLNET1639, and the Resolve/Probe split that makes it safe

MOVE NO-SUCH-NAME TO X compiled with zero diagnostics and died at run time. So did DISPLAY, COMPUTE,
IF, ADD, STRING, CALL USING, a subscript and PERFORM UNTIL — all ten probed positions. One line owned
all of them: ReferenceResolver.Resolve's item-null arm, where a name resolving to NOTHING returned
silent null into the "references then fail loud" staging — a posture that is right for unsupported
SHAPES of names that DID resolve, and wrong for a name no declaration gives.

The fix splits the QUESTION instead of patching seventy call sites. Resolve (demanding) reports
COBOLNET1639 at that one arm — "not defined" when the name exists nowhere, "does not uniquely
identify" when qualifiers or ambiguity defeat it (§8.4.2.1 "a statement shall contain a reference
that uniquely identifies that resource" / §8.4.2.2) — deduped per source reference. Probe keeps the
silent null for the nine sites that ask "IS this a data item?" with a legal alternative on no: SET's
three format sniffs, COMPUTE's three boolean-reroute sniffs, the two routing predicates the code
already documents as diagnostic-free, INVOKE's class-name receiver, SET's EXCEPTION-OBJECT sender,
CALL BY CONTENT's expression arm, PtrBinder's pointer peek. The audit that classified the ~70 sites
found one real trap: DynTrySetSize peeked through ResolveReceiving with no IndexFieldOf guard, so
legal SET index-name TO n would have drawn the new error — the guard is now there. Tier-C rejected
items resolve to an ITEM and fail later at PlaceForItem, so feature-debt staging and the typo
diagnostic are distinct channels by construction, not by convention.

Found while probing R22's name-collision control; note R30 existed before this entry did (rule 8).
Negative fixture undefined-name-reference rejects at all four editions. Gate: FULL Unit + FULL
Conformance + characterization — the full legs, because this change stares at every reference in the
corpus — with the one red being the DIAGNOSTICS.md sync drift test racing the doc regen mid-run,
green on re-run. Unit 4088/4089 (the red: the doc regen racing the in-flight run; green on re-run) \u00b7 Conformance 4277/4278 (the red: the pinning test this commit rewrites \u2014 the same single test that broke CI on the R22 push, both shards) \u00b7 characterization 33/33 \u00b7 post-rebuild: the rewritten test, the fixture at all four editions, and the drift guards 16/16.

