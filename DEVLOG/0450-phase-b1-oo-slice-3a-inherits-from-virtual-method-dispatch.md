## Entry 450 — Phase B1: OO slice 3a — INHERITS FROM + virtual method dispatch (polymorphism)

Built OO slice 3a: single inheritance + polymorphism. `CLASS-ID. DOG INHERITS FROM ANIMAL` → `class DOG : ANIMAL`;
OO methods are now `virtual`; a subclass method matching a base method **overrides** it; invoking a method through
a base-typed OBJECT REFERENCE that holds a subclass instance dispatches to the override (virtual dispatch). Proven:
`INVOKE ANIMAL "NEW" RETURNING A; INVOKE A "SPEAK"` → `GENERIC`; `INVOKE DOG "NEW" RETURNING A; INVOKE A "SPEAK"`
→ `WOOF` (A is ANIMAL-typed but holds a DOG). A subclass with no override inherits the base method.

**The inheritance-emission model (the key design):** a single per-instance `ProgramState` lives on the ROOT class;
every subclass INHERITS that one field — its `.ctor` just chains to the base ctor (which allocates + initializes the
shared State), so a subclass adds no State field or `InitializeState`. Base methods read base-offset data, all in
the one State (slice 3a: a subclass adds no OBJECT data of its own — see COBOL0113). Classes are emitted
**base-before-subclass** (an inheritance-depth sort in `EmitAssembly`) so a subclass's emit can resolve the base
`TypeDefinition`, its State field, and its ctor.

- **Grammar:** `INHERITS` lexer token (corpus-clean) + `classIdParagraph : CLASS_ID DOT className (INHERITS FROM
  className)? DOT`. `SELF`/`SUPER` were already tokens in `objectReference`.
- **Routing:** `IrModule.BaseClassName` set from the CLASS-ID paragraph (`ExtractBaseClassName`); `className()` is
  now an array ([0] = class, [1] = base).
- **Emit (`CilEmitter`):** `EmitClassModule` resolves the base type, sets it as `_programType.BaseType`, and for a
  subclass uses the inherited State field (`ResolveInheritedField`) + a chaining `.ctor` (`EmitSubclassConstructor`)
  instead of `EmitProgramState`. `EmitClassMethodBody` emits the public OO method `virtual` (root = NewSlot; an
  override matching a base method = ReuseSlot). `EmitInvoke` resolves the target method by walking the base chain
  (`ResolveInheritedMethod`) and `callvirt`s it → runtime polymorphism.
- **Deferred, but LOUD (never silent):** subclass own OBJECT data → **COBOL0113**; `INVOKE SELF`/`SUPER` → **COBOL0112**
  (`CallBinder` detects the objectReference SELF/SUPER alts); INHERITS FROM a base not in the compilation group →
  **COBOL0114**.

**Adversarial review (Agent tool) — caught two real bugs the happy-path tests missed, both fixed:**
- **(major, wrong-output miscompile)** A case-mismatched override (`METHOD-ID. speak` overriding `SPEAK`) silently
  did NOT override: COBOL is case-insensitive so it was *detected* as an override (ReuseSlot), but emitted under the
  source-cased name, and the .NET override slot match is case-SENSITIVE → it took a new slot → dispatch went to the
  base (`GENERIC` not `WOOF`). Fix: an override is emitted under the **base method's exact name**.
- **(major, false-positive diagnostic)** COBOL0113 false-fired on a subclass whose only "WS" item was a method's
  LINKAGE `OCCURS … INDEXED BY` index-name (synthetic index-names are force-placed in the WS area, ISO §8.5.1.2,
  with `USAGE COMP`). Fix: the subclass-own-data check now reads the OBJECT's own `workingStorageSection`
  **parse subtree** (its `dataDescriptionEntry` entries), not the flattened symbol list. (The review's scope-note —
  unknown base silently degrading — is the COBOL0114 fix above.)
  The review also verified correct: `ldfld` of the inherited State, ctor chaining over a 2-/3-level hierarchy
  (State allocated once), virtual/override slot mechanics, base-before-subclass ordering (+ entry-point preserved),
  base-chain method resolution + covariant `_OBJ_` storage, and no non-OO regression.

Conformance: `tests/conformance/2002/oo_inherit.cob` (ANIMAL/DOG polymorphism → `GENERIC/WOOF/WOOF`). Regression
net: 6 new `OoTests` cases (polymorphism, case-mismatched override, SUPER→0112, subclass-data→0113, LINKAGE-index
allowed, unknown-base→0114). **Scope note (slice 3b):** subclass own OBJECT data (extend the shared State + make
inherited data resolvable in a subclass method) + INVOKE SELF/SUPER.

Guard ALL GREEN — **1204 unit / 524 integration / 364 NIST** (+oo_inherit conformance, +6 OoTests). `guard-fast.sh`.

