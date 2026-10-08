       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1943A.
      *> kb/Work PB1943 - a REPORT SECTION sum counter may be reference-modified.
      *>
      *> 13.18.54.4 GR5: "If a data-name immediately follows the level number in the entry containing the SUM
      *> clause, the data-name is the name of the sum counter, not the name of the associated printable item".
      *>   cite.py: OK  13.18.54.4 5)  (General rules)
      *> 13.18.54.4 GR1: the sum counter "is a conceptual data item that behaves as a data item of the category
      *> numeric. The number of decimal digits in the sum counter, both integral and fractional, is derived from the
      *> corresponding number of digits, excluding insertion editing characters, in the PICTURE clause". It states
      *> NO usage, so the counter's representation is the implementor's (docs/CONFORMANCE.md A.4.11): a signed
      *> USAGE DISPLAY item of the PICTURE's digits.
      *>   cite.py: OK  13.18.54.4 1)  (General rules)
      *> 8.4.3.3.3 SR1: identifier-1 may be "a numeric data item of usage display or national that is not
      *> subordinate to a strongly-typed group item", so the DISPLAY counter may be reference-modified.
      *>   cite.py: OK  8.4.3.3.3 1)  (Syntax rules)
      *> 13.18.54.4 GR12: "It is permissible for procedure division statements to alter the content of sum counters"
      *> - a reference-modified counter is also a RECEIVER.
      *>   cite.py: OK  13.18.54.4 12)  (General rules)
      *> Before the fix every one of these references drew COBOLNET1639 "'CF-T(1:2)' is not defined", naming a rule
      *> the program had not broken (GR5 declares the name).
      *>
      *> DERIVATION. WS-X = 1234. One GENERATE adds 1234 into CF-T (GR7 c: an identifier addend is added on every
      *> GENERATE), so the counter's four digits are 1 2 3 4.
      *>   CF-T (1:2)          = digits 1-2          = "12"
      *>   CF-T OF CFG (2:2)   = digits 2-3          = "23"   (qualified by the report group entry)
      *>   CF-T OF R1 (1:3)    = digits 1-3          = "123"  (qualified by the report-name)
      *>   MOVE "00" TO CF-T (1:2) replaces digits 1-2; the counter is then 0034, and MOVE CF-T TO WS-D gives 0034.
      *>   CF-T (3:)           = the omitted length runs to the rightmost position (8.4.3.3.4 GR5 c): digits 3-4 of
      *>   the signed counter, "3" and the last digit 4, which carries the sign (GR1: the counter is signed) as in
      *>   any signed DISPLAY item: positive 4 is "D" in the default IBM overpunch convention (--sign-encoding ibm,
      *>   docs/CONFORMANCE.md) - "3D".
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1943A.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-X     PIC 9999 VALUE 1234.
       01  WS-A     PIC XX   VALUE SPACES.
       01  WS-B     PIC XX   VALUE SPACES.
       01  WS-C     PIC XXX  VALUE SPACES.
       01  WS-D     PIC 9999 VALUE 0.
       01  WS-E     PIC XX   VALUE SPACES.
       REPORT SECTION.
       RD  R1 CONTROL IS FINAL PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(3) VALUE "DET".
       01  CFG TYPE IS CONTROL FOOTING FINAL.
           02  LINE PLUS 1.
               03  CF-T COLUMN 1  PIC 9999 SUM WS-X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DET.
           MOVE CF-T (1:2) TO WS-A.
           MOVE CF-T OF CFG (2:2) TO WS-B.
           MOVE CF-T OF R1 (1:3) TO WS-C.
           DISPLAY "A=[" WS-A "] B=[" WS-B "] C=[" WS-C "]".
           MOVE "00" TO CF-T (1:2).
           MOVE CF-T TO WS-D.
           MOVE CF-T (3:) TO WS-E.
           DISPLAY "D=[" WS-D "] E=[" WS-E "]".
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
