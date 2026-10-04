      *> reject-at: 85
      *> kb/Work PB1133 - the RECURSIVE clause is a COBOL-2002 introduction (11.10.2 / 11.10.4), so the legal 2002+
      *>   shape of a RECURSIVE program with working-storage and a contained program is refused at 85 by the
      *>   edition gate (COBOLNET0885), not by a storage staging. The positive twin is
      *>   conformance/2002/pb1133_recursive_container_static_ws.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1133N1 RECURSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC 9(2) GLOBAL VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB1133N1IN".
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1133N1IN.
       PROCEDURE DIVISION.
       P2.
           ADD 1 TO WS-X.
           GOBACK.
       END PROGRAM PB1133N1IN.
       END PROGRAM PB1133N1.
