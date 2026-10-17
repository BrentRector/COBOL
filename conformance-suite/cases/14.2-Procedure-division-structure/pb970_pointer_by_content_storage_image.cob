      *>--- conformance-case
      *> id: pb970_pointer_by_content_storage_image
      *> rules: [GR-14.2.3-9, DOC-A.1-216, DOC-A.1-217]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB970 arm 2 - a POINTER / PROGRAM-POINTER argument
      *> passed BY CONTENT to a same-length NON-POINTER formal of a
      *> program called with no program-specifier and no NESTED
      *> phrase arrives as the pointer's STORAGE IMAGE.
      *> ISO 14.8.2.3.3 rule 1: "the formal parameter shall be of the
      *> same length as the corresponding argument" - the whole rule
      *> when the formal is not of class object or pointer, so a
      *> POINTER into PIC X(8) or BINARY-DOUBLE conforms.
      *> ISO 14.2.3 GR9: "That argument is moved to this allocated
      *> record without conversion" - the formal holds the pointer's
      *> 8 storage positions, in a record that does not occupy the
      *> argument's storage.
      *> The 8 positions are docs/CONFORMANCE.md DOC-A.1-216: NULL
      *> (any category) is eight X"00"; a data-pointer is its storage
      *> area's base plus its displacement as a big-endian 64-bit
      *> integer; a program-pointer is a distinct nonzero token.
      *> Derivation (no measured value is used):
      *>   NULL P            -> all LOW-VALUE           NULL-IMAGE DIFF
      *>   P = ADDRESS OF W  -> nonzero                 NONNULL DIFF
      *>   Q = P             -> equal pointers, equal images: SAME
      *>   the callee's MOVE ALL "Z" reaches only its record: P = Q
      *>   read as BINARY-DOUBLE: SET P UP BY 3 moves it +3, DOWN BY 3
      *>   moves it -3 (base + displacement)
      *>   PP = ENTRY        -> nonzero, not the data token: DIFF
      *>   PP NULL, then P NULL -> NULL-IMAGE DIFF, then SAME (every
      *>   category's NULL is the zero address)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB970PTR02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P  USAGE POINTER.
       01 Q  USAGE POINTER.
       01 PP USAGE PROGRAM-POINTER.
       01 W  PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
           CALL "PB970PTRC" USING BY CONTENT P
           SET P TO ADDRESS OF W
           SET Q TO P
           CALL "PB970PTRC" USING BY CONTENT P
           IF P = Q
               DISPLAY "CALLER-POINTER-UNCHANGED"
           ELSE
               DISPLAY "CALLER-POINTER-CHANGED"
           END-IF
           CALL "PB970PTRC" USING BY CONTENT Q
           CALL "PB970PTRN" USING BY CONTENT P
           SET P UP BY 3
           CALL "PB970PTRN" USING BY CONTENT P
           SET P DOWN BY 3
           CALL "PB970PTRN" USING BY CONTENT P
           SET PP TO ENTRY "PB970PTRC"
           CALL "PB970PTRC" USING BY CONTENT PP
           SET PP TO NULL
           CALL "PB970PTRC" USING BY CONTENT PP
           SET P TO NULL
           CALL "PB970PTRC" USING BY CONTENT P
           STOP RUN.
       END PROGRAM PB970PTR02.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB970PTRC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 LAST-IMAGE PIC X(8) VALUE SPACES.
       01 KIND       PIC X(13).
       01 AGE        PIC X(4).
       LINKAGE SECTION.
       01 L PIC X(8).
       PROCEDURE DIVISION USING L.
           IF L = LOW-VALUES
               MOVE "NULL-IMAGE" TO KIND
           ELSE
               MOVE "NONNULL-IMAGE" TO KIND
           END-IF
           IF L = LAST-IMAGE
               MOVE "SAME" TO AGE
           ELSE
               MOVE "DIFF" TO AGE
           END-IF
           DISPLAY "C " KIND " " AGE
           MOVE L TO LAST-IMAGE
           MOVE ALL "Z" TO L
           GOBACK.
       END PROGRAM PB970PTRC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB970PTRN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PREV  USAGE BINARY-DOUBLE VALUE 0.
       01 DIFF  PIC +9(4).
       LINKAGE SECTION.
       01 N USAGE BINARY-DOUBLE.
       PROCEDURE DIVISION USING N.
           IF PREV = 0
               DISPLAY "N FIRST"
           ELSE
               COMPUTE DIFF = N - PREV
               DISPLAY "N DIFF " DIFF
           END-IF
           MOVE N TO PREV
           GOBACK.
       END PROGRAM PB970PTRN.
