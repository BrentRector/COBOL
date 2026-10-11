## Entry 1282 — 2026-08-09 18:15 PDT — PB59 family 7b: one classifier answers every shape, and a wrong answer, a crash and an open screen all trace to the same null

The deferred half of family 7 landed on three probes that each showed a different face of one defect.
`OperandCategory` answered null for groups, ALL literals and (differently wrong) ref-mod views — and
each null was someone's problem: `MAX(N9(1:1) "A")` ANSWERED 0 on legal source (the ref-mod arm read
the INNER numeric category while its own doc cited §8.4.3.3.4 GR6 — GR6c rewrites the view to class
alphanumeric, the list is uniform, and "A" wins by §8.8.4.2.7); `MAX(G1 G2)` CRASHED at run time
("no numeric render recipe" — the type resolution saw no category and picked the numeric row while
`IsStringOperand` independently said groups are strings, the exact two-halves split both sites' PB48
comments warn about); and `DISPLAY-OF(group)` sailed past the §15.26.3 r1 class-national screen —
AR-15.26.3-1's own repro, whose note's root-cause paragraph had predicted all of it.

The landed shape: the ONE classifier totalizes — a group answers its §8.5.2.1 class (alphanumeric;
the National/Boolean arms are the staged GROUP-USAGE/bit-group legs, same shape as ClassOfPlace's), an
ALL literal its literal's category (GR9), a ref-mod view the GR6 rewrites through the ONE
`RefModPlace.CategoryOf` reader — and `IsStringOperand`'s field arm now CONSULTS OperandCategory, so
the body choice and the type resolution cannot drift apart again. The former null-for-groups doc
defended itself as "the honest answer"; its own §8.5.2.10 analysis (a group is national only under
GROUP-USAGE) proves every expressible group is FIXED category alphanumeric, so the doc is rewritten to
carry the measured cost instead. Verified both directions: MR=A, MG=WXYZ, DISPLAY-OF(group/ALL "A")
→ 1546 (two negatives), NATIONAL-OF(group) and DISPLAY-OF(SPACE) stay admitted (the golden's NG/SP
legs — the PB25 figurative channel re-verified live). Wave-local: Conformance 1030/1030, Unit 57/57,
SpecTraceability 10/10 after AR-15.26.3-1 → CONFORMS (GAP 4157 → 4156; the row sits outside PB59's
28). Residue recorded on the row: the national-edited leg rides the 0899 staging until it lands.
Next: the BASECONVERT screen set (PB59's last member), then one battery for the 7b+BASECONVERT batch.

