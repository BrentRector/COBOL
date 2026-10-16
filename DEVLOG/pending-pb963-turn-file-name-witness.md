## PB963: >>TURN file-name-1 ON and OFF arms get their spec-derived golden

kb/Work PB963 (analysis) said that the file-name-1 halves of ISO §7.3.25.4 GR6 ("if file-name-1 is specified, checking
is enabled only for exception conditions associated with that file-name", `cite.py`: OK §7.3.25.4 6)) and GR8 (the
same words with "disabled", OK §7.3.25.4 8)) had no golden witness. Wave 1095 H re-probed both of the note's programs
on its own build. The ON probe leaves F2 unchecked and raises EC-I-O-PERMANENT-ERROR for F1. The OFF probe leaves F1
unchecked and runs F2's declarative, then terminates as §14.6.13.1.3 5) requires. Both behave to the rule, so this
landing changes no compiler code; it adds the witness.

`conformance:2002/pb963_turn_file_name_arms` (PB963A) reads two empty files to their at end condition in eight
phases. Each phase reopens both files and runs `SET LAST EXCEPTION TO OFF` before each READ (§14.9.39.4 28)), so
EXCEPTION-STATUS in the AT END phrase shows only that READ's raise (§14.9.30.4 24) a); §15.33.3 1)). The phases cover:
- GR6 for one file, then a second file added while the first keeps its state;
- GR8 for one file;
- GR8 with GR3's level-2 expansion (`EC-I-O F2 OFF` turns EC-I-O-AT-END off for F2 only);
- a file-less ON ending a file-scoped OFF;
- `F1 F2 ON` followed by `F2 OFF`;
- GR5 (OK §7.3.25.4 5)) with a file-name TURN between `READ F1` and its `AT END`. That READ stays unchecked. The READ
  nested in the phrase and the statements after it are checked, and F2 stays unchecked.

Every expected line comes from the rules, not from a run. The negative `pb963-turn-file-name-below-2002` is rejected
with COBOLNET0900 at 85, and it compiles there once the directive is removed.

Rows: PB1381 had already re-verdicted GR-7.3.25.4-6 and -8 CONFORMS, with only unit witnesses for the file-name
halves. Batch `w1095h-pb963` adds the goldens to -3, -5, -6 and -8 (+7 test-ref, GAP 85 → 85). Batch
`w1095h-pb963-notes` corrects the "test-ref left empty" notes on -3 and -5. PB963 is flipped to landed with closes_rows
-5, -6 and -8. Fixed in passing: the header of `2002/l1c33_turn_level2_file_name` cited §9.1.13 and §14.6.13.1 for
text those clauses do not contain. It now cites §9.1.13.1, §14.6.13.1.1 and §15.33.3 1), all checked.
