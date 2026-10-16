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
