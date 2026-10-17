*>--- conformance-case
*> id: pb427-strong-group-argument-not-same-type
*> rules: [SR-14.8.2.2-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.8.2.2, the sentence after rules 1 and 2 and qualified by neither of them: "If either
*> the formal parameter or the corresponding argument is a strongly-typed group item, both shall be of
*> the same type."  14.8.3.2 states the same sentence over a RETURNING pair and 9.3.8.2.3 rule 7 over an
*> interface pair; 8.5.1.12.1 says it once more from the other side -- "Two fixed-length groups are
*> always compatible, unless they are strongly typed and have different type definitions."
*> PLAING is an ordinary alphanumeric group of four bytes and LF is a strongly-typed group of four bytes,
*> so every LENGTH-based conformance test the boundary applies is satisfied and only the strongly-typed
*> sentence refuses the crossing.  Before kb/Work PB427 that sentence was implemented NOWHERE: this
*> program compiled and ran, handing a strongly-typed formal an argument of no type at all -- the
*> activation-boundary form of the same integrity breach 8.5.3.3 exists to prevent.
*> 85 is not listed: TYPEDEF STRONG is a 2002 introduction (13.18.58).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB427ARG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PLAING.
          05 PA PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "WXYZ" TO PA
           CALL "PB427ARGIN" AS NESTED USING PLAING
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB427ARGIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CT-T IS TYPEDEF STRONG.
          05 CA PIC X(4).
       LINKAGE SECTION.
       01 LF TYPE CT-T.
       PROCEDURE DIVISION USING LF.
       CMAIN.
           DISPLAY "CA=" CA OF LF
           GOBACK.
       END PROGRAM PB427ARGIN.
       END PROGRAM PB427ARG.
