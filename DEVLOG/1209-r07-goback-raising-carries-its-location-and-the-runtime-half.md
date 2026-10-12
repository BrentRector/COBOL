## Entry 1209 — 2026-08-07 20:24 PDT — R07: GOBACK … RAISING carries its location — and the runtime half was a two-arm dispatch in miniature

**R07 is LANDED.** `BoundRaising` had no location fields at all (unlike its sibling `BoundRaise`), so
`GOBACK/EXIT … RAISING` under `>>TURN … WITH LOCATION` answered 63 spaces / one space from BOTH the returning
element and the activator. The fields now bind per THIS name at THIS line (§7.3.25.4 GR7) at the ONE
construction point (`EcBindRaising`), so all four callers — GOBACK · EXIT PROGRAM · EXIT FUNCTION · method
return — get them; the Table 12 name is the verb's first word (GOBACK, or EXIT for the three EXIT formats).
`SetPropagating`'s `Set` carries the operands, which fixes the RETURNING element's own status too — the
activator pickup dispatches without re-Setting, so one Set serves both elements (§14.9.18 GR).

⚠ **THE GATE'S FIRST RED WAS THE TWO-ARM SHAPE IN MINIATURE:** `ExceptionState` is an ENGINE plus a STATIC
FACADE, and editing the engine's `SetPropagating` alone left the facade's 2-arg forwarder — the generated
code's actual call target — so the run failed CS1501 while grep swore the signature existed. I first blamed a
stale DLL and rebuilt twice; the DLL was never stale. Which arm did you fix?

**Fact ECT054** (two programs): callee `GOBACK RAISING EC-USER-R07` under WITH LOCATION → the caller reads
`S=[GOBACK…] L=[ECT054S; SUB-P; …]`. **GATE:** EC conformance 66/66 · Unit 4048/4048 · Debug + Release clean.

