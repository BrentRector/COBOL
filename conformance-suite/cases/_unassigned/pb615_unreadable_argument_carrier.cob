*>--- conformance-case
*> id: pb615_unreadable_argument_carrier
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB615 - a SUPPLIED argument whose carrier the formal
      *> cannot read is a CONFORMANCE VIOLATION, not an omitted argument.
      *> ISO 14.8.2.3.3 rule 1: with no program-specifier and no NESTED
      *> phrase, "the formal parameter shall be of the same length as the
      *> corresponding argument". W-P is a data-pointer - 8 character
      *> positions (CONFORMANCE.md DOC-A.1-216) - and L-N is PIC 9(3),
      *> 3 positions: a violation.
      *> ISO 14.9.4.4 GR3 d): "If a violation of these rules is detected,
      *> the EC-PROGRAM-ARG-MISMATCH exception condition is set to exist
      *> if checking for it is enabled in both the activated program and
      *> activating runtime element, the program call is not successful"
      *> - the >>TURN below covers both units. GR3 h) 1: the ON EXCEPTION
      *> phrase runs; the callee's statements never do (the call was not
      *> successful), so CALLEE-RAN never appears. Before PB615 the formal
      *> read as ZERO and the callee ran - a silent wrong value.
      *> A genuinely OMITTED argument is the OTHER case (GR11): the call
      *> succeeds and the omitted-argument condition is true.
      *> Derived: MISMATCH-CAUGHT, then OMITTED-SEEN, then NOT-EXC.
      >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB615MAIN02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "PB615SUB02" USING BY CONTENT W-P
               ON EXCEPTION DISPLAY "MISMATCH-CAUGHT"
               NOT ON EXCEPTION DISPLAY "NOT-EXC"
           END-CALL
           CALL "PB615SUB02" USING OMITTED
               ON EXCEPTION DISPLAY "MISMATCH-CAUGHT"
               NOT ON EXCEPTION DISPLAY "NOT-EXC"
           END-CALL
           STOP RUN.
       END PROGRAM PB615MAIN02.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB615SUB02.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-N PIC 9(3).
       PROCEDURE DIVISION USING L-N.
       SUB-PARA.
           IF L-N IS OMITTED
               DISPLAY "OMITTED-SEEN"
           ELSE
               DISPLAY "CALLEE-RAN " L-N
           END-IF
           GOBACK.
       END PROGRAM PB615SUB02.
