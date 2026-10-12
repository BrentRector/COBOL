## Entry 956 — 2026-07-21 16:55 PDT — Wave D (cont.): FLAG flagging Incr 3 (part) — i REF-MOD-ZERO-LENGTH (tri-state directive + EC-BOUND-REF-MOD TurnState)

The first state-coupled option. i (§7.3.15.4 GR4 i; E.2 item 23) flags a reference modification ONLY when the
`>>REF-MOD-ZERO-LENGTH` directive is UNSPECIFIED at the site (neither explicit ON nor OFF) AND EC-BOUND-REF-MOD
checking is on (a zero-length result would then raise the exception). Threaded `GroupBindContext.Session`'s
`RefModZeroLengthState` + `TurnState` into `FlagConformancePass`.

* **Tri-state:** `RefModZeroLengthState.IsUnspecifiedAt(line)` added — the distinction `IsOnAt` cannot make (it
  folds absence to OFF): true iff NO toggle precedes the site.
* **EC state:** `TurnState.Enabled("EC-BOUND-REF-MOD", null, line)` — the EC-hierarchy-aware fold the EC model uses.
* **The parse node — a real bug caught by probing:** my first cut hooked `refModSpec` (the default-mode ref-mod),
  but a ref-mod on a DATA REFERENCE (`MOVE W(2:2)`) parses through `subscriptOrRefMod : subToken+` where a
  `SUB_COLON` sub-token marks the ref-mod (the grammar leaves subscript-vs-refmod to the binder, mirroring
  `ReferenceResolver.InterpretSubscripts`). So the common case never hit `VisitRefModSpec` and i silently didn't
  fire. Fixed: handle BOTH paths — `VisitRefModSpec` and `VisitSubscriptOrRefMod` (when a direct `subToken` is a
  `SUB_COLON`). (Lesson reinforced: verify by RUNNING, not by the plausible-looking node.)

**Gate.** Build 0W/0E · CLI-probed (unspecified + EC on → 1621; `>>REF-MOD-ZERO-LENGTH OFF` specified → none; no
`>>TURN EC-BOUND-REF-MOD` → none) · 41 FlagDirectiveTests (3 new) · characterization 33/33 byte-exact. **Detectors
done: 11 of 19.** NEXT (Incr 3 remainder) = d MOVE-TO-SAME-NAME (same-DDE via name resolution) + e
RANGE-EXCEPTION-FOR-INDEX (SET-index + EC-RANGE-INDEX), then Incr 4 (new-analysis).

