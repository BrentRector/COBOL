## Entry 045 — 2026-03-14: HELLO WORLD RUNS — First Executable Output

### The Milestone

A COBOL program compiles and runs for the first time through the ANTLR4-based pipeline:

```cobol
IDENTIFICATION DIVISION.
PROGRAM-ID. HELLO.
PROCEDURE DIVISION.
MAIN-PARA.
    DISPLAY "HELLO WORLD".
    STOP RUN.
```

Output: `HELLO WORLD`

### Debugging Session (3 bugs found)

**Bug 1: SemanticModel.ParagraphsInOrder was empty.**
The SemanticModel was created AFTER SemanticBuilder ran, but nobody populated its paragraph
list. Fix: populate from SymbolTable after both semantic passes.

**Bug 2: IrLoadConst stored values into locals (stloc) but the local variable plumbing
had a type mismatch or allocation bug.**
The `GetLocalForValue` closure created locals on demand, but the round-trip through
`stloc.0` / `ldloc.0` silently failed. Fix: IrLoadConst now pushes directly onto the
CIL evaluation stack (no local storage). This is the canonical approach for single-use
constants.

**Bug 3: LowerDisplay iterated `ctx.children` looking for `ITerminalNode`, but the string
literal "HELLO WORLD" was wrapped in a `literal` rule context, not a direct terminal.**
The binder only saw empty terminal nodes. Fix: also check for `LiteralContext` and
`IdentifierContext` children.

### Diagnostic approach that worked

Added per-method IL dump (`[IL] ldstr "..."`, `[IL] call ...`) which immediately showed
`ldstr ""` — the empty string proving bug 3. The user's suggestion to verify each link
in the chain (entry point → paragraph call → IL instructions) was decisive.

### Generated IL

```
Main:                           Para_MAIN-PARA:
  nop                             nop
  call Para_MAIN-PARA()           ldstr "HELLO WORLD"
  ret                             call Console.WriteLine(string)
                                  ret
```

---

