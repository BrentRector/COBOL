*>--- conformance-case
*> id: intrinsic_max_collating
*> rules: [RV-15.59.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> CA23 (CONFORMANCE-FIX-QUEUE): FUNCTION MAX/MIN/ORD-MAX/ORD-MIN determine the greatest/least by the
      *> 8.8.4.2 relation-condition rules (15.59.4 r1 ...), which use the current alphanumeric PROGRAM COLLATING
      *> SEQUENCE (8.8.4.2.7). Pre-fix they compared with raw code-unit order (string.CompareOrdinal), so MAX
      *> DISAGREED with the program's own relation conditions under a non-default PCS. Under ALPHABET AL (Z lowest
      *> ... A highest), 'A' collates ABOVE 'Z', so MAX(A Z) = "A" and the relation prints A-GT-Z.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CA23.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XX PROGRAM COLLATING SEQUENCE AL.
       SPECIAL-NAMES. ALPHABET AL IS "ZYXWVUTSRQPONMLKJIHGFEDCBA".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X VALUE "A".
       01 Z PIC X VALUE "Z".
       PROCEDURE DIVISION.
           DISPLAY "MAX=" FUNCTION MAX(A Z).
           IF A > Z DISPLAY "A-GT-Z" ELSE DISPLAY "Z-GT-A" END-IF.
           STOP RUN.
