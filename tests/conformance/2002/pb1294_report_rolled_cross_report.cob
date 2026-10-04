      *> kb/Work PB1294 - ISO 13.18.54.3 SR4 g) and 13.18.54.4 GR7 c) 2): a SUM clause whose data-name-1 is an entry
      *> of a DIFFERENT report description, and whose UPON phrase names a detail of one.
      *> SR4 g): "If data-name-1 specifies an entry in a different report description, there are no restrictions on
      *>   the combinations of report types of each report group."
      *>   cite.py: OK  13.18.54.3 4)  (Syntax rules)
      *> GR7 a): "If data-name-1 is the name of an entry in a different report group description, adding takes place
      *>   when the report group description containing data-name-1 is processed."
      *>   cite.py: OK  13.18.54.4 7) a)  (General rules)
      *> GR7 c) 2.: "If an UPON phrase is specified, whenever any GENERATE statement is executed for a detail
      *>   referenced by the UPON phrase."   cite.py: OK  13.18.54.4 7) c)  (General rules)
      *> 13.18.54.3 SR7: "Data-name-2 shall be the name of a detail. It may be qualified only by a report-name."
      *>   cite.py: OK  13.18.54.3 7)  (Syntax rules)
      *>
      *> DERIVATION. Two reports. RA's detail DA holds X1 (SOURCE WS-A); RB's detail DB prints WS-A and its FINAL
      *> footing CFB holds TOT, SUM X1 OF RA (a rolled total of RA's entry, added when RA processes DA - GR7 a), and UT,
      *> SUM WS-A UPON DA OF RA (added whenever a GENERATE executes for RA's detail DA - GR7 c) 2.). The program
      *> generates DA then DB for WS-A = 3 and again for WS-A = 4. DB's own GENERATEs are not events of either clause,
      *> so TOT = 3 + 4 = 7 and UT = 3 + 4 = 7 whatever DB prints. RB's file: line 1 DB(3) `003`, line 2 DB(4) `004`,
      *> line 3 the footing printed by TERMINATE RB: TOT PIC 9999 at column 1 and UT PIC 9999 at column 6 `0007 0007`.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1294XRP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTA ASSIGN TO "PB1294XRA.TXT".
           SELECT PRTB ASSIGN TO "PB1294XRB.TXT".
           SELECT CHK ASSIGN TO "PB1294XRB.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTA REPORT IS RA.
       FD  PRTB REPORT IS RB.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-A    PIC 999   VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  RA.
       01  DA TYPE DE LINE PLUS 1.
           05  X1 COLUMN 1 PIC 999 SOURCE WS-A.
       RD  RB CONTROL IS FINAL.
       01  DB TYPE DE LINE PLUS 1.
           05  COLUMN 1 PIC 999 SOURCE WS-A.
       01  CFB TYPE CF FINAL LINE PLUS 1.
           05  TOT COLUMN 1 PIC 9999 SUM X1 OF RA.
           05  UT COLUMN 6 PIC 9999 SUM WS-A UPON DA OF RA.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRTA.
           OPEN OUTPUT PRTB.
           INITIATE RA.
           INITIATE RB.
           MOVE 3 TO WS-A.
           GENERATE DA.
           GENERATE DB.
           MOVE 4 TO WS-A.
           GENERATE DA.
           GENERATE DB.
           TERMINATE RA.
           TERMINATE RB.
           CLOSE PRTA.
           CLOSE PRTB.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHK-REC TO WS-BYTE
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           EVALUATE TRUE
               WHEN WS-BYTE = X"0A"
                   PERFORM SHOW-LINE
               WHEN WS-BYTE = X"0C"
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   MOVE 0 TO WS-LN
               WHEN WS-BYTE = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:16) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
