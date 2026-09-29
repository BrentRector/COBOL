      *> kb/Work PB1394 - the conforming separator forms of ISO 8.3.5,
      *>  which the separator screen must admit. Rule 2: a comma or
      *>  semicolon followed by a space is a separator anywhere a space
      *>  is (MOVE 1 TO N, M ; MOVE 2 TO N; M). Rule 3: a period
      *>  followed by a space ends the sentence. Rule 5: a literal
      *>  opening delimiter preceded by a space or '(' and a closing
      *>  delimiter followed by a space, comma, semicolon, period or
      *>  ')'. Expected, derived: N=001 M=001, then N=002 M=002, then
      *>  the literals ABCDEF, the IF with a parenthesized literal
      *>  comparison is true (Y), and T(2) is set from a literal that
      *>  sits between '(' ... ')' neighbours.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73ASEP85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(3).
       01 M PIC 9(3).
       01 A PIC X(2) VALUE "AB".
       01 TT.
          05 T PIC X(2) OCCURS 2.
       PROCEDURE DIVISION.
           MOVE 1 TO N, M.
           DISPLAY "N=" N " M=" M.
           MOVE 2 TO N; M.
           DISPLAY "N=" N " M=" M.
           DISPLAY "AB", "CD"; "EF".
           IF (A = "AB") DISPLAY "Y" ELSE DISPLAY "N".
           MOVE "XY" TO T (2).
           DISPLAY T (2).
           STOP RUN.
