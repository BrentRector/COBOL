      *> PB1689 (owner 2026-09-29: follow ISO) - ISO 8.9 reserves none of CHANNEL, GENERIC, PACKED,
      *>   END-INVOKE, END-MERGE, END-METHOD, END-SORT and GnuCOBOL 3.2 has no entry for them, so
      *>   each is a legal user-defined word (data-name and paragraph-name).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1689U.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CHANNEL PIC 9 VALUE 1.
       01  GENERIC PIC 9 VALUE 2.
       01  PACKED  PIC 9 VALUE 3.
       01  END-INVOKE PIC 9 VALUE 4.
       01  END-MERGE  PIC 9 VALUE 5.
       01  END-METHOD PIC 9 VALUE 6.
       01  END-SORT   PIC 9 VALUE 7.
       PROCEDURE DIVISION.
           DISPLAY CHANNEL GENERIC PACKED END-INVOKE END-MERGE
                   END-METHOD END-SORT
           ADD CHANNEL GENERIC PACKED END-SORT TO END-MERGE
           DISPLAY END-MERGE
           PERFORM END-METHOD
           STOP RUN.
       END-METHOD.
           DISPLAY "PARA".
