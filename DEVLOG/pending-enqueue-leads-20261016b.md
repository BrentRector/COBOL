## Seven new defect notes filed from four implementer reports and the operator's survey lead, and one note appended

The clerk grepped each lead against `kb/Work/` first (MANDATORY-PRACTICES R1) and filed one note per mechanism (R3).

Filed:
- PB3261: MOVE evaluates the receiving subscript before the sending reference modifier, so the wrong declarative runs (PB370 round w1091e; holds GR-14.9.49.4-11 PARTIAL).
- PB3262: eleven conformance goldens use a relation of two literals (PB160 round w1091g; COBOLNET3146 only warns).
- PB3263, PB3264, PB3265: the three PB3242 leads (PB3160 slices carry no site block; `work.named_paths` scans whole bodies; `member_index.resolve` does not resolve dotted Python names), kept as three notes because each is a different reader or data set.
- PB3266: `cite.py#rule_path` numbers the trailing unnumbered paragraphs of §6.3.5 as `2)`, about 40 misnumbered citations (PB3168, carried; no existing note named it).
- PB3267: `prune_worktrees.py#contained` absorbs a FINDINGS-stamped branch into any successor that contains it, so its review finisher is never planned (operator steer 2026-10-16 07:50, item 4; PB3181 and PB3027 are landed and keep their text).

Appended:
- PB1546: `audit_code_citations.py --help` crashes on a cp1252 console because `parse_args()` runs before the stdout reconfigure (`scripts/spec/audit_code_citations.py:1105`); the note already owned the mechanism.

Unused ids: PB3268. No compiler, spec or code change; clause numbers inherited from the reports are marked as not re-derived where the clerk's `cite.py --check` did not pass.
