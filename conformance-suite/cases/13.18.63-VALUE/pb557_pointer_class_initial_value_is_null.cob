*>--- conformance-case
*> id: pb557_pointer_class_initial_value_is_null
*> rules: [SR-13.18.63.3-9, DOC-A.1-125]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB557 - the POSITIVE half of the SR9 family, and the reason its prohibition costs nothing.
      *> ISO 13.18.63.4 GR4: "When VALUE clauses take effect, data items with a VALUE clause are initialized to
      *> the specified value and data items of class message-tag, class object, and class pointer are
      *> initialized to null" - with GR4 c) putting that among the occasions "when the object or runtime element
      *> is placed in initial state". So a pointer-class item is null at initial state with NO VALUE clause
      *> written, which is exactly what 13.18.63.3 SR9 forbids writing one for.
      *>
      *> Without this, the three negatives that reject a VALUE clause on these usages would be a screen with no
      *> evidence that the thing it refuses was unnecessary - an over-reject and a correct reject look the same
      *> from the negative side. Every value below is COMPUTED FROM GR4, not measured.
      *>
      *> The last pair is the control that the items are otherwise LIVE: 8.4.3.11 ADDRESS OF gives P a real
      *> address and the 8.8.4.2.16 pointer relation then answers P NOT = NULL.
      *> COBOL-2002 because USAGE POINTER, OBJECT REFERENCE and PROGRAM-POINTER are all post-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB557-NULL-INITIAL-2002.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       01 O USAGE OBJECT REFERENCE.
       01 PP USAGE PROGRAM-POINTER.
       01 T PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
           IF P = NULL DISPLAY "P-NULL" ELSE DISPLAY "P-SET" END-IF
           IF O = NULL DISPLAY "O-NULL" ELSE DISPLAY "O-SET" END-IF
           IF PP = NULL DISPLAY "PP-NULL" ELSE DISPLAY "PP-SET" END-IF
           SET P TO ADDRESS OF T
           IF P = NULL DISPLAY "P2-NULL" ELSE DISPLAY "P2-SET" END-IF
           STOP RUN.
