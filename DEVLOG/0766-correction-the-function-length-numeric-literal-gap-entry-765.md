## Entry 766 — 2026-07-11 06:45 PDT — CORRECTION: the "FUNCTION LENGTH numeric-literal gap" (Entry 765) was a FALSE alarm — reading §15.50.3 shows the error is spec-correct

Went to fix the LENGTH gap I flagged in Entry 765 and did the thing I keep having to relearn: read the ACTUAL governing
§ FIRST. **ISO §15.50.3 (Argument rule) item 1: a LITERAL argument "shall be an alphanumeric, national, or boolean
literal"** — a NUMERIC literal is NOT a valid FUNCTION LENGTH argument. (The `Num1` in the §15.34 function table means a
numeric *data item*, which §15.50.3 admits as "a data item of any class or category" — that path IS handled, by the
`BoundFieldOperand` arm → `ImageWidth`.) So `BindLengthFold` erroring on a numeric/figurative literal is SPEC-CORRECT,
not a gap. My Entry-765 flag reasoned from §15.50.1 ("depending on the class of the argument") + the function-table row
WITHOUT reading §15.50.3 — the exact "derive from the spec, don't assume" trap.

Corrected: the in-code comment now cites §15.50.3 (the numeric/figurative-literal error is correct), the error message
names the section, task #18 (the "fix") is closed as not-a-gap. Net: the §6h audit stands, and the one apparent spec
gap it surfaced was a false alarm dissolved by reading the argument rule. Comment-only + message change; battery green.
(The meta-lesson compounds: "validate against the spec" also means read the SPECIFIC governing rule, not the nearest
general sentence — I flagged a false gap by stopping at §15.50.1.)

