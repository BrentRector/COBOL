      *> kb/Work PB1420 -- ISO 8.4.3.2.3 SR12 (cite.py --check 8.4.3.2.3 "An integer function other than the
      *> integer form of the ABS function shall not be specified where an unsigned integer is required" -> OK):
      *> the integer form of ABS is the ONE integer function that MAY stand where an unsigned integer is required.
      *> The two positions that require one are 15.18.3 r3 (CONCAT: "If argument-1 or argument-2 is numeric, it
      *> shall be usage display or national and shall be an unsigned integer") and 15.12.3 r1 (BASECONVERT: with a
      *> base below 11, "an unsigned integer data item or literal").
      *> ABS(-3) is 3 and ABS(N) over N = -42 is 42 (15.7.4 r1: the negation when the argument is negative); a numeric
      *> function's returned value used as text is its significant digits with no padding (CONFORMANCE.md item 92).
      *>   CONCAT(ABS(-3) "A")        = "3" + "A"                        = 3A
      *>   CONCAT("A" ABS(N) "Z")     = "A" + "42" + "Z"                  = A42Z
      *>   BASECONVERT(ABS(10) 10 16) = decimal 10 expressed in base 16   = A
      *> Base 16 is not below 11, so r1's unsigned-integer half does not apply and an INTEGER function is an ordinary
      *> numeric-string argument there: BASECONVERT(INTEGER(10) 16 2) reads the digits "10" in base 16 (sixteen) and
      *> writes them in base 2 = 10000. (The nested INTEGER function itself is the SR12 REJECT at CONCAT and at a base
      *> below 11: conformance:negative/pb1420-concat-integer-function-unsigned-position.)
      *> CONCAT and BASECONVERT are COBOL 2023 functions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1420POS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N   PIC S9(3) VALUE -42.
       01 W2  PIC XX.
       01 W4  PIC X(4).
       01 W1  PIC X.
       01 W5  PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION CONCAT(FUNCTION ABS(-3) "A") TO W2
           DISPLAY "[" W2 "]"
           MOVE FUNCTION CONCAT("A" FUNCTION ABS(N) "Z") TO W4
           DISPLAY "[" W4 "]"
           MOVE FUNCTION BASECONVERT(FUNCTION ABS(10) 10 16) TO W1
           DISPLAY "[" W1 "]"
           MOVE FUNCTION BASECONVERT(FUNCTION INTEGER(10) 16 2) TO W5
           DISPLAY "[" W5 "]"
           STOP RUN.
       END PROGRAM PB1420POS.
