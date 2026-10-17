*>--- conformance-case
*> id: pb922_exception_object_reference
*> rules: [SR-8.4.3.1.3-7, SR-8.4.3.6.3-1, SR-8.4.3.6.3-2, FMT-8.4.3.6.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB922 - EXCEPTION-OBJECT IS A NAME THE STANDARD DECLARES, AND IT RESOLVES EVERYWHERE, NOT ONLY
      *> INSIDE SET. Every expected line below is derived from the rule text, never from a run.
      *>
      *> ISO 8.4.3.6.3 SR2 - "EXCEPTION-OBJECT is implicitly described as class object and category object
      *>   reference, as an external data item, and as a universal object reference." No data description entry
      *>   declares the name, so a compiler whose resolver does not know it answers "'EXCEPTION-OBJECT' is not
      *>   defined" about a name the standard itself declares - which is what this case exists to forbid.
      *> ISO 8.4.3.6.4 GR1 - "EXCEPTION-OBJECT references the current exception object. If an exception object
      *>   is not associated with the current exception, EXCEPTION-OBJECT is set to null."
      *> ISO 8.4.3.6.4 GR2 - "There is one instance of EXCEPTION-OBJECT in a run unit."
      *>
      *> This is the INTRODUCING edition: the object-orientation facility, and with it the predefined object
      *> reference and the 8.9 reservation of the word, arrived in COBOL-2002. The '85 counterpart is
      *> tests/conformance/85/pb922_exception_object_user_word_85.cob, where the same spelling is an ORDINARY
      *> user-defined word; the receiving refusal is tests/conformance/negative/pb922-exception-object-receiving.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC:
      *>
      *> SR2-NULL=YES      `IF EXCEPTION-OBJECT = NULL` is a SENDING reference in a relation condition - legal
      *>                   source by SR2's implicit description, and refused as "not defined" by a resolver that
      *>                   does not know the name. No statement in this program raises an exception, so no
      *>                   exception object is associated with the current exception and GR1 makes the reference
      *>                   null: YES.
      *> GR1-COPY=NULL     `SET U TO EXCEPTION-OBJECT` copies that same null reference into a universal object
      *>                   reference (14.9.39.4 GR14), so U is null too.
      *> GR2-ONE=SAME      GR2 gives the run unit ONE instance, so a second reference to it - here compared
      *>                   against the register itself in a relation condition, a second sending position -
      *>                   denotes the same object reference: SAME.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB922EXOBJ02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U             USAGE OBJECT REFERENCE.
       01 V             USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
           IF EXCEPTION-OBJECT = NULL
              DISPLAY "SR2-NULL=YES"
           ELSE
              DISPLAY "SR2-NULL=NO"
           END-IF

           SET U TO EXCEPTION-OBJECT
           IF U = NULL
              DISPLAY "GR1-COPY=NULL"
           ELSE
              DISPLAY "GR1-COPY=SET"
           END-IF

           SET V TO EXCEPTION-OBJECT
           IF V = EXCEPTION-OBJECT
              DISPLAY "GR2-ONE=SAME"
           ELSE
              DISPLAY "GR2-ONE=DIFFERENT"
           END-IF
           STOP RUN.
