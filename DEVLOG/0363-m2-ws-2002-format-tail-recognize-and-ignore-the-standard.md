## Entry 363 — M2 (WS-2002-FORMAT tail): recognize-and-ignore the standard `>>` directives we don't yet act on

Survey of M2 candidates (grep of the grammar/binder) found UDF (`FUNCTION-ID`) and pointer/national data absent
(large multi-part features), and the EXIT forms already complete (`EXIT PARAGRAPH/SECTION/PERFORM [CYCLE]` are in
`exitStatement`). The most tractable, genuinely-useful next piece was the WS-2002-FORMAT tail.

Problem: `ConditionalCompilationProcessor`'s default case passed any *unrecognized* `>>` directive through to the
lexer. So a real COBOL-2002 program using `>>CALL-CONVENTION`, `>>TURN`, `>>LISTING`, `>>PAGE`, `>>LEAP-SECOND`,
etc. failed to compile — the `>>` tokenized as two `>` operators followed by stray words.

Fix: a `KnownIgnoredDirectives` set of standard ISO §7.3 directives that CobolSharp recognizes but does not yet
act on (CALL-CONVENTION, LISTING, PAGE, LEAP-SECOND, PROPAGATE, FLAG-85, FLAG-NATIVE-ARITHMETIC,
REF-MOD-ZERO-LENGTH, TURN, COBOL-WORDS). The default case now: consume (blank) such a directive when emitting so
the program compiles with default behavior; drop it in an omitted branch; and **leave a genuinely unknown `>>`
word in place** so it still surfaces downstream (a typo like `>>IFF` is not silently swallowed). The
behavior-bearing directives — DEFINE/IF/ELSE/END-IF/EVALUATE/WHEN/END-EVALUATE here and SOURCE FORMAT earlier in
ReferenceFormatProcessor — are deliberately not in the set.

Caveat (documented): consuming `>>TURN`/`>>LEAP-SECOND`/`>>CALL-CONVENTION` means their semantics are not
applied — for `>>TURN` that is consistent with not (yet) performing those EC runtime checks; for
`>>CALL-CONVENTION` it means the default COBOL convention is always used. Real implementations of these remain
future M2/M3 work.

Test: a program with `>>CALL-CONVENTION IS COBOL`, `>>LISTING ON/OFF`, and `>>TURN EC-ALL CHECKING ON`
interspersed between statements now compiles and runs → `HELLO`. Still an exact no-op on directive-free source.

Guard ALL GREEN: **1047 unit / 447 integration / 364 NIST**, 0 regressions.

