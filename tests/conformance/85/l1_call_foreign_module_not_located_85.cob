      *> ISO §14.9.4.4 GR3 b)/h) (Annex A.1 item 16) — CALL of a
      *> non-COBOL program at COBOL-85: not located, ON EXCEPTION runs.
      *>   cite.py --check 14.9.4.4 "If the program being called is not a
      *>     COBOL program, the rules for program-name formation and for
      *>     locating the program are defined by the implementor." -> OK 3)
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
      *> THE DETERMINATION PINNED (docs/CONFORMANCE.md DOC-A.1-16, "The
      *> same at every edition"): the runtime assembly
      *> Cobol.Net.Runtime.dll, a non-COBOL .NET assembly deployed beside
      *> every COBOL.NET program, is not located by CALL.
      *> This edition has no TURN directive and no EXCEPTION-STATUS
      *> function, so only the phrase taken is observable.
      *> DERIVATION of every .out line:
      *>   SUB-RAN / SUB-CALLED   control: the contained COBOL program is
      *>       located and called (GR3 g)), then NOT ON EXCEPTION (GR3 i)).
      *>   LIT-NOT-FOUND   literal names the non-COBOL assembly: not
      *>       located (GR3 b)), ON EXCEPTION runs (GR3 h) 1.), NOT ON
      *>       EXCEPTION does not.
      *>   ID-NOT-FOUND    the same name held in an identifier.
      *>   END             control continues after each CALL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1CNC85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NAME PIC X(17) VALUE "Cobol.Net.Runtime".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "L1CNC85S"
               ON EXCEPTION DISPLAY "SUB-NOT-FOUND"
               NOT ON EXCEPTION DISPLAY "SUB-CALLED"
           END-CALL
           CALL "Cobol.Net.Runtime"
               ON EXCEPTION DISPLAY "LIT-NOT-FOUND"
               NOT ON EXCEPTION DISPLAY "LIT-CALLED"
           END-CALL
           CALL WS-NAME
               ON EXCEPTION DISPLAY "ID-NOT-FOUND"
               NOT ON EXCEPTION DISPLAY "ID-CALLED"
           END-CALL
           DISPLAY "END"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1CNC85S.
       PROCEDURE DIVISION.
       SUB-PARA.
           DISPLAY "SUB-RAN"
           EXIT PROGRAM.
       END PROGRAM L1CNC85S.
       END PROGRAM L1CNC85.
