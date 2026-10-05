*> reject-at: 85 2002 2014 2023
*> ISO 1989:2023 12.4.5.2 SR11 sentence 1 - "Format 3 shall be specified only for a sequential file or
*> a report file." The RECORD DELIMITER clause is printed in 12.4.5.1 Format 3 (sequential) ALONE
*> (Format 1, the indexed format, prints no such clause), so an entry that writes it specifies Format 3
*> - and here the same entry says ORGANIZATION IS INDEXED. The mirror of SR8/SR9's first sentences,
*> except that no KEY clause is involved (kb/Work PB773), so the rule has its own code, COBOLNET2912,
*> rather than the key-clause band COBOLNET0863.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB773IXRD.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "pb773ixrd.dat"
        ORGANIZATION IS INDEXED
        RECORD KEY IS IX-KEY
        RECORD DELIMITER IS STANDARD-1.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KEY PIC X(5).
   05 IX-DATA PIC X(5).
PROCEDURE DIVISION.
MAIN.
    DISPLAY "UNREACHED"
    STOP RUN.
