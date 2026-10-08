*> reject-at: 85 2002 2014 2023
*> ISO 1989:2023 12.4.5.12.3 SR2: "Data-name-1 and data-name-2 shall reference a data item of
*> category alphanumeric or category national". PIC XXBXX is category ALPHANUMERIC-EDITED (8.5.2.4),
*> which 8.5.2.1 Table 2 lists as a category of its own, and 8.5.2.1 says "Use of the name of a data
*> class or data category in the rules of COBOL refers to the category unless class is specifically
*> indicated". The model carries the edited category as PicCategory.Alphanumeric with an edit mask, so
*> a screen that read the category alone accepted this key without a word (kb/Work PB850).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB850RKAE.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "pb850rkae.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS IX-KEY.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KEY PIC XXBXX.
   05 IX-DATA PIC X(10).
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT IXF
    CLOSE IXF
    STOP RUN.
