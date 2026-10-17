*>--- conformance-case
*> id: pb59_repertoire_identity
*> rules: [RV-15.26.4-1, RV-15.26.4-2, RV-15.26.4-3, RV-15.66.4-1, RV-15.66.4-2, RV-15.66.4-3, DOC-A.1-33, DOC-A.1-36]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB59 / RV-15.26.4-1, RV-15.66.4-1..3, RV-15.19.4-1 — the Annex A.1 item-33 correspondence is
      *> the TOTAL UTF-16 IDENTITY, proved from BOTH sides of the declared boundary with ORD as the
      *> encoding-immune oracle. A PIC X item and FUNCTION DISPLAY-OF of the same wide character must
      *> name the SAME character: pre-fix, ORD over the item gave the true ordinal while DISPLAY-OF
      *> substituted '?' (ordinal 64) — the one expression where the runtime contradicted the
      *> documented UTF-16 repertoire (CONFORMANCE.md item 188). U+0160 ordinal = 352+1 = 353.
      *> Round trips both directions; argument-2 accepted-and-inert on both functions (§15.26.4 r2 /
      *> §15.66.4 r2 have no character to substitute for under a total correspondence).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59REPID.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AX    PIC X(1) VALUE "Š".
       01 N1    PIC N(1) VALUE N"Š".
       01 A1    PIC X(1).
       01 NR    PIC N(1).
       01 ORDV  PIC 9(6).
       PROCEDURE DIVISION.
           COMPUTE ORDV = FUNCTION ORD(AX)
           DISPLAY "ORD-X  =" ORDV
           MOVE FUNCTION DISPLAY-OF(N1) TO A1
           COMPUTE ORDV = FUNCTION ORD(A1)
           DISPLAY "ORD-DOF=" ORDV
           MOVE FUNCTION NATIONAL-OF(AX) TO NR
           IF NR = N1
               DISPLAY "NOF-RT =SAME"
           ELSE
               DISPLAY "NOF-RT =DIFFER"
           END-IF
           MOVE FUNCTION DISPLAY-OF(N1, "?") TO A1
           COMPUTE ORDV = FUNCTION ORD(A1)
           DISPLAY "A2-DOF =" ORDV
           MOVE FUNCTION NATIONAL-OF(AX, N"#") TO NR
           IF NR = N1
               DISPLAY "A2-NOF =SAME"
           ELSE
               DISPLAY "A2-NOF =DIFFER"
           END-IF
           STOP RUN.
