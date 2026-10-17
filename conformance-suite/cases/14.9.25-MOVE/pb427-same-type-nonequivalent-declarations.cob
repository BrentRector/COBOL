*>--- conformance-case
*> id: pb427-same-type-nonequivalent-declarations
*> rules: [SR-14.9.25.3-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.25.3 SR2: "If identifier-2 references a strongly-typed group item, identifier-1
*> shall be specified and be described as a group item of the same type."  8.5.3.1 defines "same type",
*> and its FIRST conjunct is only that the two type declarations "have the same type-name" -- they must
*> also have "the same presence or absence of the EXTERNAL clause and the STRONG phrase, and for each
*> elementary item in one type declaration there is a corresponding elementary item in the other type
*> declaration, starting at the same relative byte or bit position and having the same length in bytes
*> or bits".
*> Here the two source elements each declare SHAPE-T.  The names match; NOTHING else does -- one
*> elementary item of 6 bytes against two of 3.  They are NOT equivalent declarations, so OUTER-X and
*> INNER-Y are not of the same type and the MOVE is refused.
*> Before kb/Work PB427 the predicate compared the type NAME and stopped, so this program compiled and
*> RAN, depositing the alphabetic characters 'ABC' and 'DEF' into two PIC 9(3) numeric items -- exactly
*> the data-integrity breach 8.5.3.3's restrictions exist to prevent, and silently.
*> 85 is not listed: TYPEDEF STRONG is a 2002 introduction (13.18.58).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB427NEQ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SHAPE-T IS TYPEDEF STRONG.
          05 SA PIC X(6).
       01 OUTER-X TYPE SHAPE-T IS GLOBAL.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABCDEF" TO SA OF OUTER-X
           CALL "PB427NEQIN" AS NESTED
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB427NEQIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SHAPE-T IS TYPEDEF STRONG.
          05 SB PIC 9(3).
          05 SC PIC 9(3).
       01 INNER-Y TYPE SHAPE-T.
       PROCEDURE DIVISION.
       CMAIN.
           MOVE OUTER-X TO INNER-Y
           GOBACK.
       END PROGRAM PB427NEQIN.
       END PROGRAM PB427NEQ.
