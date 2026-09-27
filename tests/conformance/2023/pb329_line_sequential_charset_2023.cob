      *> kb/Work PB329 - THE IMPLEMENTOR-DEFINED LINE SEQUENTIAL
      *> CHARACTER SET (Annex A.1 item 115, required + documented;
      *> the determination is docs/CONFORMANCE.md DOC-A.1-115: the
      *> set is every character whose code point is U+0020 or above).
      *> ONE set, three rules, and this golden walks all three plus
      *> the boundary:
      *>
      *>   14.9.30.4 GR16 - "If the execution of the READ statement is
      *>   successful but the record area contains one or more
      *>   characters not in the implementor-defined character set for
      *>   a line sequential file, the I-O status in the read file
      *>   connector is set to '09'." (9.1.13.2 item 7 says the same
      *>   from the status side.)  R1 below.
      *>
      *>   14.9.51.4 GR23 - "For a line sequential file, if the record
      *>   area contains one or more characters that are not in the
      *>   implementor-defined character set defined for a line
      *>   sequential file, the execution of the WRITE statement is
      *>   unsuccessful and the I-O status in the write file connector
      *>   is set to '71'."  W2 below.
      *>
      *>   14.9.35.4 GR17 d) - the same sentence for REWRITE.  X2.
      *>
      *>   9.1.13.10 item 1 adds that after a '71' "the write or
      *>   rewrite operation was unsuccessful and the record area
      *>   remains unchanged" - proved by re-reading the file and
      *>   finding only the records the successful writes released.
      *>
      *> HOW A BAD CHARACTER GETS ONTO THE MEDIUM AT ALL: all three
      *> rules are organization-scoped ("for a line sequential file"),
      *> so a RECORD sequential connector carries no such rule and its
      *> print stream frames the record with CR/LF. Writing an extract
      *> or print file and reading it back under a different
      *> description is exactly the idiom 9.1.13.2's warning statuses
      *> exist for.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB329L23.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEEDF ASSIGN TO "pb329l23.dat"
              ORGANIZATION IS SEQUENTIAL
              FILE STATUS IS SEED-ST.
           SELECT LINF ASSIGN TO "pb329l23.dat"
              ORGANIZATION IS LINE SEQUENTIAL
              FILE STATUS IS LIN-ST.
           SELECT OUTF ASSIGN TO "pb329l23w.dat"
              ORGANIZATION IS LINE SEQUENTIAL
              FILE STATUS IS OUT-ST.
       DATA DIVISION.
       FILE SECTION.
       FD SEEDF.
       01 S-REC PIC X(7).
       FD LINF.
       01 L-REC PIC X(7).
       FD OUTF.
       01 O-REC PIC X(7).
       WORKING-STORAGE SECTION.
       01 SEED-ST PIC XX.
       01 LIN-ST PIC XX.
       01 OUT-ST PIC XX.
       01 W-ORD PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
      *> ---- A. seed one line holding U+0000 and one holding only
      *>         members of the set, through the record sequential
      *>         print stream (no GR23 there).
           OPEN OUTPUT SEEDF.
           MOVE "AB CD" TO S-REC.
           MOVE LOW-VALUE TO S-REC(3:1).
           WRITE S-REC BEFORE ADVANCING 1 LINE.
           MOVE "EFGHI" TO S-REC.
           WRITE S-REC BEFORE ADVANCING 1 LINE.
           CLOSE SEEDF.
      *> ---- B. READ it back LINE SEQUENTIAL: GR16.
           OPEN INPUT LINF.
           READ LINF AT END DISPLAY "EOF1".
           DISPLAY "R1=" LIN-ST.
           MOVE FUNCTION ORD(L-REC(3:1)) TO W-ORD.
           DISPLAY "R1ORD3=" W-ORD.
           READ LINF AT END DISPLAY "EOF2".
           DISPLAY "R2=" LIN-ST "[" L-REC "]".
           CLOSE LINF.
      *> ---- C. WRITE: GR23, and the medium left unchanged.
           OPEN OUTPUT OUTF.
           MOVE "JKLMNOP" TO O-REC.
           WRITE O-REC.
           DISPLAY "W1=" OUT-ST.
           MOVE LOW-VALUE TO O-REC(3:1).
           WRITE O-REC.
           DISPLAY "W2=" OUT-ST.
           MOVE "UVWXYZA" TO O-REC.
           WRITE O-REC.
           DISPLAY "W3=" OUT-ST.
           CLOSE OUTF.
      *> ---- D. REWRITE: GR17 d) reaches the SAME set. The lines are
      *>         written full-width (7 non-space characters): GR17 b)
      *>         compares O-REC's 7 bytes with the line being replaced,
      *>         and a line WRITE trimmed of trailing spaces (GR21) is
      *>         shorter, so its REWRITE would be '44' (kb/Work PB1168).
           OPEN I-O OUTF.
           READ OUTF AT END DISPLAY "EOF3".
           DISPLAY "R3=" OUT-ST "[" O-REC "]".
           MOVE "PQRSTUV" TO O-REC.
           REWRITE O-REC.
           DISPLAY "X1=" OUT-ST.
           READ OUTF AT END DISPLAY "EOF4".
           DISPLAY "R4=" OUT-ST "[" O-REC "]".
           MOVE LOW-VALUE TO O-REC(2:1).
           REWRITE O-REC.
           DISPLAY "X2=" OUT-ST.
           CLOSE OUTF.
      *> ---- E. the file holds exactly the two records the successful
      *>         writes released, the first of them rewritten.
           OPEN INPUT OUTF.
           READ OUTF AT END DISPLAY "EOF5".
           DISPLAY "F1=" OUT-ST "[" O-REC "]".
           READ OUTF AT END DISPLAY "EOF6".
           DISPLAY "F2=" OUT-ST "[" O-REC "]".
           READ OUTF AT END DISPLAY "F3=EOF".
           CLOSE OUTF.
      *> ---- F. the boundary. SPACE (U+0020) is the LOWEST member;
      *>         DEL (U+007F) and the Latin-1 supplement are members
      *>         too, so an ordinary 8-bit text file never reports 09.
           OPEN OUTPUT OUTF.
           MOVE "A B" TO O-REC.
           MOVE X"7F" TO O-REC(4:1).
           MOVE X"FF" TO O-REC(5:1).
           MOVE "Z" TO O-REC(6:1).
           WRITE O-REC.
           DISPLAY "B1=" OUT-ST.
           CLOSE OUTF.
           OPEN INPUT OUTF.
           READ OUTF AT END DISPLAY "EOF7".
           DISPLAY "B2=" OUT-ST.
           MOVE FUNCTION ORD(O-REC(4:1)) TO W-ORD.
           DISPLAY "B2ORD4=" W-ORD.
           MOVE FUNCTION ORD(O-REC(5:1)) TO W-ORD.
           DISPLAY "B2ORD5=" W-ORD.
           CLOSE OUTF.
           STOP RUN.
