      *> kb/Work PB1094 - the DYNAMIC LENGTH STRUCTURE of a
      *> dynamic-length item is the item's physical form in the record a
      *> file carries (ISO 12.3.7.4 GR18 and GR19; docs/CONFORMANCE.md
      *> section 3 D-DL3).  GR18: "data described with
      *> dynamic-length-structure-name-1 is prefixed by a length field",
      *> a signed or unsigned binary field, four characters for PREFIXED
      *> and two for SHORT PREFIXED (GR18's table: 2147483647 and
      *> 4294967295 need 32 bits, 32767 and 65535 need 16); this
      *> implementation's binary byte order is most significant byte
      *> first (DOC-A.1-205).  GR19: "a delimiter shall directly follow
      *> the data", one alphanumeric character position of zero bits.
      *> A structure with both phrases has the length field first and
      *> the delimiter last.  An item whose DYNAMIC LENGTH clause names
      *> no structure (13.18.19.3 SR3) is the implementor's: bare data.
      *> The program never sees those characters (8.5.1.11.2: a
      *> variable-length item behaves as though contiguous with its
      *> neighbors in a procedural operation), so the record is read
      *> back through a SECOND description of the same file - a
      *> dynamic-length record with no structure, whose content is the
      *> record's characters as the file holds them - and each
      *> character's code (FUNCTION ORD less one) is displayed.  The
      *> expected codes below are derived from GR18 and GR19, never
      *> read from a run: record 1 is
      *>   K  "KK"                       75 75
      *>   A  DELIMITED "AB"             65 66 0
      *>   B  SHORT PREFIXED "CDE"       0 3 67 68 69
      *>   C  PREFIXED DELIMITED "F"     0 0 0 1 70 0
      *>   D  SIGNED PREFIXED "GH"       0 0 0 2 71 72
      *>   E  no structure "IJK"         73 74 75
      *>   G  SIGNED SHORT PREFIXED "L"  0 1 76
      *>   Z  "Z"                        90
      *> and record 2 holds every dynamic item empty, so each structure
      *> shows alone: 0 (A's delimiter), 0 0 (B), 0 0 0 0 0 (C),
      *> 0 0 0 0 (D), nothing (E), 0 0 (G).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1094-IMAGE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DYNAMIC LENGTH STRUCTURE DS-DELIM IS DELIMITED
           DYNAMIC LENGTH STRUCTURE DS-SHORT IS SHORT PREFIXED
           DYNAMIC LENGTH STRUCTURE DS-BOTH IS DELIMITED PREFIXED
           DYNAMIC LENGTH STRUCTURE DS-SIGNED IS SIGNED PREFIXED
           DYNAMIC LENGTH STRUCTURE DS-SSHORT IS SIGNED SHORT PREFIXED.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT TYPED-FILE ASSIGN TO "pb1094img.seq"
               ORGANIZATION IS SEQUENTIAL.
           SELECT RAW-FILE ASSIGN TO "pb1094img.seq"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD TYPED-FILE.
       01 TR.
          05 TK PIC X(2).
          05 TA PIC X DYNAMIC LENGTH DS-DELIM.
          05 TB PIC X DYNAMIC LENGTH DS-SHORT.
          05 TC PIC X DYNAMIC LENGTH DS-BOTH.
          05 TD PIC X DYNAMIC LENGTH DS-SIGNED.
          05 TE PIC X DYNAMIC LENGTH.
          05 TG PIC X DYNAMIC LENGTH DS-SSHORT.
          05 TZ PIC X(1).
       FD RAW-FILE.
       01 RAW PIC X DYNAMIC LENGTH.
       WORKING-STORAGE SECTION.
       01 I   PIC 9(4).
       01 N   PIC 999.
       01 CODE-VALUE PIC 9(4).
       01 LINE-LEN PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT TYPED-FILE.
           MOVE "KK" TO TK.
           MOVE "AB" TO TA.
           MOVE "CDE" TO TB.
           MOVE "F" TO TC.
           MOVE "GH" TO TD.
           MOVE "IJK" TO TE.
           MOVE "L" TO TG.
           MOVE "Z" TO TZ.
           WRITE TR.
           MOVE "YY" TO TK.
           MOVE "" TO TA TB TC TD TE TG.
           MOVE "W" TO TZ.
           WRITE TR.
           CLOSE TYPED-FILE.
      *> What the file holds, through the second description.
           OPEN INPUT RAW-FILE.
           PERFORM 2 TIMES
               READ RAW-FILE
                   AT END DISPLAY "END"
                   NOT AT END PERFORM SHOW-CODES
               END-READ
           END-PERFORM.
           CLOSE RAW-FILE.
      *> What the program gets back through the typed description.
           MOVE "ZZ" TO TK.
           MOVE "QQQQQQ" TO TA TB TC TD TE TG.
           MOVE "?" TO TZ.
           OPEN INPUT TYPED-FILE.
           PERFORM 2 TIMES
               READ TYPED-FILE
                   AT END DISPLAY "END"
                   NOT AT END PERFORM SHOW-TYPED
               END-READ
           END-PERFORM.
           CLOSE TYPED-FILE.
           STOP RUN.
       SHOW-CODES.
           MOVE FUNCTION LENGTH(RAW) TO LINE-LEN.
           DISPLAY "LEN " LINE-LEN " CODES" WITH NO ADVANCING.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > LINE-LEN
               COMPUTE CODE-VALUE = FUNCTION ORD(RAW(I:1)) - 1
               MOVE CODE-VALUE TO N
               DISPLAY " " N WITH NO ADVANCING
           END-PERFORM.
           DISPLAY " ".
       SHOW-TYPED.
           DISPLAY TK "[" TA "][" TB "][" TC "][" TD "][" TE "]["
               TG "]" TZ " " FUNCTION LENGTH(TA) "/"
               FUNCTION LENGTH(TB) "/" FUNCTION LENGTH(TC) "/"
               FUNCTION LENGTH(TD) "/" FUNCTION LENGTH(TE) "/"
               FUNCTION LENGTH(TG).
