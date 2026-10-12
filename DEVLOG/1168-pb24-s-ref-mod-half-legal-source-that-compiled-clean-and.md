## Entry 1168 — 2026-08-04 04:10 PDT — PB24's ref-mod half: legal source that compiled clean and threw, fixed by deleting an error arm

**First item off the new `kb/Work/` register, and the register picked it: `work.py next` ranked PB24 on
`crashes · silent · wrong-answer`, not on its `[MAJOR]` label.** All four of its claims were re-measured before
any code changed. Three held. One did not.

**✅ THE REF-MOD HALF — LANDED.** `FUNCTION LENGTH(WS-NAME(1:5))` is conforming source: §15.50.3 r1 admits "a
data item of any class or category" and §8.4.3.3.3 makes a reference-modified item one (both `cite.py --check`ed).
It bound to a `BoundExprError`, which renders as a NotImplemented guard — **compiled clean, threw at run time,
exit 127.** The PB7/DA7 wrong-stage family, except with nothing to deliver at either stage: the source is legal
and the answer is computable.

⭐ **THE FIX ADDED NO MACHINERY, AND FINDING THAT OUT COST ONE PROBE.** `IntrinsicRenderer`'s `Length` arm renders
its argument through the ONE string channel, and a reference-modified place renders as the **substring** — so the
runtime length over that image already IS §15.50.4's character-position count. Changing the arm from
`BoundExprError` to the runtime `BoundIntrinsicCall` was the entire change, and **all three ref-mod forms fell out
together**: literal `(1:5)` → 5, runtime-bounds `(I:L)` → 6, omitted-length `(15:)` → 6, single position → 1. That
is why the golden pins all four rather than the one form the defect was reported against — they share one path,
so a golden over one leaves three free to regress.

⚠ **AND THE FIRST TEST RUN WAS A FALSE NEGATIVE I ALMOST BELIEVED.** The build had failed on a corrupted
`obj/.../Cobol.Net.Runtime.dll` ("PE image doesn't contain managed metadata"), so the probe ran the STALE compiler
and reproduced the original exception. `feedback_fresh_build_before_no_build_test` exists for exactly this, and
the tell was in the output I had already scrolled past. Clean, rebuild, re-probe.

**⛔ WHAT IS STILL OPEN, MEASURED RATHER THAN ASSUMED.**
· **A variable-length group is SILENTLY WRONG** — `X(4)` + a `DYNAMIC LENGTH` child holding `"XYZ"` returns **4**
  where §15.50.4 r7 requires **7**. Root cause located: `BindLengthFold` has arms for ref-mod, ODO, ANY LENGTH,
  national dynamic-length and dynamic-length ELEMENTARY items, and **no arm for a GROUP with a dynamic
  descendant** — so it falls to the fixed fold, and `DataItem.ImageWidth` returns 0 for such a child. A missing
  arm in a dispatch, again. ⚠ The cheap fix was tried and does not work: the whole-group image of such a group is
  itself staged loud, so this needs a purpose-built r7 sum.
· **`PHYSICAL` does not exist** — `COBOLNET1504: takes 1 argument(s); 2 given`. §15.50.2's format is
  `FUNCTION LENGTH ( argument-1 [ PHYSICAL ] )`, an optional KEYWORD being parsed as a second argument. Rejects
  legal source.
· **§15.50.4 r9 rounding — NOT MEASURED.** Reachable only through boolean/bit items. Recorded as unverified
  rather than as absent, because "I did not look" is not "it is missing".

**⛔ ONE CLAIM DID NOT SURVIVE.** The entry said the variable-length case covered **OCCURS DEPENDING** and was
silent. An ODO group is **loud** — it raises `NotImplementedCobolFeatureException` naming §15.50.4 r7. The silent
wrong answer is specific to a **dynamic-length** child (§8.5.1.12), which is what the entry's own citation said
while its prose said otherwise. Corrected in the work note rather than carried forward. That is the fourth queue
entry this session whose summary did not survive its own repro.

**GATE.** Conformance corpus 423/423 (the new golden included) · Intrinsic+Length+RefMod 188/188 ·
characterization 33/33.

