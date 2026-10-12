## Entry 789 — 2026-07-11 14:45 PDT — P7 Step 5: the ACCEPT-verb partials renamed (the Visitor-term collision ends)

**What (Step 5 — executed BEFORE Step 4, a trivial order swap: the two are independent and the rename is
5 minutes).** `git mv Binding/Bound/StatementBinder.Accept.cs → Binding/Procedure/Verbs/AcceptDisplayBinder.cs`
and `CodeGen/CSharpEmitter.Accept.cs → CodeGen/Verbs/AcceptDisplayEmitter.cs` — pure renames, contents stay
partials of StatementBinder/CSharpEmitter (the real class splits are Steps 10/9). The two target directories are
NEW (the Step-9/10 homes). The collision was navigational, not compile-level (nothing declares a member named
`Accept`; the generated `Accept<T>` is an extension), but `CSharpEmitter` IS an `IBoundStatementVisitor` since
6b, so a file named `*.Accept.cs` misleadingly read as the visitor partial. Doc-xref sweep per the audit: 6
forward-looking docs repointed (PHASE-07:482 grep list with today's line numbers; DESIGN-binder-bound-tree §4
rename row RECONCILED — its `*.AcceptStatement.cs` target was superseded by the AcceptDisplay* names of
PHASE-07/module-topology row 27, per feedback_propagate_reconciliations; PHASE-08:549 + PHASE-11:112/:492 +
SURVEY-xcut:247 anchors updated). LANDED note added to the phase doc: `BoundAccept` + `AcceptKind` (bound-NODE
types) rode along into the Verbs/ file — the Step 9/10 split must relocate them to the bound-tree home.

**Verify.** Compile-only rename: sln builds; 281 unit · 32 characterization green (identical IL).

