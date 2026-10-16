## PB1400: an arithmetic-operand refusal names the §8.5.2.1 class of the operand

Wave 1095, group J (kb/Work PB1400, GAP campaign PB2911).

**Re-probe.** On this branch's base build, `COMPUTE N = GB + 1` with `01 GB GROUP-USAGE BIT` drew COBOLNET0844
"group item 'GB' (class alphanumeric, ISO §8.5)". The refusal is right (§8.8.1.1 admits only a numeric data item);
the reason was wrong. §8.5.2.1 gives a bit group class boolean and a national group class national, and §8.5.2.5 3)
and §8.5.2.10 3) reach a subordinate group "implicitly described" by the clause. The same text appeared for the
subordinate groups GBS and GNS, for the national group GN, and for a strongly-typed group (whose class and category
§8.5.2.1 makes its type-name). The arm beside it said "class alphanumeric" for every reference-modified operand, but
§8.4.3.3.4 GR6 keeps a reference-modified national item class national. `ExpressionBinder.NonNumericOperandKind` had
spelled the class itself in both arms, while LENGTH, MOVE and the class screen read it from the one classifier.

**Fix.** `IntrinsicArgumentRules.ClassNameOf(Place)` spells a place's class through `ClassOfPlace` and `Name`, the
one Table-2 spelling. A strongly-typed group is the one exception: the function names its type-name, because
`ClassOfItem` deliberately answers alphanumeric for it under §15.3 2)'s argument-position rule. The ref-mod, group
and trailing arms of `NonNumericOperandKind` now ask it.

**Sibling sweep.** The same mechanism (a class spelled outside `Name`) showed up wherever a `CobolClass` was printed
with `ToString().ToLowerInvariant()`: a numeric-edited subscript read "of class numericediteddeediting". Every such
message now goes through `IntrinsicArgumentRules.Name`: ReferenceResolver (subscript/ref-mod position),
DataBinder.Reports (CODE), CallBinder (CALL target, CANCEL target, BY VALUE), OoBinder (INVOKE BY VALUE), UdfBinder
(function BY VALUE), EvaluateBinder (range classes), AllOrNothingClass and ExpressionBinder's other two screens.
A Sr14 compiler-defect message that said "class" of a PicCategory now says "category". The new Unit drift test
`ClassSpellingDriftTests` fails on any interpolated class word built from an enum's `ToString()`. Two other screens
that describe a group as just "a group item" (`OperandClassScreen.Describe`, `ScreenResultant`) state no class and
are unchanged.

**Goldens.** `2002/pb1400_group_usage_operand_class` exercises a bit group and its subordinate group as boolean
operands (B-XOR, B-AND), and a national group and its subordinate group as national operands (MOVE, relation).
Every expected value is derived from §8.7.2, §13.18.29.4 GR2 b), §14.6.8.5 and §8.8.4.2.10 2). The negative
`negative/pb1400-group-usage-arithmetic-operand` expects COBOLNET0844 at 2002, 2014 and 2023.
`ArithmeticOperandClassTests.TheRefusal_NamesTheSection8521ClassOfTheOperand` pins the text at each edition.

**Rows.** GR-8.5.2.5-3 and GR-8.5.2.10-3 went from PARTIAL to CONFORMS, taking GAP from 83 to 81
(batch `w1095j-pb1400-group-usage-class`).

**Review round 1.** The branch reviewer found the refuted rule still stated as fact in the class doc of
`ArithmeticOperandClassTests`: "A group item (class alphanumeric, §8.5)". The drift test had scanned only
`ExpressionBinder.cs`, so the claim survived there and in eleven other places: the `ExpressionBinder` class doc
itself, CORRESPONDING rule 3, the MAX/MIN string arm in `IntrinsicBinder` (which admits national and bit groups),
the group-comparison note in `OperandText` (§8.8.4.2.1 treats only an alphanumeric group as elementary
alphanumeric), `TierCRejectionTests`, and four corpus fixtures. The COBOLNET1796 message text, its binder comment and
the `pb231-external-pointer-item` fixture were wrong differently: they called a strongly-typed EXTERNAL group "class
alphanumeric" where §8.5.2.1 makes its class its type-name. The SR4 verdict those explain is unchanged. All were
corrected, and `ClassSpellingDriftTests.NoSourceOrCorpusComment_SaysEveryGroupIsClassAlphanumeric` now scans every C#
source and corpus program for the universal claim. The scan reads one line at a time, so it missed the pb59 claim,
which a line break split; that one was corrected by hand.

**Review round 2.** Two findings. First, round 1's `OperandText` rewrite replaced one wrong universal with another: it
said an alphanumeric group is the only kind that holds BINARY or PACKED leaves. A strongly-typed group can hold them
too. The comment now says so and explains why such a group never reaches the image as a compare operand: §8.8.4.2.3
SR1 admits only a same-type pair, and §8.8.4.2.12 compares that pair element by element. A DISPLAY of one was probed
and shows the BINARY leaf's true bytes, as the comment says. Second, two corpus fixtures still inferred the class from
groupness ("is a group item and so of class alphanumeric"), a shape the line-at-a-time pattern did not have. The
pattern now has it, and it reads whole files with comment continuations joined. The widened scan found four more
sites (the `IntrinsicResultType.OperandCategory` doc, the `SortEmitter` group-key arm, `TierCRejectionTests` and
`da5_table_sort_group_key`), and all six are corrected. Run over the pre-PB1400 tree it reports 19 sites, the pb59
split line among them. A new theory, `TheUniversalClaimPattern_CatchesTheFormsItWasWrittenIn`, pins the forms it
catches and the true statements that must pass, since a zero over the tree cannot show the pattern works.
