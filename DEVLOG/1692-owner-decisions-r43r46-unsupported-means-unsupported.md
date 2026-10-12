## Entry 1692 — 2026-09-24 20:46 PDT — Owner decisions R43–R46: unsupported means unsupported; implement the external repository; FIND-STRING overlapping; marked PDF corrections

**Why.** The owner asked to be walked through every open owner question. The first round — ten notes asking "may we stamp
DOCUMENTED-NON-SUPPORT on rows under a declined facility?" — drew: *"If we do not support a facility, we do not support
that facility. We should do all appropriate implementations to correctly not support the facility. What am I missing?"*
Nothing: the questions were bookkeeping (D13 reserves that verdict to the owner), not engineering doubt.

- **R43 — unsupported means unsupported.** Rules wholly or partly inside a declined facility or refused element, or
  triggerable only through one, close as DOCUMENTED-NON-SUPPORT by derived selectors once the compiler correctly refuses
  or warns; the live half of a split rule takes its own verdict; §4.4's user-run-unit rules close by derivation
  (extends PB386). A LEAKING decline is a defect (PB579 Q2). Answers PB1198, PB1151, PB1255, PB1517, PB1518, PB1519,
  PB579 Q1, PB1099 Q1/Q2, PB1536 Q1, PB468 Q9.
- **R44 — implement the §8.13 external repository**, using .NET assembly metadata as the repository (reference compiled
  assemblies; producing one updates the repository; a switch flags mismatches). §8.13 says the implementor *shall*
  provide both mechanisms; GnuCOBOL offers no model. Fixes PB1086's under-reject. (The tool recorded "document not
  provided"; the owner corrected it immediately.)
- **R45 — FIND-STRING counts overlapping occurrences** (PB314; spec silent, no GnuCOBOL model).
- **R46 — marked editorial corrections** at the five sites where the published PDF drops characters (PB738).
- Decided by standing rules without asking: PB935 (enforce §13.18.60.4 GR25 on the vendor ENTRY path; fix the golden
  that pins the permissiveness) and PB1536 Q2 (retire FMT-5.2, a section heading, not a rule).
- Still open, correctly: PB1548 needs spec adjudication before it is anyone's question.

