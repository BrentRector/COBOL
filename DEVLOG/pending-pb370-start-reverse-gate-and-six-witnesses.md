## PB370: the reverse START operators are gated below 2002, six witness rows close, and GR11 finds a MOVE evaluation-order defect

kb/Work PB370 held seven `PARTIAL` rows from lane-3 batch b1. Each row's behaviour had been derived from the rule
text and measured correct, but the rows had nothing on disk that could fail: three witnesses asserted less than
their rule, three were legacy-baked, and one shape could not be built. The refuters had added real findings to four
of the rows. Re-probed on the w1091e build, every measured behaviour still held. Two of the refuter findings had
already been fixed elsewhere: kb/Work PB1010 allowed DECLARATIVES inside a method (GR-14.9.49.4-5 and -15), and
kb/Work PB321 stopped a refused '61' OPEN OUTPUT from truncating the file (GR-14.9.27.4-25). kb/Work PB344's
determination that §9.1.14 is the same in every edition answers the editions objection on GR-14.9.30.4-29.

**One finding was a live edition defect, and this branch fixes it.** §14.9.41.4 splits the START KEY operators by
search direction (GR9 a)/b) for a relative file, GR17 c)/d) for an indexed one), and §A.3 item 39 binds the reverse
set (LESS, NOT GREATER, LESS OR EQUAL) to READ PREVIOUS as one reverse-access capability. The compiler refused READ
PREVIOUS, START FIRST/LAST and WITH LENGTH at `--std 85`, but it accepted `START F KEY < K` there without a
diagnostic. The new construct `start-reverse-operators-2002` (COBOLNET0900 below 2002) is checked on the parse arm
`VersionConformancePass.VisitStartKeyPhrase`, next to its two START siblings. The written operator is folded by
`ConditionBinder.MapOperator`, the same fold the binder applies to that START, so "reverse" is defined once: NOT
LESS and NOT < fold to `>=` and stay legal at 85. The 1985 edge is derived rather than quoted, because the repo
holds no 1985 text: A.3 item 39 ties the operators to READ PREVIOUS, and the 459 NIST CCVS-85 programs use only
EQUAL, GREATER and NOT LESS. The edition sweep found no test or golden that uses a reverse operator below 2002.

**Goldens.** Each expected value was derived from the rule text before the first run, and every golden passed the
first time. `2002/pb370_start_relative_reverse` covers GR9; its `KEY < 5` leg must return RRN 4, not 2, which tells
a reverse search from a forward one. `2002/pb370_start_indexed_reverse` covers GR17 with a partial key and a
reverse leg. `85/pb370_start_forward_operators` is the complement that shows the gate does not over-reject, and
`negative/pb370-start-reverse-operator-85` uses the NOT GREATER and `<=` spellings. `85/pb370_read_relative_invalid_key`
covers GR29: its RRN 2 READ is followed by a READ NEXT that returns RRN 4, which shows the file position indicator
is the RELATIVE KEY value; it also covers '23' with INVALID KEY and §9.1.14 3)'s USE leg. `85/pb370_use_file_beats_mode`
and `2002/pb370_use_file_beats_mode_method` write the mode USE before the file USE and include a control file.
`2023/pb370_open_unsuccessful_order` covers GR25 a) to d) across a format 1 USE, a format 3 USE and an
exception-checking PERFORM. `2002/pb370_exception_object_identity` compares EXCEPTION-OBJECT with the raised object
and with a second live instance, in a program and in a method. `oo_ec_raise_object`, which only tests NOT = NULL,
is retired from GR15's row.

**Rows.** Batch `w1091e-pb370` re-verdicts six rows PARTIAL → CONFORMS, and GAP goes from 98 to 92. PB370 stays
open on GR-14.9.49.4-11 alone. kb/Work PB367b answered the question that row was blocked on, and the nonfatal-first
shape now holds. A fatal-fatal shape fails, though: `MOVE X(1:L) TO T(I)` with both operands out of range selects
EC-BOUND-SUBSCRIPT from the receiving item, but §14.9.25.4 GR1 evaluates the sending reference modifier first. The
emitted `CobolTable.At(...) = CobolString.Store(CobolString.RefMod(...))` evaluates its ref-returning left operand
first. That is a MoveEmitter evaluation-order defect outside this mechanism, and it is reported as a lead.

**Self-review.** The new code has no hot path because it runs at compile time. No rule is duplicated, since the
operator fold is reused rather than re-spelled. The one construct is checked from one arm.
