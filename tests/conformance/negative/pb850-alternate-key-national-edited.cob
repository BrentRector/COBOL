*> reject-at: 2002 2014 2023
*> ISO 1989:2023 12.4.5.6.3 SR2: "Data-name-1 and data-name-2 shall be defined as a data item of
*> category alphanumeric or national". PIC NN0NN is category NATIONAL-EDITED (8.5.2.11), a category
*> of its own in 8.5.2.1 Table 2, so it is not "of category national" and SR2 refuses it as an
*> ALTERNATE RECORD KEY. The model carries it as PicCategory.National with an edit mask; the screen
*> read the category alone and accepted it (kb/Work PB850).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB850AKNE.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "pb850akne.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS IX-KEY
        ALTERNATE RECORD KEY IS IX-ALT WITH DUPLICATES.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KEY PIC X(5).
   05 IX-ALT PIC NN0NN.
   05 IX-DATA PIC X(10).
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT IXF
    CLOSE IXF
    STOP RUN.
