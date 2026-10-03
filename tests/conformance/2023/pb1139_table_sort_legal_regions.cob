      *> kb/Work PB1139 - THE FORMAT-2 TABLE SORT IS NOT SUBJECT TO SORT SR3.
      *>   cite.py --check 14.9.40.3 "A SORT statement shall not appear in imperative-statement-1 of an
      *>     exception-checking PERFORM statement, in an input or output procedure, or in a declarative
      *>     procedure." -> OK §14.9.40.3 3)
      *> §14.9.40.3 prints SR3 under its FORMAT 1 sub-heading (SR3-SR12 are Format 1, SR13-SR15 Format 2), so the
      *> three bans bind the FILE-format SORT only, and nothing else restricts where a table SORT may be written.
      *> The statement used to draw COBOLNET1617 inside an exception-checking PERFORM, whatever its format.
      *> WHY EACH LEG CAN FAIL (expected values derived from the rules):
      *>   A  a table SORT in imperative-statement-1 of an exception-checking PERFORM sorts 312 to 123
      *>   B  a table SORT in the INPUT PROCEDURE of a file SORT sorts 321 DESCENDING to 321 and the file sort
      *>      then releases b2 and a1
      *>   C  a table SORT in the OUTPUT PROCEDURE sorts 132 ASCENDING to 123, and the file sort returns a1 then b2
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1139TL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb1139tl.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR PIC X(2).
       WORKING-STORAGE SECTION.
       01 TBL.
          05 TE PIC 9 OCCURS 3.
       01 EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           MOVE "312" TO TBL
           PERFORM
               SORT TE ASCENDING
           WHEN EC-ALL
               DISPLAY "EXC"
           END-PERFORM
           DISPLAY "A " TBL
           SORT SW ASCENDING KEY SR
               INPUT PROCEDURE IS LOADIT
               OUTPUT PROCEDURE IS SHOWIT
           STOP RUN.
       LOADIT SECTION.
       L1.
           MOVE "321" TO TBL
           SORT TE DESCENDING
           DISPLAY "B " TBL
           MOVE "b2" TO SR
           RELEASE SR
           MOVE "a1" TO SR
           RELEASE SR.
       SHOWIT SECTION.
       S1.
           MOVE "132" TO TBL
           SORT TE ASCENDING
           DISPLAY "C " TBL
           RETURN SW AT END MOVE "Y" TO EOF-FLAG END-RETURN
           PERFORM UNTIL EOF-FLAG = "Y"
               DISPLAY SR
               RETURN SW AT END MOVE "Y" TO EOF-FLAG END-RETURN
           END-PERFORM.
