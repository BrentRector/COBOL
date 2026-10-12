## PB2608: thirteen COBOL run outputs leave the repository root, and the root ignores them

The repository root tracked thirteen files that are program output, not source: `oo-ext.dat`, `oo-fac.dat`, `oo-obj.dat`,
`oo-sort-in.dat`, `oo-sort-out.dat`, `oo-two.dat`, `pb673oo.dat`, `l1m8rp-f.rpt`, `l1m8rp-fn.rpt`, `l1m8rp-o.rpt`,
`l1m8rp-p.rpt`, `pb1044.txt` and `pb1137ia.txt`. Each is the file a conformance golden's relative `ASSIGN` names. A
by-hand run from the root wrote them, and the `git add -A` of `d8f9168e7` (2026-10-04) committed them. The owner asked
for them gone before the morning: "Let's start the morning as clean as possible."

- All thirteen are deleted. Each name was grepped first: the only references are the goldens that write it and the
  note, so no test reads a root copy. The suites run every program in its own directory.
- `.gitignore` ignores `/*.dat`, `/*.rpt` and `/*.txt` at the root only. No tracked root file has those extensions, and
  a later `git add -A` after such a run can no longer commit one.
- kb/Work/PB2608 is landed.
