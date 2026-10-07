      *> reject-at: 2002 2014 2023
      *> kb/Work PB970 arm 2 - the OTHER ARM of the pointer-image
      *> pairing. tests/conformance/2002/pb970_pointer_by_content_
      *> storage_image.cob pins that a POINTER passed BY CONTENT to a
      *> same-length PIC X(8) formal conforms when the program is
      *> called with no program-specifier and no NESTED phrase
      *> (ISO 14.8.2.3.3 rule 1: "the formal parameter shall be of the
      *> same length as the corresponding argument"). Here the caller
      *> names the program in a program-specifier of its REPOSITORY
      *> paragraph, so rule 2 governs instead, and rule 2d says "the
      *> conformance rules are the same as for a MOVE statement with
      *> the argument as the sending operand" - and 14.9.25.3 SR1: "The
      *> class of identifier-1 or identifier-2 shall not be index,
      *> message-tag, object, or pointer." The pairing is refused at
      *> compile time; it must never reach the rule-1 image delivery.
      *> kb/Work PB989 - ISO 12.3.8.4 GR10 a): the details come from a program definition specified PREVIOUSLY in the
      *> compilation group; a definition that follows the REPOSITORY entry is not one. The callee below is therefore
      *> given a program prototype definition (11.10.2 Format 2) ahead of every other unit (10.6.2 SR1), which
      *> supplies the details under GR10 b) - the same signature as the definition (10.6.2 SR2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB970NEGPTRS IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(8).
       PROCEDURE DIVISION USING L.
       END PROGRAM PB970NEGPTRS.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB970NEGPTR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PB970NEGPTRS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
           CALL PB970NEGPTRS USING BY CONTENT P
           STOP RUN.
       END PROGRAM PB970NEGPTR.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB970NEGPTRS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(8).
       PROCEDURE DIVISION USING L.
           DISPLAY "IN CALLEE"
           GOBACK.
       END PROGRAM PB970NEGPTRS.
