*>--- conformance-case
*> id: w59-invoke-returning-narrower-receiver
*> rules: [GR-9.3.6-L3.7]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §9.3.6 match rule 7, first sentence (kb/Work R43 / PB1519, the LIVE half of GR-9.3.6-L3.7): "If the
      *> invocation specifies a returning item and the returning item is not usage OBJECT REFERENCE, POINTER or
      *> INDEX, there shall be a corresponding specification in the invoked method that may be a sending item in
      *> a MOVE statement with the returning item as the receiving item." Every activation is ALSO held to
      *> §14.8.3.3's identical-description rule for the returning item ("the receiving operand shall have the
      *> same ... PICTURE ... clauses"), which is STRICTER than MOVE-ability and which this implementation
      *> enforces for every invocation (COBOLNET0828). A PIC 9(5) method returning item received into a
      *> PIC 9(3) item is a legal MOVE (it truncates) but not an identical description, so the invocation is
      *> refused — the MOVE-ability half of rule 7 is therefore never weaker than what the compiler checks.
      *> The rule's second sentence introduces the EXACT-MATCH criteria, which only rank two or more same-named
      *> candidates (parametric polymorphism, Annex A.4.10 item 3, not claimed — R43 item 2).
       IDENTIFICATION DIVISION.
       CLASS-ID. W59RETC.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. FIVE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R PIC 9(5).
       PROCEDURE DIVISION RETURNING LK-R.
       MAIN.
           MOVE 12345 TO LK-R.
       END METHOD FIVE.
       END FACTORY.
       END CLASS W59RETC.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. W59RETP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS W59RETC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-R PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE W59RETC "FIVE" RETURNING WS-R.
           DISPLAY WS-R.
           STOP RUN.
       END PROGRAM W59RETP.
