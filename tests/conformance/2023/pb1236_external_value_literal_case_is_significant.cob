      *> PB1236 - ISO 13.18.22.4 GR6 via 14.8.4.3: "the VALUE clause
      *>   specification, if any, for each record name" of the source
      *>   elements describing one external data record shall be identical;
      *>   otherwise the CALL is not successful and EC-EXTERNAL-FORMAT-
      *>   CONFLICT is set to exist. A literal's characters are
      *>   significant: "ABCD" and "abcd" are different VALUE
      *>   specifications (only keyword spellings - SPACE / space - are
      *>   case-insensitive).
      *> cite.py --check 13.18.22.4 "Within a run unit, if two or more
      *>   source elements describe the same external data record" -> OK
      *>   13.18.22.4 6)
      *> Derivation: the caller says VALUE "ABCD", PB1236A VALUE "abcd":
      *>   not identical, so the CALL fails and its ON EXCEPTION path sees
      *>   EC-EXTERNAL-FORMAT-CONFLICT: A CONFLICT. The control pair
      *>   (SPACE / space, differing only in the KEYWORD's case) is
      *>   identical and the CALL succeeds: B SUB-RAN.
       >>TURN EC-EXTERNAL-FORMAT-CONFLICT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1236.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XV PIC X(4) IS EXTERNAL VALUE "ABCD".
       01 XS PIC X(4) IS EXTERNAL VALUE SPACE.
       01 ES PIC X(40).
       PROCEDURE DIVISION.
           CALL "PB1236A"
               ON EXCEPTION MOVE FUNCTION EXCEPTION-STATUS TO ES
               NOT ON EXCEPTION MOVE "SUB-RAN" TO ES
           END-CALL
           IF ES = "EC-EXTERNAL-FORMAT-CONFLICT"
               DISPLAY "A CONFLICT"
           ELSE
               IF ES = "SUB-RAN"
                   DISPLAY "A SUB-RAN"
               ELSE
                   DISPLAY "A OTHER"
               END-IF
           END-IF
           CALL "PB1236B"
               ON EXCEPTION MOVE FUNCTION EXCEPTION-STATUS TO ES
               NOT ON EXCEPTION MOVE "SUB-RAN" TO ES
           END-CALL
           IF ES = "EC-EXTERNAL-FORMAT-CONFLICT"
               DISPLAY "B CONFLICT"
           ELSE
               IF ES = "SUB-RAN"
                   DISPLAY "B SUB-RAN"
               ELSE
                   DISPLAY "B OTHER"
               END-IF
           END-IF
           STOP RUN.
       END PROGRAM PB1236.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1236A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XV PIC X(4) IS EXTERNAL VALUE "abcd".
       PROCEDURE DIVISION.
           GOBACK.
       END PROGRAM PB1236A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1236B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XS PIC X(4) IS EXTERNAL VALUE space.
       PROCEDURE DIVISION.
           GOBACK.
       END PROGRAM PB1236B.
