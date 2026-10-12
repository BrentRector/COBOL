## Entry 1181 — 2026-08-05 10:47 PDT — one operand rule written five times; the grammar half of the fix shipped a silent wrong answer, and my own harness lied first

**PB45, two thirds of it.** ISO **§8.4.3.1.2 Format 1** makes a function-identifier an identifier, so it is
admissible wherever a format writes `identifier-n | literal-n` in a SENDING role. The entry filed three positions;
the sweep it told me to run found the real shape.

## ⛔ IT WAS FAMILY-WIDE, AND THE CAUSE WAS ONE RULE WRITTEN FIVE TIMES

Every arithmetic verb in every format rejected it — ADD, SUBTRACT, MULTIPLY, DIVIDE (both operand positions) —
while COMPUTE accepted it because its RHS is an `arithmeticExpression`. The grammar wrote the sending operand
FOUR TIMES (`addOperand` / `subtractOperand` / `multiplyOperand` / `divideOperand`, each `dataReference |
literal`) and `inspectChar` was a fifth copy. All five omitted `functionCall`.

## ⛔ THE GRAMMAR HALF ALONE PRODUCED A SILENT WRONG ANSWER

Adding `functionCall` made it parse — and return the wrong number. `BindOperandExprCore` walks the operand wrapper
breadth-first for something bindable and had **no `FunctionCallContext` arm**, so it descended INTO the call and
bound its ARGUMENT: `ADD FUNCTION SQRT(W-Z) TO W-R` with `W-Z = 4` added **four** instead of **two**. It compiled.
It printed a plausible number. Nothing failed.

I nearly accepted it: my sweep printed `R=0005` and the shapes all "worked". It was caught by doing the thing the
rule exists for — comparing against the value DERIVED from the spec (`SQRT(4)` = 2, so `1 + 2 = 3`) rather than
against "did something come out". `feedback_verify_output_is_correct` earned its place again.

⚠ The same missing arm in `InspectBinder` surfaced as a MISLEADING diagnostic rather than a wrong value: "a
numeric literal is not a valid INSPECT literal", for an operand that is neither numeric nor a literal. A
fall-through tail explaining the LAST case it checked, not the case it met.

⚠ And `BindOperandExprCore`'s tail returns `new BoundNumLiteral("0")` — a wrapper holding nothing bindable
silently becomes ZERO. That is the same defect shape one level up, still live; it is now commented as such, with
the drift test named as what keeps the grammar from growing a shape the walk cannot see.

## ⚖ FOUR RULES STAY FOUR — A RECORDED CONSTRAINT, NOT A PREFERENCE

I collapsed them into one `arithmeticSendingOperand` and **reverted it**: the FROZEN legacy compiler shares this
grammar and reads `.dataReference()` / `.literal()` off `AddOperandContext` and friends BY NAME, so both a
collapse and an alias break its build — the same freeze that holds D10 until CUT 2. So the change is ADDITIVE, and
`ArithmeticSendingOperandDriftTests` pins the four alternative sets identical so they cannot drift while they must
stay separate. It also asserts the RECEIVING rules do NOT admit one (§8.4.3.2.3 SR1), so a careless "add
functionCall everywhere" fails. Collapse at CUT 2 and delete the test with them.

## 🔬 MY OWN HARNESS LIED BEFORE THE COMPILER DID

The first family sweep used a `PROGRAM-ID` containing an underscore. Every case failed with `COBOL0307` — my test
program, not the operand — and I read it as "the whole family rejects it". The conclusion was TRUE (re-measured
properly against the pre-change tree: all four verbs gave `COBOL0001 no viable alternative`) but it was right by
accident, and for an hour I was reasoning from a number that meant something else. §0 says the instruments have
lied more often than the compiler has; this instrument was mine, written thirty seconds earlier. **A sweep that
reports the SAME error for every case is reporting something about the sweep.**

⚙ **Still open:** `EVALUATE TRUE / WHEN FUNCTION SQRT(X) > 0` throws at run time — a genuinely different code
path, which is why the entry's own "do NOT assume these share one fix" was right.

⛔ **CORRECTION, SAME DAY (commit `6aad1ef8`): I FIRST WROTE THAT THIS IS "a BIND-level misclassification",
AND MEASURING REFUTED IT.** `WHEN FUNCTION PI > 1` — the same position, a function with NO ARGUMENT and so no
parenthesis — parses as a condition perfectly, while `WHEN FUNCTION SQRT(W-Z) > 1` does not, for any operator
and with or without an AND chain. **The discriminator is the ARGUMENT PARENTHESIS, not the
function-identifier**, which points at the SUBSCRIPT lexer mode rather than at the binder. I had written a
mechanism down from the symptom without measuring the one variable that separates the cases. The next step is
a TOKEN DUMP — PB8's entry named a lexer-mode cause the same way and a dump showed the lexer was never
involved.

