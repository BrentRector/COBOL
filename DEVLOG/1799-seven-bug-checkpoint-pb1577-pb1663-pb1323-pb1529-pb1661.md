## Entry 1799 — 2026-09-29 20:57 PDT — Seven-bug checkpoint: PB1577 + PB1663 + PB1323 + PB1529 + PB1661 + PB1365 + PB1685

A batch committed as one unit for the WSL Linux gate (owner: 10-20 bugs per batch, each tested individually).

- **PB1577** — a symbolic-character name as a function argument now binds as a literal of its value (`IntrinsicBinder.BindArgOperand`).
- **PB1663** — INSPECT over a hexadecimal alphanumeric literal: golden only, it already conformed once PB1125 landed.
- **PB1323** — recorded as closed by PB1145 (`ProcedureHeaderScreen`).
- **PB1529** — `CobolTiming.WholeSeconds` clamps a huge interval to the maximum meaningful suspension (unit Theory).
- **PB1661** — an EVALUATE subject that is an index data item: the sending-value clone is stored through `BoundSetTo`.
- **PB1365** (half) — TURN refuses a word after `CHECKING ON|OFF [WITH LOCATION]` (COBOLNET0718, §7.3.3 SR3); the implied-ON leg is owed and needs the diagram page rendered.
- **PB1685** — the report-writer SUM counter took `pic.Scale`, which is 0 for a numeric-edited picture, so `PIC 99.99 SUM` of 2.75 twice printed 04.00 for the 05.50 owed (§13.18.54.4 1)). The receiver-scale rule moved from `RuntimeApi.ReceiverScaleOf` to `PicInfo.ReceiverScale` and the SUM binder, MOVE and arithmetic emitters all ask it.

