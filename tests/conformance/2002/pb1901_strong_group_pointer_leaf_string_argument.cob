      *> kb/Work PB1901 -- a strongly-typed group as an intrinsic function's STRING argument.
      *> ISO 15.3 item 2 (cite.py --check 15.3 "Strongly-typed group items are treated as though they were of class
      *> and category alphanumeric, unless they are prohibited as arguments of a function" -> OK): although a
      *> strongly-typed group's class and category are its TYPE-NAME (8.5.2.1), a function argument of the
      *> alphanumeric type (UPPER-CASE's Anum1, 15.6 Table 21) takes one as alphanumeric, and UPPER-CASE (15.97.3 r1)
      *> does not prohibit it - MAX, MIN, ORD-MAX and ORD-MIN do ("nor shall it be a strongly-typed group item"). So
      *> FUNCTION UPPER-CASE(G) is LEGAL for both groups below; the note's original premise that 8.5.2.1 bars it
      *> was the wrong clause for an argument position.
      *> A strongly-typed group with a USAGE POINTER leaf is the only conforming home of a pointer inside a group
      *> (13.18.60.3 SR14, cite.py --check OK). It used to compile clean and ABORT at run time in the whole-group image refusal. Its
      *> character image is the STORAGE image with each pointer leaf's 8 positions holding its storage image (kb/Work
      *> PB1071, A.1 item 216: NULL is eight zero positions; they were SPACES before), never the
      *> reference (CONFORMANCE.md A.1 items 56 and 214; the image DISPLAY and a MOVE already read), so
      *>   G holds "ab" + the NULL image (8 zero positions) + "cd"  = 12 characters
      *>   UPPER-CASE(G)                                   = "AB" + 8 zero positions + "CD"
      *>   REVERSE(G)                                      = "dc" + 8 zero positions + "ba"   (15.78.4: the
      *>                                                      characters reversed, case untouched)
      *> and the function result has the argument's length (15.97.4 r5; 15.78.4). The zero positions are control
      *> characters, so they are asked (= LOW-VALUES) and the characters around them displayed. The plain strong
      *> group G2 is "xyz" and UPPER-CASE gives "XYZ".
      *> Strongly-typed groups are COBOL 2002; the string-argument rule is unchanged in every later edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1901POS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC-T IS TYPEDEF STRONG.
          05 A PIC X(2) VALUE "ab".
          05 P USAGE POINTER.
          05 B PIC X(2) VALUE "cd".
       01 REC-U IS TYPEDEF STRONG.
          05 X PIC X(3) VALUE "xyz".
       01 G  TYPE REC-T.
       01 G2 TYPE REC-U.
       01 R12 PIC X(12).
       01 R3  PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION UPPER-CASE(G) TO R12
           DISPLAY "[" R12(1:2) "|" R12(11:2) "]"
           IF R12(3:8) = LOW-VALUES
              DISPLAY "UPPER-MID=NULL-IMAGE"
           ELSE
              DISPLAY "UPPER-MID=OTHER"
           END-IF
           MOVE FUNCTION REVERSE(G) TO R12
           DISPLAY "[" R12(1:2) "|" R12(11:2) "]"
           IF R12(3:8) = LOW-VALUES
              DISPLAY "REVERSE-MID=NULL-IMAGE"
           ELSE
              DISPLAY "REVERSE-MID=OTHER"
           END-IF
           DISPLAY FUNCTION LENGTH(FUNCTION UPPER-CASE(G))
           MOVE FUNCTION UPPER-CASE(G2) TO R3
           DISPLAY "[" R3 "]"
           STOP RUN.
       END PROGRAM PB1901POS.
