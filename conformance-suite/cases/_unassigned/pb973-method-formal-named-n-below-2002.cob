*>--- conformance-case
*> id: pb973-method-formal-named-n-below-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> The REJECT half of conformance:2002/pb973_method_formal_named_like_emitter_member
      *> (kb/Work PB973). The object-orientation facility is gated at
      *> COBOL-2002, so at 85 the class definition and its method with a
      *> LINKAGE formal named N draw the edition band COBOLNET0900 - never
      *> a backend crash. At 2002 and later the same source compiles and
      *> runs (the formal's C# carrier lives in the tagged __formal_
      *> family, off the compiler's own __N paragraph-count member).
       IDENTIFICATION DIVISION.
       CLASS-ID. PB973NBOX.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. SETV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 N PIC 9(3).
       PROCEDURE DIVISION USING N.
           MOVE N TO V.
       END METHOD SETV.
       END OBJECT.
       END CLASS PB973NBOX.
