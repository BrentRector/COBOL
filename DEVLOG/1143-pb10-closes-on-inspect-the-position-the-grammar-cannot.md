## Entry 1143 — 2026-08-02 13:38 PDT — PB10 closes on INSPECT: the position the grammar cannot decide alone

PB10's four easy positions landed in DEVLOG 1136 — `WRITE … FROM`, `REWRITE … FROM`, `RELEASE … FROM`,
`INITIALIZE … REPLACING … BY`, all four defined by the standard itself as MOVE sending items, all four binding
through one helper. INSPECT was left because it is the one position where **the same syntactic slot is sending in
one format and receiving in three**, so no grammar rule can decide it.

### Deriving the split rather than inheriting it

The queue entry asserted the Format 1 / Formats 2-3-4 split. I re-derived it, because a queue entry's claim is a
claim:

- **§14.9.22.4 GR1** — "For purposes of determining its length, identifier-1 is treated as a sending data item."
  That is a *scoped* concession, and the scope is the argument: if identifier-1 were generally a sending item,
  qualifying it "for purposes of determining its length" would be pointless.
- **GR7** — each properly matched occurrence "is tallied (format 1) **or replaced by literal-3 (format 2)**". In
  format 2 the characters *of identifier-1* are replaced.
- **GR20** — "A format 4 INSPECT statement is interpreted and executed as though a **format 2** INSPECT statement
  specifying the same identifier-1 had been written." So CONVERTING is REPLACING wearing a hat.
- **§8.4.3.2.3 SR1** — "A function-identifier shall not be specified as a receiving operand."

Format 3 is TALLYING *and* REPLACING, so it inherits the REPLACING half. Sending in Format 1; receiving in 2, 3
and 4. All four citations validated with `cite.py --check`.

### The subtlety, and why Format 3 gets its own fixture

The obvious screen is "does this INSPECT have a TALLYING phrase? then it is Format 1, so admit the function." That
screen is **wrong on Format 3** — `INSPECT f(X) TALLYING N FOR ALL "O" REPLACING ALL "O" BY "0"` has TALLYING
present and still modifies identifier-1. The correct predicate is the *other* one: does it have REPLACING or
CONVERTING.

And that predicate already existed. `InspectEmitter` computes a local `mutated` flag, set exactly when REPLACING
or CONVERTING produced output, and uses it to decide whether to store the image back. `mutated == true` **is**
"identifier-1 is a receiving operand" — the spec rule and the codegen decision are the same fact. So the bind-time
screen keys on the same thing rather than on a parallel notion of "format number", and the emitter now ASSERTS the
correspondence: a non-field target reaching the store fails loud instead of silently dropping the write-back,
which would have computed a replacement and thrown it away.

`pb10-inspect-fn-tallying-replacing` exists precisely because a fixture set of only REPLACING and CONVERTING
would pass a naive TALLYING-keyed screen. This is the `pb1-numeric-arg2-alphanumeric` pattern again: pin the
branch that a *plausible wrong implementation* passes, not just the branches that a correct one handles.

### Structural, and the legacy compiler carried

`BoundInspect.Target` moved from `Place` to `BoundOperand`, because a function result is a value with no place.
Two analysis sites moved with it and neither was mechanical:

- `BoundStores.Visit(BoundInspect)` now asks `(Target as BoundFieldOperand)?.Place`. A function target can never
  be a store — which is not a new special case but the same rule written twice, since that arm already required
  REPLACING/CONVERTING and SR1 bars a function-identifier from exactly those.
- `UsageCollectionPass` walks it as an operand. `Op()` already delegates a field operand to `P()` and ignores a
  computed one, which is the right answer: the pass collects whole-GROUP references for the image mechanism, and
  a function's returned value is a temporary, never a declared group.

⚠ **The legacy compiler was carried, not abandoned** — this entry's own third caution, and the one that cost a
revert last time. `inspectStatement` lives in the SHARED `Core/CobolIO.g4`, so widening it to
`(functionCall | dataReference)` made `.dataReference()` nullable, and the legacy
`StringStatementBinder.BindInspect` would have dereferenced it. It now declines the new shape explicitly, which is
the established unsupported-statement contract rather than a crash. The grammar change is ADDITIVE for the same
reason: collapsing to a shared rule would delete the generated accessor the legacy binder reads. Unification
belongs to P15, when the legacy side is deleted rather than migrated.

### The diagnostic is named after the rule, not the statement

`COBOLNET1632` is `function-identifier-receiving`, not `inspect-function-target`. §8.4.3.2.3 SR1 is ONE sentence
about function-identifiers in receiving positions, and PB10's remaining positions plus PB17 will want the same
verdict. A statement-shaped code invites the next site to mint a second code for the same sentence, which is the
`one_rule_one_place` failure in its most common disguise.

