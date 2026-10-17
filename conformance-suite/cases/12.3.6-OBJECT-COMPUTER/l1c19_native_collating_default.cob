      *>--- conformance-case
      *> id: l1c19_native_collating_default
      *> rules: [GR-12.3.6.4-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.3.6.4 10) — no PCS anywhere: the NATIVE collating
      *>   sequences
      *> "When the PROGRAM COLLATING SEQUENCE clause is not specified
      *>   and the
      *> source unit is not contained within a source unit for which a
      *>   PROGRAM
      *> COLLATING SEQUENCE clause is specified, the initial program
      *>   collating
      *> sequences are the native alphanumeric collating sequence and
      *>   the native
      *> national collating sequence."
      *> OK  §12.3.6.4 10)  (General rules)
      *> Documented native sequences (docs/CONFORMANCE.md DOC-A.1-8):
      *>   the
      *> ordinal of a native character is its UTF-16 code unit plus one,
      *>   in
      *> BOTH native sets, and the native order is code-unit order.
      *> L1C19P (outer, no PCS) contains L1C19Q (no PCS): both arms of
      *>   the
      *> rule's condition hold for both. L1C19R, a separate program of
      *>   the same
      *> compilation group, DOES declare PCS (an EBCDIC alphabet) and is
      *>   never
      *> called - it must not reach L1C19P/L1C19Q.
      *> Derivation (native = code-unit order):
      *>  "a"(X61) > "B"(X42) TRUE; "0"(X30) < "A"(X41) TRUE (in EBCDIC
      *>    both
      *>  would be FALSE); N"a" > N"B" TRUE (native national, same
      *>    order);
      *>  ORD("A") = X41+1 = 66; ORD("a") = 98; MAX("a" "B") = a.
      *>  The contained program prints the same, prefixed IN-.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C19P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(5).
       01 O PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "OUT-" TO R
           PERFORM SHOW-ORDER
           CALL "L1C19Q"
           STOP RUN.
       SHOW-ORDER.
           IF "a" > "B" DISPLAY R "A1=T" ELSE DISPLAY R "A1=F" END-IF
           IF "0" < "A" DISPLAY R "A2=T" ELSE DISPLAY R "A2=F" END-IF
           IF N"a" > N"B" DISPLAY R "N1=T" ELSE DISPLAY R "N1=F" END-IF
           MOVE FUNCTION ORD("A") TO O
           DISPLAY R "ORD-A=" O
           MOVE FUNCTION ORD("a") TO O
           DISPLAY R "ORD-a=" O
           DISPLAY R "MAX=" FUNCTION MAX("a" "B").

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C19Q.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(5).
       01 O PIC 9(3).
       PROCEDURE DIVISION.
           MOVE "IN-" TO R
           IF "a" > "B" DISPLAY R "A1=T" ELSE DISPLAY R "A1=F" END-IF
           IF "0" < "A" DISPLAY R "A2=T" ELSE DISPLAY R "A2=F" END-IF
           IF N"a" > N"B" DISPLAY R "N1=T" ELSE DISPLAY R "N1=F" END-IF
           MOVE FUNCTION ORD("A") TO O
           DISPLAY R "ORD-A=" O
           MOVE FUNCTION ORD("a") TO O
           DISPLAY R "ORD-a=" O
           DISPLAY R "MAX=" FUNCTION MAX("a" "B")
           GOBACK.
       END PROGRAM L1C19Q.
       END PROGRAM L1C19P.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C19R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X
           PROGRAM COLLATING SEQUENCE IS EB.
       SPECIAL-NAMES.
           ALPHABET EB IS EBCDIC.
       PROCEDURE DIVISION.
           GOBACK.
       END PROGRAM L1C19R.
