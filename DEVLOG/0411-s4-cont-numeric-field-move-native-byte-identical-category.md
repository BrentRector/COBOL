## Entry 411 — S4 cont.: numeric field-MOVE (native, byte-identical) + category-correctness hardening of the typed MOVE cells

Two things. **(1) A latent correctness fix.** With numeric typed `long` fields now existing (410), the S3 typed
MOVE cells — which assumed the typed field is a `string` (`CobolString.Store`/`FromWindow`) — would mis-emit for a
numeric typed field (push a `long` where a `string` is expected → invalid IL). It was latent (flag-ON only, so the
guard never exercised it), but exactly the silent-mis-emit the quality bar forbids. All typed MOVE branches in
`CilDataEmitter` are now **category-correct**: `EmitMoveStringToField` and the typed↔byte field-MOVE branches are
guarded to typed **string** fields; `EmitPicMoveLiteralNumeric`'s typed branch to **numeric**; the both-typed
field-MOVE branch dispatches on category (string / numeric / mixed-fallthrough); anything unhandled falls through
to the loud `EmitLocationArgs` guard rather than mis-emitting. **(2) The feature it unlocks:** native numeric
field-MOVE — `MOVE WS-A TO WS-B` between two typed unsigned-integer `long`s lowers to `dst = src mod 10^n` (the
dst's digit count) — byte-identical to a numeric→numeric byte MOVE (high-order truncation; widening zero-extends on
display), with **no decode/encode** (the native payoff). Test (`TypedFieldFlipTests`): widening (3→5 digits) and
truncation (5→3) both byte-identical. Autonomous /loop tick, [[feedback_continue_dont_wait]]. Guard **ALL GREEN:
1196 unit / 491 integration / 364 NIST**. NEXT: numeric COMPARE (sender-materialize / native) then numeric
**arithmetic** (ADD/SUBTRACT/… via `CobolNum`, the large byte-identity-critical increment), then signed/scaled
(`decimal`) variants, OCCURS.

