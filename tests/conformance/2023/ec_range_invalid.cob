      *> EC-RANGE-INVALID (ISO §14.7.8 THROUGH phrase rule 2, spec :24863; Table 13 Nonfatal): an alphanumeric or
      *> national THRU range whose starting value collates AFTER its ending value (an inverted range) — in a level-88
      *> VALUE clause or an EVALUATE WHEN range — sets the nonfatal EC-RANGE-INVALID and the range is treated as EMPTY.
      *> A numeric descending range (rule 1) sets NO exception. Observed via FUNCTION EXCEPTION-STATUS under
      *> >>TURN EC-RANGE-INVALID CHECKING ON.
      *> A level-88 range reaches the run-time arm only when its collating sequence is UNKNOWN at compile time:
      *> §13.18.63.3 SR26 requires literal-2 less than literal-3 for a numeric range, and for a character range
      *> "when ... the runtime collating sequence is known" (cite.py OK; COBOLNET2961, kb/Work PB552), and "The
      *> runtime collating sequence is unknown when the collating sequence is defined by a locale". So the
      *> inverted 88 is ordered IN a LOCALE alphabet, and the numeric descending range is an EVALUATE WHEN range,
      *> which no syntax rule orders.
      >>TURN EC-RANGE-INVALID CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EC-RNG-IV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET LOC IS LOCALE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C PIC X VALUE "M".
          88 VALID-RANGE VALUE "A" THRU "Z".
          88 INV-RANGE   VALUE "Z" THRU "A" IN LOC.
       01 WS-N PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       MAIN-P.
      *> valid alphanumeric range (M in A..Z): membership true, no EC.
           IF VALID-RANGE DISPLAY "VALID-TRUE" ELSE DISPLAY "VALID-FALSE" END-IF.
           DISPLAY "V[" FUNCTION EXCEPTION-STATUS "]".
      *> numeric inverted range (9 THRU 1): rule 1 sets no EC; the WHEN is not taken.
           EVALUATE WS-N
               WHEN 9 THRU 1 DISPLAY "NUM-TRUE"
               WHEN OTHER    DISPLAY "NUM-FALSE"
           END-EVALUATE.
           DISPLAY "N[" FUNCTION EXCEPTION-STATUS "]".
      *> level-88 inverted alphanumeric range (Z THRU A in the locale): EC-RANGE-INVALID, membership false (empty).
           IF INV-RANGE DISPLAY "INV-TRUE" ELSE DISPLAY "INV-FALSE" END-IF.
           DISPLAY "I88[" FUNCTION EXCEPTION-STATUS "]".
      *> EVALUATE WHEN inverted alphanumeric range: EC set, WHEN not taken (empty range).
           EVALUATE WS-C
               WHEN "Z" THRU "A" DISPLAY "EVAL-MATCH"
               WHEN OTHER        DISPLAY "EVAL-OTHER"
           END-EVALUATE.
           DISPLAY "IEV[" FUNCTION EXCEPTION-STATUS "]".
           STOP RUN.
