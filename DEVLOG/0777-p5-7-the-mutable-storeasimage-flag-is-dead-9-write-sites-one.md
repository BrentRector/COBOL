## Entry 777 — 2026-07-11 12:18 PDT — P5.7: the mutable StoreAsImage FLAG IS DEAD — 9 write sites → ONE collected fact set; StoreAsImage = a read-only projection of Storage; byte-exact across the whole battery

**The topology audit's three load-bearing findings drove the design:** (1) `IsImageCapable` is
DECISION-INDEPENDENT — every flag write targeted a fixed-point Display/Binary/Packed leaf that already satisfies
its numeric arm, so every bind-time "image" read in the compiler was PHASE-STABLE all along (its leaf arm is now
defined directly on Pic, value-identical); (2) `IsCharacterImage`'s surviving readers are ALL emit-time (the
"SORT binder consults it" comment was STALE — SORT reads `IsImageCapable`); (3) the only true bind-time flag
reads were the two `NumericImagePlace` wrap decisions in `ReferenceResolver` (ref-mod base + RENAMES span leaf),
which are ORDER-DEPENDENT today (a later figurative/ref-mod flip changes an earlier answer through the overload
bridge).

**The mechanism:** every legacy write site now RECORDS into `DataBinder.ImageForcedItems` (private backing +
IReadOnlySet + `MarkImageForced`) at the exact instant the flag write fired — Tier-B/CALL-cell REDEFINES leaves,
FILE-record leaves, report print faces (resolve); figurative-fill + ref-mod-store receivers (procedure bind). A
STRUCTURAL derivation was rejected deliberately: `Ptr.ForceStringCanonical` classes never flagged their display
leaves, so a tier+Pic rule would over-promote BASED/ADDRESS-OF windows — the collected set reproduces the truth
exactly. `StorageFormPass.Run` = promoted-union (facts → temp-resync [order-preserving: a temp mirrors its
model's PRE-whole-group state] → whole-group leaves) → Classify (group forests + the INTERFACE prototype forests,
new on `GroupBindContext.InterfaceData`) → `HarmonizeStorageCrossings` (the crossing-form fixed point flipping
`Storage` directly). The two bind-time wrap decisions read `DataBinder.IsImageBackedEarly` — the same mid-bind
timing the flag had, order dependency preserved verbatim and NOTED for P7's lazy-place redesign.

**Prove-then-delete, honestly run:** stage 7a computed the set IN PARALLEL with the still-written flag and
`Verify` identity #1 (derived-Storage vs flag) ran REAL for the first time — green across the NIST corpus +
crafted cases + the full battery. Only then did 7b delete the writes: the flag declaration is now
`public bool StoreAsImage => Storage is CharImage { Category: Numeric };` (null-Storage reads answer false —
exactly the flag's early value for every legal read), identity #1 retired as tautological. One test updated with
its rationale: the resolve-only RedefinesClassification harness asserts the COLLECTED FACT (`ImageForcedItems`)
instead of the group-tail projection. Battery: conformance 3159 · unit 281 · characterization 32 BYTE-IDENTICAL ·
Release leg 281 · guard green. Exit criteria #1 (computed once) and the write-back half of #2 now FULLY hold.

