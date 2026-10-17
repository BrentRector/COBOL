*>--- conformance-case
*> id: pb53_by_content_move_rules
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.8.2.3.3 rule 2d: a BY CONTENT argument whose formal is not
      *> numeric, not an index item and not ANY LENGTH conforms by "the same
      *> [rules] as for a MOVE statement with the argument as the sending operand
      *> and the corresponding formal parameter as the receiving operand".
      *>
      *> SO THE RULE IS AN AGREEMENT, AND THIS GOLDEN ASSERTS IT AS ONE. Every
      *> pair below performs the SAME transfer twice - once across an INVOKE
      *> BY CONTENT and once as a plain MOVE - and prints both. A pair that
      *> disagrees is a violation of rule 2d whatever either value is, which is a
      *> stronger and more durable assertion than pinning the values themselves.
      *>
      *> Before fix-queue PB53 the INVOKE half of pairs 1-3 did not compile at
      *> all: OoContentMismatch fell back to 14.8.2.3.2 - the BY REFERENCE
      *> IDENTITY rule - for boolean, national and numeric-edited formals, and
      *> reported "category mismatch" citing a clause that does not govern the
      *> crossing. 14.9.25.3 Table 16 admits all three:
      *>     boolean -> national        Yes
      *>     alphanumeric -> boolean    Yes
      *>     national -> boolean        Yes
      *>
      *> Pairs 4-5 are the WIDTH-DIFFERING crossings that strict identity had
      *> made unreachable, so the emitter never needed a store discipline for
      *> them. It does now: 14.6.8.6 truncates on the right into a boolean
      *> receiver, and the crossing reaches MoveEmitter.ConvertSource - the one
      *> place that discipline is written - rather than re-deriving it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB53MOVERULES.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB53.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O  USAGE OBJECT REFERENCE CPB53.
       01 B1 PIC 1(4) USAGE BIT VALUE B"1100".
       01 A1 PIC X(4) VALUE "WXYZ".
       01 N1 PIC N(4) VALUE N"WXYZ".
       01 B6 PIC 1(6) USAGE BIT VALUE B"110011".
       01 A2 PIC X(6) VALUE "ABCDEF".
       01 RN PIC N(4).
       01 RB PIC 1(4) USAGE BIT.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CPB53 "NEW" RETURNING O.
      *> 1 - boolean -> national. 14.9.25.4 GR6a: "If the sending item is of
      *> class boolean, its boolean value shall be moved."
           INVOKE O "TAKEN" USING BY CONTENT B1.
           MOVE B1 TO RN.
           DISPLAY "1M=[" RN "]".
      *> 2 - alphanumeric -> boolean.
           INVOKE O "TAKEB" USING BY CONTENT A1.
           MOVE A1 TO RB.
           DISPLAY "2M=[" RB "]".
      *> 3 - national -> boolean.
           INVOKE O "TAKEB" USING BY CONTENT N1.
           MOVE N1 TO RB.
           DISPLAY "3M=[" RB "]".
      *> 4 - boolean(6) -> boolean(4): right truncation, 14.6.8.6.
           INVOKE O "TAKEB" USING BY CONTENT B6.
           MOVE B6 TO RB.
           DISPLAY "4M=[" RB "]".
      *> 5 - alphanumeric(6) -> boolean(4): the same truncation across categories.
           INVOKE O "TAKEB" USING BY CONTENT A2.
           MOVE A2 TO RB.
           DISPLAY "5M=[" RB "]".
      *> 6 - THE CONTROL THAT MUST NOT MOVE: an identical-description crossing,
      *> which took the proven fast path before this change and still must.
           INVOKE O "TAKEB" USING BY CONTENT B1.
           MOVE B1 TO RB.
           DISPLAY "6M=[" RB "]".
           STOP RUN.
       END PROGRAM PB53MOVERULES.

       IDENTIFICATION DIVISION.
       CLASS-ID. CPB53 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKEN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC N(4).
       PROCEDURE DIVISION USING P.
       M.
           DISPLAY "1I=[" P "]".
       END METHOD TAKEN.
       METHOD-ID. TAKEB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 Q PIC 1(4) USAGE BIT.
       PROCEDURE DIVISION USING Q.
       M.
           DISPLAY "I=[" Q "]".
       END METHOD TAKEB.
       END OBJECT.
       END CLASS CPB53.
