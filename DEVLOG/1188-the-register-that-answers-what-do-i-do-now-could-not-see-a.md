## Entry 1188 — 2026-08-06 00:07 PDT — the register that answers "what do I do now" could not see a defect that rejects legal source, and it had been that way since the register was built

**I filed PB51 — `COMPUTE WS-N = ZERO` refused as a boolean Format-2 ALL literal, MAJOR, legal source that will
not compile — and `python scripts/spec/work.py next` did not list it.** Not below the fold: absent. That is the
one command CLAUDE.md points every session at, and the item it had just been handed was invisible to it.

**THE PREDICATE.** `actionable()` selected on `wrong_answer or crashes`. A false REJECT does neither — it
crashes nothing and computes nothing wrong, it simply stops the user's program compiling — so the register
scored the outcome CLAUDE.md rule 4 calls *forbidden outright* as no harm at all.

**MEASURED BEFORE FIXED.** Nine open items were hidden: PB12, PB46, PB51 and R01 flagged `rejects_legal_source`;
PB30, PB31, PB33, PB35 and PB40 flagged `under_rejects`. The actionable count went 10 → 19 on the predicate
change alone. It is worth saying plainly what that means: **for as long as the register has existed, roughly half
its live harmful work has been unrankable**, and every session that asked it what to do next got an answer
computed over the other half.

**THE SAME PREDICATE WAS WRITTEN TWICE AND BOTH COPIES WERE WRONG THE SAME WAY.** `kb/Work.base`'s *Fix next*
view carries its own `wrong_answer or crashes`, because Bases YAML cannot import Python — and CLAUDE.md tells
readers the two are "the same list". Worse, the complementary view was NAMED "Open but nobody gets a wrong
answer", so four items that reject legal COBOL sat parked under a heading asserting they were harmless. The view
is now the exact complement and is renamed *Open but no harm flag set*. Since the copies have to exist,
`work.py check` now parses the Base's filter and fails when it stops naming every flag in `HARM_FLAGS` — proven
in the failing direction before being trusted.

**THE SECOND HOLE, WHICH THE FIRST FIX EXPOSED.** With `check` now aware of the harm flags, I added the obvious
companion guard: an OPEN defect that sets *no* harm flag can never be ranked whatever the predicate is. It found
three more — PB27, R02, R07 — invisible for that different reason, and triaging them was the point rather than
the chore:

- **R02** — the underscore is admitted in a user-defined word from COBOL-2002 (§8.3.2.1) and this compiler
  refuses it, so a legal program does not compile at three of the four editions we target. `rejects_legal_source`.
- **PB27** — carried `silent` alone. `silent` QUALIFIES a harm (silently wrong vs. loudly wrong); it does not
  name one, so the item scored as harmless. Its own text says what it does: the SPECIAL-NAMES `LOCALE` clause is
  silently swallowed by a `genericClause` catch-all instead of reaching the ratified non-support diagnostic.
  `under_rejects`.
- **R07** — a one-line migrated stub, and the only one I could not flag from its own text. **So I traced it
  instead of guessing:** `BoundRaise` carries `WithLocation`/`Location`, `EcEmitter` writes them into the
  run-unit last exception status, and that status is exactly what `FUNCTION EXCEPTION-LOCATION` (§15.30) and
  `EXCEPTION-STATEMENT` (§15.32) return — with `EcFunctions.Location`'s own comment recording the fallback, "one
  space when no location information was saved". `BoundRaising` has no such field, so `GOBACK … RAISING` under
  `>>TURN … CHECKING ON WITH LOCATION` prints a space and says nothing. `wrong_answer` + `silent`.

⭐ **THE LESSON I want to keep is not about these flags.** The register was built to end five competing
worklists, and it did — the thing it could not do was notice that its own selector had a blind spot, because a
selector reports what it finds and never what it excluded. Every green `work.py next` was evidence about the
items it ranked and no evidence at all about the ones it filtered out first. **When a tool decides what you work
on, measure its complement**: the question is not "is the list right" but "what is not on it, and why". Both new
guards are that question, made mechanical.

⚠ And a smaller one, which cost the first version of this fix a wrong instinct: I nearly widened the predicate
to include `silent`. `silent` is not a harm — it is an adjective on one — and an item flagged silent alone is an
item whose frontmatter has not yet said what it actually does. That is now written on `HARM_FLAGS` so the next
reader does not have to re-derive it.

