      *> kb/Work PB1066 (the SOURCE FORMAT half) - the implicit PUSH ALL
      *> and POP ALL that 14.9.28.4 GR14 places around an exception-
      *> checking PERFORM's handlers also save and restore the REFERENCE
      *> FORMAT. The format is the one directive state settled before
      *> every other stage (the 6.5 logical conversion reads physical
      *> lines with it as state), so the implicit ops used to reach only
      *> the states held after it: a >>SOURCE FORMAT written in a WHEN
      *> phrase outlived END-PERFORM and the fixed-form line after it
      *> was read as free form (COBOL0307: unexpected '000100').
      *>
      *> THE RULES:
      *>   14.9.28.4 GR14 - "An implicit PUSH ALL followed by TURN OFF
      *>     ALL is assumed at the end of imperative-statement-1.
      *>     Immediately preceding the END PERFORM phrase, there is an
      *>     implicit POP ALL"
      *>   7.3.22.4 GR2 - "If ALL is specified, the state of all of the
      *>     directives other than EVALUATE, IF, PAGE, POP, or PUSH are
      *>     saved." (SOURCE FORMAT is one of them.)
      *>   7.3.20.4 GR3 - "If ALL is specified, the state of all of the
      *>     directives that were previously stored by a PUSH directive
      *>     and were not removed by a POP directive shall be restored."
      *>   7.3.24.3 GR1 - the SOURCE FORMAT directive governs "the
      *>     source text or library text following the directive and
      *>     continuing through a subsequent SOURCE FORMAT directive".
      *>
      *> EXPECTED OUTPUT, DERIVED (each STRING into a 3-character item
      *> overflows, so every handler runs; FINALLY always runs):
      *>   H1, L1  the handler switches to FREE; the POP before
      *>           END-PERFORM restores FIXED, so the sequence-numbered
      *>           line 000100 is a fixed-form source line and prints.
      *>   H2, F2, L2  the same for a directive in a FINALLY phrase.
      *>   H3, L3  CONTROL: a >>SOURCE FORMAT FREE in imperative-
      *>           statement-1 is BEFORE the PUSH ALL, so it is part of
      *>           the saved state and survives the POP ALL: everything
      *>           after END-PERFORM, written at column 1 (legal only in
      *>           free form), is still free form. It goes last because
      *>           it changes the format for the rest of the file.
      *> (Every line of this file is within column 72 so that the
      *> --source-format auto detector, which the corpus runner uses,
      *> classifies it as fixed form from its comment indicators.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1066SF1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D PIC X(3).
       PROCEDURE DIVISION.
       M-1.
           PERFORM
               STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
           WHEN EC-OVERFLOW-STRING
       >>SOURCE FORMAT IS FREE
               DISPLAY "H1"
           END-PERFORM
000100     DISPLAY "L1 FIXED-AFTER".
           PERFORM
               STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
           WHEN EC-OVERFLOW-STRING
               DISPLAY "H2"
           FINALLY
       >>SOURCE FORMAT IS FREE
               DISPLAY "F2"
           END-PERFORM
000200     DISPLAY "L2 FIXED-AFTER".
           PERFORM
       >>SOURCE FORMAT IS FREE
STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
WHEN EC-OVERFLOW-STRING
DISPLAY "H3"
END-PERFORM
DISPLAY "L3 FREE-AFTER".
STOP RUN.
