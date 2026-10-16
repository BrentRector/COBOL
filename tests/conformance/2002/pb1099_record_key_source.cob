      *> kb/Work PB1099 Q1 - the SOURCE phrase of the RECORD KEY and ALTERNATE RECORD KEY clauses at its
      *> provisional introducing edition COBOL-2002 (VCR row 7.36; constructs.json record-key-source-2002;
      *> below 2002 it is COBOLNET0900, negative pb1099-record-key-source-85). Annex A.3 item 40 makes the
      *> capability processor-dependent; this implementation provides it.
      *>   cite.py --check 12.4.5.12.4 "Record-key-name-1 defines a record key consisting of the
      *>     concatenation of all occurrences of data-name-2 in the order specified" -> OK 12.4.5.12.4 2)
      *>   cite.py --check 12.4.5.6.4 (the same sentence) -> OK 12.4.5.6.4 2)
      *>   cite.py --check 14.9.41.4 "or else the length of record-key-name-1, if specified" -> OK 17) b)
      *> The prime key IX-PK is IX-B then IX-A - the REVERSE of their order in the record - and the
      *> alternate key IX-AK is IX-D then IX-C, WITH DUPLICATES. The alternate clause omits the optional
      *> words KEY and IS (12.4.5.6.2: only ALTERNATE, RECORD and SOURCE are underlined).
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES:
      *>   W1 00 / W2 00   REC1 (key ZZ01, alt 2X) and REC2 (AA02, 1Y) are released.
      *>   W3 02           REC3 (MM01, alt 2X) duplicates REC1's alternate key, which WITH DUPLICATES
      *>                   permits (12.4.5.6.4 GR4); 9.1.13.2 item 2 c) reports it '02'.
      *>   W4 22           REC4 has IX-A = 01 like REC3 and IX-B = ZZ like REC1: its CONCATENATED prime
      *>                   key ZZ01 equals REC1's (12.4.5.12.4 GR1/GR2), so the WRITE is refused '22'.
      *>   N1 00 REC2 / N2 00 REC3 / N3 00 REC1 / N4 10   sequential READ by the prime key orders the
      *>                   records by the concatenation IX-B IX-A: AA02 < MM01 < ZZ01, then at end.
      *>   R1 00 REC1      READ KEY IS IX-AK with IX-D IX-C = 2X: of the two records whose alternate key
      *>                   is 2X, the first released (14.9.30.4 GR32).
      *>   R2 00 REC2      READ KEY IS IX-PK IN IXF (8.4.2.2.2 Format 6) with IX-B IX-A = AA02.
      *>   R3 23           READ KEY IS IX-PK with QQ01: no such key, invalid key.
      *>   S1 00           START KEY IS > IX-AK with IX-D IX-C = 1Y: the alternate key becomes the key of
      *>                   reference (14.9.41.4 GR16) and the first key > 1Y is 2X.
      *>   S2 02 REC1 / S3 00 REC3 / S4 10   READ NEXT on the alternate key: the duplicates in release
      *>                   order (14.9.30.4 GR26); the first is followed by a duplicate, so '02' (GR27).
      *>   L1 00 REC3 / L2 00 REC1   START KEY IS = IX-PK WITH LENGTH 2 with IX-B = MM: the first 2
      *>                   characters of the concatenation are IX-B (14.9.41.4 GR17 a)/b)), so REC3; READ
      *>                   NEXT continues on the prime key.
      *>   D1 00 / F1 00 REC3   DELETE with IX-B IX-A = AA02 removes REC2 (14.9.10.4 GR3); START with no
      *>                   KEY phrase (GR15: EQUAL on the prime record key) on MM01 finds REC3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1099RK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb1099rk.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-PK SOURCE IS IX-B IX-A
               ALTERNATE RECORD IX-AK SOURCE IX-D IX-C WITH DUPLICATES
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-A    PIC X(2).
          05 IX-DATA PIC X(4).
          05 IX-B    PIC X(2).
          05 IX-C    PIC X(1).
          05 IX-D    PIC X(1).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IXF
           MOVE "01REC1ZZX2" TO IX-REC
           WRITE IX-REC INVALID KEY CONTINUE END-WRITE
           DISPLAY "W1 " FS
           MOVE "02REC2AAY1" TO IX-REC
           WRITE IX-REC INVALID KEY CONTINUE END-WRITE
           DISPLAY "W2 " FS
           MOVE "01REC3MMX2" TO IX-REC
           WRITE IX-REC INVALID KEY CONTINUE END-WRITE
           DISPLAY "W3 " FS
           MOVE "01REC4ZZQ9" TO IX-REC
           WRITE IX-REC INVALID KEY CONTINUE END-WRITE
           DISPLAY "W4 " FS
           CLOSE IXF

           OPEN I-O IXF
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "N1 " FS " " IX-DATA
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "N2 " FS " " IX-DATA
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "N3 " FS " " IX-DATA
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "N4 " FS

           MOVE "2" TO IX-D
           MOVE "X" TO IX-C
           READ IXF KEY IS IX-AK INVALID KEY CONTINUE END-READ
           DISPLAY "R1 " FS " " IX-DATA
           MOVE "AA" TO IX-B
           MOVE "02" TO IX-A
           READ IXF KEY IS IX-PK IN IXF INVALID KEY CONTINUE END-READ
           DISPLAY "R2 " FS " " IX-DATA
           MOVE "QQ" TO IX-B
           MOVE "01" TO IX-A
           READ IXF KEY IS IX-PK INVALID KEY CONTINUE END-READ
           DISPLAY "R3 " FS

           MOVE "1" TO IX-D
           MOVE "Y" TO IX-C
           START IXF KEY IS > IX-AK INVALID KEY CONTINUE END-START
           DISPLAY "S1 " FS
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "S2 " FS " " IX-DATA
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "S3 " FS " " IX-DATA
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "S4 " FS

           MOVE "MM" TO IX-B
           MOVE "??" TO IX-A
           START IXF KEY IS = IX-PK WITH LENGTH 2
               INVALID KEY CONTINUE END-START
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "L1 " FS " " IX-DATA
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "L2 " FS " " IX-DATA

           MOVE "AA" TO IX-B
           MOVE "02" TO IX-A
           DELETE IXF INVALID KEY CONTINUE END-DELETE
           DISPLAY "D1 " FS
           MOVE "MM" TO IX-B
           MOVE "01" TO IX-A
           START IXF INVALID KEY CONTINUE END-START
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "F1 " FS " " IX-DATA
           CLOSE IXF
           STOP RUN.
