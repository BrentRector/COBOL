## Entry 643 — 2026-07-06 20:23 PDT — Fix E (the deferred M2-OO-1h finding): a Tier-B REDEFINES over a fixed-OCCURS-VALUE entry now seeds EVERY occurrence (§13.18.63 GR9)

The 5th confirmed M2-OO-1h review finding, deferred from 642 as a pre-existing shared bug: `FieldEmitter.ImageInitOf`
built a Tier-B REDEFINES backing's initial image from a fixed-OCCURS canonical using **one** occurrence's VALUE, not
all of them. For `01 G. 05 E PIC XX OCCURS 3 VALUE "AB". 01 V REDEFINES G PIC X(6).` the backing seeded to `"AB"`
(then space-padded to the class width → `"AB    "`) instead of `"ABABAB"`. ISO §13.18.63 GR9 is explicit: *every*
occurrence of a table item with a VALUE takes that value at initialization — so the canonical's whole-image is the
per-occurrence image repeated `OCCURS` times, and the Tier-B backing (which seeds from the canonical's image) must
carry all of it.

Root fix, at the one place the image is built (not at each caller): `ImageInitOf` is now a thin wrapper around the
former body (renamed `ImageInitOfOne`) that multiplies a fixed-OCCURS entry's one-occurrence image by its count via
the new `CobolString.Repeat(image, n)` runtime helper. The recursion runs through the wrapper, so a nested OCCURS
inside a group image repeats too. This is a shared FieldEmitter path — it also governs program-scope Tier-B backings
— so it took the full-battery check the deferral called for.

Regression: `RedefinesTierBDifferentialTests.FixedOccursValue_SeedsEveryOccurrence` (the example above → `ABABAB`),
pinned via `AssertSameAsLegacy` — the legacy byte engine lays each occurrence's VALUE into storage at init, so it is
the correct oracle here and the greenfield now matches it byte-for-byte. Battery: **1959 conformance** (+1) · **216
unit** GREEN, zero regressions across the shared path. This closes all 6 confirmed findings from the 642 review.

