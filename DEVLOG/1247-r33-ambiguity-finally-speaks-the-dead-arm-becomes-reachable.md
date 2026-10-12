## Entry 1247 — 2026-08-08 19:39 PDT — R33: ambiguity finally speaks — the dead arm becomes reachable, and the 88 siblings come along

An ambiguous unqualified name resolved FIRST-MATCH (list[0]) — a wrong-answer shape: the program
compiles and reads a different item than another compiler would pick. §8.4.2.2.1 requires
qualification when spellings collide. Measured before enforcing (762 programs, zero hits), and the
design constraint resolved cleanly: TryResolve returns ONE namespace tier — the §8.4.6.2.1 rule-3a
method overlay OR the unit map, never mixed — so a plural list IS genuine ambiguity and the check
lands at ResolveUnqualified with the tier fact documented. Strict: null → ReportUnidentified's
N-declarations arm, which R30 built and nothing could reach until now. --permissive: first-declared
match, warned (the DA6/R29 disposition shape).

The rule-4 sweep found the 88 siblings in ConditionOf: the unqualified plural-88 form and the
qualified form's FirstOrDefault both bound the first match silently — both now report (strict error +
bind-first-anyway so no caller fallback runs a different reading; permissive warns).

Probes: duplicate data-names and duplicate 88s, both axes. Fixtures unqualified-ambiguous-name +
condition-name-ambiguous (all four editions). Gate: Corpus|Intrinsic|Udf 713/713 · the three ambiguity
fixtures by name · full Unit 4099/4099 · characterization 33/33.

