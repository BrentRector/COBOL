*>--- conformance-case
*> id: pb649-national-figurative-below-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> The comparison ISO 8.8.4.2.6 governs - "Two operands, one class alphanumeric and one class
      *> national, may be compared" - cannot be written below COBOL-2002, because class national itself is
      *> a 2002 introduction. So the positive golden 2002/pb649_national_figurative_collation has its
      *> edition witness here: at COBOL-85 the national literal N"AB" inside `ALL N"AB"` draws the
      *> boolean/national-data introduction gate (COBOLNET0900, construct national-data-2002).
      *>
      *> ⚠ STATED PLAINLY: this case is the EDITION gate's witness and not the collation rule's. The
      *> collation rule's own witness is the positive golden's AN-LT line, which must still take the
      *> ALPHANUMERIC program collating sequence while its national siblings do not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB649NEG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XX PROGRAM COLLATING SEQUENCE AL.
       SPECIAL-NAMES. ALPHABET AL IS "ZYXWVUTSRQPONMLKJIHGFEDCBA".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XA PIC X(2) VALUE "AC".
       PROCEDURE DIVISION.
       MAIN.
           IF ALL N"AB" < XA
               DISPLAY "LT"
           END-IF
           STOP RUN.
