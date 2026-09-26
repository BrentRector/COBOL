      *> ISO §14.9.4.4 GR3 b)/g) (Annex A.1 item 16) — CALL of a
      *> non-COBOL program: the documented determination is that no
      *> non-COBOL program is ever located, so the CALL takes the
      *> EC-PROGRAM-NOT-FOUND arm.
      *>   cite.py --check 14.9.4.4 "If the program being called is not a
      *>     COBOL program, the rules for program-name formation and for
      *>     locating the program are defined by the implementor." -> OK 3)
      *>   cite.py --check 14.9.4.4 "If the called program is a COBOL
      *>     program, its execution is described in 14.2, Procedure
      *>     division structure; otherwise the execution is defined by the
      *>     implementor." -> OK 3) g)
      *>   cite.py --check 14.9.4.4 "If the program cannot be located or
      *>     identifier-1 references a zero-length item, the
      *>     EC-PROGRAM-NOT-FOUND exception condition is set to exist."
      *>     -> OK 3) b)
      *>   cite.py --check 14.9.4.4 "If the exception condition is any of
      *>     the EC-PROGRAM or EC-EXTERNAL exception conditions and an ON
      *>     EXCEPTION phrase is specified in the CALL statement, control
      *>     is transferred to imperative-statement-1." -> OK 3) h) 1.
      *>   cite.py --check 14.9.4.4 "otherwise, control is transferred to
      *>     the end of the CALL statement or, if the NOT ON EXCEPTION
      *>     phrase is specified, to imperative-statement-2." -> OK 3) i)
      *>   cite.py --check 14.6.13.1.1 "the last exception status is set
      *>     to indicate that exception condition" -> OK
      *>   cite.py --check 15.33.3 "A 31-character, left-justified,
      *>     alphanumeric character string that is the exception-name"
      *>     -> OK 1)
      *> THE DETERMINATION PINNED (docs/CONFORMANCE.md DOC-A.1-16): a CALL
      *> resolves only to a COBOL.NET program; every other target - a
      *> .NET assembly present in the application directory (the runtime
      *> assembly Cobol.Net.Runtime.dll, deployed beside every COBOL.NET
      *> program) named by a literal or held in an identifier - is not
      *> located, so GR3 g)'s non-COBOL branch is never reached.
      *> Checking for EC-PROGRAM is enabled by the TURN below, so the
      *> exception is raised and the last exception status names it.
      *> DERIVATION of every .out line:
      *>   SUB-RAN / SUB-CALLED  control: a located COBOL program (the
      *>       contained L1CNCSUB) is called - GR3 g) - and on return
      *>       NOT ON EXCEPTION runs - GR3 i). So CALL itself works.
      *>   LIT-NOT-FOUND EC-PROGRAM-NOT-FOUND   the literal names the
      *>       non-COBOL assembly; not located - GR3 b) - so ON EXCEPTION
      *>       runs - GR3 h) 1. - and NOT ON EXCEPTION does not; the
      *>       status is the exception-name, left-justified (15.33.3; the
      *>       runner trims the trailing spaces).
      *>   ID-NOT-FOUND EC-PROGRAM-NOT-FOUND    the same name held in an
      *>       identifier (GR3 b) first bullet) - the same arm.
      *>   END   control continues at the end of each CALL (GR3 h) 1.).
       >>TURN EC-PROGRAM CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1CNC01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NAME PIC X(17) VALUE "Cobol.Net.Runtime".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "L1CNCSUB"
               ON EXCEPTION DISPLAY "SUB-NOT-FOUND"
               NOT ON EXCEPTION DISPLAY "SUB-CALLED"
           END-CALL
           CALL "Cobol.Net.Runtime"
               ON EXCEPTION
                   DISPLAY "LIT-NOT-FOUND " FUNCTION EXCEPTION-STATUS
               NOT ON EXCEPTION
                   DISPLAY "LIT-CALLED"
           END-CALL
           CALL WS-NAME
               ON EXCEPTION
                   DISPLAY "ID-NOT-FOUND " FUNCTION EXCEPTION-STATUS
               NOT ON EXCEPTION
                   DISPLAY "ID-CALLED"
           END-CALL
           DISPLAY "END"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1CNCSUB.
       PROCEDURE DIVISION.
       SUB-PARA.
           DISPLAY "SUB-RAN"
           GOBACK.
       END PROGRAM L1CNCSUB.
       END PROGRAM L1CNC01.
