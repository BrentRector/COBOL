## Entry 503 — COBOL.NET: level-77 is a root (independent item) — fixes 12 NC compile-errors

A second systematic gap from the corpus map: **level 77** items (ISO §13.18.38 — INDEPENDENT elementary data) were
nested by their numeric value in the binder's level-number stack. The pop rule `while (stack.Peek().Level >= item.Level)`
treats 77 as a deep level, so a 77 that FOLLOWS a group (whose last open subordinate is, say, level 05) does not pop
the stack — it attaches as a CHILD of that subordinate. NC101A's 77s are all at the top of WORKING-STORAGE (they pop
each other → roots), so it never bit; NC102A declares `77 THREE` / `77 P-COUNT` right after a numeric-edited 01, so
they bound under it and every reference emitted invalid C# (`WRK_NE_4.THREE = …` — CS1061, a member of a string).

**Fix.** Level 77 is always top-level, like 01: the binder now uses an effective nesting level of 1 for a 77, popping
the whole stack so it attaches as a root regardless of the surrounding levels. (Its stored `Level` stays 77, so a
following 05/01 still pops it correctly.) This moved **12 of the 24 NC compile-errors to compiling** (NC102A/106A/139A/
140A/170A/172A/173A/176A/202A/203A/251A/253A) — they now hit only runtime verb gaps, not invalid C#. +3
`DataLevelDifferentialTests` (77 after a group / after an elementary 01 / between two groups) pinned to the legacy.
Conformance 232 → 235; 14 unit; the 7 green NIST programs unaffected. Greenfield-only.

