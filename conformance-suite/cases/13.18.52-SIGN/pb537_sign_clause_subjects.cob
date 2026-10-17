      *>--- conformance-case
      *> id: pb537_sign_clause_subjects
      *> rules: [SR-13.18.52.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 13.18.52.3 SR1: "The SIGN clause may be specified only for: - a numeric data or screen description
      *> entry whose picture character-string contains the symbol 'S' - a numeric report group description
      *> entry whose picture character-string contains the symbol 'S' - an alphanumeric group item, national
      *> group item, or strongly-typed group item."  SR2: "The usage of an elementary item for which the SIGN
      *> clause is specified shall be display or national."  Every SIGN clause below is on a subject SR1 admits,
      *> with a usage SR2 admits - kb/Work PB537's screen must accept each one, including a group whose SIGN
      *> clause covers an ALPHANUMERIC subordinate (SR1 speaks of the entry that specifies the clause, and
      *> 13.18.52.4 GR1 applies it "for each numeric item subordinate to the group"), a group whose USAGE clause
      *> makes a subordinate binary (SR2 speaks of an ELEMENTARY item for which the clause is specified), and a
      *> numeric report group description entry.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT.  13.18.52.4 GR6 a)/b): with SEPARATE CHARACTER the sign occupies the
      *> leading (or trailing) character position, '+' for positive and '-' for negative.
      *>   A  PIC S9(3) SIGN LEADING SEPARATE VALUE -12     -> "-012"
      *>   G  (SIGN TRAILING SEPARATE) = GX "AB" + GN S9(2) -7 -> GX = "AB", GN = "07-"
      *>   H  (USAGE COMP SIGN LEADING) is an alphanumeric group; its binary GB is not displayed (no image to pin)
      *>   B  PIC S9(2) SIGN TRAILING SEPARATE VALUE +5      -> "05+"
      *>   The report item COLUMN 1 PIC S9(3) SIGN LEADING SEPARATE SOURCE WN, WN = -5, is "-005" (13.18.53.4 GR1's
      *>   implicit MOVE into the printable item, then GR6); the report file is read back one character per record
      *>   (the pb482 precedent - LINE SEQUENTIAL is a COBOL-2023 introduction) keeping '-', '+' and digits.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB537SGN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb537sgn.rpt".
           SELECT RDR ASSIGN TO "pb537sgn.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD PRT REPORT IS R-1.
       FD RDR.
       01 R-CHAR PIC X.
       WORKING-STORAGE SECTION.
       01 A  PIC S9(3) SIGN IS LEADING SEPARATE CHARACTER VALUE -12.
       01 G  SIGN IS TRAILING SEPARATE.
          05 GX PIC X(2) VALUE "AB".
          05 GN PIC S9(2) VALUE -7.
       01 H  USAGE COMP SIGN IS LEADING.
          05 GB PIC S9(4) VALUE -3.
       01 B  PIC S9(2) SIGN TRAILING SEPARATE VALUE +5.
       01 WN PIC S9(3) VALUE -5.
       01 EOF-SW PIC 9 VALUE 0.
       01 NIMG PIC 99 VALUE 0.
       01 IMG PIC X(10) VALUE SPACES.
       REPORT SECTION.
       RD R-1 PAGE LIMIT IS 10 LINES.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC S9(3) SIGN IS LEADING SEPARATE SOURCE WN.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "A=" A.
           DISPLAY "GX=" GX " GN=" GN.
           DISPLAY "B=" B.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           GENERATE DET-A.
           TERMINATE R-1.
           CLOSE PRT.
           OPEN INPUT RDR.
           PERFORM UNTIL EOF-SW = 1
               READ RDR
                   AT END MOVE 1 TO EOF-SW
                   NOT AT END
                       IF R-CHAR IS NUMERIC OR R-CHAR = "-"
                          OR R-CHAR = "+"
                           ADD 1 TO NIMG
                           MOVE R-CHAR TO IMG(NIMG:1)
                       END-IF
               END-READ
           END-PERFORM.
           CLOSE RDR.
           DISPLAY "IMG=" IMG.
           STOP RUN.
