      *> ISO §14.9.28.4 GR14 (Format 3 PERFORM): "An implicit PUSH ALL followed by TURN OFF ALL is assumed at
      *> the END of imperative-statement-1. Immediately preceding the END PERFORM phrase, there is an implicit
      *> POP ALL …". imp-2/3/4 (WHEN / OTHER / COMMON) and imp-5 (FINALLY) all execute BETWEEN those two
      *> points, so NO exception checking is in effect inside them — and §14.6.13.1.1: "if checking for an
      *> exception that occurs is not enabled, no exception condition is raised".
      *>
      *> The pre-PERFORM >>TURN EC-BOUND-SUBSCRIPT CHECKING ON is what makes the rule observable. imp-1 raises
      *> the NONFATAL EC-USER-DEMO so the WHEN selects and control resumes in place (GR20). The handler then
      *> performs an out-of-range subscript, WS-E (7) on a 5-occurrence table — which GR14 says cannot raise,
      *> because checking is off inside the handler (§8.4.2.3.4 GR2: a subscript beyond the highest permissible
      *> occurrence number is EC-BOUND-SUBSCRIPT, cite.py OK). It therefore takes the unchecked subscript
      *> path — a read of the compiler's scratch slot, the documented checking-off outcome for this condition —
      *> and the handler runs to completion. The FINALLY body repeats it, proving imp-5 is covered too.
      *> (The observation was an out-of-range REFERENCE MODIFICATION until kb/Work PB1707: an unchecked
      *> out-of-range reference modification now ends the run unit, so it can no longer show "checking is off".)
      *>
      *> Before the fix the handler bodies bound against the BASE TurnState — the binder cited GR21, which
      *> governs only whether an exception raised in imp-2..5 transfers control back INTO them, not whether
      *> checking is enabled there. The pre-PERFORM directive leaked in, the handler's subscript raised the
      *> FATAL EC-BOUND-SUBSCRIPT, and §14.6.13.1.3 #5 terminated the run unit abnormally with no output at all.
      >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. F3TOFFALL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-E PIC X OCCURS 5 TIMES.
       01 WS-Z PIC X(2) VALUE "??".
       01 WS-F PIC X(2) VALUE "??".
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "HELLO" TO WS-T.
           PERFORM
               RAISE EXCEPTION EC-USER-DEMO
           WHEN EC-USER-DEMO
               MOVE WS-E (7) TO WS-Z
               DISPLAY "HANDLER-DONE"
           FINALLY
               MOVE WS-E (7) TO WS-F
               DISPLAY "FINALLY-DONE"
           END-PERFORM.
           DISPLAY "AFTER".
           STOP RUN.
