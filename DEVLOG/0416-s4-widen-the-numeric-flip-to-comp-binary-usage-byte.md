## Entry 416 — S4: widen the numeric flip to COMP / BINARY usage (byte-identical) + a `--typed-fields` CLI flag

With digit-count and byte-width decoupled (415), the numeric flip widens to non-DISPLAY usage — but only where the
byte path's storage semantics match the `long` model. Verified that empirically first (compiled probes on the byte
path): `MOVE 1234567` into `PIC 9(5)` shows `34567` for COMP, COMP-4, and BINARY (truncated to the PICTURE digit
count, `% 10^5`) and the value is genuinely *stored* truncated (`IF WS-COMP = 34567` is true; `ADD 1` → `34568`).
COMP-5 shows the full `1234567` (native binary range, NO picture truncation) — different semantics.

So `IsTypedUnsignedInteger` now admits `USAGE DISPLAY | COMP | BINARY` (still unsigned, integer, ≤18 digits, has
VALUE), and excludes COMP-5, COMP-1/2 (float), and packed (COMP-3). The typed field's `Width` is now the BYTE
storage width (`loc.Length`, e.g. 4 for `9(5) COMP`); the digit count comes from the PicDescriptor — so the
materialize/encode/decode use the right window size while truncation/DISPLAY use the right digit count. Everything
else (materialize, COMPARE, arithmetic) already flows through the usage-agnostic byte codec, so no per-usage cell
work was needed. Confirmed by reflection that `_T_WS-COMP/COMP4/BIN` emit as `Int64` while `_T_WS-COMP5` is absent
(stays byte).

Added a permanent `--typed-fields` experimental CLI flag (sets `CompilationOptions.EnableTypedFields`) so the
migration's typed path can be driven/inspected from `cobolsharp` directly — output stays byte-identical. New flip
test `NumericComp_FlipsToTypedLong_AcrossAllOps_ByteIdentical` (VALUE/MOVE+truncation/DISPLAY/COMPARE/arithmetic on
COMP+BINARY) — 13 flip tests, all flag-on≡flag-off. Guard **ALL GREEN: 1196 unit / 494 integration / 364 NIST**.
NEXT: signed + scaled numerics → `decimal` (DISPLAY via materialize→GetDisplayString to stay byte-identical), then
OCCURS → `CobolTable<T>`.

