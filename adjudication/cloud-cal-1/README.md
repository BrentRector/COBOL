# Cloud adjudication calibration #1 — Annex A.1 (10 DOC rows)

- **Pinned sha:** `5e0f687ed89f646d0ceb649683618040ef55d211` (main)
- **Inputs:** `python scripts/spec/phase_b_batch.py A.1 --max-rules 10 --out /tmp/adj/in` (the generator wrote 144 files; only the ten named in-<slug>.json files were used).
- **Files:** `adjudicate.jsonl` (first-pass verdicts) · `findings.json` (defects by mechanism) · `refute.jsonl` (independent refuter over the CONFORMS rows) · `final.jsonl` (after overturns — none, so identical to adjudicate.jsonl).

## Verdicts (final)
CONFORMS 3 (125, 170, 128) · DIVERGES 6 (12, 20, 74, 213, 153, 197) · NOT-IMPLEMENTED 1 (164). Refuter overturns: 0 of 3.

## Wall-clock per phase
| Phase | Seconds |
|---|---|
| Setup + `dotnet build CobolSharp.sln -c Debug` | 53 |
| Adjudication (inputs, spec, code, probes, cite.py, write-up) | 393 |
| Refute (one subagent, 3 rows) | 231 |
| **Total** | 677 |

## Cost notes
- Token/turn counts are not exposed to the orchestrating session; roughly 45 tool calls in the main session plus one refuter subagent.
- Rows were written to adjudicate.jsonl in one batch once all ten were decided, not appended one at a time as each was decided.
- The dossiers' `determinations` and `register` lists were the same generic A.1 hits for all ten subjects (no subject-specific lines). Every per-row owner (A11, PB1397, PB1092, PB538, PB643) was found by grepping kb/Work by hand, which is the PB372 blind spot.
