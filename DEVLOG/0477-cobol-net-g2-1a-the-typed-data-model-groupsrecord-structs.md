## Entry 477 — COBOL.NET G2-1a: the typed data model — groups→record structs, Place/ReferenceResolver, qualified names, figurative VALUE, loud-failure

The first slice of the G2 core rebuild. Replaces the DEVLOG-458 flatten-to-leaves stopgap with the real
typed-native data model and the universal lvalue, and fixes the §1.4 loud-failure invariant.

- **Data model — groups ARE .NET records (COBOLNET_DESIGN §3.2).** `FieldEmitter` now emits a nested
  `record struct` (`_T_<uid>`) per group with public typed members, a composed object-initializer VALUE
  (every member set, so no `string` member is left null), and one static field per 01/77. A numeric leaf still
  gets its `NumProfile`. Member access falls straight out: `WS-REC.WS-NUM` → `WS_REC.WS_NUM`.
- **`Place` + `ReferenceResolver` (§3.3/§3.4/§14.1) — the ONE lvalue.** New `ReferenceResolver` does the
  two-phase resolve: syntactic flatten of `cobolWord dataReferenceSuffix*` (qualifiers / subscript / ref-mod
  presence), then semantic resolve — unqualified via the new `ByName` MULTIMAP (`Dictionary<string,List<DataItem>>`
  — COBOL allows duplicate names disambiguated only by qualification; the old single-valued dict silently dropped
  all but the last), or OF/IN-qualified by right-to-left narrowing (`A OF B OF C` → resolve C, find B in C, find A
  in B). Returns a `MemberPlace` (`Read()`=path, `Write(rhs)`=assignment). EVERY verb operand now routes through
  `Place` — DISPLAY/MOVE/arithmetic/COMPUTE/IF/PERFORM-count — so qualified and (next slice) subscripted/ref-mod
  references work uniformly. Subscript/ref-mod refs return null here → loud-failure until G2-1b.
- **Figurative-constant VALUE.** `VALUE ZERO/SPACE/HIGH-VALUE/LOW-VALUE/QUOTE/NULL` fill the item to its width
  (ISO §8.3.1.2; HIGH/LOW = U+00FF/U+0000 per §14.9). Fixes the `ZEROL` compile bug the G2-0 harness surfaced.
- **Loud-failure (§1.4).** Every silent `// TODO` no-op is replaced by a guarded `NotImplemented.Run(...)`
  (statement) / `NotImplemented.Value<T>(...)` (expression) that throws `NotImplementedCobolFeatureException` at run
  time (new `Cobol.Net.Runtime/Control/NotImplemented.cs`). A bound-but-unimplemented construct now fails LOUD,
  never silently wrong. (The `Value<T>` form is a void-returning helper, so no unreachable-code warning.)
- **`DataItem`** gains a global `Uid` (collision-free struct/profile names — two `REC`s, or two `NUM`s in different
  groups, no longer clash); per-scope CsName dedup; the `decimal` mention in the emitter docstring corrected to
  native `long`.

**Sequencing note (per the reviewing advisor — logging a conscious deferral).** The formal bound tree
(`BoundProgram` + `StatementBinder` + `BoundUnsupported`) and the §17 §2.2 emitter decomposition are deferred to
**G2-2**. Rationale: the data model + `Place` are the co-dependent core (the advisor's "merge G2a+G2b"); building
them in the working emitter first is the lowest-risk path to the G2 checkpoint capability, and `Place` is built
ONCE here and reused by the bound binder in G2-2 (only its construction site moves earlier — not a transitional
wrap of the type). The most urgent part the advisor tied to the bound tree — loud-failure — is already done here.

Verified: full-solution build clean Debug (0/0, warnings-as-errors); Conformance 47 green (incl. 11 new
group/qualified/nested/figurative differential tests) + Unit 3 green; legacy oracle untouched. The generated C# for
a group is clean idiomatic record-struct code (inspected). RESUME AT → G2-1b: OCCURS → arrays + subscripts + ref-mod
(port the legacy SUB_* interpreter, COBOLNET_DESIGN §3.4).

DEVLOG 477.

