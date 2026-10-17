*>--- conformance-case
*> id: pb613-bare-literal-by-value-formal
*> rules: [GR-14.9.4.4-8, GR-14.9.4.4-9]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB613 / PB3101 - the keyword-less "AB" reaches a boolean node on the LATER argument's
      *> B-AND, and 14.9.4.4 GR8 makes it literal-2 all the same. Its formal X is BY VALUE, so GR9 b)
      *> assumes BY VALUE, and 14.9.4.3 SR23 then requires a numeric literal: refused, naming SR23. The
      *> boolean arm used to hand the literal its mode without asking SR23.
      *> cite.py --check 14.9.4.3 "If literal-2 or its corresponding formal parameter is specified with the
      *>   BY VALUE phrase, literal-2 shall be a numeric literal" -> OK 14.9.4.3 23)
      *> cite.py --check 14.9.4.4 "When the BY VALUE phrase is specified or implied for the corresponding
      *>   formal parameter, BY VALUE is assumed" -> OK 14.9.4.4 9) b)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N613V.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B1 PIC 1(4) VALUE B"1100".
       01 B2 PIC 1(4) VALUE B"1010".
       PROCEDURE DIVISION.
           CALL "S613W" AS NESTED USING "AB" B1 B-AND B2
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S613W.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X PIC 9(4) BINARY.
       01 Y PIC 1(4).
       PROCEDURE DIVISION USING BY VALUE X BY REFERENCE Y.
           DISPLAY "X=" X " Y=" Y
           GOBACK.
       END PROGRAM S613W.
       END PROGRAM N613V.
