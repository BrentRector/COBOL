## Entry 1171 — 2026-08-04 13:54 PDT — PB15: the entry said four functions, the standard said twenty, and the catalog's "single source of truth" was three mechanisms

**The register's top item claimed four findings. Measuring it first — the lesson this project keeps re-earning —
turned up eight broken functions inside a population of twenty, and behind them an architectural claim that had
been false for a year.**

`docs/COBOLNET_INTRINSICS_DESIGN.md` **D2** says the catalog "is the single source of result-category truth". It
was not. Three mechanisms decided a function's result category:

    IntrinsicCatalog                 the scalar IntrinsicType column
    IntrinsicBinder ~550             RuntimeMethod is "UpperCase" or "LowerCase" or "Reverse"   (CA25)
    IntrinsicBinder ~567             sig.Name is "MAX" or "MIN"                                 (V54)

ISO §15 gives **twenty** functions a type that depends on their arguments — a table in the function's own §15.x.1
clause, "The type of this function depends on the type of argument-1 as follows". A scalar column cannot express
that, so each one needed a hand-written exception, and five got one across two separate name lists. **Nothing
reminded anyone of the other ten, and nothing could: a name list has no way to say what it omits.**

**MEASURED before touching anything.** `MOVE FUNCTION <F>(<national args>) TO <PIC X>` must be rejected —
§14.9.25.3 SR10, Table 16, National row / AN column = No. Eight functions compiled it clean: **TRIM, SUBSTITUTE,
CONCAT, BASECONVERT, FORMATTED-CURRENT-DATE, FORMATTED-DATE, FORMATTED-DATETIME, FORMATTED-TIME**. All eight now
emit `COBOLNET0819`; the five already-correct ones are unchanged. ⚠ **The entry's harm flags were wrong too:** it
was filed `wrong_answer`, and the run-time value was correct in every case — the runtime bodies operate on UTF-16
strings, so only the LABEL was wrong. It is a silent **under-rejection**, which is a different defect and a
different fix.

**THE FIX IS A COLUMN, NOT AN ELEVENTH NAME.** `IntrinsicResultRule` now sits on the catalog row and
`IntrinsicResultType.Resolve` is the ONE reader — seven spec-derived shapes covering all twenty, of which
`FollowsArgument1` alone covers ten. Adding a function is a column, never an edit to the binder.

⭐ **Resolve returns the §15.2 TYPE, not the category, and that was the one real design choice.** The obvious
shape was to return a `PicCategory`, since that is what the bound node stores. But `ResultCategory` folds
Integer, Numeric and Index into `PicCategory.Numeric` — correct for every consumer that exists today, and exactly
why resolving to a category would have made the four integer-following rules **unrepresentable**: dead members
reading as coverage. Resolving the type keeps D1's §15.2 classification intact, so ABS/RANGE/SUM/the ALGEBRAIC
trio are already right and merely inert, waiting for the first consumer of integer-ness (PB17's territory).

⛔ **THE HALF THAT WOULD HAVE BEEN MISSED, AND IT IS THIS CODEBASE'S SIGNATURE SHAPE — SIX OCCURRENCES NOW.**
`BindTrim` and `BindSubstitute` parse phrase keywords, so they build their **own** `BoundIntrinsicCall` with a
hardcoded `PicCategory.Alphanumeric` and never reach the generic path. Landing the catalog column alone would
have left the two headline functions broken **with the fix present, the column correct, and every test green.**
Found by grepping every `new BoundIntrinsicCall(` site rather than by reading the one the fix was aimed at.

**THE GUARD IS DERIVED FROM THE SPEC, NOT FROM A LIST.** `IntrinsicResultTypeDriftTests` re-reads
`specs/ISO_COBOL.md` on every run and keys on the **structural** fact — a markdown table in §15.x.1 whose header
names the "Function type" column — so a function whose clause grows a table, or a new function that has one,
fails the build until its row declares a rule. It also asserts the converse (no invented rules), that the
resolver is wired, that every enum member has an arm, and that no bespoke bind path hardcodes a string category.

⚠ **Keying it on the TABLE rather than the sentence is the whole trick, and the first cut got it wrong.** The
clauses do not agree on wording — §15.38.1 "depends on the argument type", §15.39.1 "depends on the type of
argument-1", §15.18.1 "depends upon the argument types" — and a prose key found **ten of the twenty**. A dead
guard inside the guard against a dead lookup.

**THREE THINGS I GOT WRONG, EACH CAUGHT BY RUNNING SOMETHING RATHER THAN READING IT.**

1. **A "sibling defect" I found, fixed, and then had to un-find.** `FUNCTION REVERSE(grp)` over a group of
   `PIC N` children compiles clean into a `PIC X` receiver, and `OperandCategory` discards every group's category
   while `IntrinsicArgumentRules.ClassOfPlace` derives it — one rule written twice, disagreeing. I implemented
   the §8.5.2.1 group arm and measured: **still accepted**, because the premise was false. §8.5.2.10 item 3
   defines a national group as one carrying `GROUP-USAGE NATIONAL` (§13.18.29); a group whose CHILDREN are
   national is an ordinary **alphanumeric** group, so both the alphanumeric result and the accepted MOVE are
   CORRECT. GROUP-USAGE is not modelled here, so no group this compiler can express is anything else. Reverted —
   and the revert mattered for a second reason: the change would also have silently tightened
   `CheckRepertoireArgs` (DISPLAY-OF / NATIONAL-OF) on an unrelated axis. `validate_the_premise_not_only_the_rule`
   — the citation was right and the finding was still impossible, because the construct it needed cannot be
   written.
2. **I cited §13.18.27 for GROUP-USAGE and `cite.py --check` rejected it** — the clause exists and says something
   else. The real one is **§13.18.29**, and the sharper authority is **§8.5.2.10 item 3**. This is precisely the
   failure rule 1 names: not inventing a citation, but writing down a plausible number without re-deriving it.
   Two lines of prose, one command, one wrong clause caught before it propagated into a code comment and a note.
3. **The new anti-pattern guard failed on its own documentation.** `TheResultTypeRule_IsActuallyWiredIn` forbids
   the deleted name lists from reappearing — and matched the COMMENT explaining what they were. It now strips
   `//` lines first. Left unfixed it would have taught the next person to delete the explanation rather than keep
   the invariant. **Found only because the guard was proven in the failing direction before being trusted**
   (`feedback_green_gates_arent_evidence`): removing TRIM's column fires the population assertion by name,
   injecting a real name list into CODE fires the anti-pattern one, and the comment alone fires neither.

**TWO CLAIMS CHECKED RATHER THAN TRUSTED, BOTH HELD.** §15.19 CONVERT's catalog comment said the category is
"computed per §15.19.1"; `BindConvert` genuinely does it (`dst == 3 ? National : Alphanumeric`) — so CONVERT was
never broken, which is worth recording given PB8 is the standing proof that an entry's own root cause is a claim.
And two rows of §15.18.1 CONCAT (Boolean/Numeric **usage National**) are unreachable: `PIC 9 USAGE NATIONAL` is
the §13.18.40.4 SR12 national-form leg, staged loud at the DATA DIVISION, so no such item can reach a CONCAT
call. No arm was written for them — that would be dead code reading as coverage.

⚠ **A METHOD NOTE AGAINST MYSELF.** I launched a ten-agent measurement fan-out over the same population and then
rebuilt the compiler underneath it while it ran, so its agents were probing a moving binary and its refuters
re-ran probes against a tree where eight of the defects were already fixed. Its spec derivations and its
unexplored questions (MAX/MIN's Index and all-integer rows, the ALGEBRAIC family, whether integer-ness is
observable at all) stay useful; **its accept/reject measurements do not, and none of the numbers above come from
it** — every one is a before/after I ran myself and can reproduce. Do not start a measurement fleet and then edit
its subject.

**Goldens:** `2023/pb15_result_type_follows_argument` exercises BOTH rows of the table for eight functions, so a
"fix" that labelled everything national fails it — the failure mode a negatives-only suite cannot see. Four
negative fixtures pin the rejection, each at every edition where its function exists.

