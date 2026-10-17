*>--- conformance-case
*> id: pb440_sort_procedure_range
*> rules: [GR-14.9.28.4-6, GR-14.9.40.4-11, GR-14.9.40.4-14]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB440 (sibling) - a SORT INPUT PROCEDURE range is resolved by the SAME procedure resolver
      *> as PERFORM, so it inherited the same defect: the emitter decoded "is this range empty?" as
      *> `Start <= End`, which is ALSO true of a legal INVERTED range, and dropped the whole procedure.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *>
      *> SORT 1 - INPUT PROCEDURE IS IN-B THRU IN-A, where IN-A physically PRECEDES IN-B. 14.9.28.4 GR6,
      *>   which governs the identically-composed PERFORM range, says "There is no necessary relationship
      *>   between procedure-name-1 and procedure-name-2 except that a consecutive sequence of operations is
      *>   to be executed beginning at the procedure named procedure-name-1 and ending with the execution of
      *>   the procedure named procedure-name-2" - so an inverted range is legal and is reached by the GO TO
      *>   written in IN-B. 14.9.40.4 GR11: "The compiler inserts a return mechanism after the last statement
      *>   in the input procedure", which is IN-A's fall-through. The procedure therefore RELEASES C, A, B,
      *>   and 14.9.40.4 GR8 a) - "If the contents of the corresponding key data items are not equal and the
      *>   key is associated with the ASCENDING phrase, the record containing the key data item with the lower
      *>   value is returned first" - orders them:
      *>       OUT=A / OUT=B / OUT=C.
      *>   Before the fix the whole input procedure was skipped and NO record was released at all.
      *> SORT 2 - INPUT PROCEDURE IS EMPTY-IN, a section with zero paragraphs (14.4.2). There is no first
      *>   statement to transfer to, so nothing is released and the output procedure returns no record:
      *>       EMPTYIN CNT=0.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB440SORTRANGE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S-FILE ASSIGN TO "pb440sortrange.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD  S-FILE.
       01  S-REC.
           05  S-KEY PIC X.
       WORKING-STORAGE SECTION.
       01  W-EOF PIC X VALUE "N".
       01  W-CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-SECT SECTION.
       MAIN.
           SORT S-FILE ON ASCENDING KEY S-KEY
               INPUT PROCEDURE IS IN-B THRU IN-A
               OUTPUT PROCEDURE IS OUT-P.
           MOVE "N" TO W-EOF
           SORT S-FILE ON ASCENDING KEY S-KEY
               INPUT PROCEDURE IS EMPTY-IN
               OUTPUT PROCEDURE IS COUNT-P.
           DISPLAY "EMPTYIN CNT=" W-CNT
           STOP RUN.
       IN-SECT SECTION.
       IN-A.
           EXIT.
       IN-B.
           MOVE "C" TO S-REC
           RELEASE S-REC
           MOVE "A" TO S-REC
           RELEASE S-REC
           MOVE "B" TO S-REC
           RELEASE S-REC
           GO TO IN-A.
       OUT-SECT SECTION.
       OUT-P.
           PERFORM UNTIL W-EOF = "Y"
               RETURN S-FILE AT END MOVE "Y" TO W-EOF
                   NOT AT END DISPLAY "OUT=" S-REC
               END-RETURN
           END-PERFORM.
       COUNT-SECT SECTION.
       COUNT-P.
           PERFORM UNTIL W-EOF = "Y"
               RETURN S-FILE AT END MOVE "Y" TO W-EOF
                   NOT AT END ADD 1 TO W-CNT
               END-RETURN
           END-PERFORM.
       EMPTY-IN SECTION.
