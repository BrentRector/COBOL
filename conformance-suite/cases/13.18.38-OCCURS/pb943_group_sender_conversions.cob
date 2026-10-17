*>--- conformance-case
*> id: pb943_group_sender_conversions
*> rules: [GR-13.18.38.4-8, GR-14.9.25.4-2, GR-14.9.25.4-3, GR-14.9.25.4-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB943 + PB944 - HOW A GROUP SENDER IS CONVERTED WHEN THE RECEIVER IS NOT ALPHANUMERIC.
      *> Every expected line below is derived from the rule text (or a named implementor determination),
      *> never from a run.
      *>
      *> ISO 14.9.25.4 GR1 - "If identifier-1 is a zero-length item, it is as if literal-1 were specified as a
      *>   zero-length literal." GR2 - "If literal-1 is an alphanumeric or national zero-length literal and the
      *>   receiving operand is other than a dynamic-length elementary item, literal-1 is treated as if it were
      *>   the figurative constant SPACE." GR3 - the same for a boolean zero-length literal and ZERO.
      *>   GR2/GR3 name a SET of receivers - every one but a dynamic-length elementary item - so the zero-length
      *>   legs below use national, edited and boolean receivers, not only the numeric ones PB425/PB896 cover.
      *> ISO 8.5.4 item 1 - a group containing only an occurs-depending table at zero occurrences is a
      *>   zero-length item; 13.18.38.4 GR8 a) - otherwise only the CURRENT extent of the table is sent.
      *> ISO 13.18.29.4 GR3 - a group with no GROUP-USAGE clause is an ALPHANUMERIC group, whatever its
      *>   leaves; GR1 b) / GR2 b) - a GROUP-USAGE BIT / NATIONAL group is treated as an elementary boolean /
      *>   national item of PICTURE 1(m) / N(m).
      *> ISO 14.9.25.4 GR4 - a move that is not elementary is "an alphanumeric to alphanumeric elementary move,
      *>   except that there is no conversion of data from one form of internal representation to another".
      *> ISO 14.9.25.4 GR6 a) - "If the sending item is of class boolean, its boolean value shall be moved";
      *>   GR6 d) 3 - a national sender to a numeric receiver "is treated as if it were an unsigned integer".
      *> IMPLEMENTOR DETERMINATIONS USED: D-N1 (docs/CONFORMANCE.md A.1-33) - a national position is two bytes,
      *>   UTF-16BE, in storage (13.18.60.4 GR8); D-B2 - SPACE against a boolean receiver is the boolean zero.
      *>
      *> EDITION: --std 2002, where USAGE NATIONAL, USAGE BIT and GROUP-USAGE were introduced.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC:
      *>
      *> L1NAT=YES   N = 0: NG (an ALPHANUMERIC group of one national table) is zero-length; GR1+GR2 store
      *>             SPACE, so the PIC N(4) receiver holds four national spaces. (Measured before: the ODO
      *>             extent was computed in characters against a byte image, so half the maximum image -
      *>             NUL, space, NUL, space - was sent.)
      *> L1ALN=YES   The same statement into PIC X(4): four spaces.
      *> L2IMG=YES   N = 2: the current extent is 2 national positions = 4 bytes (D-N1), sent as GR4's
      *>             uninterpreted image X"00410042" and space-filled to 8 (14.6.8).
      *> L3AED=[  /  ] N = 0: AG is zero-length; GR2's SPACE into PIC XX/XX is an ELEMENTARY move and is
      *>             edited (GR6), so the insertion '/' survives. A group move would not edit.
      *> L3BIT=[000] The same SPACE into PIC 1(3) USAGE BIT: D-B2's boolean zero.
      *> L4A0=[000]  BG is a GROUP-USAGE BIT group; at N = 0 it is a zero-length BOOLEAN item, so GR3
      *>             substitutes ZERO, repeated over PIC X(3).
      *> L4N0=YES    ...and over PIC N(3): three national zeros.
      *> L4A1=[1  ]  N = 1 and BE(1) = B"1": an elementary boolean move of its one boolean value (GR6 a)),
      *>             left-justified and space-filled (14.6.8).
      *> L5NUM=012   NG2 is a GROUP-USAGE NATIONAL group holding N"12": GR6 d) 3 decodes its two national
      *>             positions as the unsigned integer 12 (never its four UTF-16BE bytes).
      *> L5NED=YES   N = 0: GR2's SPACE into PIC NN/NN is edited: national space, space, '/', space, space.
      *> L6BIT=[010] N = 2, AG = "AB": GR4 moves the byte X"41" into the one-byte area of PIC 1(3) USAGE BIT
      *>             without conversion; its first three bits are 0 1 0.
      *> L6NAT=YES   ...and into PIC N(2) (four bytes): 41 42 20 20 - no conversion, so the area holds the
      *>             national characters U+4142 U+2020 (D-N1), not "AB" widened.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB943GRPCNV02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N               PIC 9 VALUE 0.
       01 NG.
          05 NE           PIC N USAGE NATIONAL OCCURS 0 TO 4 DEPENDING ON N.
       01 AG.
          05 AE           PIC X OCCURS 0 TO 4 DEPENDING ON N.
       01 BG GROUP-USAGE BIT.
          05 BE           PIC 1 OCCURS 0 TO 8 DEPENDING ON N.
       01 NG2 GROUP-USAGE NATIONAL.
          05 NE2          PIC N OCCURS 0 TO 4 DEPENDING ON N.
       01 R-NAT           PIC N(4) VALUE N"ZZZZ".
       01 R-ALN           PIC X(4) VALUE "ZZZZ".
       01 R-X8            PIC X(8).
       01 R-AED           PIC XX/XX VALUE "ZZ/ZZ".
       01 R-BIT           PIC 1(3) USAGE BIT VALUE B"111".
       01 R-A3            PIC X(3) VALUE "QQQ".
       01 R-N3            PIC N(3) VALUE N"QQQ".
       01 R-NUM           PIC 9(3) VALUE 777.
       01 R-NED           PIC NN/NN.
       01 R-N2            PIC N(2).
       PROCEDURE DIVISION.
      *> ---- L1: a zero-length alphanumeric group of national leaves
           MOVE 0 TO N
           MOVE NG TO R-NAT
           MOVE NG TO R-ALN
           IF R-NAT = SPACES DISPLAY "L1NAT=YES" ELSE DISPLAY "L1NAT=NO"
           END-IF
           IF R-ALN = SPACES DISPLAY "L1ALN=YES" ELSE DISPLAY "L1ALN=NO"
           END-IF
      *> ---- L2: its current extent, in bytes
           MOVE 2 TO N
           MOVE N"A" TO NE(1)
           MOVE N"B" TO NE(2)
           MOVE NG TO R-X8
           IF R-X8 = X"0041004220202020" DISPLAY "L2IMG=YES"
           ELSE DISPLAY "L2IMG=NO" END-IF
      *> ---- L3: a zero-length group into an edited and a boolean receiver
           MOVE 0 TO N
           MOVE AG TO R-AED
           MOVE AG TO R-BIT
           DISPLAY "L3AED=[" R-AED "]"
           DISPLAY "L3BIT=[" R-BIT "]"
      *> ---- L4: a bit group, zero-length and not
           MOVE AG TO R-A3
           MOVE BG TO R-A3
           MOVE BG TO R-N3
           DISPLAY "L4A0=[" R-A3 "]"
           IF R-N3 = N"000" DISPLAY "L4N0=YES" ELSE DISPLAY "L4N0=NO"
           END-IF
           MOVE 1 TO N
           MOVE B"1" TO BE(1)
           MOVE BG TO R-A3
           DISPLAY "L4A1=[" R-A3 "]"
      *> ---- L5: a national group into numeric and national-edited
           MOVE 2 TO N
           MOVE N"1" TO NE2(1)
           MOVE N"2" TO NE2(2)
           MOVE NG2 TO R-NUM
           DISPLAY "L5NUM=" R-NUM
           MOVE 0 TO N
           MOVE NG2 TO R-NED
           IF R-NED = N"  /  " DISPLAY "L5NED=YES" ELSE DISPLAY "L5NED=NO"
           END-IF
      *> ---- L6: GR4's group move into a bit and a national receiver
           MOVE 2 TO N
           MOVE "A" TO AE(1)
           MOVE "B" TO AE(2)
           MOVE AG TO R-BIT
           MOVE AG TO R-N2
           DISPLAY "L6BIT=[" R-BIT "]"
           IF R-N2 = NX"41422020" DISPLAY "L6NAT=YES"
           ELSE DISPLAY "L6NAT=NO" END-IF
           STOP RUN.
