## Entry 300 — Phase 2: 7 self-contained RL tests baselined (NIST 343→350)

Same clean-state method on the RL (relative-I/O) un-surveyed tail. 14 RL programs were un-baselined
(+ RL301M/401M flagging). Ran each from a clean output dir; **7 are genuinely clean, self-contained, and
non-vacuous**: RL104A (12/12), RL112A (12/12), RL113A (11/11), RL114A (13/13), RL115A (13/13), RL116A (3/3),
RL204A (12/12). All deterministic (two clean runs, diff-identical), rc=0 (no crash), fresh report.

**The stale-report trap bit here and forced the extra rigor.** RL111A's first survey line read "22/22, 0
FAIL\*" — but that was a *leftover* report file: re-running it from clean shows a real FAIL\* ("WRITE TO FILE
OPENED INPUT NOT ALLOWED", ISO 4.5.4 GR9(B)) **and** a Stack overflow — `D-CLOSE-FILES` infinitely
re-dispatches itself (`Dispatch → Para_D-CLOSE-FILES → Dispatch → …`). So every RL candidate was re-checked
for rc=0 + a freshly-written report, not just a 0-`FAIL*` grep. (Unlike the SEQUENTIAL SQ batch, RELATIVE
`XXXXX###` maps to a *shared* `TF###` by number — DEVLOG 255 — so RL tests can't be assumed order-independent;
the 7 each OPEN OUTPUT and rebuild their TF022, and the full guard run confirms each baseline MATCHes under
real ordering, after the existing TF022 users RL105A/107A/117A/118A.)

Deferred RL (real bugs, not baselined): RL106A (2 FAIL\*), RL119A (1 FAIL\*), RL205A (9 FAIL\*),
RL213A (20 FAIL\*), RL111A (FAIL\* + close-paragraph stack overflow), RL208A (the known Rewrite-pads-varying
bug, 5 FAIL\*); RL212A is a producer half (XXXXP021 for RL208A). RL301M/401M flagging (excluded).

Guard ALL GREEN: 1000 unit / 347 integration / **350 NIST**, 0 regressions. RL now 26 baselined of 35.

