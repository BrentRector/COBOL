## Entry 1417 — 2026-08-31 12:04 PDT — Burn-down cluster B lands with its own review fleet folded in: a widening that crashed on legal source, a probe whose Place escaped into the bound tree, three drift guards that could not fire on their own witnesses — and an owed-differential list rewritten from measurement

The PB169–PB172 wave was gated green and ready to commit. A review fleet ran against it first and returned 32
confirmed findings; this entry lands the wave and all fifteen dispositioned fixes together, because every one of
them is a hole in the wave itself. Nine new register notes, [[PB216]] through [[PB224]].

The through-line is uncomfortable and worth stating plainly: **the wave's own thesis is that a rule must be
declared in one place and guarded from outside, and the wave shipped three guards that could not fire on the
defects they were written for.** That is the shape [[feedback_green_gates_arent_evidence]] names, three times,
inside the change that was supposed to institutionalise it.

### The widening that crashed on legal source

[[PB169]] moved the STOP RUN / GOBACK `STATUS` literal off the arithmetic funnel and onto
`ExpressionBinder.LiteralOperand` — THE one §8.3.3 literal→operand mapping — and then screened **two of the six
shapes that mapping returns**. Measured on the pre-commit tree, at `--std 2023`, strict:

    STOP RUN WITH ERROR STATUS SPACE.        -> clean compile; NotImplementedCobolFeatureException at run time
    STOP RUN WITH ERROR STATUS ALL "5".      -> clean; "bound operand 'BoundAllLiteral'"
    STOP RUN WITH ERROR STATUS B"01".        -> clean; "boolean literal in a numeric context"
    STOP RUN WITH ERROR STATUS B"1" & B"0".  -> clean; the same, through the concatenation fold

**The remedy is a rendering, and getting that backwards would have minted the very defect PB169 exists to
close.** §8.3.3.6.3 SR1 admits a figurative constant "whenever 'literal' appears in a format", narrowed only
where the literal is restricted to a numeric literal — and §14.9.42.3 SR3's conditional, *"**If** literal-1 is
numeric, it shall be an integer"*, is the proof that it is not. §8.3.3.6.4 GR3's NOTE 2 names the STOP statement
BY NAME and gives the figurative a length of one character. These are conforming programs whose values
`docs/CONFORMANCE.md` item 192 already fixes. A sixth COBOLNET1704 arm would have been PB169's error, one
literal form later.

So `NumericRenderer.StatusNum` routes the whole non-numeric literal family through
`OperandText.AsString` — already THE ONE operand→character-image reader, already carrying GR3 b (one character)
and GR3 c (`ALL literal-1` once), already PCS-aware for HIGH-/LOW-VALUE — and then through the published
`FromAlphanumeric` decode. One arm covers figurative, ALL, symbolic-character, boolean, national, hex and
concatenation, so the next literal form needs no edit. Every expected exit was derived from the determination
before the run and every one matched: `SPACE` ⇒ 0, `ALL "07"` ⇒ 7, `B"01"` ⇒ 1, `B"1" & B"0"` ⇒ 10,
`X"3037"` ⇒ 7. [[PB216]].

**The structural root is the two-arm shape, inside one method.** The identifier arm had a completeness structure;
the literal arm had two ad-hoc `is` patterns over a six-shape mapping. That asymmetry also explains the fourth
hole: SR3 and SR4 were written inside the `sp.literal()` PARSE ARM, while §13.10.4 GR1 sends a constant-name's
substituted literal through the `sp.dataReference()` arm — so `01 KFRAC CONSTANT AS 1.5.` + `STATUS KFRAC`
compiled clean and exited 1. The screen is now keyed on the BOUND SHAPE and reached from both arms.

### `Pic` guarded by `IsGroup` — the spelling its own reader forbids by name

The same wave's SR2 screen opened `if (p is RefModPlace || p.Item.IsGroup) return false;`. `DataItem.OperandPic`'s
doc comment reads: *"⛔ THE ONE READER for an item's OPERAND category (D20) … Every site that decides class,
category … reads THIS — **never `Pic` guarded by `IsGroup`**."* Measured with the guard temporarily restored:
`STATUS WS-D(2:2)` on a `PIC X(3)` and `STATUS <GROUP-USAGE NATIONAL group>` were both rejected COBOLNET1704 —
**with a diagnostic quoting the rule the operand satisfies.** §8.4.3.3.4 GR6 gives a slice "the same class,
category, and **usage**" and the three lettered exceptions rewrite class and category only; §13.18.29.4 GR2 b
makes a national group an item of usage national. Reading `OperandPic` plus one GR6-aware `RefModPlace` arm
settles all four group kinds and both slice kinds with no hand-list — and the BIT group and the alphanumeric
group stay rejected, which is the control. [[PB217]].

### The probe whose Place escaped — the fleet's real catch, and the only MAJOR

`ReferenceResolver.Probe` has always been documented as "a TYPE-DISCRIMINATING sniff **whose Place is discarded
after reading its Item**". Everything `_probing` suppresses — diagnostics, OO op registration, D18
materialization — is correct on that premise. **Four call sites falsified it** by committing the probe's `Place`
into the bound tree. Measured:

    CALL "SUB" USING BY CONTENT E(XE).   -> compiles CLEAN     (XE PIC X(4), E PIC X OCCURS 3)
    MOVE E(XE) TO R.                     -> COBOLNET0844

One rule, one reference, two verdicts — and inside ONE statement, since CALL's `BY REFERENCE` arm commits
through the screened path and its `BY CONTENT` arm probed. The other half is a **wrong answer**, not a missing
diagnostic: `MaterializeViaFragment` short-circuits to the literal `"1"` under `_probing`, so
`BY CONTENT E(FUNCTION INTEGER(2))` bound occurrence **1**. That half has been live since [[PB157]] landed the
flag on 2026-08-29, under a note asserting "Every Probe caller is pure now".

**The fix is the RETURN TYPE, because a comment could not hold the invariant.** `Probe` now returns a
`ProbeResult` — `(DataItem Item, PicCategory? OperandCategory)` — and not a `Place`. The compiler then found
every leak itself: five errors, each at a site that had committed the sniff. The four now probe to discriminate
and `Resolve` to commit, the pattern `SetBinder.BindSetLocale` already used one file away. A fifth site was
looked for and does not exist; all thirteen `Refs.Probe` calls were read. [[PB221]].

### Three guards that could not fire on their own witnesses

- `NoScreenKeysOnAHandWrittenContextList`'s regex required at least one `or` — and the defect its own docstring
  names, [[PB172]]'s `BindPrimary` screen, was `context is OperandContext.Arithmetic`, a SINGLE member. It also
  read one file. **The guard could not have fired on the defect it was written for.** Widened to any identifier,
  any arity, every `.cs` under `Binding/`, and proved failing first — where the FIRST attempt at the proof
  failed to fail, because the tripwire was written fully-qualified and the pattern does not match that.
- `TheThreeAxes_AreWhatTheClausesSay` was four hand-written `InlineData` rows, so a fifth `OperandContext`
  member could take transposed r7/SR10 columns and stay green. Now driven from `Enum.GetValues`; proved by
  removing a row.
- `OperandWalkCoverageTests.Covered` memoized VISITATION, not the verdict, so an uncovered rule that was
  absorbed by a later alternative answers `true` on its next visit. Latent on today's grammar — measured, not
  assumed — and latent is not fixed. [[PB224]].

### One rule, one lane posture

§13.18.38.3 r7 is enforced at four sites under two `--permissive` postures, and [[PB170]] put one POSITION on
both sides of the split: `W(IX:2)` was a hard error under `--permissive` while `W(IX / 1:2)` warned and ran, the
same rule and position, keyed on nothing but whether the token renderer could render the bound. The axis that
settles it is what the SLOT IS: §8.4.3.3.3 SR4 makes a ref-mod bound an arithmetic position, so it takes the
occurrence-number coercion; an IDENTIFIER slot rejects in both lanes **because there is no coercion to offer**,
and that reason is now written at both identifier sites rather than left to look like an oversight. Both shapes
now agree in both lanes and both print `RM=BC`. [[PB219]].

And the fast path's screens fired INSIDE the token loop, whose exits to D18 are order-dependent, so
`MOVE E(XE ** 2)` emitted COBOLNET0844 **twice** in both lanes. A `_diagnosed` set cannot fix that — the second
diagnostic comes from another class over another node — so the screens are queued and flushed only when the
renderer commits. Deduplication by control flow, which makes the next late exit automatic. [[PB220]].

### A citation that enforced nothing, and the tool that minted it

[[PB171]] added a comment and a diagnostic quoting §8.3.3.6.3 SR1 a — "without the ALL phrase" — on a claim about
the grammar that is false: `figurativeConstant` has a distinct `ALL ZERO` alternative, so `fig.ZERO()` is
non-null for `ALL ZEROS` and the admitting arm took it. `IF ALL ZEROS IS POSITIVE` compiled clean and evaluated
`0 > 0`. Re-derived rather than inherited: §8.8.4.7.3 SR1 makes the sign-condition operand an arithmetic
expression, §8.8.1.1 restricts the literal there to a numeric literal, which is exactly SR1 a's antecedent — the
bar binds and is now enforced. [[PB218]].

Separately, `cite.py`'s rule path walks back to the nearest numbered marker, so a citation of a clause's
UNNUMBERED prose is stamped with whatever ordinal precedes it. `--check 8.8.4.2.1 "A relation condition shall
contain at least one reference to an operand that is not a literal"` prints `OK §8.8.4.2.1 13)`, and item 13 is
"Two compatible variable-length groups". **The witness is in this repository**: [[PB182]]'s sweep repaired the
phantom §8.8.4.1.1 and wrote `§8.8.4.2.1 r13` at two sites, taking the ordinal from this tool. Both are corrected
to the bare form; the tool defect is [[PB222]], open.

### The owed list, rewritten from measurement

The pre-battery sweep pre-declared ~32 differential cases owed to this batch. It was a list of cases that
MENTION the construct, and an owed list that can absorb 32 unexpected flips is [[PB209]] wearing a new coat.
Re-measured through the shared extractor with the population asserted first (1,323 extracted == 1,323 baseline
rows), every bucket now carries a derived direction per case, and **the prediction is ZERO movement, in every
bucket, in both directions** — recorded in `kb/Work/PB169.md` for the battery runner to read. Highlights: 12 of
the 14 STATUS cases carry no operand at all, and `used_binaries:850` — nominated as the one possible gain — is
rejected on `COBOLNET0817: ACCEPT FROM 'COMMAND-LINE'`, nothing to do with the phrase; the refmod bucket's
AGREE_ACCEPT exposure is five cases, not six, and every bound is a `PIC 9` name; PB172's widening matches **no**
corpus case at all (the "8" was a count of compiler SITES); and the r7 lane fix has zero reach by construction,
because `gnucobol_differential.py` never passes `--permissive`.

### The owner question, closed under the protocol

PB169 recorded that item 192 sends `STOP RUN WITH ERROR STATUS "ABEND"` to exit 0, indistinguishable from NORMAL,
and left it open pending a survey. The survey ran. GnuCOBOL 3.2 binds the phrase to ONE integer and hands the
operand's value to `cob_stop_run` → `exit(status)`, substituting 1/0 only when the operand is ABSENT; no arm
anywhere gives a non-numeric operand a distinguished exit, its testsuite pins only the operand-less forms, and no
reachable vendor documentation differs. **Consensus supports the published determination, so it stands and the
row is closed — with no behaviour changed on the strength of a survey.**

### Also landed

The last two copies of the sole-primary descent are gone: they survived [[PB172]]'s collapse because one was a
list-pattern re-implementation and the other lives in `Cobol.Net.Frontend`, out of reach of a compiler-side
helper. The shared body moved DOWN to `SoleOperand`, where the parse trees are, and both layers read it.
`EvaluateBinder.BindValueOperand` is now arm-for-arm identical to `ConditionBinder.ComparisonOperandOf` —
§14.9.13.4 GR2 makes them one question, and they differed in short-circuit ORDER (harmless, and agreeing by
luck) and in the tail, where EVALUATE built a raw computed wrapper that would compare a user-function's
alphanumeric result numerically. `ScreenResultant` was ASKED whether it is a fourth class-answer copy and the
answer — no; it is the receiving side, and it turns on an axis §8.8.1.1 does not have — is now written down at
the site rather than rediscovered. Two stale doc comments were repaired inside the change set that refuted them
([[PB223]]), including `IndexNameExpr` still calling the STATUS slot "a true arithmetic position", which is the
exact premise the same wave's new doc denies.

### Gate

Wave-local, on the landing tree: Conformance **4,058 / 4,058** on the filter
(`~Corpus|~Status|~Operand|~Index|~Subscript|~RefMod|~Arithmetic|~Condition|~Evaluate|~Call|~Invoke|~Pointer|~Set|~Intrinsic|~Directive|~Boolean|~Figurative|~Literal|~Concat|~Stop|~Goback|~Probe|~VersionMatrix`)
· Unit **5,103 / 5,103** (full) · Characterization **33 / 33**. The four new negative fixtures were confirmed to
RUN by name, not merely to be registered. **Battery #40 is owed and has NOT run**; its GnuCOBOL leg has the
measured owed list above, and there is no slack in it.

---

