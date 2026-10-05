      *> reject-at: 85
      *> The USING phrase of the ASSIGN clause (dynamic file assignment, ISO §9.1.21 / §12.4.5.3 GR3 b)) is a
      *> COBOL-2002 introduction, on BOTH arms of the clause (kb/Work PB746):
      *>   cite.py --check 9.1.21 "Dynamic file assignment allows the user to defer until runtime the association
      *>     between a file connector and a physical file" -> OK §9.1.21
      *> EDITION, DERIVED (constructs row assign-using-2002, VCR row 7.27): Annex E never names the phrase, so it
      *> predates 2023; GnuCOBOL's per-standard dialect files date it at 2002 (cobol85.conf
      *> assign-using-variable: unconformable, cobol2002.conf: ok) and cobc 3.2 -std=cobol85 refuses it.
      *> Below 2002 each arm draws COBOLNET0900 naming the phrase: the TO ... USING tail (DYNA) and the bare
      *> ASSIGN USING (DYNB). ASSIGN TO literal-1 alone (CHK) is COBOL-85 and draws nothing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB746N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT DYNA ASSIGN TO "pb746a.dat" USING WS-NAME
               ORGANIZATION IS SEQUENTIAL.
           SELECT DYNB ASSIGN USING WS-NAME
               ORGANIZATION IS SEQUENTIAL.
           SELECT CHK ASSIGN TO "pb746c.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  DYNA.
       01  DYNA-REC PIC X(5).
       FD  DYNB.
       01  DYNB-REC PIC X(5).
       FD  CHK.
       01  CHK-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-NAME PIC X(20) VALUE "pb746b.dat".
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
