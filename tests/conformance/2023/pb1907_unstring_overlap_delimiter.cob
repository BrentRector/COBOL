      *> PB1907 -- UNSTRING overlap: identifier-2 (DELIMITED BY) shares
      *> storage with identifier-4 (INTO).
      *> ISO 14.9.48.4 GR18 / Annex A.2 item 61: the result is
      *> undefined; 4.4 2): a run unit that allows it is conforming.
      *> Documented implementor choice (docs/CONFORMANCE.md D-UNS2,
      *> GnuCOBOL 3.2 libcob strings.c: cob_unstring_delimited keeps
      *> the delimiter's storage; cob_unstring_into compares against
      *> it live and sets DELIMITER IN from it AFTER the INTO store):
      *> a delimiter identifier is read from storage for each
      *> receiving area, and DELIMITER IN receives the delimiter
      *> item's content as it stands after that receiver's INTO.
      *> B1 first area: "," found after ";", so ";" -> WS-DLM; WS-D1
      *>   then receives WS-DLM's content, ";".
      *> B1 second area: the delimiter is now ";": "a" -> WS-P2,
      *>   D2 ";".
      *> B1 third area: "b,c" holds no ";": WS-P3 = "b,c". T = 3.
      *> (A snapshot would give D1=[,] P2=[a;b] D2=[,] P3=[c  ].)
      *> B2: DELIMITED BY ALL. The run of repeated delimiters is
      *>   consumed as ONE delimiting occurrence BEFORE the INTO
      *>   store, so the second ";" of "a;;b" is skipped although
      *>   WS-D3 holds "a" afterwards; the next scan then runs with
      *>   the delimiter "a". WiseOwl COBOL differs from GnuCOBOL
      *>   here (libcob consumes the run after the INTO store and
      *>   gives Y2=[;b;;;;c; ]); documented in D-UNS2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1907U2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-S   PIC X(7) VALUE ";,a;b,c".
       01 WS-DLM PIC X    VALUE ",".
       01 WS-D1  PIC X    VALUE "z".
       01 WS-D2  PIC X    VALUE "z".
       01 WS-P2  PIC X(3) VALUE "zzz".
       01 WS-P3  PIC X(3) VALUE "zzz".
       01 WS-T   PIC 99   VALUE 0.
       01 WS-S3  PIC X(10) VALUE "a;;b;;;;c;".
       01 WS-D3  PIC X    VALUE ";".
       01 WS-Y2  PIC X(9) VALUE "zzzzzzzzz".
       01 WS-Y3  PIC X(3) VALUE "zzz".
       01 WS-T3  PIC 99   VALUE 0.
       PROCEDURE DIVISION.
           UNSTRING WS-S DELIMITED BY WS-DLM
               INTO WS-DLM DELIMITER IN WS-D1
                    WS-P2  DELIMITER IN WS-D2
                    WS-P3
               TALLYING IN WS-T
           END-UNSTRING
           DISPLAY "B1 DLM=[" WS-DLM "] D1=[" WS-D1 "] P2=[" WS-P2
               "] D2=[" WS-D2 "] P3=[" WS-P3 "] T=" WS-T
           UNSTRING WS-S3 DELIMITED BY ALL WS-D3
               INTO WS-D3 WS-Y2 WS-Y3 TALLYING IN WS-T3
           END-UNSTRING
           DISPLAY "B2 D3=[" WS-D3 "] Y2=[" WS-Y2 "] Y3=[" WS-Y3
               "] T=" WS-T3
           STOP RUN.
