      *> reject-at: 2002 2014 2023
      *> kb/Work PB1617 - a figurative constant other than NULL passed BY
      *> CONTENT into a formal parameter of class pointer does not
      *> conform.
      *> ISO 14.8.2.3.3: "If the formal parameter is of class pointer or
      *> an object reference described without the ACTIVE-CLASS phrase,
      *> the conformance rules shall be the same as if a SET statement
      *> were performed in the activating runtime element with the
      *> argument as the sending operand", and no SET format sends SPACE
      *> to a data-pointer.
      *> cite.py --check 14.8.2.3.3 "If the formal parameter is of class
      *>   pointer or an object reference described without the
      *>   ACTIVE-CLASS phrase, the conformance rules shall be the same as
      *>   if a SET statement were performed" -> OK 14.8.2.3.3
      *> Before the fix the pair compiled and the run died with
      *> EC-PROGRAM-ARG-MISMATCH at the callee's pointer formal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1617PM.
       PROCEDURE DIVISION.
           CALL "PB1617PS" AS NESTED USING BY CONTENT SPACE
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1617PS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP USAGE POINTER.
       PROCEDURE DIVISION USING LP.
           IF LP = NULL DISPLAY "NULL" ELSE DISPLAY "NOT NULL".
           GOBACK.
       END PROGRAM PB1617PS.
       END PROGRAM PB1617PM.
