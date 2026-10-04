      *> kb/Work PB1167 -- ISO 13.18.2.4 GR1 a): the subject of the ANY LENGTH clause is "a zero-length item when
      *> the corresponding argument or returning item of the activating runtime element is a zero-length item"
      *> (cite.py --check 13.18.2.4 "a zero-length item when the corresponding argument or returning item of
      *> the activating runtime element is a zero-length item" -> OK 13.18.2.4 1) a)); 8.5.4 item 3 (cite.py
      *> --check 8.5.4 "A data item defined with the ANY LENGTH clause corresponding to an argument or
      *> returning item that is a zero-length item" -> OK 8.5.4 3)) says the same of the item.
      *> NFIX's ANY LENGTH formal AL is bound to a zero-length literal, so AL is zero-length (AL0=0).  NFIX then
      *> CALLs NRET RETURNING AL, so NRET's ANY LENGTH RETURNING item R corresponds to a zero-length returning
      *> item: it is zero-length too (NRETLEN=0), a MOVE into a zero-length alphanumeric receiver keeps nothing
      *> (14.6.8.5 "truncation to the right"), so it is still zero-length (NRETLEN2=0), and the delivery leaves AL
      *> zero-length (AL1=0).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1167Z.
       PROCEDURE DIVISION.
       MAIN.
           CALL "NFIX" AS NESTED USING BY CONTENT "".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NFIX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 AL PIC X ANY LENGTH.
       PROCEDURE DIVISION USING AL.
           DISPLAY "AL0=" FUNCTION LENGTH(AL).
           CALL "NRET" AS NESTED RETURNING AL.
           DISPLAY "AL1=" FUNCTION LENGTH(AL).
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NRET.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC X ANY LENGTH.
       PROCEDURE DIVISION RETURNING R.
           DISPLAY "NRETLEN=" FUNCTION LENGTH(R).
           MOVE "ZYXWVUTS" TO R.
           DISPLAY "NRETLEN2=" FUNCTION LENGTH(R).
           GOBACK.
       END PROGRAM NRET.
       END PROGRAM NFIX.
       END PROGRAM PB1167Z.
