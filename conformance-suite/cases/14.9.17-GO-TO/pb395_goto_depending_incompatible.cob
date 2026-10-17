*>--- conformance-case
*> id: pb395_goto_depending_incompatible
*> rules: [GR-14.9.17.4-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.17.4 GR2 (GO TO, Format 2), whole: "If the value of identifier-1 is 1, 2, …, or n, control is
      *> transferred to the first statement of the procedure named by procedure-name-1, procedure-name-2, …, or
      *> procedure-name-n, respectively. If the content of identifier-1 is not numeric, the EC-DATA-INCOMPATIBLE
      *> exception condition is set to exist. If the value of identifier-1 is anything other than the positive or
      *> unsigned integers 1 through n, no transfer occurs and control passes to the next statement."
      *> All three halves are measured here, in that order.
      *>
      *> The condition is the §14.6.13.2 rule 2 sibling — "When the content of a numeric sending item … is
      *> referenced during the execution of a statement and the content of that sending operand would evaluate to
      *> false in a numeric class condition, the result of the reference is undefined and an EC-DATA-INCOMPATIBLE
      *> exception condition is set to exist" — reached here through a REDEFINES window holding "AB". Table 13
      *> makes it FATAL, so the declarative's RESUME AT NEXT STATEMENT (§14.9.33.4 GR2) is what keeps the run unit
      *> alive past it; GR2 leaves the RESULT of the reference undefined, so nothing here displays a selected
      *> paragraph — only that the condition was raised and that control resumed at the statement after the GO TO.
      *>
      *> 2002 is the introducing edition of the OBSERVABLE: the §7.3 compiler-directive facility (>>TURN) and the
      *> exception-condition declaratives are COBOL-2002 introductions, so the EC half cannot be written at 85 —
      *> see conformance:negative/pb395-turn-data-incompatible-below-2002. The transfer and fall-through halves
      *> are edition-independent.
      *>
      *> kb/Work PB395 / PB230: EC-DATA-INCOMPATIBLE used to be armed by a STATEMENT-KIND test (`node is
      *> BoundMove`) where the clause hangs it on a SENDING-OPERAND reference, so GO TO … DEPENDING read the
      *> incompatible item in silence. No golden covered the §14.6.13.2 leg for any verb; this is that golden.
       >>TURN EC-DATA-INCOMPATIBLE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB395GOTODEP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 W-X PIC X(2) VALUE "AB".
       01 R REDEFINES G.
          05 W-N PIC 9(2).
       01 SEL PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-INCOMPATIBLE.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           GO TO P-A P-B DEPENDING ON SEL.
           DISPLAY "NOT-REACHED".
       P-A.
           DISPLAY "A".
           GO TO P-OUT.
       P-B.
           DISPLAY "B".
       P-OUT.
           MOVE 0 TO SEL.
           GO TO P-A2 P-B2 DEPENDING ON SEL
           DISPLAY "FELL-THROUGH"
           GO TO P-INCOMPAT.
       P-A2.
           DISPLAY "A2".
           GO TO P-INCOMPAT.
       P-B2.
           DISPLAY "B2".
       P-INCOMPAT.
           DISPLAY "SEL=[" W-X "]"
           GO TO P-A3 P-B3 DEPENDING ON W-N
           DISPLAY "AFTER-RESUME"
           STOP RUN.
       P-A3.
           DISPLAY "A3".
           STOP RUN.
       P-B3.
           DISPLAY "B3".
           STOP RUN.
