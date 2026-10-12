## Entry 1016 — 2026-07-23 10:22 PDT — Conformance fix-queue: CA19 + CA20 (inspect-string) — UNSTRING receiver (SR4) and sender (SR2) category screens (ISO §14.9.48.3)

**CA19 (MINOR/S) + CA20 (NIT/S), both in `StringUnstringBinder.cs`, landed together** (independent-minors batch item 4 —
same file, one corpus sweep covers both). Both enforce a §14.9.48.3 category syntax rule the binder had been letting
slip through, using the codebase's established STRING-side convention (a `BoundUnsupported` ⇒ `StatementEmitter` LoudStmt
naming the SR; the program compiles but louds at runtime rather than extracting silently — the §4.2.2 flagging duty).

**CA20 — SR2 sender.** ISO §14.9.48.3 SR2: identifier-1 (the sender) shall be category alphanumeric or national. The
guard at `:94` only rejected a numeric sender whose usage was NOT display (`Category: Numeric, Usage: not Display`), so a
usage-DISPLAY numeric sender (e.g. `PIC 9(5)`) slipped through and its sign-carrying zoned image was examined as
characters. FIX: reject all `PicCategory.Numeric or NumericEdited or Boolean` senders (drop the DISPLAY exemption; the
numeric-edited and boolean arms close the adjacent SR2 gaps) + fix the stale doc-comment.

**CA19 — SR4 receiver.** ISO §14.9.48.3 SR4: identifier-4 (an INTO receiver) shall be usage display + category
alphabetic/alphanumeric/numeric, or usage national + category national/numeric. `BindUnstring` resolved the receiver
with NO category screen (only COUNT IN got an integer check), so the emitter affirmatively compiled+ran the illegal
shapes (numeric-edited via the edit-mask arm, packed/COMP numeric via the numeric arm). FIX: after the receiver resolve,
`if (target is not RefModPlace && !target.Item.IsGroup && !UnstringReceiverAllowed(target.Item.Pic)) return
BoundUnsupported(… SR4)`, with the new `UnstringReceiverAllowed` whitelist encoding SR4 exactly —
`{Alphanumeric, EditMask:null}` (display alphabetic/alphanumeric, non-edited) ∪ `{National}` ∪ `{Numeric, IsFloat:false,
Usage: Display or National}`. A fixed-length group (SR10) and a reference-modified slice are alphanumeric-image receivers
and are exempted before the screen; National stays allowed (SR4-legal; the emitter defers it separately).

**Golden = a conformance UNIT test** (`UnstringSrCategoryTests`), not a corpus `.cob`/`.out`: a `BoundUnsupported` is a
runtime-loud (compiles, throws naming the feature), so it is neither a positive `.out` nor a compile-time negative —
mirroring `TierCRejectionTests`, each case compiles+runs and asserts `!ok` + the SR substring in the loud detail. Six
reject cases (numeric-edited + packed receivers → SR4; display-numeric + boolean senders → SR2) and three **positive
controls** proving the screen is category-specific, not over-broad (display-numeric + alphanumeric receivers, and an
alphanumeric sender, still bind and run). Every case spec-derived from SR2/SR4.

**Regression sweep (these turn previously-accepted source into rejections, so the sweep is load-bearing):** the NIST
UNSTRING programs NC217A + NC218A both PASS — their receivers are `X` (alphanumeric) and `DU/DS…V0` (display numeric,
SR4-allowed) and their senders are `XN`/`GRP` (alphanumeric/group), none in the rejected set; the 85-corpus
`phrase_order_overflow` uses an alphanumeric sender and receivers; the 10 `StringUnstringDifferentialTests` (display
numeric + alphanumeric receivers, COUNT IN display-numeric) all pass. **Gate:** 51 string/unstring/inspect tests green
incl. the 7 new; Debug build clean. Re-scout `wf_a09670d5-cdc` confirmed both valid with zero anchor drift.

