*>--- conformance-case
*> id: pb428-inline-method-invocation-below-2002
*> rules: [SR-8.4.3.1.3-5, SR-14.9.25.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> The REJECT half of conformance:cases/8.4.3.1-Identifier/pb428_inline_method_invocation.
      *> An inline method invocation (ISO §8.4.3.4 — the §8.4.3.1.2 Format 4
      *> identifier) belongs to the object-orientation facility, which this
      *> compiler gates at COBOL-2002 throughout. THE EDGE IS DERIVED, NOT
      *> QUOTED (the repo holds no 2002 or 2014 text): §A.4.10 lists exactly
      *> three OPTIONAL object-orientation elements and inline method
      *> invocation is NOT among them, so it is mandatory OO surface; Annex E
      *> itemizes the 2014→2023 delta and does not list it; and §8.4.3.4.4 GR1 defines the
      *> construct as the equivalent INVOKE statement, itself a 2002
      *> introduction.  kb/Work PB428.
      *> ⛔ THE POINT OF THIS FIXTURE IS THAT THE EDITION IS NAMED. Before the
      *> fix the §8.7.4 invocation operator '::' had no lexer token at all, so
      *> the construct drew three raw COBOL0001 errors about a colon at EVERY
      *> edition — and the superseded construct row recorded exactly that as a
      *> permanent residue ("no distinctive token"). '::' IS a distinctive
      *> token, so the below-2002 answer is COBOLNET0900 naming the construct.
      *> The .err names 0900; the same compile also draws 0900 for REPOSITORY
      *> CLASS, INVOKE, USAGE OBJECT REFERENCE and the CLASS-ID unit, which is
      *> expected — every one of them is the same 2002 OO band.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB428N1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB428NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB428NC.
       01 W   PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB428NC "NEW" RETURNING OBJ.
           MOVE OBJ :: "GETNAME" TO W.
           DISPLAY W.
           STOP RUN.
       END PROGRAM PB428N1.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB428NC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETNAME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-NAME PIC X(8).
       PROCEDURE DIVISION RETURNING LK-NAME.
       MAIN.
           MOVE "ACCOUNT" TO LK-NAME.
       END METHOD GETNAME.
       END OBJECT.
       END CLASS PB428NC.
