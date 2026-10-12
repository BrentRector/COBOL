## Entry 1367 — 2026-08-29 03:31 PDT — PB131 LANDED: AS NESTED resolved at BIND time — GR9 takes a keyword-less argument's mode from the CORRESPONDING FORMAL, SR15's scope is a compile-time check (COBOLNET1676), and the sweep found five of our own sibling spellings

The mechanism: BinderDriver.NestedCallablesOf builds a per-caller table — the caller's directly contained
children plus every COMMON program contained in a transitive ancestor, nearest wins, the §10.7.2 visibility
computed statically — riding the bind-ordering guarantee that ALL units' data (including LinkageFormals with
the ByValue flag) binds before any procedures, the UserFunctionSignature precedent. BindCall resolves the AS
NESTED literal against it: a hit hands the callee's bound formals to the USING loop, where a keyword-less
Format-2 identifier now derives its pass mode from ITS OWN formal (§14.9.4.4 GR9) instead of GR5's Format-1
transitive mode; a miss is COBOLNET1676 at compile time, where the old binder bound the NESTED flag,
DISCARDED it, and let ProgramTable.ResolveVisible rule 4 / ProbeSiblingModule activate any outermost program
at run time. The probe that proved the arm: `USING BY VALUE A B` against `BY VALUE LA BY REFERENCE LB` —
B=0005, the callee's writeback visible, where the old transitive mode passed B detached and the store was
silently lost on conforming source (the DIVERGES this row carried).

The diagnostic moved OFF the staging code: CallAsNestedScope (COBOLNET1676) is the ONE §14.9.4.3 SR15
diagnostic — both sentences, literal-1 required and the containment scope — replacing the 0899-staged
CallAsNestedNeedsLiteral, because a permanent conformance rejection is not recognized-not-implemented debt.
DIAGNOSTICS.md regenerated; sentence 1 gains its first negative pin (an identifier target with AS NESTED).

THE GATE FOUND FIVE OF OUR OWN SIBLING SPELLINGS — the first PB131 gate went red exactly where it should
have: pb84's pair, udf_by_value, the NBLIT07 fact and the pb6 negative all spelled `CALL … AS NESTED` at a
SIBLING callee (a PB130-era respell that predates the scope check). Each was restructured to genuine
containment — the callee moved inside the caller, byte-identical text, only the END PROGRAM placement
changed — rather than the check weakened. GR5's last residue also closed: the Format-1 transitivity golden
(85/pb131_gr5_transitivity, `BY CONTENT A B BY REFERENCE C D` with the visibility of each bare argument
derived from §14.2.3 GR8/GR9) replaces the legacy-baked differential as the rule's only witness.
SR-14.9.4.3-15 → CONFORMS, GR-14.9.4.4-5 → CONFORMS, GR-14.9.4.4-9 DIVERGES → PARTIAL,
SR-14.9.4.3-20 NOT-IMPLEMENTED → PARTIAL, GR-14.9.4.4-6 → PARTIAL (the §14.2.3 GR9 second-regime
BY CONTENT allocation rides PB133 with the rest of the activation boundary).

