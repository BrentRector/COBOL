## Entry 463 — COBOL.NET G4 (structured subset): paragraphs→methods + PERFORM + fall-through + STOP RUN

Control flow, structured subset. Each COBOL paragraph (sections flattened) becomes a `private static void` method;
`Main` runs them in source order inside `try { … } catch (StopRun) { }` — COBOL fall-through is a sequential call
chain, and `STOP RUN`/`GOBACK` `throw new StopRun()` (new runtime signal) to unwind the whole chain (ISO §14.9.43).
`PERFORM` forms: simple (`para()`), `n TIMES` (`for`), `UNTIL` (TEST BEFORE→`while`, TEST AFTER→`do/while`), `THRU`
(inline the paragraph range's calls in order), and inline `PERFORM … END-PERFORM` (with TIMES/UNTIL options).
`CollectParagraphs` builds the ordered list + a name→method index (uniquified `P_<name>`); `MethodOf` resolves a
PERFORM target. Verified: a program with `PERFORM GREET`, `PERFORM GREET 3 TIMES`, `PERFORM COUNTUP UNTIL I >= 3`,
fall-through, and `STOP RUN` → `START / HI×4 / DONE N=030` (correct); the generated C# reads like hand-written .NET.
Deferred (the PC-dispatcher slice): out-of-line `GO TO`, `ALTER`, `PERFORM VARYING`, and ranges containing `GO TO`.

