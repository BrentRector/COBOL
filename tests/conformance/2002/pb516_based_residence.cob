      *> kb/Work PB516 - ISO 13.16.3 SR16 (cite.py OK): "The BASED clause may be
      *> specified only in data description entries in the linkage section, in
      *> the working-storage section, and in the local-storage section. The level
      *> number of such data description entries shall be 1 or 77." Every admitted
      *> residence binds as a based item: a 77 and an 01 in working-storage, an
      *> entry composed BASED by a TYPE clause (13.18.57.4 GR1), and a
      *> local-storage 01. Each is pointed at BUF by SET ADDRESS OF, so
      *> each shows BUF's leading characters at its own length.
      *> Expected: WXYZ/WX/WXY.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB516RES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BUF PIC X(4) VALUE "WXYZ".
       77 A BASED PIC X(4).
       01 T TYPEDEF BASED PIC X(2).
       01 B TYPE T.
       LOCAL-STORAGE SECTION.
       01 C BASED PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET ADDRESS OF A TO ADDRESS OF BUF
           SET ADDRESS OF B TO ADDRESS OF BUF
           SET ADDRESS OF C TO ADDRESS OF BUF
           DISPLAY A "/" B "/" C
           STOP RUN.
