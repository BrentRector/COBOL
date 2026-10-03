      *> kb/Work PB1094 - a record that IS a dynamic-length item names
      *> its structure the same way a member does (ISO 12.3.7.4 GR18 and
      *> GR19; docs/CONFORMANCE.md section 3 D-DL3), and with no table
      *> beside it the structure ITSELF says where the data ends on a
      *> READ.  GR18: the data is prefixed by a binary length field,
      *> two characters for SHORT PREFIXED, most significant byte first
      *> (DOC-A.1-205), counting character positions.  GR19: a delimiter
      *> directly follows the data - "data of the length of an
      *> alphanumeric character in which all bit positions contain
      *> binary zeroes" and, for a national item, "the length of a
      *> national character" (two bytes, D-N1: a national record is its
      *> UTF-16BE byte pairs).  A second description of the same file
      *> (a dynamic-length record with no structure) reads each record
      *> as the characters the file holds, and each character's code
      *> (FUNCTION ORD less one) is displayed.  Expected codes, derived
      *> from GR18 and GR19:
      *>   SHORT PREFIXED "HELLO"   0 5 72 69 76 76 79   (7 characters)
      *>   SHORT PREFIXED empty     0 0
      *>   DELIMITED "AB"           65 66 0
      *>   DELIMITED empty          0
      *>   national DELIMITED "AB"  0 65 0 66 0 0        (delimiter = 2)
      *>   national SHORT PREFIXED "AB"  0 2 0 65 0 66   (2 positions)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1094-ELEMENTARY.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DYNAMIC LENGTH STRUCTURE DS-SHORT IS SHORT PREFIXED
           DYNAMIC LENGTH STRUCTURE DS-DELIM IS DELIMITED.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-SHORT ASSIGN TO "pb1094ele1.seq"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F-DELIM ASSIGN TO "pb1094ele2.seq"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F-NDEL ASSIGN TO "pb1094ele3.seq"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F-NSHORT ASSIGN TO "pb1094ele4.seq"
               ORGANIZATION IS SEQUENTIAL.
           SELECT RAW-FILE ASSIGN USING RAW-NAME
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F-SHORT.
       01 R-SHORT PIC X DYNAMIC LENGTH DS-SHORT.
       FD F-DELIM.
       01 R-DELIM PIC X DYNAMIC LENGTH DS-DELIM.
       FD F-NDEL.
       01 R-NDEL PIC N DYNAMIC LENGTH DS-DELIM.
       FD F-NSHORT.
       01 R-NSHORT PIC N DYNAMIC LENGTH DS-SHORT.
       FD RAW-FILE.
       01 RAW PIC X DYNAMIC LENGTH.
       WORKING-STORAGE SECTION.
       01 RAW-NAME PIC X(20).
       01 I   PIC 9(4).
       01 N   PIC 999.
       01 CODE-VALUE PIC 9(4).
       01 LINE-LEN PIC 9(4).
       01 MORE PIC X VALUE "Y".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F-SHORT F-DELIM F-NDEL F-NSHORT.
           MOVE "HELLO" TO R-SHORT.
           WRITE R-SHORT.
           MOVE "" TO R-SHORT.
           WRITE R-SHORT.
           MOVE "AB" TO R-DELIM.
           WRITE R-DELIM.
           MOVE "" TO R-DELIM.
           WRITE R-DELIM.
           MOVE N"AB" TO R-NDEL.
           WRITE R-NDEL.
           MOVE N"AB" TO R-NSHORT.
           WRITE R-NSHORT.
           CLOSE F-SHORT F-DELIM F-NDEL F-NSHORT.
           MOVE "pb1094ele1.seq" TO RAW-NAME.
           PERFORM DUMP-RAW.
           MOVE "pb1094ele2.seq" TO RAW-NAME.
           PERFORM DUMP-RAW.
           MOVE "pb1094ele3.seq" TO RAW-NAME.
           PERFORM DUMP-RAW.
           MOVE "pb1094ele4.seq" TO RAW-NAME.
           PERFORM DUMP-RAW.
      *> Read back through the typed descriptions.
           MOVE "QQQQQQ" TO R-SHORT R-DELIM.
           MOVE N"QQQQ" TO R-NDEL R-NSHORT.
           OPEN INPUT F-SHORT F-DELIM F-NDEL F-NSHORT.
           READ F-SHORT.
           DISPLAY "SHORT [" R-SHORT "] " FUNCTION LENGTH(R-SHORT).
           READ F-SHORT.
           DISPLAY "SHORT [" R-SHORT "] " FUNCTION LENGTH(R-SHORT).
           READ F-DELIM.
           DISPLAY "DELIM [" R-DELIM "] " FUNCTION LENGTH(R-DELIM).
           READ F-DELIM.
           DISPLAY "DELIM [" R-DELIM "] " FUNCTION LENGTH(R-DELIM).
           READ F-NDEL.
           DISPLAY "NDEL  [" R-NDEL "] " FUNCTION LENGTH(R-NDEL).
           READ F-NSHORT.
           DISPLAY "NSHORT [" R-NSHORT "] " FUNCTION LENGTH(R-NSHORT).
           CLOSE F-SHORT F-DELIM F-NDEL F-NSHORT.
           STOP RUN.
       DUMP-RAW.
           OPEN INPUT RAW-FILE.
           MOVE "Y" TO MORE.
           PERFORM UNTIL MORE = "N"
               READ RAW-FILE
                   AT END MOVE "N" TO MORE
                   NOT AT END PERFORM SHOW-CODES
               END-READ
           END-PERFORM.
           CLOSE RAW-FILE.
       SHOW-CODES.
           MOVE FUNCTION LENGTH(RAW) TO LINE-LEN.
           DISPLAY "LEN " LINE-LEN " CODES" WITH NO ADVANCING.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > LINE-LEN
               COMPUTE CODE-VALUE = FUNCTION ORD(RAW(I:1)) - 1
               MOVE CODE-VALUE TO N
               DISPLAY " " N WITH NO ADVANCING
           END-PERFORM.
           DISPLAY " ".
