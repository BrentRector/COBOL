      *> I-O status '04' on a record-sequential READ (ISO §9.1.13.2 item 3 / §14.9.30
      *> GR14; clarified COBOL-2023 Annex E.2 item 15, version-invariant behavior). A
      *> READ whose physical record length is outside the file's min/max record size
      *> is SUCCESSFUL but sets status '04' (the record is still delivered). A
      *> FIXED-length record sequential file carries no record length on the medium
      *> (§9.1.7.2: the length is "determined by any information the implementor may
      *> add to the record on the physical storage medium", and this format adds none
      *> -- docs/CONFORMANCE.md DOC-A.1-146 (a), kb/Work PB1514), so its physical
      *> record is the next integer-1 bytes of the READING description. Here three
      *> 5-character records are written (a 15-byte physical file), then read back
      *> through a 10-character record description: the first physical record is
      *> bytes 1-10 ('00'), the second is the 5 bytes left at the end of the file --
      *> shorter than the 10-byte fixed length, so '04' (§14.9.30.4 GR14), and the
      *> record area to the right of the 5 bytes read is UNDEFINED by the same rule,
      *> so only those 5 bytes are shown -- and the third READ hits end-of-file.
      *> The L leg writes ONE 15-byte record and reads it through the same 10-byte
      *> description: a fixed-length file has no record longer than the reading
      *> description by construction, so the 15 bytes are the 10-byte record '00'
      *> and the 5-byte partial record '04' -- never one truncated '04' record.
      *>   python scripts/spec/cite.py --check 14.9.30.4 "the portion of the record
      *>   area that is to the right of the last valid character read is undefined"
      *>                                          -> OK  §14.9.30.4 14)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IOSTAT-04.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-OUT ASSIGN TO "iostat04.txt"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F-IN ASSIGN TO "iostat04.txt"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS WS-FS.
           SELECT L-OUT ASSIGN TO "iostat04l.txt"
               ORGANIZATION IS SEQUENTIAL.
           SELECT L-IN ASSIGN TO "iostat04l.txt"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD F-OUT.
       01 R5 PIC X(5).
       FD F-IN.
       01 R10 PIC X(10).
       FD L-OUT.
       01 R15 PIC X(15).
       FD L-IN.
       01 L10 PIC X(10).
       WORKING-STORAGE SECTION.
       01 WS-FS PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F-OUT.
           WRITE R5 FROM "AAAAA".
           WRITE R5 FROM "BBBBB".
           WRITE R5 FROM "CCCCC".
           CLOSE F-OUT.
           OPEN INPUT F-IN.
           READ F-IN.
           DISPLAY "S1=[" WS-FS "] R=[" R10 "]".
           READ F-IN.
           DISPLAY "S2=[" WS-FS "] R=[" R10(1:5) "]".
           READ F-IN AT END DISPLAY "S3=EOF".
           CLOSE F-IN.
           OPEN OUTPUT L-OUT.
           WRITE R15 FROM "DDDDDDDDDDEEEEE".
           CLOSE L-OUT.
           OPEN INPUT L-IN.
           READ L-IN.
           DISPLAY "L1=[" WS-FS "] R=[" L10 "]".
           READ L-IN.
           DISPLAY "L2=[" WS-FS "] R=[" L10(1:5) "]".
           READ L-IN AT END DISPLAY "L3=EOF".
           CLOSE L-IN.
           STOP RUN.
