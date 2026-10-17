      *>--- conformance-case
      *> id: pb327_national_fd_record
      *> rules: [GR-14.9.27.4-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB327 - A NATIONAL LEAF IN AN FD RECORD. 13.18.60.4 GR8
      *> leaves a national character's storage size to the implementor
      *> ("National characters shall be represented in the storage of
      *> the computer as characters of a uniform size equal to or a
      *> multiple of the size of characters in the computer's
      *> alphanumeric character set") and COBOL.NET pins TWO bytes,
      *> UTF-16BE, high-order first (determination D-N1). So S-REC is
      *> an EIGHT-byte record: three national positions (six bytes)
      *> then two alphanumeric ones. The secondary 01 S-BYTES shares
      *> that area (9.1.2 - records are aligned on its leftmost byte),
      *> so ORD over each of its positions reads the record image BYTE
      *> BY BYTE: a one-byte-per-position layout would put "A" at
      *> byte 1 and a little-endian pair would put it at byte 1 too,
      *> so the 001/066 pair pins BOTH the width and the order.
      *> The EXTERNAL leg is 14.9.27.4 GR5's guarantee - "If the file
      *> connector associated with file-name-1 is an external file
      *> connector, there is only one record area associated with the
      *> file connector for the run unit" - which places NO restriction
      *> on the record's categories or usages: the called program
      *> WRITEs through the SAME connector and the SAME record area,
      *> and both records come back in order.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB327FD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb327sf.dat".
           SELECT EF ASSIGN TO "pb327ef.dat".
       DATA DIVISION.
       FILE SECTION.
       FD SF.
       01 S-REC.
          05 S-N PIC N(3).
          05 S-X PIC X(2).
       01 S-BYTES PIC X(8).
       FD EF IS EXTERNAL.
       01 E-REC.
          05 E-N PIC N(2).
       WORKING-STORAGE SECTION.
       01 W-I PIC 99.
       01 W-O PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT SF.
           MOVE N"AB" TO S-N.
           MOVE "YZ" TO S-X.
           WRITE S-REC.
           CLOSE SF.
           OPEN INPUT SF.
           READ SF AT END DISPLAY "EOF".
           DISPLAY "N=[" S-N "] X=[" S-X "]".
           PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 8
               MOVE FUNCTION ORD(S-BYTES(W-I:1)) TO W-O
               DISPLAY "B" W-I "=" W-O
           END-PERFORM.
           CLOSE SF.
           OPEN OUTPUT EF.
           MOVE N"PQ" TO E-N.
           WRITE E-REC.
           CALL "PB327FDB".
           CLOSE EF.
           OPEN INPUT EF.
           READ EF AT END DISPLAY "EOF1".
           DISPLAY "E1=[" E-N "]".
           READ EF AT END DISPLAY "EOF2".
           DISPLAY "E2=[" E-N "]".
           CLOSE EF.
           STOP RUN.
       END PROGRAM PB327FD.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB327FDB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EF ASSIGN TO "pb327ef.dat".
       DATA DIVISION.
       FILE SECTION.
       FD EF IS EXTERNAL.
       01 O-REC.
          05 O-N PIC N(2).
       PROCEDURE DIVISION.
       MAIN.
           MOVE N"RS" TO O-N.
           WRITE O-REC.
       END PROGRAM PB327FDB.
