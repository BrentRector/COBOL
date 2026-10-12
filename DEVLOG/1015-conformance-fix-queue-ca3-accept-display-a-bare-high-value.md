## Entry 1015 — 2026-07-23 10:13 PDT — Conformance fix-queue: CA3 (accept-display) — a bare HIGH-VALUE/LOW-VALUE in DISPLAY/STRING renders the PROGRAM COLLATING SEQUENCE extreme, not the native pin (ISO §8.3.3.6.4 GR6/GR7); + a §12.3.7 k)5 alphabet-THRU bug discovered

**CA3 (MINOR/S, accept-display-misc) landed** (independent-minors batch item 3). ISO §8.3.3.6.4 GR6/GR7 (+ NOTE 2 —
DISPLAY is a runtime reference): a HIGH-VALUE/LOW-VALUE referenced at runtime is the character with the highest/lowest
ordinal position in the runtime **alphanumeric PROGRAM COLLATING SEQUENCE**, not the native pin. `OperandText.AsString`
routed a bare figurative to `AsStringVisitor.Visit(BoundFigurative)`, which hardcoded `FigurativeConstants.Fill(kind,
null)` → the native pin U+00FF/U+0000, ignoring the declared PCS — while the MOVE (`MoveEmitter`) and relation
(`ConditionRenderer`) paths both thread `ctx.Data.Collating`, an internal inconsistency and a GR6/GR7 divergence.

**Fix** (mirrors the intrinsic interception): a `BoundFigurative` arm at the `AsString` ENTRY —
`new string(FigurativeConstants.Fill(fig.Kind, num.Collating, null, num.NationalCollating), 1)` — with two new
pass-through properties `NumericRenderer.Collating`/`NationalCollating` exposing `ctx.Data.*` (the same tables the MOVE
and relation paths use). The interception is at the entry because the `AsStringVisitor` has no access to the renderer's
collating context; `cat=null` because a bare figurative in a DISPLAY/STRING/STOP value position is an alphanumeric value
(§8.3.3.6.4 GR1), so the alphanumeric PCS applies; width 1 (GR3b). The visitor's `Visit(BoundFigurative)` stays as the
unreachable native-pin fallback the visitor interface requires. **Byte-stable:** with no PCS declared `num.Collating` is
null and `Fill` returns the native pin, so characterization is 33/33 unchanged; only a declared non-native PCS shifts
output. **Golden** `2002/ca3_figurative_display_pcs` — `ALPHABET AL IS "ZYX…A"` puts 'Z' at the lowest ordinal position,
so LOW-VALUE = 'Z' (§8.3.3.6.4 GR7 + §12.3.7 k)1.b); MOVE LOW-VALUE and bare DISPLAY LOW-VALUE now BOTH print 'Z'
(pre-fix bare = X"00"). The expected value is SPEC-DERIVED; `--run` only verified the fix attains it. **Gate:** golden
compile-strict+run+match; manifest 5/5; collating/figurative neighbor sweep + characterization 33/33 green.

**⚠ Discovered while probing CA3 (a SEPARATE bug, logged as a new queue candidate DA1, NOT worked around):** an
`ALPHABET AL IS X"FF" THRU X"00"` used as the PROGRAM COLLATING SEQUENCE leaves the collating table at the native pins
(HIGH-VALUE stayed X"FF", LOW-VALUE X"00") instead of reversing — a **§12.3.7 k)5** violation ("the native run from
operand-1 to operand-2, either direction, ascending positions": X"FF" THRU X"00" must descend, putting X"FF" at position
1 and X"00" at position 256, so HIGH-VALUE = X"00"). A plain string-literal alphabet ("ZYX…A") reorders correctly, so
the defect is isolated to the `THRU`/hex-operand arm of `DataBinder.AlphabetBind` (likely `AlphabetOperands` not decoding
a hex literal to a single-char operand). CA3's golden deliberately uses the working string form; DA1 is filed for its own
spec-first fix.

