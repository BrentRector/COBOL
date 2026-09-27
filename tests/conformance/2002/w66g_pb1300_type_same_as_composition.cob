      *> kb/Work PB1300 -- TYPE / SAME AS transfer the WHOLE described
      *> clause set, including BASED and a VALUE-implied PICTURE, and
      *> SAME AS takes a group's USAGE / SIGN only from the group kinds
      *> its rules name.
      *> cite.py --check 13.18.57.4 "excluding the level-number, name,
      *>   alignment, and the GLOBAL, SELECT WHEN, and TYPEDEF clauses
      *>   specified for type-name-1" -> OK §13.18.57.4 1)
      *> cite.py --check 13.18.58.4 "All other data description clauses
      *>   and subordinate data descriptions are assumed by data defined
      *>   using the type-name" -> OK §13.18.58.4 3)
      *> cite.py --check 13.18.57.4 "When the description of type-name-1
      *>   includes an implicit PICTURE clause derived from a VALUE
      *>   clause, that implicit PICTURE clause becomes part of the
      *>   description of the subject of the entry" -> OK §13.18.57.4 3)
      *> cite.py --check 13.18.49.4 "If an alphanumeric group item or
      *>   strongly-typed group item to which data-name-1 is subordinate
      *>   contains a USAGE clause" -> OK §13.18.49.4 3)
      *> cite.py --check 13.18.49.4 "If an alphanumeric group item,
      *>   national group item, or strongly-typed group item to which
      *>   data-name-1 is subordinate contains a SIGN clause"
      *>   -> OK §13.18.49.4 5)
      *> DERIVATION:
      *>  BASED is in neither GR-1 exclusion list, so X (TYPE T, T BASED)
      *>  and S (SAME AS B, B BASED) are based: after SET ADDRESS OF they
      *>  ARE Y's storage -> X=[HELLO]; MOVE "WORLD" TO X -> Y=[WORLD];
      *>  S=[WORLD].
      *>  U's implicit PICTURE is X(4) (SR9: the length of "ABCD"), and
      *>  GR3 makes it W's: W holds "Q" padded -> W=[Q   ] LEN 4;
      *>  V (no own VALUE) -> V=[ABCD] LEN 4.
      *>  TG's member M implies X(2); R reproduces it -> R=[XY7] LEN 3.
      *>  U2 implies X(2). W2's own literal "QRSTUV" is 6 characters;
      *>  §13.18.63.3 SR4 bounds a literal by "the size indicated by an
      *>  explicit PICTURE clause" and W2's is implicit, so no syntax
      *>  rule applies and the value initializes the 2-character item
      *>  (DETERMINATION, kb/Work PB1300) -> W2=[QR] LEN 2.
      *>  AE is under an ALPHANUMERIC group with USAGE COMP-5, so GR3
      *>  gives SA that usage: PIC 9(4) COMP-5 -> BYTE-LENGTH 2.
      *>  E is under H, a group subordinate to a GROUP-USAGE NATIONAL
      *>  group, so H is a NATIONAL group (§13.18.29.3 SR3) and GR3 does
      *>  not carry its USAGE: ES is E's own PIC 9(3), usage display
      *>  -> BYTE-LENGTH 3 (E itself is national: 6).
      *>  NE is under a NATIONAL group with SIGN LEADING SEPARATE; GR5
      *>  names national groups, so NS is PIC S9(3) SIGN LEADING
      *>  SEPARATE, usage display -> BYTE-LENGTH 4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W66GPB13.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T TYPEDEF BASED PIC X(5).
       01  X TYPE T.
       01  B BASED PIC X(5).
       01  S SAME AS B.
       01  Y               PIC X(5) VALUE "HELLO".
       01  U TYPEDEF VALUE "ABCD".
       01  W TYPE U VALUE "Q".
       01  V TYPE U.
       01  TG TYPEDEF.
           05  M VALUE "XY".
           05  N           PIC 9 VALUE 7.
       01  R TYPE TG.
       01  U2 TYPEDEF VALUE "AB".
       01  W2 TYPE U2 VALUE "QRSTUV".
       01  A USAGE COMP-5.
           05  AE          PIC 9(4).
       01  SA SAME AS AE.
       01  G GROUP-USAGE NATIONAL.
           05  H USAGE NATIONAL.
               10  E       PIC 9(3).
       01  ES SAME AS E.
       01  NG GROUP-USAGE NATIONAL SIGN LEADING SEPARATE.
           05  NE          PIC S9(3).
       01  NS SAME AS NE.
       PROCEDURE DIVISION.
           SET ADDRESS OF X TO ADDRESS OF Y.
           DISPLAY "X=[" X "]".
           MOVE "WORLD" TO X.
           DISPLAY "Y=[" Y "]".
           SET ADDRESS OF S TO ADDRESS OF Y.
           DISPLAY "S=[" S "]".
           DISPLAY "W=[" W "] LEN " FUNCTION LENGTH(W).
           DISPLAY "V=[" V "] LEN " FUNCTION LENGTH(V).
           DISPLAY "R=[" R "] LEN " FUNCTION LENGTH(R).
           DISPLAY "W2=[" W2 "] LEN " FUNCTION LENGTH(W2).
           DISPLAY "SA " FUNCTION BYTE-LENGTH(SA).
           DISPLAY "E " FUNCTION BYTE-LENGTH(E)
               " ES " FUNCTION BYTE-LENGTH(ES).
           DISPLAY "NS " FUNCTION BYTE-LENGTH(NS).
           STOP RUN.
