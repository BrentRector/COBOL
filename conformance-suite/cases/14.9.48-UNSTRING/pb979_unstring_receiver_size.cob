      *>--- conformance-case
      *> id: pb979_unstring_receiver_size
      *> rules: [GR-14.9.48.4-5, GR-14.9.48.4-8, GR-14.9.48.4-11,
      *>    GR-14.9.48.4-12, GR-14.9.48.4-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB979 - UNSTRING's receiving areas, stored "according to the rules for the MOVE
      *> statement" (ISO 14.9.48.4 GR11 c) and examined over "the size of the current receiving area"
      *> (GR11 b). Before the fix a reference-modified receiver without DELIMITED BY was staged as
      *> "not implemented" (COBOLNET1756) and aborted the run unit. Every value is derived from the rules:
      *>
      *> RM1  S = "ABCDEFGHIJ", R = "********": INTO R(2:3) R(6:2) - GR11 b) examines the slice's size,
      *>      3 then 2 ("ABC", "DE"); "FGHIJ" is unexamined with every area acted upon: GR15 b) overflow.
      *>      -> R = "*ABC*DE*", OVF.
      *> RM2  WITH POINTER P = 4 INTO R(1:4) - four characters from position 4 ("DEFG"); GR13 leaves
      *>      P = 4 + 4 = 8; "HIJ" remains: overflow. -> R = "DEFG*DE*", P = 08, OVF.
      *> RM3  DELIMITED BY "," INTO R(2:3) over "HELLO,X" - "HELLO" moved into a 3-position unique item
      *>      (8.4.3.3.4 GR6) by the alphanumeric MOVE: truncated on the right -> R = "DHEL*DE*".
      *> SEP  N2 PIC S9(3) SIGN LEADING SEPARATE over "1234567": GR11 b) "one less than the size" -> 3
      *>      characters, "123" -> +123; X3 takes the next 3, "456"; "7" remains: overflow.
      *> GR8  "12,,34" DELIMITED BY "," INTO N1 N3 N4 A1 (N3 = 999, A1 = "XXX" before): two contiguous
      *>      delimiters ZERO-fill a numeric receiver and N4 takes "34" -> 012 000 034; A1 is not acted
      *>      upon (the sender is exhausted, GR11 g) and keeps "XXX".
      *> GR8A the same into A2 A3 AJ (A3 = "YYY" before) -> A2 "12 ", A3 spaces (GR8), AJ JUSTIFIED
      *>      RIGHT right-aligns "34" (14.9.25.4 GR6 c) -> "   34".
      *> DLM  "AB;CD" DELIMITED BY ";" INTO A2 DELIMITER IN R(8:1) A3 DELIMITER IN R(1:1) - the first
      *>      delimiter ";" lands in R(8:1); the second area ends at the end of the sender, so GR11 d)
      *>      space-fills R(1:1) -> A2 "AB ", A3 "CD ", R = " HEL*DE;" (from RM3's "DHEL*DE*").
      *> GRP  INTO G (G1 X(2), G2 X(3)): a group's size is its 5 character positions -> "ABCDE".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB979UNSTRSIZE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S   PIC X(10) VALUE "ABCDEFGHIJ".
       01 S2  PIC X(7)  VALUE "HELLO,X".
       01 S3  PIC X(7)  VALUE "1234567".
       01 S4  PIC X(6)  VALUE "12,,34".
       01 S5  PIC X(5)  VALUE "AB;CD".
       01 R   PIC X(8)  VALUE ALL "*".
       01 P   PIC 99    VALUE 4.
       01 N1  PIC 9(3).
       01 N2  PIC S9(3) SIGN LEADING SEPARATE.
       01 N3  PIC 9(3)  VALUE 999.
       01 N4  PIC 9(3).
       01 X3  PIC X(3).
       01 A1  PIC X(3)  VALUE "XXX".
       01 A2  PIC X(3).
       01 A3  PIC X(3)  VALUE "YYY".
       01 AJ  PIC X(5)  JUSTIFIED RIGHT.
       01 G.
          05 G1 PIC X(2).
          05 G2 PIC X(3).
       PROCEDURE DIVISION.
           UNSTRING S INTO R(2:3) R(6:2)
               ON OVERFLOW DISPLAY "RM1  [" R "] OVF"
               NOT ON OVERFLOW DISPLAY "RM1  [" R "] NO-OVF"
           END-UNSTRING
           UNSTRING S INTO R(1:4) WITH POINTER P
               ON OVERFLOW DISPLAY "RM2  [" R "] " P " OVF"
               NOT ON OVERFLOW DISPLAY "RM2  [" R "] " P " NO-OVF"
           END-UNSTRING
           UNSTRING S2 DELIMITED BY "," INTO R(2:3)
           DISPLAY "RM3  [" R "]"
           UNSTRING S3 INTO N2 X3
               ON OVERFLOW DISPLAY "SEP  " N2 " [" X3 "] OVF"
           END-UNSTRING
           UNSTRING S4 DELIMITED BY "," INTO N1 N3 N4 A1
           DISPLAY "GR8  " N1 " " N3 " " N4 " [" A1 "]"
           UNSTRING S4 DELIMITED BY "," INTO A2 A3 AJ
           DISPLAY "GR8A [" A2 "] [" A3 "] [" AJ "]"
           UNSTRING S5 DELIMITED BY ";" INTO A2 DELIMITER IN R(8:1)
               A3 DELIMITER IN R(1:1)
           DISPLAY "DLM  [" A2 "] [" A3 "] [" R "]"
           UNSTRING S INTO G
               ON OVERFLOW DISPLAY "GRP  [" G "] OVF"
           END-UNSTRING
           STOP RUN.
