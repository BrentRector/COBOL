      *> kb/Work PB1398 - a function-identifier is the data item its
      *> type (ISO 15.2) describes, so ISO 15.50.3 r1 and 15.14.3 r1
      *> ("a data item of any class or category", through 8.4.3.2.1 "A
      *> function-identifier references the unique data item that
      *> results from the evaluation of a function") admit EVERY
      *> function result as the argument of FUNCTION LENGTH and
      *> FUNCTION BYTE-LENGTH, one leg per type:
      *>   alphanumeric (15.2 item 1) - 1 byte per position (3, 3);
      *>   national (item 3) - 3 positions, 2 bytes each (3, 6);
      *>   boolean (item 2, implicit usage bit) - positions, and 8 to
      *>     the byte rounded up by 15.14.4 r4: 16 -> 2, 9 -> 2, 8 -> 1;
      *>   integer (item 5) and numeric (item 4) - the 15.4 temporary
      *>     the implementor describes in 15.4.1, S9(30) (30, 30), which
      *>     a nested LENGTH / BYTE-LENGTH result is as well.
      *> The index function has its own golden (2023). Before the fix
      *> every line but the first two drew COBOLNET1627 "argument-1 is
      *> a numeric literal".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1398A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-X        PIC X(3) VALUE "abc".
       PROCEDURE DIVISION.
           DISPLAY "AN-L "
               FUNCTION LENGTH(FUNCTION UPPER-CASE(W-X)).
           DISPLAY "AN-B "
               FUNCTION BYTE-LENGTH(FUNCTION UPPER-CASE(W-X)).
           DISPLAY "NA-L "
               FUNCTION LENGTH(FUNCTION NATIONAL-OF(W-X)).
           DISPLAY "NA-B "
               FUNCTION BYTE-LENGTH(FUNCTION NATIONAL-OF(W-X)).
           DISPLAY "BO-L "
               FUNCTION LENGTH(FUNCTION BOOLEAN-OF-INTEGER(5, 16)).
           DISPLAY "BO-B "
               FUNCTION BYTE-LENGTH(FUNCTION BOOLEAN-OF-INTEGER(5, 16)).
           DISPLAY "BO9-B "
               FUNCTION BYTE-LENGTH(FUNCTION BOOLEAN-OF-INTEGER(5, 9)).
           DISPLAY "BO8-B "
               FUNCTION BYTE-LENGTH(FUNCTION BOOLEAN-OF-INTEGER(5, 8)).
           DISPLAY "IN-L "
               FUNCTION LENGTH(FUNCTION INTEGER-OF-DATE(20240101)).
           DISPLAY "IN-B "
               FUNCTION BYTE-LENGTH(FUNCTION INTEGER-OF-DATE(20240101)).
           DISPLAY "NU-L " FUNCTION LENGTH(FUNCTION SQRT(4)).
           DISPLAY "NU-B " FUNCTION BYTE-LENGTH(FUNCTION SQRT(4)).
           DISPLAY "NE-L " FUNCTION LENGTH(FUNCTION LENGTH(W-X)).
           DISPLAY "NE-B "
               FUNCTION BYTE-LENGTH(FUNCTION BYTE-LENGTH(W-X)).
           STOP RUN.
