## Entry 1399 — 2026-08-30 13:12 PDT — PB176: the seventh two-arm dispatch — and the skeptics who kept the mirror honest

The shape the project's memory calls its most reproducible defect, one more time: WriteGroupImage (the
receiving half) has carried a capability guard for phases while GroupImage (the sending half, THE ONE
reader) emitted .AsImage() unconditionally. PB164's VLG probes constructed the witness — a group with
BOTH an OCCURS DEPENDING table and a dynamic member takes the ODO sender over a struct that never
receives AsImage, and legal source failed BACKEND compilation with CS1061 — proven pre-existing at
4006248f by stash/build/probe/pop before a line changed. The fix is the mirror: the same guard, in the
read switch.

Except a mirror can be hung wrong, and two read-only skeptics dispatched on the one substantive risk
question each earned their tokens. The first BROKE the cut: the guard sat before the OdoGroupPlace
unwrap, and CallStringRead — alone among callers — hands the wrapper in, so a Tier-B WINDOWED ODO group
with a dynamic member would have traded its valid window read for a loud throw on the Format-2
CALL/RETURNING/INVOKE paths. Unwrap first, guard second: the MemberPlace inner meets the guard with the
exact predicate (fires iff the struct lacks AsImage), the window shapes keep their arm. The second
PROVED the rest sufficient — the predicate lattice (IsImageCapable implies ElementImageCapable) makes
every guarded site over-refuse at worst, and the CurrentImage gate is arm-for-arm equivalent to its
emission condition — and then enumerated what the sweep had NOT reached: the day-old DISPLAY arm
missing the window-shape test (fixed in-change), the OO method formals emitting image methods with no
gate at all, CallStringWrite's string-into-struct fall-through (the WRITE twin of this very defect,
CS0029), and AsStorageImage spelling .AsImage() against the ONE-reader law stated in GroupImage's own
doc. Registered PB177 (the boundary family plus the missing bind screens) and PB178 (the AsStorageImage
window arm) — found by review, not by a user's program.

The rejection lock pins the restored posture (compiles, throws Tier-C, names the dynamic mechanism);
the sibling sweep confirms every other direct .AsImage() emission guarded; one stale SortEmitter
comment corrected. Wave gate GREEN (Conformance 1354, Unit 5027, characterization 33). Battery #36
accrues (PB168 + PB164-VLG + PB176) — due next, then the queued USAGE INDEX owner question.

