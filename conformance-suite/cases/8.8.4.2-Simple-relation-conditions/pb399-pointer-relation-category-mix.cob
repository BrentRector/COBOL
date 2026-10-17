*>--- conformance-case
*> id: pb399-pointer-relation-category-mix
*> rules: [SR-8.8.4.2.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.8.4.2.3 SR5 (FORMAT 3): "Identifier-3 and identifier-4 shall reference data items of class
      *> message-tag, object, or pointer, and shall be of the same category."  §8.8.4.2.1 item 11 states the
      *> same restriction from the other side — a comparison is defined for "Two operands of class pointer
      *> where each operand is of the same category."  A data-pointer and a program-pointer are ONE class
      *> (§8.5.2.1 Table 2 gathers data-pointer, function-pointer and program-pointer into class pointer)
      *> and TWO categories.
      *>
      *> ⛔ NEITHER HALF OF THE OLD BAND ASKED SR5'S CATEGORY QUESTION, and both asked class membership as
      *> `Pic?.Category == PicCategory.Pointer` — one of the three categories Table 2 folds — so a
      *> program-pointer operand answered "not a pointer" and the whole band stayed silent on it, ordering
      *> relations included (kb/Work PB399).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB399PCAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-P  USAGE POINTER.
       01 WS-PP USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       MAIN.
           IF WS-P = WS-PP
               DISPLAY "CAT-EQ"
           END-IF.
           STOP RUN.
