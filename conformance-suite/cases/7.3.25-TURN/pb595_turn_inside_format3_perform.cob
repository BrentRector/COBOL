*>--- conformance-case
*> id: pb595_turn_inside_format3_perform
*> rules: [SR-7.3.25.3-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §7.3.25.3 SR5, whole: "A TURN directive shall not be specified within an exception processing
      *> PERFORM statement." Owner decision D20 (2026-07-19) settles the treatment: a FLAT ban — the whole
      *> statement, imperative-statement-1 included — reported as a SUPPRESSIBLE conformance warning
      *> (COBOLNET2187), with the program still compiling and still running, because §4.2.2 requires for a
      *> violation of the syntax rules only "a warning mechanism that optionally may be invoked by the user at
      *> compile time to indicate violations of the general formats" (kb/Work PB595).
      *>
      *> THIS GOLDEN IS THE "STILL COMPILES, STILL RUNS" HALF of that decision — the half a negative fixture
      *> cannot witness, because the program is ACCEPTED. The warning TEXT and its clause per directive word are
      *> pinned by unit:ExceptionCheckingPerformDirectiveBanDriftTests.
      *>
      *> LEG 1 — the banned directive is written inside imperative-statement-1, and the statement's OWN semantics
      *> are unaffected: §14.9.28.4 GR14 sentence 1 implicitly enables checking for the WHEN-named exception over
      *> imperative-statement-1 ("If checking for exception-name-1 … is not enabled for imperative-statement-1 by
      *> a TURN directive, an implicit TURN directive … is assumed before the first statement in
      *> imperative-statement-1"), so the STRING overflow raises EC-OVERFLOW-STRING and GR17 executes
      *> imperative-statement-2. Expected: L1=WHEN. It FAILS if the D20 warning were implemented as a REJECTION
      *> (no output at all), or if the diagnostic suppressed the statement's semantics.
      *>
      *> LEG 2 — execution continues past END-PERFORM to the end of the run unit. Expected: L2=CONTINUED. It
      *> FAILS if the violation aborted the compile or the run.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB595TURNINF3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D PIC X(3) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
       >>TURN EC-OVERFLOW-STRING CHECKING ON
               STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
           WHEN EC-OVERFLOW-STRING
               DISPLAY "L1=WHEN"
           END-PERFORM.
           DISPLAY "L2=CONTINUED".
           STOP RUN.
