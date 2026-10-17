*>--- conformance-case
*> id: pb456-set-exception-object-receiver
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.6.3 SR1 - "EXCEPTION-OBJECT shall not be specified as a receiving operand." The predefined
      *> object reference is spelled as an ordinary word, not a reserved token, so it arrives as a written data
      *> reference; 8.4.3.6.3 SR2 describes it as "class object and category object reference", which is what
      *> puts the statement in Format 5 and makes SR1 the rule it breaks.
      *> ⛔ The compiler used to answer "'EXCEPTION-OBJECT' is not defined" (COBOLNET1639) - FALSE about a name
      *> the standard itself declares - because no data description entry declares it and the SET format was
      *> chosen with the receiver left unclassified. Its three siblings cannot be written here at all: NULL,
      *> SELF and SUPER are grammar tokens, so 8.4.3.7.3 SR1 and 8.4.3.8.3 SR2 are enforced by the syntax.
      *> Rejected from 2002: EXCEPTION-OBJECT and USAGE OBJECT REFERENCE are COBOL-2002 introductions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB456N10.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET EXCEPTION-OBJECT TO U
           STOP RUN.
