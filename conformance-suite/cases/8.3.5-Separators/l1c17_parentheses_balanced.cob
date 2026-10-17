      *>--- conformance-case
      *> id: l1c17_parentheses_balanced
      *> rules: [SR-8.3.5-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.3.5 4) — parentheses: separators, balanced, delimiting
      *> "Except when appearing in a picture character-string, the
      *>  COBOL characters right parenthesis and left parenthesis are
      *>  separators. Except in pseudo-text, parentheses may appear
      *>  only in balanced pairs of left and right parentheses
      *>  delimiting subscripts, a list of function or method
      *>  arguments, a reference modifier, arithmetic or boolean
      *>  expressions, or conditions."
      *> cite.py --check 8.3.5 "Except in pseudo-text, parentheses may
      *>   appear only in balanced pairs of left and right parentheses
      *>   delimiting subscripts, a list of function or method
      *>   arguments, a reference modifier, arithmetic or boolean
      *>   expressions, or conditions." -> OK §8.3.5 4)
      *> Every legal context the rule names that exists in COBOL 85 is
      *> exercised (boolean expressions and methods arrive in 2002);
      *> the rejection half is pinned by the negatives
      *> l1c17-paren-unclosed, l1c17-paren-extra-close and
      *> l1c17-paren-not-delimiting.
      *>
      *> DERIVED OUTPUT:
      *>   PIC=01250   picture exception: "9(3)V9(2)" is ONE picture
      *>               character-string (5 digit positions, 2 after the
      *>               implied point), so 12.5 displays as 01250.
      *>   SUB=BB CC   subscripts: W-E (2) and W-E (W-I) with W-I = 3.
      *>   FN=007 AB   function argument lists: MAX (3 7 5) = 7;
      *>               UPPER-CASE ("ab") = "AB".
      *>   RM=BCD      reference modifier: "ABCDE" (2:3).
      *>   AR=015      nested arithmetic parentheses:
      *>               ((2 + 3) * (4 - 1)) = 15.
      *>   COND=T      condition: (15 > 10) AND (NOT (15 = 20)) true.
      *>   PT=OK       pseudo-text exception: ==ZZ(== holds an
      *>               UNBALANCED "(" and is legal; the source text
      *>               ZZ( is replaced by DISPLAY.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C17G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P-N  PIC 9(3)V9(2) VALUE 12.5.
       01 W-TV PIC X(6) VALUE "AABBCC".
       01 W-T REDEFINES W-TV.
          05 W-E PIC XX OCCURS 3 TIMES.
       01 W-I  PIC 9 VALUE 3.
       01 W-S  PIC X(5) VALUE "ABCDE".
       01 W-U  PIC XX.
       01 N-F  PIC 999.
       01 N-A  PIC 999.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "PIC=" P-N.
           DISPLAY "SUB=" W-E (2) " " W-E (W-I).
           COMPUTE N-F = FUNCTION MAX (3 7 5).
           MOVE FUNCTION UPPER-CASE ("ab") TO W-U.
           DISPLAY "FN=" N-F " " W-U.
           DISPLAY "RM=" W-S (2:3).
           COMPUTE N-A = ((2 + 3) * (4 - 1)).
           DISPLAY "AR=" N-A.
           IF (N-A > 10) AND (NOT (N-A = 20))
               DISPLAY "COND=T"
           ELSE
               DISPLAY "COND=F"
           END-IF.
           REPLACE ==ZZ(== BY ==DISPLAY ==.
           ZZ( "PT=OK".
           REPLACE OFF.
           STOP RUN.
