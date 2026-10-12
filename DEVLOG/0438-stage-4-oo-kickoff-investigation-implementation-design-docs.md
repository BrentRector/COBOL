## Entry 438 — Stage-4 OO kickoff: investigation + implementation design (docs/OO_IMPLEMENTATION_DESIGN.md)

With the pointer subsystem complete, the owner chose **OO → .NET classes** as the next Stage-4 work. A 3-agent
investigation workflow (`oo-investigation`, 180K tokens) mapped, in parallel: the spec requirements
(CLASS-ID/FACTORY/OBJECT/METHOD-ID/INVOKE/object-reference/SELF-SUPER/INHERITS), the exact pipeline insertion points,
and the grammar + symbol-model integration — combined with first-hand reading of ISO §11.3/§11.4/§11.7/§11.8,
§14.9.23 (INVOKE), §8.4.3.8 (SELF/SUPER), §16.2.1 (NEW).

**Key findings:** most OO lexer tokens already exist (`CLASS_ID`/`METHOD_ID`/`END_METHOD`/`INVOKE`/`SELF`/`SUPER`/
`REFERENCE`); a **stub `invokeStatement` is already `{is2002()}?`-gated** in the statement list; `SymbolKind.Class`/
`Method` are reserved-but-unused; the pipeline already emits **multiple types per assembly** (so a CLASS + a PROGRAM
unit in one file is structurally supported); `CobolParserOO.g4` is a **dead, unreferenced sketch** (merge, don't
reuse). **The elegant reuse (architecture decision):** each object instance carries its own `ProgramState` (instance
field), so the ENTIRE byte+typed data engine works per-instance with one addressing tweak (`this.State` vs the static
`State`), object identity is the .NET reference itself (no handle table), and `INVOKE` mirrors the existing
`callvirt` CALL path (`newobj` for NEW, `callvirt` for instance, `call` for factory).

Synthesized **`docs/OO_IMPLEMENTATION_DESIGN.md`**: the minimal vertical slice (a `GREETER` class + a driver), the
foundational integration (grammar/tokens/pipeline/symbols, with file:line), the per-instance-`ProgramState`
decision, the risks, and the **6 staged slices** — (1) class + instance method + `NEW` + no-arg `INVOKE`;
(2) `USING`/`RETURNING`; (3) `INHERITS` + `SELF`/`SUPER`; (4) `FACTORY` static; (5) `PROPERTY`; (6) polymorphism +
`EC-OO`. Each guard-green + a `tests/conformance/2002/` test. No code change. **Next: implement OO slice 1.**

