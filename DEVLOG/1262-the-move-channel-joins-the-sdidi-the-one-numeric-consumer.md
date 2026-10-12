## Entry 1262 — 2026-08-09 13:24 PDT — The MOVE channel joins the SDIDI: the one numeric consumer without the Dec store case — four more rows

The PB56 residue probes paid off: re-probing the pre-PB56 triage rows against the current tree found
most of the standard-mode routing family already closed by the Dec-carrier landing — EXCEPT the MOVE
channel, which reproduced its CS1503 on the spot: MOVE FUNCTION E under STANDARD-DECIMAL handed the
CobolDec NumX to the Int128 store overload, a backend compile failure on conforming source, while
COMPUTE was exact. MoveEmitter was the ONE numeric consumer without the Dec-carrier case (the
arithmetic path has carried the SDIDI overloads since D3). Both MOVE arms now land through the
§14.7 final transfer: the numeric-edited arm converts at the MASK's scale exactly as
ArithmeticEmitter's edited path does, and the plain numeric arm stores through the
Store(CobolDec, profile) overload via the new RuntimeApi.NumStoreDec (the nameof discipline — the
characterization bare-access guard stays green by construction, 33/33).

Golden pb65_dec_move_channel: MOVE FUNCTION E is the 31-digit truncation of the exact constant,
EXP10(30)/EXP10(15) is 10^15 exactly, and COMBINED-DATETIME of a COMP-2 argument agrees
byte-for-byte between MOVE and COMPUTE. FOUR rows re-verdict CONFORMS+tested (E and PI's
channel-agreement rows, EXP10's EAE row, COMBINED-DATETIME's two-channel row). Still open from the
residue set: BOOLEAN-OF-INTEGER's 63-bit bridge (RV-15.13.4-1), Power's receiver-bearing arm swap
(RV-15.64.4-1), and the native FromDouble clamp-sentinel family (RV-15.75.4-1 + the native leg of
EXP10) — refreshed on PB65. GAP 4,191 → 4,187; the defect backlog stands at 185.

