## Entry 2044 — 2026-10-14 18:42 PDT — A method's CONSTANT entries are the method's own (kb/Work PB3164)

**Re-probe (w1080e build, base 2b60d9fa9).** The note's repro p10 printed `A A` / `B A`: method SHOWA's
`01 K CONSTANT AS "A"` replaced the object's data item K in its sibling SHOWB. p11 (each method its own K) drew a false
COBOLNET1547, §13.10.3 SR9 compared across two methods. The sibling arms measured beside them were all wrong too: a
method's own data item lost to its object's constant of the same name (`A O`, expected `A M`); a method's `PIC X(N)` took
a sibling method's constant N (a 3-position item where 4 was declared); a method could not `ADD 1 TO N` against its
object's data item N (COBOLNET1548, "constant-name as a receiving operand"); and two interface prototypes declaring one
constant-name drew SR9.

**Rule.** A method definition is a source unit, so a source element of its own (§3.164). The constant-names it declares
are local names (§8.4.6.2.2; §13.18.27.3 SR4 bars GLOBAL in a method definition), and a local name "may be referenced only
in the source element in which it is declared" (§8.4.6.2.1). The object's and factory's constant-names are global names
(§8.4.6.2.2), so a method sees them, and §11.7.4 GR5 makes any word a method's data division defines hide the containing
definition's declaration of that word inside the method. SR9 compares a duplicated constant-name within one source
element: the PB1009 determination, now applied to methods.

**Fix (root).** The class binder kept ONE constant table for the object or factory and every method it bound, so a method
constant was in effect object-wide. The table is now a per-source-element `DataBinder.ConstantScope` (bound constants,
declared entries and, for a method, the other words its data division defines): the binder's own, plus one per method
(`OoMethodDataScope.Constants`). `DataBinder.ConstantScopeOf` is the one answer to which element's constant a word names,
and every reader asks it: `FindConstant`, `IsFailedConstant`, `ClosesConstantCycle`, `NamesConstant` and the
PICTURE-expansion guard. A declared entry carries its element (`DeclaredConstant.Scope`), so both its value and its SR9
comparison stay inside that element. The two declaration walks (the pre-bind walk and the in-order fallback) now share
`DeclareConstant`. The pre-bind reader (`PreBindConstants`) already walked these scopes over the parse tree, so the two
readers now agree. `COBOLNET_OO_DESIGN.md` names the rule beside the method data-name precedence.

**Goldens.** `2002/pb3164_method_constant_scope` covers the instance and factory arms, the hiding of an object's constant
by a method's index-name and condition-name, PICTURE repetition by a method constant declared after its user, and a
method reading its object's data item and constants. Negative `pb3164-method-duplicate-constant-unequal` shows that SR9
inside ONE method still refuses, and it compares against that method's own entry, never against the sibling's.

**Rows.** GR-11.7.4-5 and SR-13.10.3-9 already read CONFORMS, a false green that this landing makes true for constant
entries. No row moves, so `closes_rows: []` with its reason.

**Self-review / sweep.** Every reader of the old `_constants` / `_declaredConstants` pair was rerouted (grep: only
`DataBinder.Constants.cs` and `NamesConstant` touched them). The sweep found two more places where a method's
declarations are not method-scoped. They are separate mechanisms, so they are reported as leads, not fixed here. First,
a method's TYPEDEF joins the class's one `TypeDecls` table (a second method's type-name of the same spelling is a false
COBOLNET1529). Second, a constant's `LENGTH OF` operand resolves during the method's data bind against the class-wide
name maps, so it measures the object's item instead of the method's (`A 3` where `A 5`), or reports "not defined".

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261015T011512Z-416a78; Conformance
6,105/11,677 · Unit 33,146/33,650 · Characterization 6/36 in leg 1). The golden and the negative both passed in leg 1.
`semgrep/verify.py` PASS with no count raised. The Linux gate is the lander's (batched gating).

