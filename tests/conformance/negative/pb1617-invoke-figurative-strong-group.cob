      *> reject-at: 2002 2014 2023
      *> kb/Work PB1617 - a figurative constant passed BY CONTENT through
      *> INVOKE into a STRONGLY-TYPED GROUP formal parameter does not
      *> conform.
      *> ISO 14.8.2.2: "If either the formal parameter or the
      *> corresponding argument is a strongly-typed group item, both shall
      *> be of the same type"; its rule 2 makes the BY CONTENT rules those
      *> of a MOVE, and 14.9.25.3 SR2 admits into a strongly-typed group
      *> only "a group item of the same type". A figurative constant is
      *> literal-2 (8.3.3.6.3 SR1) and never a typed group.
      *> cite.py --check 14.8.2.2 "If either the formal parameter or the
      *>   corresponding argument is a strongly-typed group item, both
      *>   shall be of the same type" -> OK 14.8.2.2
      *> cite.py --check 14.9.25.3 "If identifier-2 references a
      *>   strongly-typed group item, identifier-1 shall be specified"
      *>   -> OK 14.9.25.3 2)
      *> Before the fix INVOKE refused every figurative as "not yet
      *> carried" (COBOLNET0828) - the refusal now names the rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1617SM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1617SK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB1617SK.
       PROCEDURE DIVISION.
           INVOKE PB1617SK "NEW" RETURNING O
           INVOKE O "TAKES" USING BY CONTENT SPACE
           STOP RUN.
       END PROGRAM PB1617SM.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1617SK INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKES.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 TT TYPEDEF STRONG.
          05 TA PIC X(2).
          05 TB PIC X(2).
       LINKAGE SECTION.
       01 LS TYPE TT.
       PROCEDURE DIVISION USING LS.
           DISPLAY "S=[" LS "]"
           GOBACK.
       END METHOD TAKES.
       END OBJECT.
       END CLASS PB1617SK.
