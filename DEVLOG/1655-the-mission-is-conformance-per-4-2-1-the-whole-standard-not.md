## Entry 1655 — 2026-09-22 23:28 PDT — The mission is conformance per §4.2.1 — the whole standard — not "per §4.2.16"

Owner, reading the ledger: "Why do we say v1.0 is defined as 100% conforming per ISO §4.2.16 … The compiler(s) should
be a faithful implementation of the ISO COBOL specification. Not simply one section of it." The owner is right, and
the page was wrong. §4.2.16 is **User documentation** (`OK §4.2.16 (User documentation)`); the conformance definition is
**§4.2.1** (`OK §4.2.1 (General)`: "shall provide the required normative elements specified in Clause 6, Reference format
through Clause 16, Standard classes and optionally meet the normative elements identified in A.4 … and meet the criteria
of 4.2.2 through 4.2.17"). D13 has always been §4.2.1 — CLAUDE.md, PROMPT.md and plan §0 say so — but the mission was
re-typed as "per §4.2.16" into the ledger generator and five kb/docs pages (DESIGN-spec-conformance-review.md,
kb/Context/Goals.md, kb/Search/Frequently Asked Questions.md, kb/Spec/Constraints.md, kb/Spec/Overview.md). All six
now cite §4.2.1, and the ledger's mission sentence says what it means: every required element of Clauses 6–16, every
criterion of §4.2.2–§4.2.17 (the §4.2.16 documentation among them), Annex A.4 modules implemented or documented as
not provided — in each of the four editions — measured as every normative rule at zero GAP. Ledger v65.

⚠ The citation audits passed every one of these: the clause number is real and its heading is about conformance
documentation, so a check that validates quoted fragments cannot see it (`a_real_clause_can_answer_a_different_question`).

