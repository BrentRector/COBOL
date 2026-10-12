## Entry 233 — CALL USING BY CONTENT / BY REFERENCE is transitive (IC224A + IC225A → CLEAN)

Resuming the NIST effort on the IC (inter-program communication) suite. IC224A (22 FAIL*) and
IC225A (3 FAIL*) both failed with "VALUE OF DNn HAS [BEEN] CHANGED" on `LEV 2 CALL STATEMENT` —
i.e. a `CALL … USING BY CONTENT` argument was leaking the callee's modifications back to the caller.

Root cause — the BY-phrase was not transitive. The grammar attaches a `BY CONTENT` / `BY REFERENCE`
phrase to only its **first** data-name (`callByContent : BY? CONTENT (dataReference | literal)`), so
`CALL "IC224A-1" USING BY CONTENT DN1, DN2, DN3, DN4` parsed as
`[ByContent DN1, bare DN2, bare DN3, bare DN4]`, and `CallBinder` bound each bare argument as the
default BY REFERENCE. DN2/DN3/DN4 were therefore passed by reference and the subprogram's writes
propagated back — exactly the observed failures. (`CreateByContent` already copies correctly; the bug
was purely the per-argument mode assignment.)

Fix (`CallBinder.BindCall`): make the passing mode **transitive**, per ISO §14.8 CALL general rule 5
("Both the BY CONTENT and BY REFERENCE phrases are transitive across the parameters that follow them
until another BY CONTENT or BY REFERENCE phrase is encountered. If neither … is specified prior to the
first parameter, the BY REFERENCE phrase is assumed."). The binder now tracks a `currentMode`
(initialised to BY REFERENCE), updates it on each explicit `callByReference`/`callByContent`/
`callByValue`, and assigns a bare argument the most recent explicit mode. No grammar change — the
existing token stream already carries the information; only the binder's mode assignment was wrong.

Verified: IC224A 22→0 FAIL* ("044 OF 044 TESTS WERE EXECUTED SUCCESSFULLY"), IC225A 3→0 FAIL*
("036 OF 036 …"). Both baselined to `tests/nist/valid/` and added to `scripts/guard.sh`
(NIST baselines now 150 = 94 NC + 42 IF + 12 SM + 2 IC). Full guard ALL GREEN, no regression from
the CALL-path change (1000 unit / 345 integration / 150 NIST).

IC suite remaining: IC203A (CANCEL re-initialisation), IC227A (EXTERNAL file sharing — file-I/O
wall), IC114A (file I/O in subprogram — file-I/O wall), and 5 COMPILE_FAIL (IC228A/233A/234A/235A/
401M — nested-program GLOBAL/duplicate-name visibility).

