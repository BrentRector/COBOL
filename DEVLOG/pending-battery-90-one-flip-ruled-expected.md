## Battery #90: every compiler leg green; one GnuCOBOL flip, PROGRAM-ID. short., ruled expected

The operator ran battery #90 on main `f5fec711b`: every landing since battery #89 (DEVLOG 1933-2050), in a fresh
detached worktree, cold compile cache, 763 s wall. Every compiler leg is green. Conformance ran 11,708 of 11,708,
Unit 33,700 with no failures, and characterization 36 of 36. The NIST guard reads 362 MATCH and 0 REGRESSION, and
every static audit is clean.

The GnuCOBOL differential found one per-case flip. `syn_definition:37` compiles `PROGRAM-ID. short.`. GnuCOBOL
refuses it because `short` is a C keyword. The ISO text does not: SHORT is reserved only inside the DYNAMIC LENGTH
STRUCTURE clause (§8.10), so it is a legal program-name (§8.3.2.2.21, §11.10.2). At #89 our compiler also refused it,
but by accident: the generated C# failed to compile. That defect was fixed between the two batteries, most likely by
PB2839's single identifier mapping. The battery's build compiles and runs the program. The other three sources in the
case still refuse, each for an ISO reason.

The flip is ruled expected (kb/Work PB3187), and the per-case baseline is re-recorded from a full re-run with that one
row moved. The script's own verdict was NOT GREEN on the flip alone, a miss against #89's 0-flip bar.

The branch review found the ruling cited §11.10.3 as SR1-SR5. The clause has six syntax rules (SR1 for all formats,
SR2-SR6 for Format 1; SR6 is the RECURSIVE-in-an-initial-container rule). The note now names all six, checked with
`cite.py --check 11.10.3` on SR6, and lists §11.10.3 in its `spec_refs`. None of the six constrains a program-name's
spelling, so the verdict stands.
