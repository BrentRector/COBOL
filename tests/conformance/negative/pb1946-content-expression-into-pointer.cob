      *> reject-at: 2002 2014 2023
      *> kb/Work PB1946 (verdict kb/Work PB1936) -- ISO 14.8.2.3.3: a formal
      *> parameter of class pointer takes its argument by the SET rules, and no
      *> SET format sends an arithmetic expression into a pointer receiver
      *> (14.9.39.3 SR17: identifier-5 and identifier-6 are of category
      *> data-pointer, so only a pointer identifier is sent). The BY CONTENT
      *> expression N3 * 2 into the USAGE POINTER nested-program formal is
      *> COBOLNET1688 -- the value verdict asks the SET question before it
      *> asks the numeric-sender question.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1946P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N3 PIC 9(3) VALUE 123.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB1946PS" AS NESTED USING BY CONTENT N3 * 2
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1946PS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L USAGE POINTER.
       PROCEDURE DIVISION USING L.
           DISPLAY "PTR"
           GOBACK.
       END PROGRAM PB1946PS.
       END PROGRAM PB1946P.
