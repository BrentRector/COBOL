      *> ISO §13.18.55.4 GR11 — records of a file are not synchronized
      *> (Annex A.1 item 196, Not provided): three odd-length records
      *> carrying a SYNCHRONIZED item are written with no slack.
      *>
      *> THE RULE. §13.18.55.4 11): "Any rules for synchronization of
      *>   the records of a file, as this affects the synchronization of
      *>   elementary items, shall be specified by the implementor."
      *>   OK  §13.18.55.4 11)  (General rules)
      *> Annex A.1 196): "SYNCHRONIZED clause (how records of a file are
      *>   handled). This item is optional."   OK  §A.1 196)
      *>
      *> THE DOCUMENTED CHOICE, docs/CONFORMANCE.md DOC-A.1-196: "Not
      *>   provided. COBOL.NET has no rule for synchronizing the
      *>   records of a file: a record area is never aligned or padded
      *>   as a whole, and a SYNCHRONIZED elementary item in a record
      *>   sits at exactly the offset it would have without the clause
      *>   (item 195), so the record written to and read from the file
      *>   is byte-for-byte the record's own image."
      *>
      *> WHAT THIS ADDS to conformance:2023/l1_sync_file_record (one
      *>   4-byte record, which no per-record padding to a 2- or 4-byte
      *>   boundary could disturb): THREE records of an ODD length, 5
      *>   bytes, so any rule that aligns or pads a record as a whole
      *>   (a slack byte after each record, or a record start on an even
      *>   boundary) moves records 2 and 3 of the synchronized
      *>   description FS away from where the unsynchronized description
      *>   FP looks for them. FS and FP name the SAME external file and
      *>   differ ONLY in the SYNCHRONIZED clause on the binary item.
      *>
      *> DERIVATION. Under the documented choice both descriptions put
      *>   A at byte 1, N at bytes 2-3, B at 4 and C at 5 (item 195: the
      *>   clause changes no offset), and nothing is written between or
      *>   after records. So each record written through FS is read back
      *>   through FP with the SAME field values -> REC1/REC2/REC3=YES
      *>   (both items are S9(4) COMP, one representation, so the
      *>   comparison depends only on byte PLACEMENT). -1234, 258 and
      *>   4660 give three different two-byte images, so a shifted read
      *>   cannot match by coincidence. After three 5-byte records there
      *>   is no next logical record, so the fourth READ takes AT END
      *>   (§9.1.13.4 1) a) "NEXT was specified or implied and the end
      *>   of the physical file has been reached" OK) -> EOF4=YES;
      *>   trailing slack would surface as a fourth (partial) record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DNS196.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FS ASSIGN TO "l1dns196.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FP ASSIGN TO "l1dns196.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD FS.
       01 RS.
          05 RS-A PIC X.
          05 RS-N PIC S9(4) COMP SYNCHRONIZED.
          05 RS-B PIC X.
          05 RS-C PIC X.
       FD FP.
       01 RP.
          05 RP-A PIC X.
          05 RP-N PIC S9(4) COMP.
          05 RP-B PIC X.
          05 RP-C PIC X.
       WORKING-STORAGE SECTION.
       01 WS-OK  PIC X(3).
       01 WS-EOF PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FS
           MOVE "a" TO RS-A
           MOVE -1234 TO RS-N
           MOVE "b" TO RS-B
           MOVE "c" TO RS-C
           WRITE RS
           MOVE "d" TO RS-A
           MOVE 258 TO RS-N
           MOVE "e" TO RS-B
           MOVE "f" TO RS-C
           WRITE RS
           MOVE "g" TO RS-A
           MOVE 4660 TO RS-N
           MOVE "h" TO RS-B
           MOVE "i" TO RS-C
           WRITE RS
           CLOSE FS
           OPEN INPUT FP
           MOVE "NO" TO WS-OK
           READ FP
               AT END MOVE "EOF" TO WS-OK
               NOT AT END
                   IF RP-A = "a" AND RP-N = -1234
                      AND RP-B = "b" AND RP-C = "c"
                       MOVE "YES" TO WS-OK
                   END-IF
           END-READ
           DISPLAY "REC1=" WS-OK
           MOVE "NO" TO WS-OK
           READ FP
               AT END MOVE "EOF" TO WS-OK
               NOT AT END
                   IF RP-A = "d" AND RP-N = 258
                      AND RP-B = "e" AND RP-C = "f"
                       MOVE "YES" TO WS-OK
                   END-IF
           END-READ
           DISPLAY "REC2=" WS-OK
           MOVE "NO" TO WS-OK
           READ FP
               AT END MOVE "EOF" TO WS-OK
               NOT AT END
                   IF RP-A = "g" AND RP-N = 4660
                      AND RP-B = "h" AND RP-C = "i"
                       MOVE "YES" TO WS-OK
                   END-IF
           END-READ
           DISPLAY "REC3=" WS-OK
           READ FP
               AT END MOVE "YES" TO WS-EOF
               NOT AT END MOVE "NO" TO WS-EOF
           END-READ
           DISPLAY "EOF4=" WS-EOF
           CLOSE FP
           STOP RUN.
