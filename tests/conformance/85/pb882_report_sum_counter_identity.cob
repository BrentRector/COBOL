      *> ⛔ A SUM COUNTER'S IDENTITY IS ITS ENTRY, AND ITS DATA-NAME IS A NAME THE PROCEDURE DIVISION MAY USE
      *> (kb/Work PB882 × PB840).  Two rules of one clause, and both of them used to be wrong.
      *>
      *> (1) THE IDENTITY.  ISO 13.18.54.4 GR1: "Each entry containing a SUM clause establishes an independent
      *> sum counter and size error indicator."  EACH ENTRY — so two entries that spell their data-name the same
      *> way still have two counters.  The source below is conforming: 8.4.2.2.1's uniqueness requirement bites on
      *> a REFERENCE ("a statement shall contain a reference that uniquely identifies that resource"), and neither
      *> CF-T is referenced anywhere.  COBOL.NET keyed its counters by that data-name, so the second registration
      *> destroyed the first and BOTH printable faces rendered the second counter: `0022  0022` printed where the
      *> standard owes `0011  0022`.
      *>
      *> (2) THE NAME.  ISO 13.18.54.4 GR5: "If a data-name immediately follows the level number in the entry
      *> containing the SUM clause, the data-name is the name of the sum counter, not the name of the associated
      *> printable item, if any."  GR12: "It is permissible for procedure division statements to alter the content
      *> of sum counters."  ALTERING presupposes REFERENCING, so the counter's data-name belongs in the source
      *> element's name space; `MOVE 500 TO CF-U` used to draw COBOLNET1639 'is not defined'.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT.  One detail, one CONTROL FOOTING FINAL, one GENERATE, one TERMINATE.
      *> 13.18.54.4 GR2 sets every counter to zero at the INITIATE; GR7 c) 1) accumulates each addend "whenever any
      *> GENERATE statement is executed for the current report", i.e. once here; 14.9.46.4 GR2 produces the CONTROL
      *> FOOTING FINAL during the TERMINATE, and GR4 moves each counter to its printable item.
      *>   CF-T at COLUMN 1  SUM WS-A  — 0 + 11                                              => 0011
      *>   CF-T at COLUMN 7  SUM WS-B  — its OWN counter (GR1), 0 + 22                       => 0022
      *>   CF-U at COLUMN 13 SUM WS-A  — altered to 500 after the INITIATE (GR12), + 11       => 0511
      *> WS-SEEN is CF-U read AFTER the TERMINATE.  GR2's second sentence resets a counter "at the end of the
      *> processing of the report group in which it is printed", and that group was just produced, so the counter
      *> reads 00 — a value the rule fixes, not a measurement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB882SCI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb882sci.txt".
           SELECT CHK ASSIGN TO "pb882sci.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-SCI.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF   PIC X     VALUE "N".
       01  WS-A     PIC 99    VALUE 11.
       01  WS-B     PIC 99    VALUE 22.
       01  WS-SEEN  PIC 99    VALUE 99.
       01  WS-I     PIC 99    VALUE 0.
       01  WS-LINE  PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-SCI CONTROL IS FINAL PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(3) VALUE "DET".
       01  CFG TYPE IS CONTROL FOOTING FINAL.
           02  LINE PLUS 1.
               03  CF-T COLUMN 1  PIC 9999 SUM WS-A.
               03  CF-T COLUMN 7  PIC 9999 SUM WS-B.
               03  CF-U COLUMN 13 PIC 9999 SUM WS-A.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-SCI.
           MOVE 500 TO CF-U.
           GENERATE DET.
           TERMINATE R-SCI.
           MOVE CF-U TO WS-SEEN.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
           DISPLAY "SEEN=[" WS-SEEN "]".
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-X0D
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
