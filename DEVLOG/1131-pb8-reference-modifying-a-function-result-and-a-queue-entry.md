## Entry 1131 - 2026-07-30 13:36 PDT - PB8: reference-modifying a function result, and a queue entry whose root cause was reasoned instead of measured

**The entry told me where the bug was, and it was wrong.** PB8 read: reference-modifying a function result is a
parse error; root cause "already located" in `CobolLexer.g4`'s `OnDefaultLParen`; the fix is "a lexer-mode change
PLUS a `functionCall` ref-mod tail — the riskiest category in this codebase", with the lexer's own comment quoted
in support ("the SUBSCRIPT-mode decision at '(' is frozen at lex time and cannot be repaired later"). The session
prompt repeated the warning and told me to budget for it and not start it late.

I dumped the tokens first. In **both** keyword-present shapes the ref-mod paren is already lexed in **DEFAULT
mode**:

```
MOVE FUNCTION CURRENT-DATE (1:4) TO X
  -> FUNCTION IDENTIFIER LPAREN INTEGERLIT COLON INTEGERLIT RPAREN         (COLON, not SUB_COLON)
MOVE FUNCTION UPPER-CASE("abc") (1:2) TO T
  -> ... RPAREN LPAREN INTEGERLIT COLON INTEGERLIT RPAREN
```

After a function NAME the lexer's own FUNCTION suppression already keeps the paren out of SUBSCRIPT mode, and
after the argument list's `)` the previous token is not a data-name so the SUBSCRIPT trigger never fires. Both
therefore land on the DEFAULT-mode `refModPart` rule that had existed all along for `dataReference`. **The lexer
was never touched.** The whole budgeted risk was an artifact of the entry having been *reasoned* from
`OnDefaultLParen`'s source rather than *measured* — `use_antlr_tree_dump` is in memory for exactly this, and the
correction is now the first thing PB8's entry says. `CobolLexerModeDriftTests` pins the mode per shape so the
conclusion cannot rot, and names what breaks if the trigger is ever widened.

**The defect was wider than its two repros, and the worst case was unrecorded.** Establishing the whole
before-state first:

| shape | before |
|---|---|
| `FUNCTION CURRENT-DATE (1:4)` | COBOL0001 parse error |
| `FUNCTION UPPER-CASE("abc") (1:2)` | COBOL0001 parse error |
| `CURRENT-DATE (1:8)` keyword-omitted | COBOLNET1543 — read as an ARGUMENT LIST |
| `UPPER-CASE("abc") (2:3)` keyword-omitted | **compiled clean, threw at RUN TIME** — the PB7/DA7 wrong-stage family |
| `FUNCTION LOCALE-DATE(CURRENT-DATE (1:8))` — **the standard's own D.14.3.6 example** | COBOLNET1543 |

**And the grammar tail alone would have made it worse.** After adding `refModPart` to `functionCall` I checked
the value rather than the exit status: `FUNCTION UPPER-CASE("abcdefgh") (2:3)` compiled and printed **ABC**
instead of BCD, because the binder ignored the new node. A loud parse error had become a silent wrong answer. It
cost one compile to find and would have been invisible to any test that only asked whether the program built.

**The rule that decides the design is one the queue entry never mentioned.** §8.4.3.2.3 SR6: "If a function's
definition permits arguments and a left parenthesis immediately follows ... the left parenthesis is ALWAYS
treated as the left parenthesis of that function's arguments." So `(` after a ZERO-argument function is a
reference modifier, and after an argument-permitting one it is the argument list no matter what follows —
`FUNCTION RANDOM (1:4)` is the standard's own trap (the SR6 NOTE) and must be rejected as a malformed *argument
list*, not as a class error about a ref-mod that was never written. That is a catalog question, not a grammar
one, so the grammar parses both and the binder decides. Validated mechanically, as were SR2, SR3 and
§8.4.3.1.4 GR1(f)→(g), which fixes the order: the argument list binds to the name, then the modifier applies to
the identifier on its left.

**Where each piece went, and why not elsewhere.**

- **A rider, not a wrapper.** The ref-mod hangs on `BoundIntrinsicCall.RefMod`. The alphanumeric string channel
  is selected by pattern-matching `BoundComputedOperand { Expr: BoundIntrinsicCall }` at four separate sites; a
  wrapper node would have silently stopped matching at every one, and that failure mode is a *dropped ref-mod*,
  not a compile error — the same silent wrong answer this fix exists to remove. §8.4.3.3.4 GR6 preserves class
  and category for the three categories SR2 admits, so the rider leaves `ResultCategory` correct with no new rule.
- **One slicer.** The slice reuses `CobolString.RefMod`, so the §8.4.3.3.4 item-5c bounds check and
  EC-BOUND-REF-MOD come for free instead of being re-implemented. A user-defined function needed nothing new: its
  result is already a real `Place` (the §8.4.3.2.4 GR1 caller temp), so it goes through `RefModPlace` like a data
  item, and the temp's own category answers SR2.
- **One reader.** A ref-mod reaches the binder through two carriers — the parsed DEFAULT-mode `refModPart` and a
  SUBSCRIPT-mode captured group — and `ReferenceResolver` had the reduction written inline twice. Both now go
  through `ReadRefMod` → `RefModSpec`. The `(int)` cast and the omitted-length sentinel moved into `RuntimeApi`
  so the place and value channels cannot render different positions.
- **One place per rule.** SR3 was briefly enforced two ways — structurally by `refModPart?` on the function side,
  by counting on the data side. That is one rule in two mechanisms, so the arity went back to `*` and both sides
  now report COBOLNET1630.

**Two siblings the sweep turned up, neither of them PB8's own repro.**

**SR3 was unenforced for data items, silently.** `MOVE A (3:4)(2:2)` compiled clean and returned `A(2:2)`:
`dataReferenceSuffix*` admits unlimited ref-mods and the resolver kept only the first of each carrier via `??=`,
with the DEFAULT-mode form outranking the SUBSCRIPT-mode one — so the *second* modifier won. Neither the
composition nor the rejection the standard requires. That closed inventory row `SR-8.4.3.3.3-3`, which had stood
at state GAP with an empty code-location.

**COBOLNET1627 and COBOLNET1628 were printing their slugs, not their codes.** `DiagnosticDescriptor` carries both
a `Code` (`COBOLNETnnnn`) and a stable kebab `Id`, and `Error(string, string)` takes the *code* — so passing
`descriptor.Id` compiles perfectly and emits `error intrinsic-argument-class:` where `DIAGNOSTICS.md`, the user
and `--suppress` all expect `error COBOLNET1627:`. Both shipped **today**, in PB1 and PB6, under a green battery
that never looked at the code position. I hit it because I copied the same call shape. The root cause is an API
asymmetry — `Error` had a descriptor overload and `Warning` did not, so a `--permissive` site had nothing correct
to reach for. `Warning` now has one, and `DiagnosticEmitFormDriftTests` is a source-form check because no runtime
test can see a mistake only a caller can make. I made it fail on the pre-fix source before trusting it: the tree
still compiled with 0 errors while the test went red, which is the whole argument for its existence.

**PB9, recorded rather than absorbed.** `RANDOM` is the one zero-argument intrinsic that cannot be written in the
keyword-omitted form — it lexes as a reserved word, so `MOVE RANDOM TO T` never reaches `cobolWord`. I swept all
nine: the other eight bind. It is a *tokenization* root cause, not PB7's suffix-arity one, and its fix is a
`cobol-words.json` row carrying the mandatory edition-gate sweep — so it is a queue item, not a quiet addition
here. Verified identical on the pre-PB8 compiler by stashing, so it is not a regression I introduced.

**A red I caused, recorded because it is the same defect I spent the day fixing.** My new drift test
located the repo root itself (`AppContext.BaseDirectory` walking up for `src`) instead of using the shared
`CobolNet.Tests.Shared.TestRepo`. `TestRepoDriftTests` caught it in the full Unit leg and named the file and
the exact API to use. One rule written down in two places, in a commit whose whole theme is that - and the
guard that caught it is the same kind of source-form check I had just argued for. It also cost a full
Conformance run: I edited the golden while that run was in flight, so I killed it and re-ran on a stable
tree rather than read a verdict measured over a mutating one.

**Verification.** Baseline confirmed green before any code change (Conformance 4150/4150, Unit 963/963, both
matching plan §0). Nine legal shapes produce spec-derived values including D.14.3.6; six rejection cases each
report their own rule; wave-local gate 2184/2184 zero skipped; corpus 388/388. Inventory GAP 3800 → 3798.

