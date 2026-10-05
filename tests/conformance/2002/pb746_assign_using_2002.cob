      *> ISO 1989:2023 §14.9.27.4 GR26 -> §12.4.5.3 GR3 b) at the phrase's INTRODUCING edition, 2002. The ASSIGN
      *> clause's USING phrase (dynamic file assignment, §9.1.21) is honoured at --std 2002 exactly as at 2023:
      *> the connector is associated with the physical file named by data-name-1's content at the time of
      *> execution of each OPEN, so one connector writes two files and reads them back (kb/Work PB324).
      *>   cite.py --check 12.4.5.3 "the file connector referenced by file-name-1 is associated with a physical
      *>     file identified by the content of the data item referenced by data-name-1" -> OK §12.4.5.3 3) b)
      *> EDITION, DERIVED (kb/Work PB746; constructs row assign-using-2002, VCR row 7.27): Annex E never names
      *> the phrase, so it predates 2023, and GnuCOBOL's dialect files date it at 2002 (cobol85.conf
      *> assign-using-variable: unconformable; cobol2002.conf: ok). This program was the 85 golden
      *> pb324_assign_using_85, written FROM the undated belief that 85 had the phrase; below 2002 it is now
      *> negative/pb746-assign-using-below-2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB746A2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT DYN8 ASSIGN USING WS-NAME
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-ST.
           SELECT OPTIONAL CHK8 ASSIGN TO "dyn8.txt"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-CK.
       DATA DIVISION.
       FILE SECTION.
       FD  DYN8.
       01  DYN-REC PIC X(5).
       FD  CHK8.
       01  CHK-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-NAME PIC X(20) VALUE "pb324c.dat".
       01  WS-ST   PIC XX.
       01  WS-CK   PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT DYN8.
           DISPLAY "OPENC=" WS-ST.
           MOVE "CCCCC" TO DYN-REC.
           WRITE DYN-REC.
           CLOSE DYN8.
           MOVE "pb324d.dat" TO WS-NAME.
           OPEN OUTPUT DYN8.
           DISPLAY "OPEND=" WS-ST.
           MOVE "DDDDD" TO DYN-REC.
           WRITE DYN-REC.
           CLOSE DYN8.
           OPEN INPUT CHK8.
           DISPLAY "CHK=" WS-CK.
           CLOSE CHK8.
           MOVE "pb324c.dat" TO WS-NAME.
           OPEN INPUT DYN8.
           READ DYN8.
           DISPLAY "READC=" DYN-REC.
           CLOSE DYN8.
           MOVE "pb324d.dat" TO WS-NAME.
           OPEN INPUT DYN8.
           READ DYN8.
           DISPLAY "READD=" DYN-REC.
           CLOSE DYN8.
           STOP RUN.
