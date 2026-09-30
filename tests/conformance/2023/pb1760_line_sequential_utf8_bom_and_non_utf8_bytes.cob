       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1760LS.
      *> kb/Work PB1760 (owner decision R51) - A LINE SEQUENTIAL FILE
      *> WITH NO CODE-SET IS UTF-8 TEXT (DOC-A.1-115). THIS GOLDEN
      *> PUTS RAW BYTES ON THE MEDIUM THROUGH A RECORD SEQUENTIAL FD
      *> (ONE BYTE PER POSITION, NO FRAMING) AND READS THEM BACK AS A
      *> LINE SEQUENTIAL FILE:
      *>   EF BB BF "AB" LF  "caf" E9 LF  "OK" LF
      *> WHY EACH LEG CAN FAIL:
      *>  R1 - A UTF-8 BYTE-ORDER MARK OPENING THE FILE IS NOT RECORD
      *>       DATA: THE RECORD IS "AB", STATUS '00'.
      *>  R2 - X'E9' ALONE IS NOT UTF-8 (A LATIN-1 "e-ACUTE"). IT
      *>       FORMS NO CHARACTER, SO THE RECORD AREA HOLDS ONE OUTSIDE
      *>       THE LINE SEQUENTIAL CHARACTER SET: THE READ IS
      *>       SUCCESSFUL WITH '09' (§14.9.30.4 GR16: "If the execution
      *>       of the READ statement is successful but the record area
      *>       contains one or more characters not in the
      *>       implementor-defined character set for a line sequential
      *>       file, the I-O status in the read file connector is set
      *>       to '09'"). "caf" IS DELIVERED; POSITION 4 IS NOT U+00E9.
      *>  X2 - REWRITING THAT RECORD AREA IS '71' (§14.9.35.4 GR17 d).
      *>  R3 - THE NEXT LINE READS NORMALLY: "OK", '00'.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RAW ASSIGN TO "pb1760ls.txt"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FR.
           SELECT LS ASSIGN TO "pb1760ls.txt"
               ORGANIZATION IS LINE SEQUENTIAL FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD RAW.
       01 RREC PIC X(14).
       FD LS.
       01 LREC PIC X(6).
       WORKING-STORAGE SECTION.
       01 FR PIC XX.
       01 FS PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RAW.
           MOVE X"EFBBBF41420A636166E90A4F4B0A" TO RREC.
           WRITE RREC.
           DISPLAY "RAW=" FR.
           CLOSE RAW.
           OPEN I-O LS.
           READ LS.
           DISPLAY "R1=" FS " [" LREC "]".
           READ LS.
           DISPLAY "R2=" FS " [" LREC(1:3) "]".
           IF LREC(4:1) = X"E9" DISPLAY "R2-4=E9"
           ELSE DISPLAY "R2-4=NOT"
           END-IF.
           REWRITE LREC.
           DISPLAY "X2=" FS.
           READ LS.
           DISPLAY "R3=" FS " [" LREC "]".
           READ LS AT END DISPLAY "R4=AT-END".
           CLOSE LS.
           STOP RUN.
