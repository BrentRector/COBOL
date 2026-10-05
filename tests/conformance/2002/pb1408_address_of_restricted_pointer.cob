      *> kb/Work PB1408 -- the TYPE OF identifier-1 in ADDRESS OF identifier-1.
      *> 8.4.3.11.4 GR2: "If identifier-1 is a strongly-typed group item or a
      *> restricted data-pointer, the data-address-identifier is a restricted
      *> data-pointer that is restricted to the type of identifier-1."
      *> RP is a restricted data-pointer declared TYPE PT (13.18.60.3 SR18
      *> admits the restriction only in a type declaration), so it is a typed
      *> item of type PT: ADDRESS OF RP is restricted to PT -- not to T-REC,
      *> the type of the item RP's VALUE addresses.
      *> 14.9.39.3 SR19, first sentence: PP is restricted to PT, so
      *> identifier-6 (ADDRESS OF RP) shall be restricted to the same type:
      *> legal.  Second/last sentence: BP is a based item TYPE PT, so a pointer
      *> restricted to PT may address it.  The inner address (RP, restricted to
      *> T-REC through ADDRESS OF S, a strongly-typed group) reaches BS.
      *> 14.8.2.3.2: "if either is a restricted pointer, both shall be
      *> restricted and of the same type" -- ADDRESS OF RP into a formal
      *> restricted to PT, and ADDRESS OF S into one restricted to T-REC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1408OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 T-X PIC X(2).
          05 T-G.
             10 T-A PIC X(4).
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       01 PPT IS TYPEDEF USAGE POINTER TO PT.
       01 S TYPE T-REC.
       01 RP TYPE PT.
       01 PP TYPE PPT.
       01 BP TYPE PT BASED.
       01 BS TYPE T-REC BASED.
       01 G-T IS TYPEDEF.
          05 GP TYPE PT.
       01 G TYPE G-T.
       01 PP2 TYPE PPT.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "AB" TO T-X OF S
           MOVE "WXYZ" TO T-A OF T-G OF S
           SET RP TO ADDRESS OF S
           SET PP TO ADDRESS OF RP
           SET ADDRESS OF BP TO PP
           SET ADDRESS OF BS TO BP
      *> GP is a restricted data-pointer NESTED in a group (its TYPE clause
      *> is its own, so its type is still PT): its address is legal into PP2.
           SET PP2 TO ADDRESS OF GP OF G
           DISPLAY "X=" T-X OF BS " A=" T-A OF T-G OF BS
           CALL "ONPT" AS NESTED USING BY CONTENT ADDRESS OF RP
           CALL "ONREC" AS NESTED USING BY CONTENT ADDRESS OF S
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ONPT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 T-X PIC X(2).
          05 T-G.
             10 T-A PIC X(4).
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       01 PPT IS TYPEDEF USAGE POINTER TO PT.
       LINKAGE SECTION.
       01 F TYPE PPT.
       PROCEDURE DIVISION USING F.
           DISPLAY "ONPT"
           GOBACK.
       END PROGRAM ONPT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ONREC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 T-X PIC X(2).
          05 T-G.
             10 T-A PIC X(4).
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       LINKAGE SECTION.
       01 FR TYPE PT.
       PROCEDURE DIVISION USING FR.
           DISPLAY "ONREC"
           GOBACK.
       END PROGRAM ONREC.
       END PROGRAM PB1408OK.
