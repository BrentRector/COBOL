      *> reject-at: 2002 2014 2023
      *> 7.3.3 SR1: 'A compiler directive shall be specified on one
      *> line, except for the EVALUATE and the IF directives for which
      *> specific rules are specified.' The >>DEFINE below is continued
      *> with a column-7 hyphen; the continuation line is refused by
      *> name and does not extend the directive's operand, so XX stays
      *> "AB" (kb/Work PB1360).
000100 >>DEFINE XX AS "AB"
000200-    "CD"
000300 IDENTIFICATION DIVISION.
000400 PROGRAM-ID. PB1360DF.
000500 PROCEDURE DIVISION.
000600 >>IF XX = "ABCD"
000700     DISPLAY "JOINED".
000800 >>END-IF
000900     STOP RUN.
