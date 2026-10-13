## Entry 1987 — 2026-10-12 21:12 PDT — PB2623: METHOD is not a COBOL-2023 reserved word, and the END METHOD marker still reads

**Defect.** `ReservedWords.Table.cs` reserved METHOD at COBOL-2023 on a generator line (`iso2023.Add('METHOD') # OCR omission (DEVLOG 578)`) whose claim is false: PDF printed page 206 reads MERGE, MESSAGE-TAG, METHOD-ID, MINUS, MODE, MOVE, MULTIPLY. `01 METHOD PIC X.` was refused with COBOLNET0901 at 2023 (re-probed on this build: 0901 at 2002, 2014 and 2023), citing a clause that does not reserve the word.

**Decision.** ISO §4.2.8 makes the §8.9 words the reserved words and §8.10 lists no METHOD, so METHOD is a user word at 2023, exactly as every other 2002/2014 word the 2023 list dropped (POSITION, COBOL, ...). The generator line is removed; the table and `reserved-words.json` regenerate to `R2023 = false`, provenance "dropped by 2023 §8.9". `docs/CONFORMANCE.md` D-RW2 records the determination (owner may overturn), `cobol-words.json` and `VERSION_TEST_MATRIX_DESIGN.md` lose the false claim. The alternative (an Addendum typo correction) would be an added reserved word that §4.2.10 3) obliges the documentation to list, and would refuse a legal program. The inherited index citation "END METHOD marker 9.1.15" was wrong too (the marker is §10.7.2); `cite.py --check` caught it.

**Sibling arm, found by the fix.** With METHOD free at 2023 the §8.9 reservation gate retypes every METHOD token of a program that declares the word, including the keyword of `END METHOD`, so `01 METHOD` plus any method definition failed to parse. The fix is one grammar rule, `methodKeyword : METHOD | {wordAhead("METHOD")}? IDENTIFIER` (`CobolOO.g4`), used by the END METHOD marker and EXIT METHOD (read under `--permissive` at 2023); the three C# readers of `ExitStatementContext.METHOD()` read `methodKeyword()`. The other words 2023 dropped have no 2023 format that spells them (CLOCK-UNITS, EVERY, POSITION, PROCEDURES, PROCEED, REFERENCES are 1985-only constructs). `DESIGN-frontend-grammar.md` carries the rule.

**Goldens.** `conformance/2023/pb2623_method_is_a_user_word_at_2023` (data item METHOD plus a method closed by END METHOD), `negative/pb2623-method-reserved-at-2002-2014` (0901), and `MethodUserWordTests` (EXIT METHOD under `--permissive` at 2023 with a METHOD data item compiles; the same program at 2014 is refused).

**Rows.** None claimed, none closed.

**Arch-oracle.** `compare_oracle.py` against 00435d6e90c0: 4 of 7792 cases differ. Two are ADDED (this branch's two goldens). Two are DIAGNOSTICS on `negative/l1c22-format3-in-method` and `negative/l1c22-format3-in-program`, both already-rejected nested-method programs whose first error (COBOL0001 at the inner method) is unchanged and is the `.err` the corpus pins: the cascade at the stray `END METHOD` that followed used to read `COBOLNET0901: 'METHOD' is a reserved word in COBOL-2023` (the false claim this note removes) and now reads a plain COBOL0001 syntax error. Baseline re-recorded at c41f1b82af2c.

**Gate.** `LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261013T031258Z-099e52). Semgrep verify PASS, no count moved.

