      *> A TRAILING-P DEPENDING ITEM IS AN UNSIGNED INTEGER (train 1021 review, PB858).
      *> ISO/IEC 1989:2023 §13.18.43.3 SR6: "Data-name-1 shall describe an elementary
      *> unsigned integer"; §5.5 2) b) 2. makes "integer" "a fixed-point numeric data
      *> item ... whose description does not include any digit positions to the right of
      *> the radix point", and §13.18.40.4 GR14 puts the assumed decimal point of PIC 9P
      *> "to the right of the string of 'P's". So LN PIC 9P is legal (no COBOLNET2922).
      *> MOVE 20 TO LN gives LN the value 20; §13.18.43.4 GR13 a) writes a 20-byte record,
      *> and after the READ GR15 makes LN "the number of bytes in the record just read",
      *> 20 again. Displayed through LN-SHOW PIC 99: [ABABABABABABABABABAB] 20.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB858RP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb858rp.dat"
               ORGANIZATION SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD SF RECORD IS VARYING IN SIZE FROM 10 TO 30 CHARACTERS
             DEPENDING ON LN.
       01 SF-REC PIC X(30).
       WORKING-STORAGE SECTION.
       01 LN PIC 9P.
       01 LN-SHOW PIC 99.
       PROCEDURE DIVISION.
           OPEN OUTPUT SF
           MOVE ALL "AB" TO SF-REC
           MOVE 20 TO LN
           WRITE SF-REC
           CLOSE SF
           MOVE 0 TO LN
           OPEN INPUT SF
           MOVE SPACES TO SF-REC
           READ SF
           MOVE LN TO LN-SHOW
           DISPLAY "[" SF-REC(1:20) "] " LN-SHOW
           CLOSE SF
           STOP RUN.
