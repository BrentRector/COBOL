      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1127 - ISO 14.9.22.3 SR3 (cite.py --check 14.9.22.3 "shall not be a zero-length literal" -> OK 3):
      *> a zero-length BEFORE delimiter (literal-2) is refused like every other literal position - it used to
      *> run and change the answer (BEFORE "" tallied the whole item). Expected: COBOLNET1757 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB1127ZD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(5) VALUE "AAXAA".
       01 N PIC 99 VALUE 0.
       PROCEDURE DIVISION.
           INSPECT X TALLYING N FOR CHARACTERS BEFORE ""
           DISPLAY N
           STOP RUN.
