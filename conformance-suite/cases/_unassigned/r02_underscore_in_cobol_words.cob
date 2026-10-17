*>--- conformance-case
*> id: r02_underscore_in_cobol_words
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.3.2.1: "Each character of a COBOL word ... shall be selected from
      *> the set of basic letters, basic digits, extended letters, and the basic
      *> special characters HYPHEN AND UNDERSCORE. The hyphen or underscore shall
      *> not appear as the first or last character in such words."
      *>
      *> The underscore is a COBOL-2002 introduction (the '85 word character set
      *> is hyphen-only), so `01 MY_NAME PIC X(6).` is legal at three of the four
      *> editions this compiler targets - and it did not parse at ANY of them
      *> (fix-queue R02). It died as `no viable alternative at input 'MY_'`, which
      *> is an unexplained rejection rather than a named edition diagnostic; the
      *> COBOL-85 rejection is now the construct gate user-word-underscore-2002
      *> (COBOLNET0900), pinned by the version matrix.
      *>
      *> WATCH THE SECOND HALF OF THAT SENTENCE. "nor last" held only in the
      *> ALPHA-start lexer alternative; the two DIGIT-start alternatives ended in
      *> a `*` over the separator class, so `1A-` and `42-X-` were ACCEPTED - a
      *> pre-existing hole for the HYPHEN that the underscore would have silently
      *> inherited. Both are rejected now (negative fixture
      *> r02-word-trailing-separator), which is why this golden asserts the legal
      *> digit-start shapes explicitly: they are what that fix must not break.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R02_UNDERSCORE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MY_NAME    PIC X(6) VALUE "hello!".
       01 MIXED_A-B  PIC 9(3) VALUE 42.
       01 TBL_A.
          05 EL_M    PIC X(3) OCCURS 3 TIMES.
       01 IDX_1      PIC 9    VALUE 2.
      *> a user word that merely BEGINS with a reserved word: the 8.9 funnel must
      *> not flag it, and the underscore must not make it look like one.
       01 MOVE_X     PIC X(4) VALUE "keep".
      *> the legal digit-start shapes, which the "nor last" fix must leave alone
       01 42-DATANAMES PIC X(3) VALUE "dn ".
       01 11A          PIC X(3) VALUE "11a".
       01 R  PIC X(6).
       01 L  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN_PARA_1.
           DISPLAY "1=[" MY_NAME "]".
           DISPLAY "2=" MIXED_A-B.
      *> subscripting through the SUBSCRIPT-mode twin, which shares the fragment.
      *> All three elements are set first so the read below is unambiguous - an
      *> uninitialised element would print spaces and read like a failure.
           MOVE "aaa" TO EL_M(1).
           MOVE "bbb" TO EL_M(2).
           MOVE "ccc" TO EL_M(3).
      *> read with an UNDERSCORED subscript variable (IDX_1 = 2 -> "bbb")
           MOVE EL_M(IDX_1) TO R.
           DISPLAY "3=[" R "]".
      *> write through the same underscored subscript, then read back literally
           MOVE "xyz" TO EL_M(IDX_1).
           MOVE EL_M(2) TO R.
           DISPLAY "4=[" R "]".
      *> an underscored name as an intrinsic argument
           MOVE FUNCTION UPPER-CASE(MOVE_X) TO R.
           DISPLAY "5=[" R "]".
           MOVE FUNCTION LENGTH(MOVE_X) TO L.
           DISPLAY "6=" L.
      *> the digit-start controls
           DISPLAY "7=[" 42-DATANAMES "]".
           DISPLAY "8=[" 11A "]".
      *> a PARAGRAPH name carrying an underscore, and PROGRAM-ID above too
           PERFORM SUB_PARA_2.
           STOP RUN.
       SUB_PARA_2.
           DISPLAY "9=paragraph".
