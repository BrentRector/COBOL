*>--- conformance-case
*> id: pb488-cond-var-level-66
*> rules: [SR-13.16.3-24]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.16.3 syntax rule 24 - "A condition-name may be associated with any data description
*> entry that contains a level-number except the following: ... b) A level 66 entry."
*>
*> kb/Work PB488: this was a WRONG ANSWER, not a permissiveness. A level-66 RENAMES entry never enters the
*> binder's level stack, so the old `stack.Peek()` reader walked BACK PAST the alias and bound COND-R to the
*> preceding `05 B` - the probe printed BOUND-TO-B, i.e. the condition-name the programmer wrote over the
*> renamed span answered about a different item. Every edition rejects: the exclusion is COBOL-85 text too
*> ("Condition-names may be associated with any data description entry which contains a level-number except
*> ... a level 66 item").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-COND-VAR-LEVEL-66.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC.
          05 A PIC X(2) VALUE "AB".
          05 B PIC X(2) VALUE "CD".
       66 R1 RENAMES A THRU B.
       88 COND-R VALUE "CD".
       PROCEDURE DIVISION.
           IF COND-R DISPLAY "BOUND-TO-B" ELSE DISPLAY "NOT-B" END-IF
           STOP RUN.
