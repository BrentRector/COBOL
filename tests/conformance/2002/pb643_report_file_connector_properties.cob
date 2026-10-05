      *> kb/Work PB643 sibling sweep - a REPORT file's connector gets
      *> the same file-control and file-description properties as any
      *> other file: its CODE-SET and its SHARING mode (and RESERVE).
      *>
      *> §13.4.5.2 Format 3 (report) prints CODE-SET for a report file
      *>   cite.py: OK  §13.4.5.2  (General formats)
      *> "The CODE-SET clause identifies alphabets to be used for
      *> converting data ... from the native character set to the
      *> coded character set on the storage medium during output
      *> operations."
      *>   cite.py: OK  §13.18.13.4 1)  (General rules)
      *> SHARING clause, §12.4.5.15 (a Format 3 file control entry)
      *>   cite.py: OK  §12.4.5.15  (SHARING clause)
      *>
      *> DERIVATION.
      *>   SHARE: RPT is open in the output mode under SHARING WITH ALL
      *>     OTHER, so a second connector (CHK, also ALL OTHER) may open
      *>     the same physical file in the input mode -> '00'. Before
      *>     the fix the clause was not registered and CHK got '61'.
      *>   CODE: the detail line "ABC" is converted to EBCDIC on the
      *>     medium; CHK (no CODE-SET) reads the first byte raw ->
      *>     X"C1" (EBCDIC A). Before the fix it read "A" (native).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB643RPT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET EB IS EBCDIC.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb643rpt2.txt"
               RESERVE 2 AREAS
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT CHK ASSIGN TO "pb643rpt2.txt"
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS C-ST.
       DATA DIVISION.
       FILE SECTION.
       FD RPT CODE-SET IS EB REPORT IS R.
       FD CHK.
       01 CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01 C-ST PIC XX.
       REPORT SECTION.
       RD R.
       01 DET TYPE DE.
          02 LINE PLUS 1.
             03 COLUMN 1 PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT
           INITIATE R
           GENERATE DET
           OPEN INPUT CHK
           DISPLAY "SHARE " C-ST
           CLOSE CHK
           TERMINATE R
           CLOSE RPT
           OPEN INPUT CHK
           READ CHK
           IF CHK-REC = X"C1"
               DISPLAY "CODE EBCDIC"
           ELSE
               DISPLAY "CODE NATIVE"
           END-IF
           CLOSE CHK
           STOP RUN.
