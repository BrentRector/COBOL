*>--- conformance-case
*> id: pb449-set-up-by-non-index-receiver
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39.2 Format 2 prints SET { index-name-3 } ... { UP BY | DOWN BY } arithmetic-expression-2, and
      *> 14.9.39.4 GR4 is written "For each occurrence of index-name-3": Format 2 has NO identifier alternative
      *> and NO syntax rule of its own. The only other formats written with UP/DOWN BY are Format 10, whose
      *> receiving operand is identifier-9 "of category data-pointer" (14.9.39.3 SR23), and Format 14, whose
      *> data-name-2 is a dynamic-capacity register (SR29). An ordinary integer data item is none of the three,
      *> so no printed general format admits SET WS-N UP BY 4 and the statement is refused.
      *> It used to fall through to the Format-2 index binding and EXECUTE - the compiler answered N=0005 for a
      *> statement no format admits. kb/Work PB449.
      *> All four editions reject: Format 2 is COBOL-85 and its receiving brace has never had an identifier
      *> alternative, so nothing here is edition-dependent.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB449N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9(4) VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET WS-N UP BY 4
           DISPLAY WS-N
           STOP RUN.
