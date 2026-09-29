       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB317NR.
      *> kb/Work PB317. ISO 14.9.27.4 GR11: "The NO REWIND phrase will
      *> be ignored if it does not apply to the storage medium on which
      *> the file resides. If the NO REWIND phrase is ignored, the OPEN
      *> statement is successful and the I-O status associated with
      *> file-name-1 is set to '07'." 9.1.13.2 item 6 fixes which medium
      *> that is: '07' is "An OPEN or CLOSE statement ... successfully
      *> executed but ... an OPEN statement with the NO REWIND phrase
      *> references a physical file on a non-reel/unit medium". Every
      *> file this implementation provides is 14.9.6.4 GR2 category (a)
      *> Non-unit (docs/CONFORMANCE.md section 7, A.1 item 24), so the
      *> phrase is ALWAYS ignored here and 14.9.27.4 GR12 - the rule for
      *> a medium that permits rewinding - never applies.
      *> EVERY EXPECTED VALUE IS COMPUTED FROM THOSE RULES:
      *>   OUT-PLAIN    00  9.1.13.2 item 1 (no phrase written).
      *>   CLOSE-PLAIN  00  same.
      *>   IN-NOREW     07  GR11 + 9.1.13.2 item 6.
      *>   READ1        00  and AAAAAAAA: GR11 IGNORES the phrase, so
      *>                    14.9.27.4 GR14 sets the file position
      *>                    indicator to 1 exactly as for a plain OPEN.
      *>   OUT-NOREW    07  GR11 again - the phrase is ignored on OUTPUT
      *>                    too; SR6 admits INPUT and OUTPUT only.
      *>   READ2-EOF    Y and 10: proof the phrase was IGNORED rather
      *>                    than honoured as GR12 b) would honour it -
      *>                    14.9.27.4 GR18 still created an empty file,
      *>                    so the reopened file is at end (9.1.13.4
      *>                    item 1 a).
      *>   CLOSE-NOREW  07  the CLOSE twin, Table 14 cell c,g - here to
      *>                    prove the two statements now agree.
      *>   OPT-ABSENT   07  an OPTIONAL INPUT file that is not present.
      *>                    9.1.13.2 item 4 a) DESCRIBES '05' for that
      *>                    open; GR11 ASSIGNS '07', and 14.9.27.4
      *>                    assigns '05' only in GR17, whose EXTEND/I-O
      *>                    modes SR6 bars from carrying the phrase. The
      *>                    statement clause's assignment governs
      *>                    (docs/CONFORMANCE.md section 3).
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb317nr.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-ST.
           SELECT OPTIONAL G ASSIGN TO "pb317nrg.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-GS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(8).
       FD G.
       01 G-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 WS-ST PIC XX.
       01 WS-GS PIC XX.
       01 WS-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           DISPLAY "OUT-PLAIN=" WS-ST
           MOVE "AAAAAAAA" TO F-REC
           WRITE F-REC
           MOVE "BBBBBBBB" TO F-REC
           WRITE F-REC
           CLOSE F
           DISPLAY "CLOSE-PLAIN=" WS-ST
           OPEN INPUT F WITH NO REWIND
           DISPLAY "IN-NOREW=" WS-ST
           READ F AT END CONTINUE END-READ
           DISPLAY "READ1=" WS-ST " " F-REC
           CLOSE F
           OPEN OUTPUT F WITH NO REWIND
           DISPLAY "OUT-NOREW=" WS-ST
           CLOSE F
           OPEN INPUT F
           READ F AT END MOVE "Y" TO WS-EOF END-READ
           DISPLAY "READ2-EOF=" WS-EOF " " WS-ST
           CLOSE F WITH NO REWIND
           DISPLAY "CLOSE-NOREW=" WS-ST
           OPEN INPUT G WITH NO REWIND
           DISPLAY "OPT-ABSENT=" WS-GS
           CLOSE G
           STOP RUN.
