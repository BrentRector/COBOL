## Entry 1352 — 2026-08-29 00:15 PDT — PB123 LANDED: EXCEPTION-FILE reaches a container's GLOBAL FD — BindExceptionFileArg scanned the program's OWN FD list while every other file-name site rides FilesByName; the sweep caught the unqualified LINAGE-COUNTER doing the same

The two-arm-dispatch family's eighth arm. §13.18.30 makes a GLOBAL FD's file-name visible in every contained
program, and BinderDriver merges those into FilesByName (never Files — the child must not re-register or
CANCEL-close the owner's connector); BindExceptionFileArg alone resolved against ctx.Data.Files, so a
contained program naming the container's GLOBAL FD — legal per §15.28.3 r1 — drew "not the name of a file
connector". Now it rides the ONE FilesByName resolution (SelectName always equals CobolName at construction,
so the keyed lookup is equivalent for local files); both r1 halves (FD-described, not SD) still bind on the
resolved model. The SWEEP found the same blind spot in ExpressionBinder.LinageFileOf's UNQUALIFIED arm — the
qualified arm already used FilesByName — so a contained program whose only LINAGE file is the container's
GLOBAL one now resolves LINAGE-COUNTER instead of drawing COBOLNET0864, and own-plus-inherited still requires
qualification (§8.4.3.14 SR3). Golden 2023/pb123_exception_file_global_fd pins both arms in one contained
pair. AR-15.3-4 → CONFORMS.

⚠ PROCESS: while PB122's wave-local gate ran in the background I edited the tree and rebuilt the compiler
for this fix — the SECOND freeze violation this campaign (DEVLOG 1340 logged the first). The gate's one red
was the EXPECTED drift artifact (the renamed Algebraic test's stale inventory test-ref, cured by the PB122
close), not a phantom from the violation — but that is luck, not discipline: the unfreeze signal is the
completion notification, and scouting during a freeze means READS ONLY. The batch re-ran one clean gate over
the frozen tree before committing.

