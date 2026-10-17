*>--- conformance-case
*> id: pb15_result_type_follows_argument
*> rules: [AR-15.39.3-1, RV-15.39.4-1, AR-15.96.3-3, RV-15.96.4-3,
*>    FMT-15.87.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> THE 15.x.1 RESULT-TYPE TABLES: A FUNCTION'S TYPE FOLLOWS ITS ARGUMENTS.
      *>
      *> Twenty 15 functions carry a table in their own .1 clause - "The type of this function depends on the
      *> type of argument-1 as follows" - with an ARGUMENT property on the left and the FUNCTION TYPE on the
      *> right. A national argument-1 makes the function national; an alphanumeric one makes it alphanumeric.
      *>
      *> THIS GOLDEN EXISTS BECAUSE THE CATALOG STORED ONE SCALAR TYPE (fix-queue PB15). Ten of the twenty
      *> were labelled Alphanumeric no matter what they were given. That is a SILENT under-rejection: a
      *> national result labelled alphanumeric walks straight through the 14.9.25.3 Table-16 MOVE guard, so
      *> MOVE FUNCTION TRIM(national-item) TO a PIC X item compiled clean. The four negative fixtures
      *> pb15-*-national-result-to-an pin the rejection; THIS program pins the other half - that the LEGAL
      *> direction still computes the right value, in both the national and the alphanumeric row of the table.
      *>
      *> The two halves matter equally. A "fix" that labelled everything national would pass the negatives and
      *> break every alphanumeric call, which is why each function below is exercised through BOTH rows.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB15RT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N1   PIC N(8) VALUE N"ABCDEFGH".
       01 W-N2   PIC N(8) VALUE N"BBBBBBBB".
       01 W-NPAD PIC N(8) VALUE N"  CDE   ".
       01 W-X1   PIC X(8) VALUE "ABCDEFGH".
       01 W-X2   PIC X(8) VALUE "BBBBBBBB".
       01 W-XPAD PIC X(8) VALUE "  CDE   ".
       01 W-NR   PIC N(20).
       01 W-XR   PIC X(20).
       01 W-DATE PIC 9(9) VALUE 100000.
       PROCEDURE DIVISION.
      *> 15.96.1 TRIM - National row and Alphanumeric row of the same table.
           MOVE FUNCTION TRIM(W-NPAD) TO W-NR
           DISPLAY "TRIM-N=[" W-NR "]"
           MOVE FUNCTION TRIM(W-XPAD) TO W-XR
           DISPLAY "TRIM-X=[" W-XR "]"
      *> 15.87.1 SUBSTITUTE - argument-2/argument-3 pairs match argument-1's class (15.87.3).
           MOVE FUNCTION SUBSTITUTE(W-N1 N"A" N"Z") TO W-NR
           DISPLAY "SUBST-N=[" W-NR "]"
           MOVE FUNCTION SUBSTITUTE(W-X1 "A" "Z") TO W-XR
           DISPLAY "SUBST-X=[" W-XR "]"
      *> 15.18.1 CONCAT - 15.18.3 r2 makes the argument list uniform in usage, so argument-1 decides.
           MOVE FUNCTION CONCAT(W-N1 W-N2) TO W-NR
           DISPLAY "CONCAT-N=[" W-NR "]"
           MOVE FUNCTION CONCAT(W-X1 W-X2) TO W-XR
           DISPLAY "CONCAT-X=[" W-XR "]"
      *> 15.39.1 FORMATTED-DATE - argument-1 is the FORMAT literal (15.39.3 r1 admits "a national or
      *> alphanumeric literal"), so the format's type decides the function's - not the integer date it renders.
           MOVE FUNCTION FORMATTED-DATE(N"YYYYMMDD" W-DATE) TO W-NR
           DISPLAY "FMTDATE-N=[" W-NR "]"
           MOVE FUNCTION FORMATTED-DATE("YYYYMMDD" W-DATE) TO W-XR
           DISPLAY "FMTDATE-X=[" W-XR "]"
      *> 15.97.1 / 15.57.1 / 15.78.1 - the three CA25 already covered; kept so a regression that "fixes" the
      *> new ten by breaking the old three cannot pass.
           MOVE FUNCTION UPPER-CASE(W-N1) TO W-NR
           DISPLAY "UPPER-N=[" W-NR "]"
           MOVE FUNCTION REVERSE(W-X1) TO W-XR
           DISPLAY "REVERSE-X=[" W-XR "]"
      *> 15.59.1 MAX - the six-row uniform-argument table; 15.59.3 r2 requires one class throughout.
           MOVE FUNCTION MAX(W-N1 W-N2) TO W-NR
           DISPLAY "MAX-N=[" W-NR "]"
           MOVE FUNCTION MAX(W-X1 W-X2) TO W-XR
           DISPLAY "MAX-X=[" W-XR "]"
           STOP RUN.
