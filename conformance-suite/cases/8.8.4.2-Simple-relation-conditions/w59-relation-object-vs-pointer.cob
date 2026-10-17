*>--- conformance-case
*> id: w59-relation-object-vs-pointer
*> rules: [SR-8.8.4.2.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.8.4.2.3 SR5 (kb/Work R43 / PB1198, the LIVE halves of SR-8.8.4.2.3-5): "Identifier-3 and
      *> identifier-4 shall reference data items of class message-tag, object, or pointer, and shall be of the
      *> same category." An object reference and a data-pointer are of different classes and so of different
      *> categories; the relation is refused. The message-tag third of the rule cannot arise: USAGE MESSAGE-TAG
      *> is refused by name (COBOLNET1943) because the asynchronous messaging facility is not claimed (Annex A.3
      *> item 4), which R43 item 2 records as the declined half. The pointer-vs-program-pointer half is
      *> negative/pb399-pointer-relation-category-mix.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W59OBJPTR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-O USAGE OBJECT REFERENCE.
       01 WS-P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           IF WS-O = WS-P
               DISPLAY "EQ"
           END-IF.
           STOP RUN.
