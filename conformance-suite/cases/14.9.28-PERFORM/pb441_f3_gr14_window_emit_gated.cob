*>--- conformance-case
*> id: pb441_f3_gr14_window_emit_gated
*> rules: [GR-14.9.28.4-14]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.28.4 GR14 (Format 3 PERFORM), last two sentences: "An implicit PUSH ALL followed by TURN OFF
      *> ALL is assumed at the end of imperative-statement-1. Immediately preceding the END PERFORM phrase, there
      *> is an implicit POP ALL followed by an implicit TURN directive with OFF specified for any exception
      *> conditions that were implicitly turned on before the first statement in imperative-statement-1."
      *> GR16 places imperative-statement-5 (FINALLY) inside that window — "the end of the PERFORM statement
      *> begins at imperative-statement-5" — so imp-2 (WHEN) and imp-5 (FINALLY) BOTH run with checking off, and
      *> §14.6.13.1.1 then applies: "if checking for an exception that occurs is not enabled, no exception
      *> condition is raised" (NOTE 3: the results of the OPERATION are undefined — the run unit is not
      *> terminated, so nothing here displays the truncated N).
      *>
      *> N is PIC 9 and each ADD 9 makes a two-digit result, so §14.7.5 no-phrase rule 4 would set the FATAL
      *> EC-SIZE-TRUNCATION — §14.6.13.1.3 #5 terminates the run unit abnormally when nothing handles it. That
      *> the WHEN body and the FINALLY body both run to completion is the whole measurement.
      *>
      *> The declarative is the two-sided control: it fires EXACTLY ONCE, for the ADD written AFTER END-PERFORM,
      *> where GR14's implicit POP ALL has restored the pre-PERFORM >>TURN. If checking leaked INTO the window
      *> "DECLARATIVE=…" would print three times; if the >>TURN were inert it would print none.
      *>
      *> kb/Work PB441: imp-2/3/4 are emitted as separate pc-range cases and escaped the enclosing statement's
      *> EC region, but imp-5 is emitted INLINE — and the EC-SIZE guard is decided at EMIT time from that region,
      *> so the FINALLY ADD terminated the run unit while the identical WHEN ADD did not.
       >>TURN EC-SIZE-TRUNCATION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB441GR14WIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       DECLARATIVES.
       SZ SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE-TRUNCATION.
       SZ-P.
           DISPLAY "DECLARATIVE=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           PERFORM
               RAISE EXCEPTION EC-USER-DEMO
               DISPLAY "IMP1-AFTER"
           WHEN EC-USER-DEMO
               ADD 9 TO N
               DISPLAY "WHEN-DONE"
           FINALLY
               ADD 9 TO N
               DISPLAY "FINALLY-DONE"
           END-PERFORM.
           MOVE 5 TO N.
           ADD 9 TO N.
           DISPLAY "AFTER-PERFORM".
           STOP RUN.
