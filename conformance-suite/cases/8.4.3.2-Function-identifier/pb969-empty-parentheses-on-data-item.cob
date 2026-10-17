*>--- conformance-case
*> id: pb969-empty-parentheses-on-data-item
*> rules: [FMT-8.4.3.2.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.2.3.2 writes a subscript list as `( subscript ... )` - at least one subscript - and 8.4.3.3.2
      *> a reference modifier as `( leftmost-position : [ length ] )`. Empty parentheses are the zero-argument
      *> form of a function-identifier (8.4.3.2.2 brackets argument-1 inside them), and WS-X is a data item,
      *> so `WS-X()` is neither form a parenthesis after a data-name can take (kb/Work PB969: the grammar
      *> admits the empty group for the function-identifier, and the resolver refuses it by name here).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB969NEG1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY WS-X()
           STOP RUN.
