*>--- conformance-case
*> id: pb239_call_program_address_not_found
*> rules: [GR-8.4.3.13.4-4, SR-14.9.4.3-4]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB239 - a PROGRAM-ADDRESS-IDENTIFIER argument that cannot
      *> be located, with EC-PROGRAM-NOT-FOUND checking ENABLED. 8.4.3.13.4
      *> GR4: "the EC-PROGRAM-NOT-FOUND exception condition is set to exist".
      *> 14.9.4.4 GR3a: identifier-2 is "evaluated ... at the beginning of
      *> the execution of the CALL statement. If an exception condition
      *> exists, no program is called and execution proceeds as specified
      *> in General rule 3h" - and GR3h item 1 runs ON EXCEPTION for an
      *> EC-PROGRAM condition. So PB239PQ is never activated (no "CALLED"
      *> line) and the phrase runs: EXC. The located spelling of the same
      *> statement calls normally and takes NOT ON EXCEPTION (GR3i).
       >>TURN EC-PROGRAM-NOT-FOUND CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB239NF.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB239PQ" USING ADDRESS OF PROGRAM "PB239ZZ"
               ON EXCEPTION DISPLAY "EXC"
               NOT ON EXCEPTION DISPLAY "NOEXC"
           END-CALL
           CALL "PB239PQ" USING ADDRESS OF PROGRAM "PB239NF"
               ON EXCEPTION DISPLAY "EXC"
               NOT ON EXCEPTION DISPLAY "NOEXC"
           END-CALL
           STOP RUN.
       END PROGRAM PB239NF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB239PQ.
       DATA DIVISION.
       LINKAGE SECTION.
       01 Q USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION USING Q.
       MAIN.
           DISPLAY "CALLED"
           GOBACK.
       END PROGRAM PB239PQ.
