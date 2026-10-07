      *> kb/Work PB2096 - ISO 1989:2023 14.2.3 GR8: "If the argument is
      *> passed by reference, the activated runtime element operates as
      *> if the formal parameter occupies the same storage area as the
      *> argument." A contained program that names its container's
      *> GLOBAL formal as a BY REFERENCE argument passes on the storage
      *> that formal occupies (13.18.27.4 GR2: the contained program
      *> references the name "without describing it again", so it is
      *> the same formal), so an area formal of the next activated
      *> element is laid over the argument's own storage: a store
      *> through a second formal passed the same argument is visible
      *> through the first, and survives the return. And 8.8.4.8.4
      *> GR1c: the omitted-argument condition of such a forward is the
      *> GLOBAL formal's own.
      *> cite.py --check 14.2.3 "If the argument is passed by reference,
      *>   the activated runtime element operates as if the formal
      *>   parameter occupies the same storage area as the argument"
      *>   -> OK 14.2.3 8)
      *> cite.py --check 13.18.27.4 "may reference that name without
      *>   describing it again" -> OK 13.18.27.4 2)
      *> cite.py --check 8.8.4.8.4 "is itself a formal parameter for
      *>   which the omitted-argument condition is true" -> OK
      *>   8.8.4.8.4 1)
      *> Before the fix the forward was not recognized as a formal: it
      *> crossed through a fresh carrier and no area, so LD-1R printed
      *> 0001, N stayed 0001, LE-A printed ABCD, G ended ABXY, and the
      *> omitted GLOBAL formal arrived PRESENT.
      *> DERIVATION: N = 1; P2096D02 stores 7 through LD-2, the same
      *>   storage as LD-1 -> LD-1R = 0007, N = 0007. G = "ABCD":
      *>   P2096G02 stores "XY" through LE-B2 -> LE-A = "ABXY"; then
      *>   "QRST" through LE-A -> LE-B1 LE-B2 = "QR" "ST", G = "QRST".
      *>   The OMITTED argument forwarded twice is still omitted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2096M02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 1.
       01 G.
          05 G1 PIC X(2) VALUE "AB".
          05 G2 PIC X(2) VALUE "CD".
       PROCEDURE DIVISION.
           CALL "P2096B02" USING N
           DISPLAY "N=" N
           CALL "P2096E02" USING G
           DISPLAY "G=" G
           CALL "P2096O02" USING OMITTED
           STOP RUN.
       END PROGRAM P2096M02.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2096B02.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LF PIC 9(4) GLOBAL.
       PROCEDURE DIVISION USING LF.
           CALL "P2096C02"
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2096C02.
       PROCEDURE DIVISION.
           CALL "P2096D02" USING LF LF
           GOBACK.
       END PROGRAM P2096C02.
       END PROGRAM P2096B02.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2096D02.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LD-1 PIC X(4).
       01 LD-1R REDEFINES LD-1 PIC 9(4).
       01 LD-2 PIC 9(4).
       PROCEDURE DIVISION USING LD-1 LD-2.
           MOVE 7 TO LD-2
           DISPLAY "LD-1R=" LD-1R
           GOBACK.
       END PROGRAM P2096D02.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2096E02.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG GLOBAL.
          05 LG1 PIC X(2).
          05 LG2 PIC X(2).
       PROCEDURE DIVISION USING LG.
           CALL "P2096F02"
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2096F02.
       PROCEDURE DIVISION.
           CALL "P2096G02" USING LG LG
           GOBACK.
       END PROGRAM P2096F02.
       END PROGRAM P2096E02.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2096G02.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LE-A PIC X(4).
       01 LE-B.
          05 LE-B1 PIC X(2).
          05 LE-B2 PIC X(2).
       PROCEDURE DIVISION USING LE-A LE-B.
           MOVE "XY" TO LE-B2
           DISPLAY "LE-A=" LE-A
           MOVE "QRST" TO LE-A
           DISPLAY "LE-B=" LE-B1 "/" LE-B2
           GOBACK.
       END PROGRAM P2096G02.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2096O02.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LO PIC X(4) GLOBAL.
       PROCEDURE DIVISION USING OPTIONAL LO.
           CALL "P2096P02"
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2096P02.
       PROCEDURE DIVISION.
           CALL "P2096Q02" USING LO
           GOBACK.
       END PROGRAM P2096P02.
       END PROGRAM P2096O02.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2096Q02.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LQ PIC X(4).
       PROCEDURE DIVISION USING OPTIONAL LQ.
           IF LQ IS OMITTED
             DISPLAY "LQ-OMITTED"
           ELSE
             DISPLAY "LQ-PRESENT"
           END-IF
           GOBACK.
       END PROGRAM P2096Q02.
