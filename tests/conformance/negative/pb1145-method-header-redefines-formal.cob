      *> reject-at: 2002 2014 2023
      *> kb/Work PB1145 - ISO 14.2.2 SR1: "The data description entry for
      *>   data-name-1 shall not contain a BASED clause or a REDEFINES
      *>   clause." The program arm already refused a REDEFINES formal; the
      *>   METHOD arm carried no such check and the program reached the
      *>   backend (CS0103). Both arms now call ONE screen. The method
      *>   below has a REDEFINES formal B and a RETURNING item that is also
      *>   a formal (SR6 - the method arm mis-cited it "SR4").
      *> cite.py --check 14.2.2 "The data description entry for data-name-1
      *>   shall not contain a BASED clause or a REDEFINES clause" -> OK
      *>   14.2.2 1)
      *> cite.py --check 14.2.2 "Data-name-2 shall not be the same as
      *>   data-name-1" -> OK  14.2.2 6)
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1145M INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC 9(3).
       01 B REDEFINES A PIC X(3).
       PROCEDURE DIVISION USING B RETURNING B.
           GOBACK.
       END METHOD M.
       END OBJECT.
       END CLASS PB1145M.
