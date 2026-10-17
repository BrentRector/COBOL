*>--- conformance-case
*> id: pb177_method_group_boundary
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB177 arm A. THE OO METHOD BOUNDARY'S GROUP-IMAGE CROSSING.
      *>
      *> §14.2.3 GR8: "If the argument is passed by reference, the activated runtime element operates as if the
      *> formal parameter occupies the same storage area as the argument" - which COBOL.NET realizes through the
      *> generated AsImage()/FromImage() round trip. §14.9.23.4 GR8 delivers the RETURNING item the same way.
      *> So every leaf value the invoker sets must come back byte-identical, whatever the leaf's usage.
      *>
      *> THE DEFECT: OoEmitter.EmitMethod spelled `{root}.FromImage({param})` and `{formal}.AsImage()` itself,
      *> with NO capability consult - while RecordStructEmitter emits that codec for exactly ElementImageCapable
      *> items. A method DECLARING an imageless group formal therefore failed BACKEND compilation (CS1061), and
      *> nothing invoked it: DataBinder.Oo applies no image screen, and the pair comparator of the time
      *> (OoConformance.DescriptionMismatch, deleted by kb/Work PB3101) had a !formal.IsImageCapable arm
      *> that ran only when an INVOKE or an override/implements PAIR existed. The three
      *> sites now route through PlaceRenderer, THE ONE channel; held by BoundaryImageChannelTests.
      *>
      *> LEG 1 is also the REGRESSION NET for a repro that DISSOLVED: PB177's note named `05 N USAGE INDEX` as
      *> its crash shape twice, but PicInfo.IndexItem is (Numeric, Usage.Index) and ByteForm maps Usage.Index to
      *> Binary, so HasImageByteForm is TRUE and an INDEX-leaf group has been fully image-capable since R40.
      *> The formal here spans EVERY leaf kind the R40 + PB164 widening admits, so the dissolution stays proved.
      *> LEGS 2 and 3 are the SURVIVING live repros, compiled but never invoked - the pointer/object-class leaf
      *> (PicCategory.Pointer is in neither ElementImageCapable list; carried under a STRONG TYPEDEF because
      *> 13.18.60.3 SR14 admits a subordinate pointer usage nowhere else - see LEG 2) and the variable-length group
      *> (IsImageCapable short-circuits on the dynamic axis). Both crashed the backend before this fix; both now
      *> emit the documented Tier-C loud in a body nobody runs, so the compilation group builds.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB177MGB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CMGB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CMGB.
       01 T.
          05 TE PIC X OCCURS 9 INDEXED BY IX.
       01 G.
          05 GX PIC X(3).
          05 GC PIC S9(4) COMP.
          05 GP PIC S9(5) PACKED-DECIMAL.
          05 GF COMP-1.
          05 GN USAGE INDEX.
       01 H.
          05 HX PIC X(3).
          05 HC PIC S9(4) COMP.
          05 HP PIC S9(5) PACKED-DECIMAL.
          05 HF COMP-1.
          05 HN USAGE INDEX.
       01 WS-D PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CMGB "NEW" RETURNING O.
           MOVE "abc" TO GX.
           MOVE -1234 TO GC.
           MOVE -12345 TO GP.
           MOVE 2.5 TO GF.
           SET IX TO 7.
           SET GN TO IX.
           INVOKE O "XFER" USING G RETURNING H.
      *> §14.2.3 GR8 / §14.9.23.4 GR8 - the round trip is byte-identical, leaf kind by leaf kind. Compared
      *> rather than DISPLAYed: §14.9.11.4 GR1 leaves the FORM of a signed COMP / PACKED / float display image
      *> to the implementor, and this golden is about the CROSSING, not about that latitude.
           IF HX = "abc"  DISPLAY "X=OK" ELSE DISPLAY "X=WRONG" END-IF.
           IF HC = -1234  DISPLAY "C=OK" ELSE DISPLAY "C=WRONG" END-IF.
           IF HP = -12345 DISPLAY "P=OK" ELSE DISPLAY "P=WRONG" END-IF.
           IF HF = 2.5    DISPLAY "F=OK" ELSE DISPLAY "F=WRONG" END-IF.
           MOVE 0 TO WS-D.
           SET IX TO HN.
           IF IX = 7 MOVE 1 TO WS-D END-IF.
           DISPLAY "I=" WS-D.
           STOP RUN.
       END PROGRAM PB177MGB.

       IDENTIFICATION DIVISION.
       CLASS-ID. CMGB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.

       METHOD-ID. XFER.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG.
          05 LGX PIC X(3).
          05 LGC PIC S9(4) COMP.
          05 LGP PIC S9(5) PACKED-DECIMAL.
          05 LGF COMP-1.
          05 LGN USAGE INDEX.
       01 LH.
          05 LHX PIC X(3).
          05 LHC PIC S9(4) COMP.
          05 LHP PIC S9(5) PACKED-DECIMAL.
          05 LHF COMP-1.
          05 LHN USAGE INDEX.
       PROCEDURE DIVISION USING LG RETURNING LH.
       MAIN-P.
           MOVE LGX TO LHX.
           MOVE LGC TO LHC.
           MOVE LGP TO LHP.
           MOVE LGF TO LHF.
           SET LHN TO LGN.
       END METHOD XFER.

      *> LEG 2 - the surviving pointer-leaf repro. NEVER INVOKED: the whole point is that a merely-DECLARED
      *> method must not emit uncompilable C#.
      *> The pointer leaf sits under a STRONG TYPEDEF because 13.18.60.3 SR14 (kb/Work PB183) admits a
      *> pointer/object usage at a subordinate level ONLY there - "an elementary data item at level 1 or an
      *> elementary data item subordinate to a type declaration that includes the STRONG phrase". The plain
      *> `01 LP. 05 LPP USAGE POINTER.` this leg was written with is nonconforming source, and the strong
      *> typedef is the ONE conforming spelling of the shape under test. The repro is unchanged: LP is still a
      *> group formal whose leaf is PicCategory.Pointer, in neither ElementImageCapable list.
       METHOD-ID. TIERCPTR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TPTR IS TYPEDEF STRONG.
          05 LPP USAGE POINTER.
          05 LPA PIC X(4).
       01 LP TYPE TPTR.
       PROCEDURE DIVISION USING LP.
       MAIN-P.
           CONTINUE.
       END METHOD TIERCPTR.

      *> LEG 3 - the surviving variable-length repro, on the RETURNING side (§14.9.23.4 GR8's delivery, the
      *> third of the three sites). Also never invoked.
       METHOD-ID. TIERCVL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LV.
          05 LVD PIC X DYNAMIC LENGTH.
          05 LVA PIC X(4).
       PROCEDURE DIVISION RETURNING LV.
       MAIN-P.
           CONTINUE.
       END METHOD TIERCVL.

       END OBJECT.
       END CLASS CMGB.
