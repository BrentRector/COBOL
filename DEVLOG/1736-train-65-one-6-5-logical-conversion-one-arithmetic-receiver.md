## Entry 1736 — 2026-09-27 12:27 PDT — Train 65: one §6.5 logical conversion, one arithmetic receiver classification, CODE-SET membership at the WRITE, CONSTANT literal as written

Four wave-65 clusters landed as one train: B (PB1491 + PB1493), C (PB1142), D (PB1150 + PB1542) and F (PB1230).
GAP 1061 → 1047, with 14 rows closed.

**B — PB1491 + PB1493: the §6.5 logical conversion is ONE pass over lines.** The fixed-form converter was a
character buffer. It joined each continuation onto "the last line written" by stripping that line's newline, and it
had two literal scanners that disagreed. The re-probe reproduced every failure the notes described. A blank line
between the parts of a continued literal crashed the compiler ("8 origin(s) for 9"). A comment line in the same place
gave COBOL0001. A comment line between PIC and X(4) gave COBOL0307. `X*>` compiled silently, and so did `*>` at
`--std 85`. A short record lost the trailing spaces of its continued literal. The fix is
`ReferenceFormatProcessor.LogicalConversion.cs`. A continuation joins the LATEST LOGICAL line, tracked by index. A
comment line or a blank line keeps an empty slot and never resets an open literal (§6.3.5 2), §6.5 2) and 6)). The
program-text area is always padded to margin R (DOC-A.1-157). One scanner, `ScanProgramText`, ends a literal only at
the quotation symbol that opened it. Comments are removed in both reference formats, so no later stage sees
comment-text. The same pass enforces §6.2.3.2 SR2 (COBOLNET2495), SR3 (COBOLNET2496) and the fixed-form 2002 gate on
`*>` (VCR 7.23). The gate applies to fixed form only because below 2002 free form exists only through the
DOC-A.1-158 auto-detection extension, where `*>` is the only comment. One claim in the notes did NOT hold:
`REPLACE ==AAA== *>junk BY ==BBB==.` was supposed to be legal, but §6.3.7.3 puts BY inside the comment, so
COBOLNET2449 is correct. Goldens: 85/pb1491_continued_literal_logical_line and the two 2002/pb1493_floating_comment
twins. Negatives: three. Unit tests: 20. Eight rows closed. Self-review found an SR2 diagnostic column that is off by
one in the doubled-quote-at-margin branch; only the reported position is affected. ⚠ The branch left both notes at
`status: open` although the report said they had landed. I flipped them to `landed`, as the report stated.

**C — PB1142: the arithmetic operand screens listed operand ARMS by hand, and missed the inline invocation.** Each
list omitted the arm PB428 added to the grammar. As a Format-1 receiver, an inline method invocation was silently
DROPPED by ADD, SUBTRACT and MULTIPLY and crashed DIVIDE's emitter. As a sender, §8.8.1.1's class screen covered
only BindPrimary's function arm, so under STRICT an alphanumeric result was digit-decoded through three other paths:
the operand walk's function arm, both inline-invocation arms, and the keyword-omitted function. Examples: `MULTIPLY
FUNCTION UPPER-CASE("12") BY B` → 39.6, and `COMPUTE C = UPPER-CASE("12") + 1` → 13. The note held and turned out
wider than it said. Receivers are now defined POSITIVELY by one `ArithmeticOperandRole`, which all four verbs ask
(COBOLNET1689); `ArithmeticOperandRoleDriftTests` pins it against the .g4 and was proven by a mutant that turned 5
cases red. Senders take one `ExpressionBinder.BindIdentifierOperand` / `ScreenIdentifierOperand` (COBOLNET0844). A
sole comparand keeps its short-circuit through `SoleInlineInvocation`. Golden 2002/pb1142_arithmetic_identifier_operands,
two negatives, and the `ArithmeticIdentifierOperandTests` matrix. Rows FMT-14.9.26.2, SR-14.9.26.3-1 and
SR-8.4.3.4.3-1 are now CONFORMS.

**D — PB1150 + PB1542: a CODE-SET file refuses an unrepresentable character at the output STATEMENT.** The one R47
screen answered false for any file with a CODE-SET clause. An EBCDIC indexed WRITE of `€` answered '00' and then
crashed at CLOSE, and a STANDARD-2 WRITE of `é` put X'E9' on the medium with '00'. The re-probe reproduced both
notes as written. The screen now asks the file's coded character set (`CodeSetConversion.Represents`). ISO 646 sets
carry their 128-entry identity, which is automatic for any identity set smaller than the channel. The channel
encoding is a bijection, so a keyed store re-persists untouched records byte-exactly. Line sequential takes the same
set as its ceiling ('71' / '09'). Status is '91', the condition R47 already names; EC-DATA-CONVERSION was rejected.
Goldens: 85/pb1542_code_set_unrepresentable_85 and 2023/pb1542_code_set_line_sequential. DOC-A.1-187 is now
CONFORMS.

**F — PB1230: a CONSTANT carries literal-1 as written.** The evaluator dropped a leading `+` and stored normalized
dot-decimal text, so `AS +5` displayed `5` and `AS 1,5` under DECIMAL-POINT IS COMMA drew COBOLNET0895. §13.10.3 SR9
compared folded values, so `AS 5` / `AS 2 + 3` compiled. Now `CtNumber.Literal` and `ConstantDef.Text` hold the
source text, and integer positions read `ConstantDef.IntegerText`. The VALUE arm normalizes a substituted constant
as it would the written literal; fixing only the procedure half crashed the backend. SR9 compares the written
specifications, built from the operand's tokens so a comment is excluded, through the one §7.2.3.4 9) c) matcher
(`TextWordSequence`, which COPY REPLACING now shares). Golden 2002/pb1230_constant_literal_as_written and negative
pb1230-constant-dup-same-value. SR-13.10.3-1 and SR-13.10.3-9 are now CONFORMS.

**The train.** Every cluster came in by `git apply -3` from its branch diff. There were two conflicts, both whole
elements of a golden manifest (2002: B|C; negative: B+C|F), and I kept both sides. The traceability inventory merged
cleanly. I verified it by dry-running all four implementers' `record_verdicts` batches against the merged tree: 0
rows changed, and B's retirements are already applied. There were no conflict markers in the index. The generators
(diagnostics doc, constructs, VCR, drift-rules index) reproduce the merged files. The gate is ONE build, then the WHOLE
Conformance assembly unfiltered (filter `~CobolNet.Tests.Conformance`, which selects all 9104):
`Passed! - Failed: 0, Passed: 9104` in 10 m 42 s. Unit: `Passed: 29560`. Characterization: `Passed: 33`. The legacy
integration assembly: `Passed: 503, Skipped: 1`. The external GnuCOBOL corpus was fetched into the fresh worktree; it
did not fail. Semgrep verify: PASS, with every count equal to the baseline. For the lander's review pass I read every
source diff and checked the drift rules of each changed file and 14 citations with `cite.py --check`. The review
found 0 correctness findings, so no cluster was dropped. Leads filed:

- PB1640: library text skips every reference-format diagnostic.
- PB1641: `13.2 + FUNCTION SQRT(16)` gives 17.1. This is a WRONG ANSWER.
- PB1642: the .g4 rule-reference loader is duplicated across five drift tests.
- PB1643: DISPLAY under DECIMAL-POINT IS COMMA prints '.'. This needs an implementor determination.

The sequence-area auto-detection lead is PB1362's own mechanism, so I added its repro there. PB1492 carries a
re-probe note, because its apostrophe half is probably fixed by `ScanProgramText`. Diagnostic codes claimed:
COBOLNET2495 and 2496.

