      *> reject-at: 2002 2014 2023
      *> kb/Work PB1630 - the predefined NULL passed BY CONTENT into a
      *> formal parameter that is not of class pointer or object
      *> reference does not conform.
      *> NULL is an identifier of class pointer: 8.4.3.10.1 "NULL is a
      *> predefined address of class pointer". 14.8.2.3.3 rule 2d makes
      *> an alphanumeric formal's conformance "the same as for a MOVE
      *> statement with the argument as the sending operand", and
      *> 14.9.25.3 SR1: "The class of identifier-1 or identifier-2 shall
      *> not be index, message-tag, object, or pointer."
      *> cite.py --check 8.4.3.10.1 "NULL is a predefined address of
      *>   class pointer" -> OK 8.4.3.10.1
      *> cite.py --check 14.9.25.3 "The class of identifier-1 or
      *>   identifier-2 shall not be index, message-tag, object, or
      *>   pointer." -> OK 14.9.25.3 1)
      *> Before the fix NULL was read as a LOW-VALUE fill and the
      *> callee received one NUL character padded with spaces.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1630NM.
       PROCEDURE DIVISION.
           CALL "PB1630NS" AS NESTED USING BY CONTENT NULL
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1630NS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LX PIC X(4).
       PROCEDURE DIVISION USING LX.
           DISPLAY "[" LX "]"
           GOBACK.
       END PROGRAM PB1630NS.
       END PROGRAM PB1630NM.
