*> reject-at: 2002 2014 2023
*> kb/Work PB1930 - the refusing half of the CALL lane. 14.8.2.3.3 rule 2 a): "If the formal parameter is numeric, the
*> conformance rules are the same as for a COMPUTE statement with the argument as the sending operand", and a COMPUTE
*> sender shall be numeric (8.8.1.1). A function-identifier is now an identifier argument (14.9.4.4 GR8), so the verdict
*> is the ACTIVATION'S conformance (COBOLNET1688, 14.8.2 via 14.9.4.3 SR25) and not the arithmetic screen's: an
*> ALPHANUMERIC function into a numeric formal stays refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1930NEGN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(3) VALUE "abc".
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB1930NEGT" AS NESTED
               USING BY CONTENT FUNCTION UPPER-CASE(X)
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1930NEGT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 9.
       PROCEDURE DIVISION USING L.
           DISPLAY "L=" L
           GOBACK.
       END PROGRAM PB1930NEGT.
       END PROGRAM PB1930NEGN.
