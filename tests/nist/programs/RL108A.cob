000100 IDENTIFICATION DIVISION.                                         RL1084.2
000200 PROGRAM-ID.                                                      RL1084.2
000300     RL108A.                                                      RL1084.2
000400****************************************************************  RL1084.2
000500*                                                              *  RL1084.2
000600*    VALIDATION FOR:-                                          *  RL1084.2
000700*                                                              *  RL1084.2
000800*    "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".RL1084.2
000900*                                                              *  RL1084.2
001000*    "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".RL1084.2
001100*                                                              *  RL1084.2
001200****************************************************************  RL1084.2
001300*                                                              *  RL1084.2
001400*      X-CARDS USED BY THIS PROGRAM ARE :-                     *  RL1084.2
001500*                                                              *  RL1084.2
001600*        X-55  - SYSTEM PRINTER NAME.                          *  RL1084.2
001700*        X-61  - "LITERAL" IN "ASSIGN TO" CLAUSE FOR           *  RL1084.2
001800*                RELATIVE I-O DATA FILE.                       *  RL1084.2
001900*        X-69  - ADDITIONAL "VALUE OF" CLAUSE.                 *  RL1084.2
002000*        X-74  - VALUE OF IMPLEMENTOR-NAME.                    *  RL1084.2
002100*        X-75  - OBJECT OF "VALUE" CLAUSE.                     *  RL1084.2
002200*        X-82  - SOURCE COMPUTER NAME.                         *  RL1084.2
002300*        X-83  - OBJECT COMPUTER NAME.                         *  RL1084.2
002400*                                                              *  RL1084.2
002500****************************************************************  RL1084.2
002600*RL108A *                                                         RL1084.2
002700***************************************************               RL1084.2
002800*GENERAL:    THIS RUN UNIT IS THE FIRST OF A SERIES WHICH         RL1084.2
002900*            PROCESSES A RELATIVE I-O FILE.  THE FUNCTION OF THIS RL1084.2
003000*            PROGRAM IS TO CREATE A RELATIVE FILE SEQUENTIALLY    RL1084.2
003100*            (ACCESS MODE SEQUENTIAL) AND VERIFY THAT IT WAS      RL1084.2
003200*            CREATED CORRECTLY.  THE FILE IS IDENTIFED AS "RL-FS1"RL1084.2
003300*            AND IS PASSED TO SUBSEQUENT RUN UNITS FOR PROCESSING.RL1084.2
003400*                                                                 RL1084.2
003500*    THIS PROGRAM TESTS THE NEW SYNTACTICAL CONSTRUCTS AND        RL1084.2
003600*    SEMANTIC ACTIONS OF THE FOLLOWING ELEMENTS:                  RL1084.2
003700*        - ASSIGN                                                 RL1084.2
003800*        - ORGANIZATION                                           RL1084.2
003900*        - ACCESS                                                 RL1084.2
004000*        - READ                                                   RL1084.2
004100*        - WRITE                                                  RL1084.2
004200****************************************************************  RL1084.2
004300 ENVIRONMENT DIVISION.                                            RL1084.2
004400 CONFIGURATION SECTION.                                           RL1084.2
004500 SOURCE-COMPUTER.                                                 RL1084.2
004600     XXXXX082.                                                    RL1084.2
004700 OBJECT-COMPUTER.                                                 RL1084.2
004800     XXXXX083.                                                    RL1084.2
004900 INPUT-OUTPUT SECTION.                                            RL1084.2
005000 FILE-CONTROL.                                                    RL1084.2
005100     SELECT PRINT-FILE ASSIGN TO                                  RL1084.2
005200     XXXXX055.                                                    RL1084.2
005300     SELECT   RL-FS1 ASSIGN TO                                    RL1084.2
005400     XXXXX061                                                     RL1084.2
005500             ORGANIZATION RELATIVE                                RL1084.2
005600             ACCESS SEQUENTIAL.                                   RL1084.2
005700*                                                                 RL1084.2
005800 DATA DIVISION.                                                   RL1084.2
005900 FILE SECTION.                                                    RL1084.2
006000 FD  PRINT-FILE.                                                  RL1084.2
006100 01  PRINT-REC PICTURE X(120).                                    RL1084.2
006200 01  DUMMY-RECORD PICTURE X(120).                                 RL1084.2
006300 FD  RL-FS1                                                       RL1084.2
006400     LABEL RECORDS STANDARD                                       RL1084.2
006500C    VALUE OF                                                     RL1084.2
006600C    XXXXX074                                                     RL1084.2
006700C    IS                                                           RL1084.2
006800C    XXXXX075                                                     RL1084.2
006900G    XXXXX069                                                     RL1084.2
007000     BLOCK CONTAINS 1 RECORDS                                     RL1084.2
007100     RECORD CONTAINS 120 CHARACTERS.                              RL1084.2
007200 01  RL-FS1R1-F-G-120.                                            RL1084.2
007300     02 FILLER PIC X(120).                                        RL1084.2
007400 WORKING-STORAGE SECTION.                                         RL1084.2
007500 01  WRK-CS-09V00 PIC S9(9) USAGE COMP VALUE ZERO.                RL1084.2
007600 01  FILE-RECORD-INFORMATION-REC.                                 RL1084.2
007700     03 FILE-RECORD-INFO-SKELETON.                                RL1084.2
007800        05 FILLER                 PICTURE X(48)       VALUE       RL1084.2
007900             "FILE=      ,RECORD=      /0,RECNO=000000,UPDT=00".  RL1084.2
008000        05 FILLER                 PICTURE X(46)       VALUE       RL1084.2
008100             ",ODO=0000,PGM=     ,LRECL=000000,BLKSIZ  =0000".    RL1084.2
008200        05 FILLER                 PICTURE X(26)       VALUE       RL1084.2
008300             ",LFIL=000000,ORG=  ,LBLR= ".                        RL1084.2
008400        05 FILLER                 PICTURE X(37)       VALUE       RL1084.2
008500             ",RECKEY=                             ".             RL1084.2
008600        05 FILLER                 PICTURE X(38)       VALUE       RL1084.2
008700             ",ALTKEY1=                             ".            RL1084.2
008800        05 FILLER                 PICTURE X(38)       VALUE       RL1084.2
008900             ",ALTKEY2=                             ".            RL1084.2
009000        05 FILLER                 PICTURE X(7)        VALUE SPACE.RL1084.2
009100     03 FILE-RECORD-INFO          OCCURS  10  TIMES.              RL1084.2
009200        05 FILE-RECORD-INFO-P1-120.                               RL1084.2
009300           07 FILLER              PIC X(5).                       RL1084.2
009400           07 XFILE-NAME           PIC X(6).                      RL1084.2
009500           07 FILLER              PIC X(8).                       RL1084.2
009600           07 XRECORD-NAME         PIC X(6).                      RL1084.2
009700           07 FILLER              PIC X(1).                       RL1084.2
009800           07 REELUNIT-NUMBER     PIC 9(1).                       RL1084.2
009900           07 FILLER              PIC X(7).                       RL1084.2
010000           07 XRECORD-NUMBER       PIC 9(6).                      RL1084.2
010100           07 FILLER              PIC X(6).                       RL1084.2
010200           07 UPDATE-NUMBER       PIC 9(2).                       RL1084.2
010300           07 FILLER              PIC X(5).                       RL1084.2
010400           07 ODO-NUMBER          PIC 9(4).                       RL1084.2
010500           07 FILLER              PIC X(5).                       RL1084.2
010600           07 XPROGRAM-NAME        PIC X(5).                      RL1084.2
010700           07 FILLER              PIC X(7).                       RL1084.2
010800           07 XRECORD-LENGTH       PIC 9(6).                      RL1084.2
010900           07 FILLER              PIC X(7).                       RL1084.2
011000           07 CHARS-OR-RECORDS    PIC X(2).                       RL1084.2
011100           07 FILLER              PIC X(1).                       RL1084.2
011200           07 XBLOCK-SIZE          PIC 9(4).                      RL1084.2
011300           07 FILLER              PIC X(6).                       RL1084.2
011400           07 RECORDS-IN-FILE     PIC 9(6).                       RL1084.2
011500           07 FILLER              PIC X(5).                       RL1084.2
011600           07 XFILE-ORGANIZATION   PIC X(2).                      RL1084.2
011700           07 FILLER              PIC X(6).                       RL1084.2
011800           07 XLABEL-TYPE          PIC X(1).                      RL1084.2
011900        05 FILE-RECORD-INFO-P121-240.                             RL1084.2
012000           07 FILLER              PIC X(8).                       RL1084.2
012100           07 XRECORD-KEY          PIC X(29).                     RL1084.2
012200           07 FILLER              PIC X(9).                       RL1084.2
012300           07 ALTERNATE-KEY1      PIC X(29).                      RL1084.2
012400           07 FILLER              PIC X(9).                       RL1084.2
012500           07 ALTERNATE-KEY2      PIC X(29).                      RL1084.2
012600           07 FILLER              PIC X(7).                       RL1084.2
012700 01  TEST-RESULTS.                                                RL1084.2
012800     02 FILLER                   PIC X      VALUE SPACE.          RL1084.2
012900     02 FEATURE                  PIC X(20)  VALUE SPACE.          RL1084.2
013000     02 FILLER                   PIC X      VALUE SPACE.          RL1084.2
013100     02 P-OR-F                   PIC X(5)   VALUE SPACE.          RL1084.2
013200     02 FILLER                   PIC X      VALUE SPACE.          RL1084.2
013300     02  PAR-NAME.                                                RL1084.2
013400       03 FILLER                 PIC X(19)  VALUE SPACE.          RL1084.2
013500       03  PARDOT-X              PIC X      VALUE SPACE.          RL1084.2
013600       03 DOTVALUE               PIC 99     VALUE ZERO.           RL1084.2
013700     02 FILLER                   PIC X(8)   VALUE SPACE.          RL1084.2
013800     02 RE-MARK                  PIC X(61).                       RL1084.2
013900 01  TEST-COMPUTED.                                               RL1084.2
014000     02 FILLER                   PIC X(30)  VALUE SPACE.          RL1084.2
014100     02 FILLER                   PIC X(17)  VALUE                 RL1084.2
014200            "       COMPUTED=".                                   RL1084.2
014300     02 COMPUTED-X.                                               RL1084.2
014400     03 COMPUTED-A               PIC X(20)  VALUE SPACE.          RL1084.2
014500     03 COMPUTED-N               REDEFINES COMPUTED-A             RL1084.2
014600                                 PIC -9(9).9(9).                  RL1084.2
014700     03 COMPUTED-0V18 REDEFINES COMPUTED-A   PIC -.9(18).         RL1084.2
014800     03 COMPUTED-4V14 REDEFINES COMPUTED-A   PIC -9(4).9(14).     RL1084.2
014900     03 COMPUTED-14V4 REDEFINES COMPUTED-A   PIC -9(14).9(4).     RL1084.2
015000     03       CM-18V0 REDEFINES COMPUTED-A.                       RL1084.2
015100         04 COMPUTED-18V0                    PIC -9(18).          RL1084.2
015200         04 FILLER                           PIC X.               RL1084.2
015300     03 FILLER PIC X(50) VALUE SPACE.                             RL1084.2
015400 01  TEST-CORRECT.                                                RL1084.2
015500     02 FILLER PIC X(30) VALUE SPACE.                             RL1084.2
015600     02 FILLER PIC X(17) VALUE "       CORRECT =".                RL1084.2
015700     02 CORRECT-X.                                                RL1084.2
015800     03 CORRECT-A                  PIC X(20) VALUE SPACE.         RL1084.2
015900     03 CORRECT-N    REDEFINES CORRECT-A     PIC -9(9).9(9).      RL1084.2
016000     03 CORRECT-0V18 REDEFINES CORRECT-A     PIC -.9(18).         RL1084.2
016100     03 CORRECT-4V14 REDEFINES CORRECT-A     PIC -9(4).9(14).     RL1084.2
016200     03 CORRECT-14V4 REDEFINES CORRECT-A     PIC -9(14).9(4).     RL1084.2
016300     03      CR-18V0 REDEFINES CORRECT-A.                         RL1084.2
016400         04 CORRECT-18V0                     PIC -9(18).          RL1084.2
016500         04 FILLER                           PIC X.               RL1084.2
016600     03 FILLER PIC X(2) VALUE SPACE.                              RL1084.2
016700     03 COR-ANSI-REFERENCE             PIC X(48) VALUE SPACE.     RL1084.2
016800 01  CCVS-C-1.                                                    RL1084.2
016900     02 FILLER  PIC IS X(99)    VALUE IS " FEATURE              PARL1084.2
017000-    "SS  PARAGRAPH-NAME                                          RL1084.2
017100-    "       REMARKS".                                            RL1084.2
017200     02 FILLER                     PIC X(20)    VALUE SPACE.      RL1084.2
017300 01  CCVS-C-2.                                                    RL1084.2
017400     02 FILLER                     PIC X        VALUE SPACE.      RL1084.2
017500     02 FILLER                     PIC X(6)     VALUE "TESTED".   RL1084.2
017600     02 FILLER                     PIC X(15)    VALUE SPACE.      RL1084.2
017700     02 FILLER                     PIC X(4)     VALUE "FAIL".     RL1084.2
017800     02 FILLER                     PIC X(94)    VALUE SPACE.      RL1084.2
017900 01  REC-SKL-SUB                   PIC 9(2)     VALUE ZERO.       RL1084.2
018000 01  REC-CT                        PIC 99       VALUE ZERO.       RL1084.2
018100 01  DELETE-COUNTER                PIC 999      VALUE ZERO.       RL1084.2
018200 01  ERROR-COUNTER                 PIC 999      VALUE ZERO.       RL1084.2
018300 01  INSPECT-COUNTER               PIC 999      VALUE ZERO.       RL1084.2
018400 01  PASS-COUNTER                  PIC 999      VALUE ZERO.       RL1084.2
018500 01  TOTAL-ERROR                   PIC 999      VALUE ZERO.       RL1084.2
018600 01  ERROR-HOLD                    PIC 999      VALUE ZERO.       RL1084.2
018700 01  DUMMY-HOLD                    PIC X(120)   VALUE SPACE.      RL1084.2
018800 01  RECORD-COUNT                  PIC 9(5)     VALUE ZERO.       RL1084.2
018900 01  ANSI-REFERENCE                PIC X(48)    VALUE SPACES.     RL1084.2
019000 01  CCVS-H-1.                                                    RL1084.2
019100     02  FILLER                    PIC X(39)    VALUE SPACES.     RL1084.2
019200     02  FILLER                    PIC X(42)    VALUE             RL1084.2
019300     "OFFICIAL COBOL COMPILER VALIDATION SYSTEM".                 RL1084.2
019400     02  FILLER                    PIC X(39)    VALUE SPACES.     RL1084.2
019500 01  CCVS-H-2A.                                                   RL1084.2
019600   02  FILLER                        PIC X(40)  VALUE SPACE.      RL1084.2
019700   02  FILLER                        PIC X(7)   VALUE "CCVS85 ".  RL1084.2
019800   02  FILLER                        PIC XXXX   VALUE             RL1084.2
019900     "4.2 ".                                                      RL1084.2
020000   02  FILLER                        PIC X(28)  VALUE             RL1084.2
020100            " COPY - NOT FOR DISTRIBUTION".                       RL1084.2
020200   02  FILLER                        PIC X(41)  VALUE SPACE.      RL1084.2
020300                                                                  RL1084.2
020400 01  CCVS-H-2B.                                                   RL1084.2
020500   02  FILLER                        PIC X(15)  VALUE             RL1084.2
020600            "TEST RESULT OF ".                                    RL1084.2
020700   02  TEST-ID                       PIC X(9).                    RL1084.2
020800   02  FILLER                        PIC X(4)   VALUE             RL1084.2
020900            " IN ".                                               RL1084.2
021000   02  FILLER                        PIC X(12)  VALUE             RL1084.2
021100     " HIGH       ".                                              RL1084.2
021200   02  FILLER                        PIC X(22)  VALUE             RL1084.2
021300            " LEVEL VALIDATION FOR ".                             RL1084.2
021400   02  FILLER                        PIC X(58)  VALUE             RL1084.2
021500     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".RL1084.2
021600 01  CCVS-H-3.                                                    RL1084.2
021700     02  FILLER                      PIC X(34)  VALUE             RL1084.2
021800            " FOR OFFICIAL USE ONLY    ".                         RL1084.2
021900     02  FILLER                      PIC X(58)  VALUE             RL1084.2
022000     "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".RL1084.2
022100     02  FILLER                      PIC X(28)  VALUE             RL1084.2
022200            "  COPYRIGHT   1985 ".                                RL1084.2
022300 01  CCVS-E-1.                                                    RL1084.2
022400     02 FILLER                       PIC X(52)  VALUE SPACE.      RL1084.2
022500     02 FILLER  PIC X(14) VALUE IS "END OF TEST-  ".              RL1084.2
022600     02 ID-AGAIN                     PIC X(9).                    RL1084.2
022700     02 FILLER                       PIC X(45)  VALUE SPACES.     RL1084.2
022800 01  CCVS-E-2.                                                    RL1084.2
022900     02  FILLER                      PIC X(31)  VALUE SPACE.      RL1084.2
023000     02  FILLER                      PIC X(21)  VALUE SPACE.      RL1084.2
023100     02 CCVS-E-2-2.                                               RL1084.2
023200         03 ERROR-TOTAL              PIC XXX    VALUE SPACE.      RL1084.2
023300         03 FILLER                   PIC X      VALUE SPACE.      RL1084.2
023400         03 ENDER-DESC               PIC X(44)  VALUE             RL1084.2
023500            "ERRORS ENCOUNTERED".                                 RL1084.2
023600 01  CCVS-E-3.                                                    RL1084.2
023700     02  FILLER                      PIC X(22)  VALUE             RL1084.2
023800            " FOR OFFICIAL USE ONLY".                             RL1084.2
023900     02  FILLER                      PIC X(12)  VALUE SPACE.      RL1084.2
024000     02  FILLER                      PIC X(58)  VALUE             RL1084.2
024100     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".RL1084.2
024200     02  FILLER                      PIC X(13)  VALUE SPACE.      RL1084.2
024300     02 FILLER                       PIC X(15)  VALUE             RL1084.2
024400             " COPYRIGHT 1985".                                   RL1084.2
024500 01  CCVS-E-4.                                                    RL1084.2
024600     02 CCVS-E-4-1                   PIC XXX    VALUE SPACE.      RL1084.2
024700     02 FILLER                       PIC X(4)   VALUE " OF ".     RL1084.2
024800     02 CCVS-E-4-2                   PIC XXX    VALUE SPACE.      RL1084.2
024900     02 FILLER                       PIC X(40)  VALUE             RL1084.2
025000      "  TESTS WERE EXECUTED SUCCESSFULLY".                       RL1084.2
025100 01  XXINFO.                                                      RL1084.2
025200     02 FILLER                       PIC X(19)  VALUE             RL1084.2
025300            "*** INFORMATION ***".                                RL1084.2
025400     02 INFO-TEXT.                                                RL1084.2
025500       04 FILLER                     PIC X(8)   VALUE SPACE.      RL1084.2
025600       04 XXCOMPUTED                 PIC X(20).                   RL1084.2
025700       04 FILLER                     PIC X(5)   VALUE SPACE.      RL1084.2
025800       04 XXCORRECT                  PIC X(20).                   RL1084.2
025900     02 INF-ANSI-REFERENCE           PIC X(48).                   RL1084.2
026000 01  HYPHEN-LINE.                                                 RL1084.2
026100     02 FILLER  PIC IS X VALUE IS SPACE.                          RL1084.2
026200     02 FILLER  PIC IS X(65)    VALUE IS "************************RL1084.2
026300-    "*****************************************".                 RL1084.2
026400     02 FILLER  PIC IS X(54)    VALUE IS "************************RL1084.2
026500-    "******************************".                            RL1084.2
026600 01  CCVS-PGM-ID                     PIC X(9)   VALUE             RL1084.2
026700     "RL108A".                                                    RL1084.2
026800 PROCEDURE DIVISION.                                              RL1084.2
026900 CCVS1 SECTION.                                                   RL1084.2
027000 OPEN-FILES.                                                      RL1084.2
027100     OPEN    OUTPUT PRINT-FILE.                                   RL1084.2
027200     MOVE  CCVS-PGM-ID TO TEST-ID. MOVE CCVS-PGM-ID TO ID-AGAIN.  RL1084.2
027300     MOVE    SPACE TO TEST-RESULTS.                               RL1084.2
027400     PERFORM HEAD-ROUTINE THRU COLUMN-NAMES-ROUTINE.              RL1084.2
027500     MOVE    ZERO TO REC-SKL-SUB.                                 RL1084.2
027600     PERFORM CCVS-INIT-FILE 9 TIMES.                              RL1084.2
027700 CCVS-INIT-FILE.                                                  RL1084.2
027800     ADD     1 TO REC-SKL-SUB.                                    RL1084.2
027900     MOVE    FILE-RECORD-INFO-SKELETON                            RL1084.2
028000          TO FILE-RECORD-INFO (REC-SKL-SUB).                      RL1084.2
028100 CCVS-INIT-EXIT.                                                  RL1084.2
028200     GO TO CCVS1-EXIT.                                            RL1084.2
028300 CLOSE-FILES.                                                     RL1084.2
028400     PERFORM END-ROUTINE THRU END-ROUTINE-13. CLOSE PRINT-FILE.   RL1084.2
028500 TERMINATE-CCVS.                                                  RL1084.2
028600S    EXIT PROGRAM.                                                RL1084.2
028700STERMINATE-CALL.                                                  RL1084.2
028800     STOP     RUN.                                                RL1084.2
028900 INSPT. MOVE "INSPT" TO P-OR-F. ADD 1 TO INSPECT-COUNTER.         RL1084.2
029000 PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.           RL1084.2
029100 FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.          RL1084.2
029200 DE-LETE.  MOVE "*****" TO P-OR-F.  ADD 1 TO DELETE-COUNTER.      RL1084.2
029300     MOVE "****TEST DELETED****" TO RE-MARK.                      RL1084.2
029400 PRINT-DETAIL.                                                    RL1084.2
029500     IF REC-CT NOT EQUAL TO ZERO                                  RL1084.2
029600             MOVE "." TO PARDOT-X                                 RL1084.2
029700             MOVE REC-CT TO DOTVALUE.                             RL1084.2
029800     MOVE     TEST-RESULTS TO PRINT-REC. PERFORM WRITE-LINE.      RL1084.2
029900     IF P-OR-F EQUAL TO "FAIL*"  PERFORM WRITE-LINE               RL1084.2
030000        PERFORM FAIL-ROUTINE THRU FAIL-ROUTINE-EX                 RL1084.2
030100          ELSE PERFORM BAIL-OUT THRU BAIL-OUT-EX.                 RL1084.2
030200     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              RL1084.2
030300     MOVE SPACE TO CORRECT-X.                                     RL1084.2
030400     IF     REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.         RL1084.2
030500     MOVE     SPACE TO RE-MARK.                                   RL1084.2
030600 HEAD-ROUTINE.                                                    RL1084.2
030700     MOVE CCVS-H-1  TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  RL1084.2
030800     MOVE CCVS-H-2A TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  RL1084.2
030900     MOVE CCVS-H-2B TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  RL1084.2
031000     MOVE CCVS-H-3  TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  RL1084.2
031100 COLUMN-NAMES-ROUTINE.                                            RL1084.2
031200     MOVE CCVS-C-1 TO DUMMY-RECORD. PERFORM WRITE-LINE.           RL1084.2
031300     MOVE CCVS-C-2 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL1084.2
031400     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE.        RL1084.2
031500 END-ROUTINE.                                                     RL1084.2
031600     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE 5 TIMES.RL1084.2
031700 END-RTN-EXIT.                                                    RL1084.2
031800     MOVE CCVS-E-1 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL1084.2
031900 END-ROUTINE-1.                                                   RL1084.2
032000      ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO      RL1084.2
032100      ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.               RL1084.2
032200      ADD PASS-COUNTER TO ERROR-HOLD.                             RL1084.2
032300*     IF PASS-COUNTER EQUAL TO ERROR-HOLD GO TO END-ROUTINE-12.   RL1084.2
032400      MOVE PASS-COUNTER TO CCVS-E-4-1.                            RL1084.2
032500      MOVE ERROR-HOLD TO CCVS-E-4-2.                              RL1084.2
032600      MOVE CCVS-E-4 TO CCVS-E-2-2.                                RL1084.2
032700      MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM WRITE-LINE.           RL1084.2
032800  END-ROUTINE-12.                                                 RL1084.2
032900      MOVE "TEST(S) FAILED" TO ENDER-DESC.                        RL1084.2
033000     IF       ERROR-COUNTER IS EQUAL TO ZERO                      RL1084.2
033100         MOVE "NO " TO ERROR-TOTAL                                RL1084.2
033200         ELSE                                                     RL1084.2
033300         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       RL1084.2
033400     MOVE     CCVS-E-2 TO DUMMY-RECORD.                           RL1084.2
033500     PERFORM WRITE-LINE.                                          RL1084.2
033600 END-ROUTINE-13.                                                  RL1084.2
033700     IF DELETE-COUNTER IS EQUAL TO ZERO                           RL1084.2
033800         MOVE "NO " TO ERROR-TOTAL  ELSE                          RL1084.2
033900         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      RL1084.2
034000     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   RL1084.2
034100     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.           RL1084.2
034200      IF   INSPECT-COUNTER EQUAL TO ZERO                          RL1084.2
034300          MOVE "NO " TO ERROR-TOTAL                               RL1084.2
034400      ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                   RL1084.2
034500      MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.            RL1084.2
034600      MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.          RL1084.2
034700     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM WRITE-LINE.           RL1084.2
034800 WRITE-LINE.                                                      RL1084.2
034900     ADD 1 TO RECORD-COUNT.                                       RL1084.2
035000Y    IF RECORD-COUNT GREATER 50                                   RL1084.2
035100Y        MOVE DUMMY-RECORD TO DUMMY-HOLD                          RL1084.2
035200Y        MOVE SPACE TO DUMMY-RECORD                               RL1084.2
035300Y        WRITE DUMMY-RECORD AFTER ADVANCING PAGE                  RL1084.2
035400Y        MOVE CCVS-C-1 TO DUMMY-RECORD PERFORM WRT-LN             RL1084.2
035500Y        MOVE CCVS-C-2 TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES     RL1084.2
035600Y        MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM WRT-LN          RL1084.2
035700Y        MOVE DUMMY-HOLD TO DUMMY-RECORD                          RL1084.2
035800Y        MOVE ZERO TO RECORD-COUNT.                               RL1084.2
035900     PERFORM WRT-LN.                                              RL1084.2
036000 WRT-LN.                                                          RL1084.2
036100     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               RL1084.2
036200     MOVE SPACE TO DUMMY-RECORD.                                  RL1084.2
036300 BLANK-LINE-PRINT.                                                RL1084.2
036400     PERFORM WRT-LN.                                              RL1084.2
036500 FAIL-ROUTINE.                                                    RL1084.2
036600     IF   COMPUTED-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE. RL1084.2
036700     IF     CORRECT-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE.RL1084.2
036800     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 RL1084.2
036900     MOVE  "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.   RL1084.2
037000     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL1084.2
037100     MOVE   SPACES TO INF-ANSI-REFERENCE.                         RL1084.2
037200     GO TO  FAIL-ROUTINE-EX.                                      RL1084.2
037300 FAIL-ROUTINE-WRITE.                                              RL1084.2
037400     MOVE   TEST-COMPUTED TO PRINT-REC PERFORM WRITE-LINE         RL1084.2
037500     MOVE   ANSI-REFERENCE TO COR-ANSI-REFERENCE.                 RL1084.2
037600     MOVE   TEST-CORRECT TO PRINT-REC PERFORM WRITE-LINE 2 TIMES. RL1084.2
037700     MOVE   SPACES TO COR-ANSI-REFERENCE.                         RL1084.2
037800 FAIL-ROUTINE-EX. EXIT.                                           RL1084.2
037900 BAIL-OUT.                                                        RL1084.2
038000     IF     COMPUTED-A NOT EQUAL TO SPACE GO TO BAIL-OUT-WRITE.   RL1084.2
038100     IF     CORRECT-A EQUAL TO SPACE GO TO BAIL-OUT-EX.           RL1084.2
038200 BAIL-OUT-WRITE.                                                  RL1084.2
038300     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  RL1084.2
038400     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 RL1084.2
038500     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL1084.2
038600     MOVE   SPACES TO INF-ANSI-REFERENCE.                         RL1084.2
038700 BAIL-OUT-EX. EXIT.                                               RL1084.2
038800 CCVS1-EXIT.                                                      RL1084.2
038900     EXIT.                                                        RL1084.2
039000 SECT-RL108A-001 SECTION.                                         RL1084.2
039100 REL-INIT-001.                                                    RL1084.2
039200     MOVE     "FILE CREATE RL-FS1" TO FEATURE.                    RL1084.2
039300     OPEN     OUTPUT    RL-FS1.                                   RL1084.2
039400     MOVE     "RL-FS1" TO XFILE-NAME (1).                         RL1084.2
039500     MOVE     "R1-F-G" TO XRECORD-NAME (1).                       RL1084.2
039600     MOVE CCVS-PGM-ID  TO XPROGRAM-NAME (1).                      RL1084.2
039700     MOVE     000120   TO XRECORD-LENGTH (1).                     RL1084.2
039800     MOVE     "RC"     TO CHARS-OR-RECORDS (1).                   RL1084.2
039900     MOVE     0001     TO XBLOCK-SIZE (1).                        RL1084.2
040000     MOVE     000500   TO RECORDS-IN-FILE (1).                    RL1084.2
040100     MOVE     "RL"     TO XFILE-ORGANIZATION (1).                 RL1084.2
040200     MOVE     "S"      TO XLABEL-TYPE (1).                        RL1084.2
040300     MOVE     000001   TO XRECORD-NUMBER (1).                     RL1084.2
040400 REL-TEST-001.                                                    RL1084.2
040500     MOVE    FILE-RECORD-INFO-P1-120 (1) TO RL-FS1R1-F-G-120.     RL1084.2
040600     WRITE   RL-FS1R1-F-G-120                                     RL1084.2
040700             INVALID KEY GO TO REL-FAIL-001                       RL1084.2
040800         NOT INVALID KEY GO TO REL-TEST-001-A                     RL1084.2
040900     END-WRITE.                                                   RL1084.2
041000 REL-TEST-001-A.                                                  RL1084.2
041100     IF      XRECORD-NUMBER (1) EQUAL TO 500                      RL1084.2
041200             GO TO REL-WRITE-001.                                 RL1084.2
041300     ADD     000001 TO XRECORD-NUMBER (1).                        RL1084.2
041400     GO      TO REL-TEST-001.                                     RL1084.2
041500 REL-DELETE-001.                                                  RL1084.2
041600     PERFORM DE-LETE.                                             RL1084.2
041700     GO TO   REL-WRITE-001.                                       RL1084.2
041800 REL-FAIL-001.                                                    RL1084.2
041900     PERFORM FAIL.                                                RL1084.2
042000     MOVE   "BOUNDARY VIOLATION"  TO RE-MARK.                     RL1084.2
042100 REL-WRITE-001.                                                   RL1084.2
042200     MOVE     "VIII-37 4.9.4(A)"   TO ANSI-REFERENCE.             RL1084.2
042300     MOVE   "REL-TEST-001" TO   PAR-NAME                          RL1084.2
042400     MOVE   "FILE CREATED, LFILE "  TO COMPUTED-A.                RL1084.2
042500     MOVE    XRECORD-NUMBER (1) TO CORRECT-18V0.                  RL1084.2
042600     PERFORM PRINT-DETAIL.                                        RL1084.2
042700     CLOSE   RL-FS1.                                              RL1084.2
042800 REL-INIT-002.                                                    RL1084.2
042900     OPEN    INPUT     RL-FS1.                                    RL1084.2
043000     MOVE    ZERO      TO WRK-CS-09V00.                           RL1084.2
043100 REL-TEST-002.                                                    RL1084.2
043200     READ    RL-FS1                                               RL1084.2
043300             AT END GO TO REL-TEST-002-1                          RL1084.2
043400         NOT AT END GO TO REL-TEST-002-A                          RL1084.2
043500     END-READ.                                                    RL1084.2
043600 REL-TEST-002-A.                                                  RL1084.2
043700     MOVE    RL-FS1R1-F-G-120 TO FILE-RECORD-INFO-P1-120 (1).     RL1084.2
043800     ADD     1 TO WRK-CS-09V00.                                   RL1084.2
043900     IF      WRK-CS-09V00 GREATER 500                             RL1084.2
044000             MOVE "MORE THAN 500 RECORDS" TO RE-MARK              RL1084.2
044100             GO TO REL-TEST-002-1.                                RL1084.2
044200     GO      TO REL-TEST-002.                                     RL1084.2
044300 REL-DELETE-002.                                                  RL1084.2
044400     PERFORM DE-LETE.                                             RL1084.2
044500     PERFORM PRINT-DETAIL.                                        RL1084.2
044600     GO TO   CCVS-EXIT.                                           RL1084.2
044700 REL-TEST-002-1.                                                  RL1084.2
044800     IF      XRECORD-NUMBER (1) NOT EQUAL TO 500                  RL1084.2
044900             PERFORM FAIL                                         RL1084.2
045000     ELSE                                                         RL1084.2
045100             PERFORM PASS.                                        RL1084.2
045200     GO      TO REL-WRITE-002.                                    RL1084.2
045300 REL-WRITE-002.                                                   RL1084.2
045400     MOVE   "VIII-26 4.5.4" TO ANSI-REFERENCE.                    RL1084.2
045500     MOVE   "REL-TEST-002"  TO PAR-NAME.                          RL1084.2
045600     MOVE   "FILE VERIFIED, LFILE" TO COMPUTED-A.                 RL1084.2
045700     MOVE    XRECORD-NUMBER (1) TO CORRECT-18V0.                  RL1084.2
045800     PERFORM PRINT-DETAIL.                                        RL1084.2
045900     CLOSE   RL-FS1.                                              RL1084.2
046000 CCVS-EXIT SECTION.                                               RL1084.2
046100 CCVS-999999.                                                     RL1084.2
046200     GO TO CLOSE-FILES.                                           RL1084.2
       END PROGRAM RL108A.
000100 IDENTIFICATION DIVISION.                                         RL1094.2
000200 PROGRAM-ID.                                                      RL1094.2
000300     RL109A.                                                      RL1094.2
000400****************************************************************  RL1094.2
000500*                                                              *  RL1094.2
000600*    VALIDATION FOR:-                                          *  RL1094.2
000700*                                                              *  RL1094.2
000800*    "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".RL1094.2
000900*                                                              *  RL1094.2
001000*    "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".RL1094.2
001100*                                                              *  RL1094.2
001200****************************************************************  RL1094.2
001300*                                                              *  RL1094.2
001400*      X-CARDS USED BY THIS PROGRAM ARE :-                     *  RL1094.2
001500*                                                              *  RL1094.2
001600*        X-55  - SYSTEM PRINTER NAME.                          *  RL1094.2
001700*        X-82  - SOURCE COMPUTER NAME.                         *  RL1094.2
001800*        X-83  - OBJECT COMPUTER NAME.                         *  RL1094.2
001900*                                                              *  RL1094.2
002000****************************************************************  RL1094.2
002100*GENERAL:    THE FUNCTION OF THIS PROGRAM IS TO PROCESS A RELATIVERL1094.2
002200*            I-O FILE RANDOMLY (ACCESS MODE IS RANDOM).  THE FILE RL1094.2
002300*            USED AS INPUT IS THAT FILE CREATED BY RL108A.        RL1094.2
002400*                                                                 RL1094.2
002500*            FIRST THE FILE IS VERIFED AS TO THE EXISTANCE AND    RL1094.2
002600*            ACCURACY OF THE 500  RECORDS CREATED IN THE FIRST    RL1094.2
002700*            PROGRAM.  SECONDLY, RECORDS OF THE FILE ARE SEL-     RL1094.2
002800*            ECTIVELY UPDATED; AND THIRDLY, THE ACCURACY OF EACH  RL1094.2
002900*            RECORD IN THE FILE IS AGAIN VERIFIED.                RL1094.2
003000*                                                                 RL1094.2
003100*     THIS PROGRAM TESTS THE NEW SYNTACTICAL CONSTRUCTS AND       RL1094.2
003200*     SEMENTIC ACTIONS OF THE FOLLOWING ELEMENTS:                 RL1094.2
003300*        - ORGANIZATION                                           RL1094.2
003400*        - ACCESS                                                 RL1094.2
003500*        - READ                                                   RL1094.2
003600*        - REWRITE                                                RL1094.2
003700*                                                                 RL1094.2
003800*                                                                 RL1094.2
003900*            X-CARD PARAMETERS WHICH MUST BE SUPPLIED FOR THIS    RL1094.2
004000*            PROGRAM ARE:                                         RL1094.2
004100*                                                                 RL1094.2
004200*                 X-21   IMPLEMENTOR-NAME IN ASSIGN TO CLAUSE FOR RL1094.2
004300*                         RELATIVE  I-O DATA FILE                 RL1094.2
004400*                 X-55   SYSTEM PRINTER                           RL1094.2
004500*                 X-69   ADDITIONAL VALUE OF CLAUSES              RL1094.2
004600*                 X-74   VALUE OF IMPLEMENTOR-NAME                RL1094.2
004700*                 X-75   OBJECT OF VALUE OF CLAUSE                RL1094.2
004800*                 X-82   SOURCE-COMPUTER                          RL1094.2
004900*                 X-83   OBJECT-COMPUTER.                         RL1094.2
005000*                                                                 RL1094.2
005100***************************************************               RL1094.2
005200 ENVIRONMENT DIVISION.                                            RL1094.2
005300 CONFIGURATION SECTION.                                           RL1094.2
005400 SOURCE-COMPUTER.                                                 RL1094.2
005500     XXXXX082.                                                    RL1094.2
005600 OBJECT-COMPUTER.                                                 RL1094.2
005700     XXXXX083.                                                    RL1094.2
005800 INPUT-OUTPUT SECTION.                                            RL1094.2
005900 FILE-CONTROL.                                                    RL1094.2
006000     SELECT PRINT-FILE ASSIGN TO                                  RL1094.2
006100     XXXXX055.                                                    RL1094.2
006200     SELECT  RL-FR1 ASSIGN TO                                     RL1094.2
006300     XXXXX061                                                     RL1094.2
006400             ORGANIZATION IS RELATIVE                             RL1094.2
006500             ACCESS  MODE IS RANDOM                               RL1094.2
006600             RELATIVE RL-FR1-KEY.                                 RL1094.2
006700 DATA DIVISION.                                                   RL1094.2
006800 FILE SECTION.                                                    RL1094.2
006900 FD  PRINT-FILE.                                                  RL1094.2
007000 01  PRINT-REC PICTURE X(120).                                    RL1094.2
007100 01  DUMMY-RECORD PICTURE X(120).                                 RL1094.2
007200 FD  RL-FR1                                                       RL1094.2
007300     LABEL RECORDS STANDARD                                       RL1094.2
007400C    VALUE OF                                                     RL1094.2
007500C    XXXXX074                                                     RL1094.2
007600C    IS                                                           RL1094.2
007700C    XXXXX075                                                     RL1094.2
007800G    XXXXX069                                                     RL1094.2
007900     BLOCK CONTAINS 1 RECORDS                                     RL1094.2
008000     RECORD CONTAINS 120 CHARACTERS.                              RL1094.2
008100 01  RL-FR1R1-F-G-120.                                            RL1094.2
008200     02 FILLER PICTURE X(120).                                    RL1094.2
008300 WORKING-STORAGE SECTION.                                         RL1094.2
008400 01  WRK-CS-09V00 PIC S9(09)      USAGE COMP VALUE ZERO.          RL1094.2
008500 01  RL-FR1-KEY        PIC 9(09)  USAGE COMP VALUE ZERO.          RL1094.2
008600 01  WRK-DS-09V00-002 PIC S9(9) VALUE ZERO.                       RL1094.2
008700 01  WRK-CS-09V00-002 PIC S9(09)       USAGE COMP VALUE ZERO.     RL1094.2
008800 01  WRK-CS-09V00-003 PIC S9(09)       USAGE COMP VALUE ZERO.     RL1094.2
008900 01  I-O-ERROR-RL-FR1 PIC X(3) VALUE "NO ".                       RL1094.2
009000 01  WRK-CS-09V00-001 PIC S9(09)       USAGE COMP VALUE ZERO.     RL1094.2
009100 01  WRK-CS-09V00-004 PIC S9(09)       USAGE COMP VALUE ZERO.     RL1094.2
009200 01  WRK-CS-09V00-005 PIC S9(09)       USAGE COMP VALUE ZERO.     RL1094.2
009300 01  WRK-DS-09V00-001 PIC S9(09)      VALUE ZERO.                 RL1094.2
009400 01  FILE-RECORD-INFORMATION-REC.                                 RL1094.2
009500     03 FILE-RECORD-INFO-SKELETON.                                RL1094.2
009600        05 FILLER                 PICTURE X(48)       VALUE       RL1094.2
009700             "FILE=      ,RECORD=      /0,RECNO=000000,UPDT=00".  RL1094.2
009800        05 FILLER                 PICTURE X(46)       VALUE       RL1094.2
009900             ",ODO=0000,PGM=     ,LRECL=000000,BLKSIZ  =0000".    RL1094.2
010000        05 FILLER                 PICTURE X(26)       VALUE       RL1094.2
010100             ",LFIL=000000,ORG=  ,LBLR= ".                        RL1094.2
010200        05 FILLER                 PICTURE X(37)       VALUE       RL1094.2
010300             ",RECKEY=                             ".             RL1094.2
010400        05 FILLER                 PICTURE X(38)       VALUE       RL1094.2
010500             ",ALTKEY1=                             ".            RL1094.2
010600        05 FILLER                 PICTURE X(38)       VALUE       RL1094.2
010700             ",ALTKEY2=                             ".            RL1094.2
010800        05 FILLER                 PICTURE X(7)        VALUE SPACE.RL1094.2
010900     03 FILE-RECORD-INFO          OCCURS  10  TIMES.              RL1094.2
011000        05 FILE-RECORD-INFO-P1-120.                               RL1094.2
011100           07 FILLER              PIC X(5).                       RL1094.2
011200           07 XFILE-NAME           PIC X(6).                      RL1094.2
011300           07 FILLER              PIC X(8).                       RL1094.2
011400           07 XRECORD-NAME         PIC X(6).                      RL1094.2
011500           07 FILLER              PIC X(1).                       RL1094.2
011600           07 REELUNIT-NUMBER     PIC 9(1).                       RL1094.2
011700           07 FILLER              PIC X(7).                       RL1094.2
011800           07 XRECORD-NUMBER       PIC 9(6).                      RL1094.2
011900           07 FILLER              PIC X(6).                       RL1094.2
012000           07 UPDATE-NUMBER       PIC 9(2).                       RL1094.2
012100           07 FILLER              PIC X(5).                       RL1094.2
012200           07 ODO-NUMBER          PIC 9(4).                       RL1094.2
012300           07 FILLER              PIC X(5).                       RL1094.2
012400           07 XPROGRAM-NAME        PIC X(5).                      RL1094.2
012500           07 FILLER              PIC X(7).                       RL1094.2
012600           07 XRECORD-LENGTH       PIC 9(6).                      RL1094.2
012700           07 FILLER              PIC X(7).                       RL1094.2
012800           07 CHARS-OR-RECORDS    PIC X(2).                       RL1094.2
012900           07 FILLER              PIC X(1).                       RL1094.2
013000           07 XBLOCK-SIZE          PIC 9(4).                      RL1094.2
013100           07 FILLER              PIC X(6).                       RL1094.2
013200           07 RECORDS-IN-FILE     PIC 9(6).                       RL1094.2
013300           07 FILLER              PIC X(5).                       RL1094.2
013400           07 XFILE-ORGANIZATION   PIC X(2).                      RL1094.2
013500           07 FILLER              PIC X(6).                       RL1094.2
013600           07 XLABEL-TYPE          PIC X(1).                      RL1094.2
013700        05 FILE-RECORD-INFO-P121-240.                             RL1094.2
013800           07 FILLER              PIC X(8).                       RL1094.2
013900           07 XRECORD-KEY          PIC X(29).                     RL1094.2
014000           07 FILLER              PIC X(9).                       RL1094.2
014100           07 ALTERNATE-KEY1      PIC X(29).                      RL1094.2
014200           07 FILLER              PIC X(9).                       RL1094.2
014300           07 ALTERNATE-KEY2      PIC X(29).                      RL1094.2
014400           07 FILLER              PIC X(7).                       RL1094.2
014500 01  TEST-RESULTS.                                                RL1094.2
014600     02 FILLER                   PIC X      VALUE SPACE.          RL1094.2
014700     02 FEATURE                  PIC X(20)  VALUE SPACE.          RL1094.2
014800     02 FILLER                   PIC X      VALUE SPACE.          RL1094.2
014900     02 P-OR-F                   PIC X(5)   VALUE SPACE.          RL1094.2
015000     02 FILLER                   PIC X      VALUE SPACE.          RL1094.2
015100     02  PAR-NAME.                                                RL1094.2
015200       03 FILLER                 PIC X(19)  VALUE SPACE.          RL1094.2
015300       03  PARDOT-X              PIC X      VALUE SPACE.          RL1094.2
015400       03 DOTVALUE               PIC 99     VALUE ZERO.           RL1094.2
015500     02 FILLER                   PIC X(8)   VALUE SPACE.          RL1094.2
015600     02 RE-MARK                  PIC X(61).                       RL1094.2
015700 01  TEST-COMPUTED.                                               RL1094.2
015800     02 FILLER                   PIC X(30)  VALUE SPACE.          RL1094.2
015900     02 FILLER                   PIC X(17)  VALUE                 RL1094.2
016000            "       COMPUTED=".                                   RL1094.2
016100     02 COMPUTED-X.                                               RL1094.2
016200     03 COMPUTED-A               PIC X(20)  VALUE SPACE.          RL1094.2
016300     03 COMPUTED-N               REDEFINES COMPUTED-A             RL1094.2
016400                                 PIC -9(9).9(9).                  RL1094.2
016500     03 COMPUTED-0V18 REDEFINES COMPUTED-A   PIC -.9(18).         RL1094.2
016600     03 COMPUTED-4V14 REDEFINES COMPUTED-A   PIC -9(4).9(14).     RL1094.2
016700     03 COMPUTED-14V4 REDEFINES COMPUTED-A   PIC -9(14).9(4).     RL1094.2
016800     03       CM-18V0 REDEFINES COMPUTED-A.                       RL1094.2
016900         04 COMPUTED-18V0                    PIC -9(18).          RL1094.2
017000         04 FILLER                           PIC X.               RL1094.2
017100     03 FILLER PIC X(50) VALUE SPACE.                             RL1094.2
017200 01  TEST-CORRECT.                                                RL1094.2
017300     02 FILLER PIC X(30) VALUE SPACE.                             RL1094.2
017400     02 FILLER PIC X(17) VALUE "       CORRECT =".                RL1094.2
017500     02 CORRECT-X.                                                RL1094.2
017600     03 CORRECT-A                  PIC X(20) VALUE SPACE.         RL1094.2
017700     03 CORRECT-N    REDEFINES CORRECT-A     PIC -9(9).9(9).      RL1094.2
017800     03 CORRECT-0V18 REDEFINES CORRECT-A     PIC -.9(18).         RL1094.2
017900     03 CORRECT-4V14 REDEFINES CORRECT-A     PIC -9(4).9(14).     RL1094.2
018000     03 CORRECT-14V4 REDEFINES CORRECT-A     PIC -9(14).9(4).     RL1094.2
018100     03      CR-18V0 REDEFINES CORRECT-A.                         RL1094.2
018200         04 CORRECT-18V0                     PIC -9(18).          RL1094.2
018300         04 FILLER                           PIC X.               RL1094.2
018400     03 FILLER PIC X(2) VALUE SPACE.                              RL1094.2
018500     03 COR-ANSI-REFERENCE             PIC X(48) VALUE SPACE.     RL1094.2
018600 01  CCVS-C-1.                                                    RL1094.2
018700     02 FILLER  PIC IS X(99)    VALUE IS " FEATURE              PARL1094.2
018800-    "SS  PARAGRAPH-NAME                                          RL1094.2
018900-    "       REMARKS".                                            RL1094.2
019000     02 FILLER                     PIC X(20)    VALUE SPACE.      RL1094.2
019100 01  CCVS-C-2.                                                    RL1094.2
019200     02 FILLER                     PIC X        VALUE SPACE.      RL1094.2
019300     02 FILLER                     PIC X(6)     VALUE "TESTED".   RL1094.2
019400     02 FILLER                     PIC X(15)    VALUE SPACE.      RL1094.2
019500     02 FILLER                     PIC X(4)     VALUE "FAIL".     RL1094.2
019600     02 FILLER                     PIC X(94)    VALUE SPACE.      RL1094.2
019700 01  REC-SKL-SUB                   PIC 9(2)     VALUE ZERO.       RL1094.2
019800 01  REC-CT                        PIC 99       VALUE ZERO.       RL1094.2
019900 01  DELETE-COUNTER                PIC 999      VALUE ZERO.       RL1094.2
020000 01  ERROR-COUNTER                 PIC 999      VALUE ZERO.       RL1094.2
020100 01  INSPECT-COUNTER               PIC 999      VALUE ZERO.       RL1094.2
020200 01  PASS-COUNTER                  PIC 999      VALUE ZERO.       RL1094.2
020300 01  TOTAL-ERROR                   PIC 999      VALUE ZERO.       RL1094.2
020400 01  ERROR-HOLD                    PIC 999      VALUE ZERO.       RL1094.2
020500 01  DUMMY-HOLD                    PIC X(120)   VALUE SPACE.      RL1094.2
020600 01  RECORD-COUNT                  PIC 9(5)     VALUE ZERO.       RL1094.2
020700 01  ANSI-REFERENCE                PIC X(48)    VALUE SPACES.     RL1094.2
020800 01  CCVS-H-1.                                                    RL1094.2
020900     02  FILLER                    PIC X(39)    VALUE SPACES.     RL1094.2
021000     02  FILLER                    PIC X(42)    VALUE             RL1094.2
021100     "OFFICIAL COBOL COMPILER VALIDATION SYSTEM".                 RL1094.2
021200     02  FILLER                    PIC X(39)    VALUE SPACES.     RL1094.2
021300 01  CCVS-H-2A.                                                   RL1094.2
021400   02  FILLER                        PIC X(40)  VALUE SPACE.      RL1094.2
021500   02  FILLER                        PIC X(7)   VALUE "CCVS85 ".  RL1094.2
021600   02  FILLER                        PIC XXXX   VALUE             RL1094.2
021700     "4.2 ".                                                      RL1094.2
021800   02  FILLER                        PIC X(28)  VALUE             RL1094.2
021900            " COPY - NOT FOR DISTRIBUTION".                       RL1094.2
022000   02  FILLER                        PIC X(41)  VALUE SPACE.      RL1094.2
022100                                                                  RL1094.2
022200 01  CCVS-H-2B.                                                   RL1094.2
022300   02  FILLER                        PIC X(15)  VALUE             RL1094.2
022400            "TEST RESULT OF ".                                    RL1094.2
022500   02  TEST-ID                       PIC X(9).                    RL1094.2
022600   02  FILLER                        PIC X(4)   VALUE             RL1094.2
022700            " IN ".                                               RL1094.2
022800   02  FILLER                        PIC X(12)  VALUE             RL1094.2
022900     " HIGH       ".                                              RL1094.2
023000   02  FILLER                        PIC X(22)  VALUE             RL1094.2
023100            " LEVEL VALIDATION FOR ".                             RL1094.2
023200   02  FILLER                        PIC X(58)  VALUE             RL1094.2
023300     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".RL1094.2
023400 01  CCVS-H-3.                                                    RL1094.2
023500     02  FILLER                      PIC X(34)  VALUE             RL1094.2
023600            " FOR OFFICIAL USE ONLY    ".                         RL1094.2
023700     02  FILLER                      PIC X(58)  VALUE             RL1094.2
023800     "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".RL1094.2
023900     02  FILLER                      PIC X(28)  VALUE             RL1094.2
024000            "  COPYRIGHT   1985 ".                                RL1094.2
024100 01  CCVS-E-1.                                                    RL1094.2
024200     02 FILLER                       PIC X(52)  VALUE SPACE.      RL1094.2
024300     02 FILLER  PIC X(14) VALUE IS "END OF TEST-  ".              RL1094.2
024400     02 ID-AGAIN                     PIC X(9).                    RL1094.2
024500     02 FILLER                       PIC X(45)  VALUE SPACES.     RL1094.2
024600 01  CCVS-E-2.                                                    RL1094.2
024700     02  FILLER                      PIC X(31)  VALUE SPACE.      RL1094.2
024800     02  FILLER                      PIC X(21)  VALUE SPACE.      RL1094.2
024900     02 CCVS-E-2-2.                                               RL1094.2
025000         03 ERROR-TOTAL              PIC XXX    VALUE SPACE.      RL1094.2
025100         03 FILLER                   PIC X      VALUE SPACE.      RL1094.2
025200         03 ENDER-DESC               PIC X(44)  VALUE             RL1094.2
025300            "ERRORS ENCOUNTERED".                                 RL1094.2
025400 01  CCVS-E-3.                                                    RL1094.2
025500     02  FILLER                      PIC X(22)  VALUE             RL1094.2
025600            " FOR OFFICIAL USE ONLY".                             RL1094.2
025700     02  FILLER                      PIC X(12)  VALUE SPACE.      RL1094.2
025800     02  FILLER                      PIC X(58)  VALUE             RL1094.2
025900     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".RL1094.2
026000     02  FILLER                      PIC X(13)  VALUE SPACE.      RL1094.2
026100     02 FILLER                       PIC X(15)  VALUE             RL1094.2
026200             " COPYRIGHT 1985".                                   RL1094.2
026300 01  CCVS-E-4.                                                    RL1094.2
026400     02 CCVS-E-4-1                   PIC XXX    VALUE SPACE.      RL1094.2
026500     02 FILLER                       PIC X(4)   VALUE " OF ".     RL1094.2
026600     02 CCVS-E-4-2                   PIC XXX    VALUE SPACE.      RL1094.2
026700     02 FILLER                       PIC X(40)  VALUE             RL1094.2
026800      "  TESTS WERE EXECUTED SUCCESSFULLY".                       RL1094.2
026900 01  XXINFO.                                                      RL1094.2
027000     02 FILLER                       PIC X(19)  VALUE             RL1094.2
027100            "*** INFORMATION ***".                                RL1094.2
027200     02 INFO-TEXT.                                                RL1094.2
027300       04 FILLER                     PIC X(8)   VALUE SPACE.      RL1094.2
027400       04 XXCOMPUTED                 PIC X(20).                   RL1094.2
027500       04 FILLER                     PIC X(5)   VALUE SPACE.      RL1094.2
027600       04 XXCORRECT                  PIC X(20).                   RL1094.2
027700     02 INF-ANSI-REFERENCE           PIC X(48).                   RL1094.2
027800 01  HYPHEN-LINE.                                                 RL1094.2
027900     02 FILLER  PIC IS X VALUE IS SPACE.                          RL1094.2
028000     02 FILLER  PIC IS X(65)    VALUE IS "************************RL1094.2
028100-    "*****************************************".                 RL1094.2
028200     02 FILLER  PIC IS X(54)    VALUE IS "************************RL1094.2
028300-    "******************************".                            RL1094.2
028400 01  CCVS-PGM-ID                     PIC X(9)   VALUE             RL1094.2
028500     "RL109A".                                                    RL1094.2
028600 PROCEDURE DIVISION.                                              RL1094.2
028700 CCVS1 SECTION.                                                   RL1094.2
028800 OPEN-FILES.                                                      RL1094.2
028900     OPEN    OUTPUT PRINT-FILE.                                   RL1094.2
029000     MOVE  CCVS-PGM-ID TO TEST-ID. MOVE CCVS-PGM-ID TO ID-AGAIN.  RL1094.2
029100     MOVE    SPACE TO TEST-RESULTS.                               RL1094.2
029200     PERFORM HEAD-ROUTINE THRU COLUMN-NAMES-ROUTINE.              RL1094.2
029300     MOVE    ZERO TO REC-SKL-SUB.                                 RL1094.2
029400     PERFORM CCVS-INIT-FILE 9 TIMES.                              RL1094.2
029500 CCVS-INIT-FILE.                                                  RL1094.2
029600     ADD     1 TO REC-SKL-SUB.                                    RL1094.2
029700     MOVE    FILE-RECORD-INFO-SKELETON                            RL1094.2
029800          TO FILE-RECORD-INFO (REC-SKL-SUB).                      RL1094.2
029900 CCVS-INIT-EXIT.                                                  RL1094.2
030000     GO TO CCVS1-EXIT.                                            RL1094.2
030100 CLOSE-FILES.                                                     RL1094.2
030200     PERFORM END-ROUTINE THRU END-ROUTINE-13. CLOSE PRINT-FILE.   RL1094.2
030300 TERMINATE-CCVS.                                                  RL1094.2
030400S    EXIT PROGRAM.                                                RL1094.2
030500STERMINATE-CALL.                                                  RL1094.2
030600     STOP     RUN.                                                RL1094.2
030700 INSPT. MOVE "INSPT" TO P-OR-F. ADD 1 TO INSPECT-COUNTER.         RL1094.2
030800 PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.           RL1094.2
030900 FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.          RL1094.2
031000 DE-LETE.  MOVE "*****" TO P-OR-F.  ADD 1 TO DELETE-COUNTER.      RL1094.2
031100     MOVE "****TEST DELETED****" TO RE-MARK.                      RL1094.2
031200 PRINT-DETAIL.                                                    RL1094.2
031300     IF REC-CT NOT EQUAL TO ZERO                                  RL1094.2
031400             MOVE "." TO PARDOT-X                                 RL1094.2
031500             MOVE REC-CT TO DOTVALUE.                             RL1094.2
031600     MOVE     TEST-RESULTS TO PRINT-REC. PERFORM WRITE-LINE.      RL1094.2
031700     IF P-OR-F EQUAL TO "FAIL*"  PERFORM WRITE-LINE               RL1094.2
031800        PERFORM FAIL-ROUTINE THRU FAIL-ROUTINE-EX                 RL1094.2
031900          ELSE PERFORM BAIL-OUT THRU BAIL-OUT-EX.                 RL1094.2
032000     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              RL1094.2
032100     MOVE SPACE TO CORRECT-X.                                     RL1094.2
032200     IF     REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.         RL1094.2
032300     MOVE     SPACE TO RE-MARK.                                   RL1094.2
032400 HEAD-ROUTINE.                                                    RL1094.2
032500     MOVE CCVS-H-1  TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  RL1094.2
032600     MOVE CCVS-H-2A TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  RL1094.2
032700     MOVE CCVS-H-2B TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  RL1094.2
032800     MOVE CCVS-H-3  TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  RL1094.2
032900 COLUMN-NAMES-ROUTINE.                                            RL1094.2
033000     MOVE CCVS-C-1 TO DUMMY-RECORD. PERFORM WRITE-LINE.           RL1094.2
033100     MOVE CCVS-C-2 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL1094.2
033200     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE.        RL1094.2
033300 END-ROUTINE.                                                     RL1094.2
033400     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE 5 TIMES.RL1094.2
033500 END-RTN-EXIT.                                                    RL1094.2
033600     MOVE CCVS-E-1 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL1094.2
033700 END-ROUTINE-1.                                                   RL1094.2
033800      ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO      RL1094.2
033900      ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.               RL1094.2
034000      ADD PASS-COUNTER TO ERROR-HOLD.                             RL1094.2
034100*     IF PASS-COUNTER EQUAL TO ERROR-HOLD GO TO END-ROUTINE-12.   RL1094.2
034200      MOVE PASS-COUNTER TO CCVS-E-4-1.                            RL1094.2
034300      MOVE ERROR-HOLD TO CCVS-E-4-2.                              RL1094.2
034400      MOVE CCVS-E-4 TO CCVS-E-2-2.                                RL1094.2
034500      MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM WRITE-LINE.           RL1094.2
034600  END-ROUTINE-12.                                                 RL1094.2
034700      MOVE "TEST(S) FAILED" TO ENDER-DESC.                        RL1094.2
034800     IF       ERROR-COUNTER IS EQUAL TO ZERO                      RL1094.2
034900         MOVE "NO " TO ERROR-TOTAL                                RL1094.2
035000         ELSE                                                     RL1094.2
035100         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       RL1094.2
035200     MOVE     CCVS-E-2 TO DUMMY-RECORD.                           RL1094.2
035300     PERFORM WRITE-LINE.                                          RL1094.2
035400 END-ROUTINE-13.                                                  RL1094.2
035500     IF DELETE-COUNTER IS EQUAL TO ZERO                           RL1094.2
035600         MOVE "NO " TO ERROR-TOTAL  ELSE                          RL1094.2
035700         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      RL1094.2
035800     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   RL1094.2
035900     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.           RL1094.2
036000      IF   INSPECT-COUNTER EQUAL TO ZERO                          RL1094.2
036100          MOVE "NO " TO ERROR-TOTAL                               RL1094.2
036200      ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                   RL1094.2
036300      MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.            RL1094.2
036400      MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.          RL1094.2
036500     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM WRITE-LINE.           RL1094.2
036600 WRITE-LINE.                                                      RL1094.2
036700     ADD 1 TO RECORD-COUNT.                                       RL1094.2
036800Y    IF RECORD-COUNT GREATER 50                                   RL1094.2
036900Y        MOVE DUMMY-RECORD TO DUMMY-HOLD                          RL1094.2
037000Y        MOVE SPACE TO DUMMY-RECORD                               RL1094.2
037100Y        WRITE DUMMY-RECORD AFTER ADVANCING PAGE                  RL1094.2
037200Y        MOVE CCVS-C-1 TO DUMMY-RECORD PERFORM WRT-LN             RL1094.2
037300Y        MOVE CCVS-C-2 TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES     RL1094.2
037400Y        MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM WRT-LN          RL1094.2
037500Y        MOVE DUMMY-HOLD TO DUMMY-RECORD                          RL1094.2
037600Y        MOVE ZERO TO RECORD-COUNT.                               RL1094.2
037700     PERFORM WRT-LN.                                              RL1094.2
037800 WRT-LN.                                                          RL1094.2
037900     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               RL1094.2
038000     MOVE SPACE TO DUMMY-RECORD.                                  RL1094.2
038100 BLANK-LINE-PRINT.                                                RL1094.2
038200     PERFORM WRT-LN.                                              RL1094.2
038300 FAIL-ROUTINE.                                                    RL1094.2
038400     IF   COMPUTED-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE. RL1094.2
038500     IF     CORRECT-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE.RL1094.2
038600     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 RL1094.2
038700     MOVE  "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.   RL1094.2
038800     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL1094.2
038900     MOVE   SPACES TO INF-ANSI-REFERENCE.                         RL1094.2
039000     GO TO  FAIL-ROUTINE-EX.                                      RL1094.2
039100 FAIL-ROUTINE-WRITE.                                              RL1094.2
039200     MOVE   TEST-COMPUTED TO PRINT-REC PERFORM WRITE-LINE         RL1094.2
039300     MOVE   ANSI-REFERENCE TO COR-ANSI-REFERENCE.                 RL1094.2
039400     MOVE   TEST-CORRECT TO PRINT-REC PERFORM WRITE-LINE 2 TIMES. RL1094.2
039500     MOVE   SPACES TO COR-ANSI-REFERENCE.                         RL1094.2
039600 FAIL-ROUTINE-EX. EXIT.                                           RL1094.2
039700 BAIL-OUT.                                                        RL1094.2
039800     IF     COMPUTED-A NOT EQUAL TO SPACE GO TO BAIL-OUT-WRITE.   RL1094.2
039900     IF     CORRECT-A EQUAL TO SPACE GO TO BAIL-OUT-EX.           RL1094.2
040000 BAIL-OUT-WRITE.                                                  RL1094.2
040100     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  RL1094.2
040200     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 RL1094.2
040300     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL1094.2
040400     MOVE   SPACES TO INF-ANSI-REFERENCE.                         RL1094.2
040500 BAIL-OUT-EX. EXIT.                                               RL1094.2
040600 CCVS1-EXIT.                                                      RL1094.2
040700     EXIT.                                                        RL1094.2
040800 SECT-RL109A-001 SECTION.                                         RL1094.2
040900 REL-INIT-003.                                                    RL1094.2
041000     OPEN    INPUT  RL-FR1.                                       RL1094.2
041100     MOVE   "REL-TEST-003"   TO PAR-NAME.                         RL1094.2
041200     MOVE    ZERO TO   RL-FR1-KEY.                                RL1094.2
041300     MOVE    ZERO TO   WRK-CS-09V00-002                           RL1094.2
041400     MOVE    ZERO  TO  WRK-CS-09V00-003                           RL1094.2
041500*                                                                 RL1094.2
041600     MOVE    01 TO REC-CT.                                        RL1094.2
041700     MOVE   "READ RANDOM"  TO FEATURE.                            RL1094.2
041800 REL-TEST-003-R.                                                  RL1094.2
041900     ADD     1 TO WRK-CS-09V00-003                                RL1094.2
042000     MOVE    WRK-CS-09V00-003 TO RL-FR1-KEY.                      RL1094.2
042100     IF      RL-FR1-KEY GREATER +501                              RL1094.2
042200             MOVE "INVALID KEY NOT TAKEN" TO COMPUTED-A           RL1094.2
042300             MOVE RL-FR1-KEY TO CORRECT-18V0                      RL1094.2
042400             PERFORM FAIL                                         RL1094.2
042500             PERFORM PRINT-DETAIL                                 RL1094.2
042600             ADD 1 TO REC-CT                                      RL1094.2
042700             GO TO REL-WRITE-003.                                 RL1094.2
042800     READ    RL-FR1                                               RL1094.2
042900             INVALID GO TO REL-WRITE-003                          RL1094.2
043000         NOT INVALID GO TO REL-TEST-003-A                         RL1094.2
043100     END-READ.                                                    RL1094.2
043200 REL-TEST-003-A.                                                  RL1094.2
043300     MOVE    RL-FR1R1-F-G-120 TO FILE-RECORD-INFO-P1-120 (1).     RL1094.2
043400     IF      XRECORD-NUMBER (1) EQUAL TO RL-FR1-KEY               RL1094.2
043500             GO TO  REL-TEST-003-R.                               RL1094.2
043600     MOVE   "YES" TO I-O-ERROR-RL-FR1.                            RL1094.2
043700     ADD     1 TO WRK-CS-09V00-002                                RL1094.2
043800     GO TO   REL-TEST-003-R.                                      RL1094.2
043900 REL-WRITE-003.                                                   RL1094.2
044000     MOVE   "VIII-26 4.5.4"  TO ANSI-REFERENCE.                   RL1094.2
044100     IF      RL-FR1-KEY NOT EQUAL TO 501                          RL1094.2
044200             MOVE "WRONG KEY/NOT 500" TO CORRECT-A                RL1094.2
044300             MOVE  RL-FR1-KEY TO COMPUTED-18V0                    RL1094.2
044400             PERFORM FAIL                                         RL1094.2
044500     ELSE                                                         RL1094.2
044600             PERFORM PASS.                                        RL1094.2
044700     PERFORM PRINT-DETAIL.                                        RL1094.2
044800*                                                                 RL1094.2
044900*01                                                               RL1094.2
045000*                                                                 RL1094.2
045100     ADD     1 TO REC-CT.                                         RL1094.2
045200     IF      XRECORD-NUMBER (1) NOT EQUAL TO 500                  RL1094.2
045300             MOVE "WRONG RECORD/NOT 500" TO CORRECT-A             RL1094.2
045400             MOVE  XRECORD-NUMBER (1) TO COMPUTED-18V0            RL1094.2
045500             PERFORM FAIL                                         RL1094.2
045600     ELSE                                                         RL1094.2
045700             PERFORM PASS.                                        RL1094.2
045800     PERFORM PRINT-DETAIL.                                        RL1094.2
045900*                                                                 RL1094.2
046000*02                                                               RL1094.2
046100*                                                                 RL1094.2
046200     ADD     1 TO REC-CT.                                         RL1094.2
046300     IF      WRK-CS-09V00-003 NOT EQUAL TO 501                    RL1094.2
046400             MOVE "INCORRECT RECORD COUNT" TO RE-MARK             RL1094.2
046500             MOVE  WRK-CS-09V00-003 TO COMPUTED-18V0              RL1094.2
046600             MOVE 501  TO CORRECT-18V0                            RL1094.2
046700             PERFORM FAIL                                         RL1094.2
046800     ELSE                                                         RL1094.2
046900             PERFORM PASS.                                        RL1094.2
047000     PERFORM PRINT-DETAIL.                                        RL1094.2
047100*                                                                 RL1094.2
047200*03                                                               RL1094.2
047300*                                                                 RL1094.2
047400     ADD     1 TO REC-CT.                                         RL1094.2
047500     IF      I-O-ERROR-RL-FR1 EQUAL TO "YES"                      RL1094.2
047600             MOVE WRK-CS-09V00-002 TO COMPUTED-18V0               RL1094.2
047700             MOVE "RECORDS DID NOT COMPARE" TO RE-MARK            RL1094.2
047800             PERFORM FAIL                                         RL1094.2
047900     ELSE                                                         RL1094.2
048000             PERFORM PASS.                                        RL1094.2
048100     PERFORM PRINT-DETAIL.                                        RL1094.2
048200*                                                                 RL1094.2
048300*04                                                               RL1094.2
048400*                                                                 RL1094.2
048500     ADD      1 TO REC-CT.                                        RL1094.2
048600     CLOSE    RL-FR1.                                             RL1094.2
048700 REL-INIT-004-R .                                                 RL1094.2
048800     MOVE    "VIII-30 4.6.4"  TO ANSI-REFERENCE.                  RL1094.2
048900     MOVE    "REL-TEST-004" TO PAR-NAME.                          RL1094.2
049000     OPEN I-O RL-FR1.                                             RL1094.2
049100     MOVE     ZERO TO RL-FR1-KEY.                                 RL1094.2
049200     MOVE     ZERO TO WRK-CS-09V00-002.                           RL1094.2
049300     MOVE     ZERO TO WRK-CS-09V00-003.                           RL1094.2
049400     MOVE     ZERO TO WRK-CS-09V00-004.                           RL1094.2
049500     MOVE     ZERO TO WRK-CS-09V00-005.                           RL1094.2
049600*                                                                 RL1094.2
049700     MOVE     01 TO REC-CT.                                       RL1094.2
049800     MOVE     SPACE TO  FILE-RECORD-INFO-P1-120 (1).              RL1094.2
049900     MOVE    "REWRITE"  TO FEATURE.                               RL1094.2
050000 REL-TEST-004-R.                                                  RL1094.2
050100     ADD     5 TO  WRK-CS-09V00-003.                              RL1094.2
050200     MOVE    WRK-CS-09V00-003 TO RL-FR1-KEY.                      RL1094.2
050300      IF     RL-FR1-KEY GREATER 505                               RL1094.2
050400             MOVE "INVALID KEY/NOT TAKEN" TO COMPUTED-A           RL1094.2
050500             MOVE  RL-FR1-KEY TO CORRECT-18V0                     RL1094.2
050600             PERFORM FAIL                                         RL1094.2
050700             PERFORM PRINT-DETAIL                                 RL1094.2
050800             ADD 1 TO REC-CT                                      RL1094.2
050900             GO TO REL-TEST-004-3.                                RL1094.2
051000     READ    RL-FR1                                               RL1094.2
051100             INVALID KEY GO TO REL-TEST-004-1                     RL1094.2
051200         NOT INVALID KEY GO TO REL-TEST-004-A                     RL1094.2
051300     END-READ.                                                    RL1094.2
051400 REL-TEST-004-A.                                                  RL1094.2
051500     MOVE    RL-FR1R1-F-G-120 TO FILE-RECORD-INFO-P1-120 (1)      RL1094.2
051600     ADD     01 TO UPDATE-NUMBER (1).                             RL1094.2
051700     MOVE    CCVS-PGM-ID   TO XPROGRAM-NAME (1).                  RL1094.2
051800     MOVE    FILE-RECORD-INFO-P1-120 (1) TO RL-FR1R1-F-G-120.     RL1094.2
051900     REWRITE RL-FR1R1-F-G-120                                     RL1094.2
052000             INVALID KEY GO TO REL-TEST-004-2                     RL1094.2
052100         NOT INVALID KEY GO TO REL-TEST-004-R                     RL1094.2
052200     END-REWRITE.                                                 RL1094.2
052300 REL-TEST-004-1.                                                  RL1094.2
052400     IF      RL-FR1-KEY LESS THAN 501                             RL1094.2
052500             ADD 1 TO  WRK-CS-09V00-004                           RL1094.2
052600             GO TO   REL-TEST-004-R.                              RL1094.2
052700     PERFORM PASS.                                                RL1094.2
052800     PERFORM PRINT-DETAIL.                                        RL1094.2
052900*                                                                 RL1094.2
053000*01                                                               RL1094.2
053100*                                                                 RL1094.2
053200     ADD     1  TO REC-CT.                                        RL1094.2
053300     GO TO   REL-TEST-004-3.                                      RL1094.2
053400 REL-TEST-004-2.                                                  RL1094.2
053500     ADD     1 TO WRK-CS-09V00-005.                               RL1094.2
053600     IF      RL-FR1-KEY LESS 501                                  RL1094.2
053700             GO TO   REL-TEST-004-R.                              RL1094.2
053800 REL-TEST-004-3.                                                  RL1094.2
053900     IF      WRK-CS-09V00-004 NOT EQUAL TO ZERO                   RL1094.2
054000             MOVE "INVALID KEY ON READ" TO COMPUTED-A             RL1094.2
054100             MOVE WRK-CS-09V00-004 TO CORRECT-18V0                RL1094.2
054200             PERFORM FAIL                                         RL1094.2
054300     ELSE                                                         RL1094.2
054400             PERFORM PASS.                                        RL1094.2
054500     PERFORM PRINT-DETAIL.                                        RL1094.2
054600*                                                                 RL1094.2
054700*02                                                               RL1094.2
054800*                                                                 RL1094.2
054900     ADD     1 TO REC-CT.                                         RL1094.2
055000     IF      WRK-CS-09V00-005 NOT EQUAL TO ZERO                   RL1094.2
055100             MOVE "INVALID KEY ON REWRITE" TO COMPUTED-A          RL1094.2
055200             MOVE  WRK-CS-09V00-005 TO CORRECT-18V0               RL1094.2
055300             PERFORM FAIL                                         RL1094.2
055400     ELSE                                                         RL1094.2
055500             PERFORM PASS.                                        RL1094.2
055600     PERFORM PRINT-DETAIL.                                        RL1094.2
055700*                                                                 RL1094.2
055800*03                                                               RL1094.2
055900*                                                                 RL1094.2
056000     ADD     1 TO REC-CT.                                         RL1094.2
056100     CLOSE   RL-FR1.                                              RL1094.2
056200 REL-INIT-005.                                                    RL1094.2
056300     MOVE   "VIII-26 4.5.4" TO ANSI-REFERENCE.                    RL1094.2
056400     MOVE   "REL-TEST-005"  TO PAR-NAME.                          RL1094.2
056500     OPEN    INPUT  RL-FR1.                                       RL1094.2
056600     MOVE    501  TO WRK-CS-09V00-003.                            RL1094.2
056700     MOVE    ZERO TO WRK-CS-09V00-004.                            RL1094.2
056800     MOVE    ZERO TO WRK-CS-09V00-005.                            RL1094.2
056900     MOVE    ZERO TO WRK-CS-09V00-002.                            RL1094.2
057000     MOVE    SPACE TO  FILE-RECORD-INFO-P1-120 (1).               RL1094.2
057100     MOVE    01 TO REC-CT.                                        RL1094.2
057200*                                                                 RL1094.2
057300     MOVE   "READ RANDOM"  TO FEATURE.                            RL1094.2
057400 REL-TEST-005-R.                                                  RL1094.2
057500     SUBTRACT 1 FROM    WRK-CS-09V00-003.                         RL1094.2
057600     MOVE    WRK-CS-09V00-003 TO RL-FR1-KEY.                      RL1094.2
057700     IF      WRK-CS-09V00-003 LESS THAN ZERO                      RL1094.2
057800             MOVE    "INVALID KEY/NOT TAKEN"  TO RE-MARK          RL1094.2
057900             MOVE   WRK-CS-09V00-003  TO COMPUTED-18V0            RL1094.2
058000             MOVE   ZERO TO CORRECT-18V0                          RL1094.2
058100             PERFORM FAIL                                         RL1094.2
058200             PERFORM PRINT-DETAIL                                 RL1094.2
058300             ADD 1 TO REC-CT                                      RL1094.2
058400             GO TO REL-TEST-005-3.                                RL1094.2
058500     READ    RL-FR1                                               RL1094.2
058600             INVALID KEY  GO TO REL-TEST-005-1                    RL1094.2
058700         NOT INVALID KEY  GO TO REL-TEST-005-A                    RL1094.2
058800     END-READ.                                                    RL1094.2
058900 REL-TEST-005-A.                                                  RL1094.2
059000     MOVE    RL-FR1R1-F-G-120 TO FILE-RECORD-INFO-P1-120 (1).     RL1094.2
059100     IF      UPDATE-NUMBER (1) EQUAL TO 00                        RL1094.2
059200             ADD 1 TO WRK-CS-09V00-004.                           RL1094.2
059300     IF      UPDATE-NUMBER (1) EQUAL TO 01                        RL1094.2
059400             ADD 1 TO WRK-CS-09V00-005.                           RL1094.2
059500     GO TO   REL-TEST-005-R.                                      RL1094.2
059600 REL-TEST-005-1.                                                  RL1094.2
059700     IF      RL-FR1-KEY GREATER ZERO                              RL1094.2
059800             ADD 1 TO WRK-CS-09V00-002                            RL1094.2
059900             GO TO REL-TEST-005-R.                                RL1094.2
060000     PERFORM PASS.                                                RL1094.2
060100     PERFORM PRINT-DETAIL.                                        RL1094.2
060200     ADD     1  TO REC-CT.                                        RL1094.2
060300*01                                                               RL1094.2
060400     GO TO   REL-TEST-005-3.                                      RL1094.2
060500 REL-TEST-005-3.                                                  RL1094.2
060600     IF      WRK-CS-09V00-004 NOT EQUAL TO 400                    RL1094.2
060700             MOVE "NON-UPDATED RECORDS" TO COMPUTED-A             RL1094.2
060800             MOVE WRK-CS-09V00-004 TO CORRECT-18V0                RL1094.2
060900             MOVE "SHOULD BE 400" TO RE-MARK                      RL1094.2
061000             PERFORM FAIL                                         RL1094.2
061100     ELSE                                                         RL1094.2
061200             PERFORM PASS.                                        RL1094.2
061300     PERFORM PRINT-DETAIL.                                        RL1094.2
061400*                                                                 RL1094.2
061500*                                                                 RL1094.2
061600*02                                                               RL1094.2
061700*                                                                 RL1094.2
061800     ADD     1 TO REC-CT.                                         RL1094.2
061900     IF      WRK-CS-09V00-005 NOT EQUAL TO 100                    RL1094.2
062000             MOVE "UPDATED RECORDS" TO COMPUTED-A                 RL1094.2
062100             MOVE WRK-CS-09V00-005 TO CORRECT-18V0                RL1094.2
062200             MOVE "SHOULD BE 100" TO RE-MARK                      RL1094.2
062300             PERFORM FAIL                                         RL1094.2
062400     ELSE                                                         RL1094.2
062500             PERFORM PASS.                                        RL1094.2
062600     PERFORM PRINT-DETAIL.                                        RL1094.2
062700*                                                                 RL1094.2
062800*03                                                               RL1094.2
062900*                                                                 RL1094.2
063000     ADD     1 TO REC-CT.                                         RL1094.2
063100     IF      WRK-CS-09V00-002 GREATER 1                           RL1094.2
063200             MOVE WRK-CS-09V00-002 TO COMPUTED-N                  RL1094.2
063300             MOVE  "INVALID KEY/READS" TO CORRECT-A               RL1094.2
063400             PERFORM FAIL                                         RL1094.2
063500     ELSE                                                         RL1094.2
063600             PERFORM PASS.                                        RL1094.2
063700     PERFORM PRINT-DETAIL.                                        RL1094.2
063800*                                                                 RL1094.2
063900*04                                                               RL1094.2
064000*                                                                 RL1094.2
064100     ADD     1 TO REC-CT.                                         RL1094.2
064200     CLOSE   RL-FR1.                                              RL1094.2
064300 CCVS-EXIT SECTION.                                               RL1094.2
064400 CCVS-999999.                                                     RL1094.2
064500     GO TO CLOSE-FILES.                                           RL1094.2
       END PROGRAM RL109A.
000100 IDENTIFICATION DIVISION.                                         RL1104.2
000200 PROGRAM-ID.                                                      RL1104.2
000300     RL110A.                                                      RL1104.2
000400****************************************************************  RL1104.2
000500*                                                              *  RL1104.2
000600*    VALIDATION FOR:-                                          *  RL1104.2
000700*                                                              *  RL1104.2
000800*    "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".RL1104.2
000900*                                                              *  RL1104.2
001000*    "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".RL1104.2
001100*                                                              *  RL1104.2
001200****************************************************************  RL1104.2
001300*GENERAL:    THIS PROGRAM IS THE THIRD OF A SERIES.  THE FUNCTION RL1104.2
001400*            OF THIS PROGRAM IS TO PROCESS THE FILE SEQUENTIALLY  RL1104.2
001500*            (ACCESS MODE IS SEQUENTIAL). THE FILE USED IS THAT   RL1104.2
001600*            RESULTING FROM RL109A.                               RL1104.2
001700*                                                                 RL1104.2
001800*            FIRST, THE FILE IS VERIFIED FOR ACCURACY OF ITS 500  RL1104.2
001900*            RECORDS.  SECONDLY, RECORDS OF THER FILE ARE         RL1104.2
002000*            SELECTIVELY DELETED AND THIRDLY THE ACCURACY OF EACH RL1104.2
002100*            RECORD IN THE FILE IS AGAIN VERIFIED.                RL1104.2
002200*                                                                 RL1104.2
002300*            X-CARD PARAMETERS WHICH MUST BE SUPPLIED FOR THIS    RL1104.2
002400*            PROGRAM ARE:                                         RL1104.2
002500*                                                                 RL1104.2
002600*                                                                 RL1104.2
002700*                 X-21   IMPLEMENTOR-NAME IN ASSIGN TO CLAUSE FOR RL1104.2
002800*                         RELATIVE  I-O DATA FILE                 RL1104.2
002900*                 X-55   SYSTEM PRINTER                           RL1104.2
003000*                 X-69   ADDITIONAL VALUE OF CLAUSES              RL1104.2
003100*                 X-74   VALUE OF IMPLEMENTOR-NAME                RL1104.2
003200*                 X-75   OBJECT OF VALUE OF CLAUSE                RL1104.2
003300*                 X-82   SOURCE-COMPUTER                          RL1104.2
003400*                 X-83   OBJECT-COMPUTER.                         RL1104.2
003500*                                                                 RL1104.2
003600***************************************************               RL1104.2
003700 ENVIRONMENT DIVISION.                                            RL1104.2
003800 CONFIGURATION SECTION.                                           RL1104.2
003900 SOURCE-COMPUTER.                                                 RL1104.2
004000     XXXXX082.                                                    RL1104.2
004100 OBJECT-COMPUTER.                                                 RL1104.2
004200     XXXXX083.                                                    RL1104.2
004300 INPUT-OUTPUT SECTION.                                            RL1104.2
004400 FILE-CONTROL.                                                    RL1104.2
004500     SELECT PRINT-FILE ASSIGN TO                                  RL1104.2
004600     XXXXX055.                                                    RL1104.2
004700     SELECT   RL-FS1 ASSIGN TO                                    RL1104.2
004800     XXXXX061                                                     RL1104.2
004900              ORGANIZATION IS RELATIVE                            RL1104.2
005000              ACCESS  MODE IS SEQUENTIAL                          RL1104.2
005100              RELATIVE KEY IS RL-FS1-KEY.                         RL1104.2
005200 DATA DIVISION.                                                   RL1104.2
005300 FILE SECTION.                                                    RL1104.2
005400 FD  PRINT-FILE.                                                  RL1104.2
005500 01  PRINT-REC PICTURE X(120).                                    RL1104.2
005600 01  DUMMY-RECORD PICTURE X(120).                                 RL1104.2
005700 FD  RL-FS1                                                       RL1104.2
005800     LABEL RECORDS STANDARD                                       RL1104.2
005900C    VALUE OF                                                     RL1104.2
006000C    XXXXX074                                                     RL1104.2
006100C    IS                                                           RL1104.2
006200C    XXXXX075                                                     RL1104.2
006300G    XXXXX069                                                     RL1104.2
006400     BLOCK CONTAINS 01 RECORDS                                    RL1104.2
006500     RECORD CONTAINS 120.                                         RL1104.2
006600 01  RL-FS1R1-F-G-120.                                            RL1104.2
006700     02 RL-WRK-120 PIC X(120).                                    RL1104.2
006800 WORKING-STORAGE SECTION.                                         RL1104.2
006900 01  RL-FS1-KEY        PIC 9(08) USAGE COMP VALUE ZERO.           RL1104.2
007000 01  WRK-CS-09V00-006 PIC S9(09) USAGE COMP VALUE ZERO.           RL1104.2
007100 01  WRK-CS-09V00-007 PIC S9(09) USAGE COMP VALUE ZERO.           RL1104.2
007200 01  WRK-CS-09V00-008 PIC S9(09) USAGE COMP VALUE ZERO.           RL1104.2
007300 01  WRK-CS-09V00-009 PIC S9(09) USAGE COMP VALUE ZERO.           RL1104.2
007400 01  WRK-CS-09V00-010 PIC S9(09) USAGE COMP VALUE ZERO.           RL1104.2
007500 01  WRK-CS-09V00-011 PIC S9(09) USAGE COMP VALUE ZERO.           RL1104.2
007600 01  I-O-ERROR-RL-FS1 PIC X(3) VALUE "NO ".                       RL1104.2
007700 01  FILE-RECORD-INFORMATION-REC.                                 RL1104.2
007800     03 FILE-RECORD-INFO-SKELETON.                                RL1104.2
007900        05 FILLER                 PICTURE X(48)       VALUE       RL1104.2
008000             "FILE=      ,RECORD=      /0,RECNO=000000,UPDT=00".  RL1104.2
008100        05 FILLER                 PICTURE X(46)       VALUE       RL1104.2
008200             ",ODO=0000,PGM=     ,LRECL=000000,BLKSIZ  =0000".    RL1104.2
008300        05 FILLER                 PICTURE X(26)       VALUE       RL1104.2
008400             ",LFIL=000000,ORG=  ,LBLR= ".                        RL1104.2
008500        05 FILLER                 PICTURE X(37)       VALUE       RL1104.2
008600             ",RECKEY=                             ".             RL1104.2
008700        05 FILLER                 PICTURE X(38)       VALUE       RL1104.2
008800             ",ALTKEY1=                             ".            RL1104.2
008900        05 FILLER                 PICTURE X(38)       VALUE       RL1104.2
009000             ",ALTKEY2=                             ".            RL1104.2
009100        05 FILLER                 PICTURE X(7)        VALUE SPACE.RL1104.2
009200     03 FILE-RECORD-INFO          OCCURS  10  TIMES.              RL1104.2
009300        05 FILE-RECORD-INFO-P1-120.                               RL1104.2
009400           07 FILLER              PIC X(5).                       RL1104.2
009500           07 XFILE-NAME           PIC X(6).                      RL1104.2
009600           07 FILLER              PIC X(8).                       RL1104.2
009700           07 XRECORD-NAME         PIC X(6).                      RL1104.2
009800           07 FILLER              PIC X(1).                       RL1104.2
009900           07 REELUNIT-NUMBER     PIC 9(1).                       RL1104.2
010000           07 FILLER              PIC X(7).                       RL1104.2
010100           07 XRECORD-NUMBER       PIC 9(6).                      RL1104.2
010200           07 FILLER              PIC X(6).                       RL1104.2
010300           07 UPDATE-NUMBER       PIC 9(2).                       RL1104.2
010400           07 FILLER              PIC X(5).                       RL1104.2
010500           07 ODO-NUMBER          PIC 9(4).                       RL1104.2
010600           07 FILLER              PIC X(5).                       RL1104.2
010700           07 XPROGRAM-NAME        PIC X(5).                      RL1104.2
010800           07 FILLER              PIC X(7).                       RL1104.2
010900           07 XRECORD-LENGTH       PIC 9(6).                      RL1104.2
011000           07 FILLER              PIC X(7).                       RL1104.2
011100           07 CHARS-OR-RECORDS    PIC X(2).                       RL1104.2
011200           07 FILLER              PIC X(1).                       RL1104.2
011300           07 XBLOCK-SIZE          PIC 9(4).                      RL1104.2
011400           07 FILLER              PIC X(6).                       RL1104.2
011500           07 RECORDS-IN-FILE     PIC 9(6).                       RL1104.2
011600           07 FILLER              PIC X(5).                       RL1104.2
011700           07 XFILE-ORGANIZATION   PIC X(2).                      RL1104.2
011800           07 FILLER              PIC X(6).                       RL1104.2
011900           07 XLABEL-TYPE          PIC X(1).                      RL1104.2
012000        05 FILE-RECORD-INFO-P121-240.                             RL1104.2
012100           07 FILLER              PIC X(8).                       RL1104.2
012200           07 XRECORD-KEY          PIC X(29).                     RL1104.2
012300           07 FILLER              PIC X(9).                       RL1104.2
012400           07 ALTERNATE-KEY1      PIC X(29).                      RL1104.2
012500           07 FILLER              PIC X(9).                       RL1104.2
012600           07 ALTERNATE-KEY2      PIC X(29).                      RL1104.2
012700           07 FILLER              PIC X(7).                       RL1104.2
012800 01  TEST-RESULTS.                                                RL1104.2
012900     02 FILLER                   PIC X      VALUE SPACE.          RL1104.2
013000     02 FEATURE                  PIC X(20)  VALUE SPACE.          RL1104.2
013100     02 FILLER                   PIC X      VALUE SPACE.          RL1104.2
013200     02 P-OR-F                   PIC X(5)   VALUE SPACE.          RL1104.2
013300     02 FILLER                   PIC X      VALUE SPACE.          RL1104.2
013400     02  PAR-NAME.                                                RL1104.2
013500       03 FILLER                 PIC X(19)  VALUE SPACE.          RL1104.2
013600       03  PARDOT-X              PIC X      VALUE SPACE.          RL1104.2
013700       03 DOTVALUE               PIC 99     VALUE ZERO.           RL1104.2
013800     02 FILLER                   PIC X(8)   VALUE SPACE.          RL1104.2
013900     02 RE-MARK                  PIC X(61).                       RL1104.2
014000 01  TEST-COMPUTED.                                               RL1104.2
014100     02 FILLER                   PIC X(30)  VALUE SPACE.          RL1104.2
014200     02 FILLER                   PIC X(17)  VALUE                 RL1104.2
014300            "       COMPUTED=".                                   RL1104.2
014400     02 COMPUTED-X.                                               RL1104.2
014500     03 COMPUTED-A               PIC X(20)  VALUE SPACE.          RL1104.2
014600     03 COMPUTED-N               REDEFINES COMPUTED-A             RL1104.2
014700                                 PIC -9(9).9(9).                  RL1104.2
014800     03 COMPUTED-0V18 REDEFINES COMPUTED-A   PIC -.9(18).         RL1104.2
014900     03 COMPUTED-4V14 REDEFINES COMPUTED-A   PIC -9(4).9(14).     RL1104.2
015000     03 COMPUTED-14V4 REDEFINES COMPUTED-A   PIC -9(14).9(4).     RL1104.2
015100     03       CM-18V0 REDEFINES COMPUTED-A.                       RL1104.2
015200         04 COMPUTED-18V0                    PIC -9(18).          RL1104.2
015300         04 FILLER                           PIC X.               RL1104.2
015400     03 FILLER PIC X(50) VALUE SPACE.                             RL1104.2
015500 01  TEST-CORRECT.                                                RL1104.2
015600     02 FILLER PIC X(30) VALUE SPACE.                             RL1104.2
015700     02 FILLER PIC X(17) VALUE "       CORRECT =".                RL1104.2
015800     02 CORRECT-X.                                                RL1104.2
015900     03 CORRECT-A                  PIC X(20) VALUE SPACE.         RL1104.2
016000     03 CORRECT-N    REDEFINES CORRECT-A     PIC -9(9).9(9).      RL1104.2
016100     03 CORRECT-0V18 REDEFINES CORRECT-A     PIC -.9(18).         RL1104.2
016200     03 CORRECT-4V14 REDEFINES CORRECT-A     PIC -9(4).9(14).     RL1104.2
016300     03 CORRECT-14V4 REDEFINES CORRECT-A     PIC -9(14).9(4).     RL1104.2
016400     03      CR-18V0 REDEFINES CORRECT-A.                         RL1104.2
016500         04 CORRECT-18V0                     PIC -9(18).          RL1104.2
016600         04 FILLER                           PIC X.               RL1104.2
016700     03 FILLER PIC X(2) VALUE SPACE.                              RL1104.2
016800     03 COR-ANSI-REFERENCE             PIC X(48) VALUE SPACE.     RL1104.2
016900 01  CCVS-C-1.                                                    RL1104.2
017000     02 FILLER  PIC IS X(99)    VALUE IS " FEATURE              PARL1104.2
017100-    "SS  PARAGRAPH-NAME                                          RL1104.2
017200-    "       REMARKS".                                            RL1104.2
017300     02 FILLER                     PIC X(20)    VALUE SPACE.      RL1104.2
017400 01  CCVS-C-2.                                                    RL1104.2
017500     02 FILLER                     PIC X        VALUE SPACE.      RL1104.2
017600     02 FILLER                     PIC X(6)     VALUE "TESTED".   RL1104.2
017700     02 FILLER                     PIC X(15)    VALUE SPACE.      RL1104.2
017800     02 FILLER                     PIC X(4)     VALUE "FAIL".     RL1104.2
017900     02 FILLER                     PIC X(94)    VALUE SPACE.      RL1104.2
018000 01  REC-SKL-SUB                   PIC 9(2)     VALUE ZERO.       RL1104.2
018100 01  REC-CT                        PIC 99       VALUE ZERO.       RL1104.2
018200 01  DELETE-COUNTER                PIC 999      VALUE ZERO.       RL1104.2
018300 01  ERROR-COUNTER                 PIC 999      VALUE ZERO.       RL1104.2
018400 01  INSPECT-COUNTER               PIC 999      VALUE ZERO.       RL1104.2
018500 01  PASS-COUNTER                  PIC 999      VALUE ZERO.       RL1104.2
018600 01  TOTAL-ERROR                   PIC 999      VALUE ZERO.       RL1104.2
018700 01  ERROR-HOLD                    PIC 999      VALUE ZERO.       RL1104.2
018800 01  DUMMY-HOLD                    PIC X(120)   VALUE SPACE.      RL1104.2
018900 01  RECORD-COUNT                  PIC 9(5)     VALUE ZERO.       RL1104.2
019000 01  ANSI-REFERENCE                PIC X(48)    VALUE SPACES.     RL1104.2
019100 01  CCVS-H-1.                                                    RL1104.2
019200     02  FILLER                    PIC X(39)    VALUE SPACES.     RL1104.2
019300     02  FILLER                    PIC X(42)    VALUE             RL1104.2
019400     "OFFICIAL COBOL COMPILER VALIDATION SYSTEM".                 RL1104.2
019500     02  FILLER                    PIC X(39)    VALUE SPACES.     RL1104.2
019600 01  CCVS-H-2A.                                                   RL1104.2
019700   02  FILLER                        PIC X(40)  VALUE SPACE.      RL1104.2
019800   02  FILLER                        PIC X(7)   VALUE "CCVS85 ".  RL1104.2
019900   02  FILLER                        PIC XXXX   VALUE             RL1104.2
020000     "4.2 ".                                                      RL1104.2
020100   02  FILLER                        PIC X(28)  VALUE             RL1104.2
020200            " COPY - NOT FOR DISTRIBUTION".                       RL1104.2
020300   02  FILLER                        PIC X(41)  VALUE SPACE.      RL1104.2
020400                                                                  RL1104.2
020500 01  CCVS-H-2B.                                                   RL1104.2
020600   02  FILLER                        PIC X(15)  VALUE             RL1104.2
020700            "TEST RESULT OF ".                                    RL1104.2
020800   02  TEST-ID                       PIC X(9).                    RL1104.2
020900   02  FILLER                        PIC X(4)   VALUE             RL1104.2
021000            " IN ".                                               RL1104.2
021100   02  FILLER                        PIC X(12)  VALUE             RL1104.2
021200     " HIGH       ".                                              RL1104.2
021300   02  FILLER                        PIC X(22)  VALUE             RL1104.2
021400            " LEVEL VALIDATION FOR ".                             RL1104.2
021500   02  FILLER                        PIC X(58)  VALUE             RL1104.2
021600     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".RL1104.2
021700 01  CCVS-H-3.                                                    RL1104.2
021800     02  FILLER                      PIC X(34)  VALUE             RL1104.2
021900            " FOR OFFICIAL USE ONLY    ".                         RL1104.2
022000     02  FILLER                      PIC X(58)  VALUE             RL1104.2
022100     "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".RL1104.2
022200     02  FILLER                      PIC X(28)  VALUE             RL1104.2
022300            "  COPYRIGHT   1985 ".                                RL1104.2
022400 01  CCVS-E-1.                                                    RL1104.2
022500     02 FILLER                       PIC X(52)  VALUE SPACE.      RL1104.2
022600     02 FILLER  PIC X(14) VALUE IS "END OF TEST-  ".              RL1104.2
022700     02 ID-AGAIN                     PIC X(9).                    RL1104.2
022800     02 FILLER                       PIC X(45)  VALUE SPACES.     RL1104.2
022900 01  CCVS-E-2.                                                    RL1104.2
023000     02  FILLER                      PIC X(31)  VALUE SPACE.      RL1104.2
023100     02  FILLER                      PIC X(21)  VALUE SPACE.      RL1104.2
023200     02 CCVS-E-2-2.                                               RL1104.2
023300         03 ERROR-TOTAL              PIC XXX    VALUE SPACE.      RL1104.2
023400         03 FILLER                   PIC X      VALUE SPACE.      RL1104.2
023500         03 ENDER-DESC               PIC X(44)  VALUE             RL1104.2
023600            "ERRORS ENCOUNTERED".                                 RL1104.2
023700 01  CCVS-E-3.                                                    RL1104.2
023800     02  FILLER                      PIC X(22)  VALUE             RL1104.2
023900            " FOR OFFICIAL USE ONLY".                             RL1104.2
024000     02  FILLER                      PIC X(12)  VALUE SPACE.      RL1104.2
024100     02  FILLER                      PIC X(58)  VALUE             RL1104.2
024200     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".RL1104.2
024300     02  FILLER                      PIC X(13)  VALUE SPACE.      RL1104.2
024400     02 FILLER                       PIC X(15)  VALUE             RL1104.2
024500             " COPYRIGHT 1985".                                   RL1104.2
024600 01  CCVS-E-4.                                                    RL1104.2
024700     02 CCVS-E-4-1                   PIC XXX    VALUE SPACE.      RL1104.2
024800     02 FILLER                       PIC X(4)   VALUE " OF ".     RL1104.2
024900     02 CCVS-E-4-2                   PIC XXX    VALUE SPACE.      RL1104.2
025000     02 FILLER                       PIC X(40)  VALUE             RL1104.2
025100      "  TESTS WERE EXECUTED SUCCESSFULLY".                       RL1104.2
025200 01  XXINFO.                                                      RL1104.2
025300     02 FILLER                       PIC X(19)  VALUE             RL1104.2
025400            "*** INFORMATION ***".                                RL1104.2
025500     02 INFO-TEXT.                                                RL1104.2
025600       04 FILLER                     PIC X(8)   VALUE SPACE.      RL1104.2
025700       04 XXCOMPUTED                 PIC X(20).                   RL1104.2
025800       04 FILLER                     PIC X(5)   VALUE SPACE.      RL1104.2
025900       04 XXCORRECT                  PIC X(20).                   RL1104.2
026000     02 INF-ANSI-REFERENCE           PIC X(48).                   RL1104.2
026100 01  HYPHEN-LINE.                                                 RL1104.2
026200     02 FILLER  PIC IS X VALUE IS SPACE.                          RL1104.2
026300     02 FILLER  PIC IS X(65)    VALUE IS "************************RL1104.2
026400-    "*****************************************".                 RL1104.2
026500     02 FILLER  PIC IS X(54)    VALUE IS "************************RL1104.2
026600-    "******************************".                            RL1104.2
026700 01  CCVS-PGM-ID                     PIC X(9)   VALUE             RL1104.2
026800     "RL110A".                                                    RL1104.2
026900 PROCEDURE DIVISION.                                              RL1104.2
027000 CCVS1 SECTION.                                                   RL1104.2
027100 OPEN-FILES.                                                      RL1104.2
027200     OPEN    OUTPUT PRINT-FILE.                                   RL1104.2
027300     MOVE  CCVS-PGM-ID TO TEST-ID. MOVE CCVS-PGM-ID TO ID-AGAIN.  RL1104.2
027400     MOVE    SPACE TO TEST-RESULTS.                               RL1104.2
027500     PERFORM HEAD-ROUTINE THRU COLUMN-NAMES-ROUTINE.              RL1104.2
027600     MOVE    ZERO TO REC-SKL-SUB.                                 RL1104.2
027700     PERFORM CCVS-INIT-FILE 9 TIMES.                              RL1104.2
027800 CCVS-INIT-FILE.                                                  RL1104.2
027900     ADD     1 TO REC-SKL-SUB.                                    RL1104.2
028000     MOVE    FILE-RECORD-INFO-SKELETON                            RL1104.2
028100          TO FILE-RECORD-INFO (REC-SKL-SUB).                      RL1104.2
028200 CCVS-INIT-EXIT.                                                  RL1104.2
028300     GO TO CCVS1-EXIT.                                            RL1104.2
028400 CLOSE-FILES.                                                     RL1104.2
028500     PERFORM END-ROUTINE THRU END-ROUTINE-13. CLOSE PRINT-FILE.   RL1104.2
028600 TERMINATE-CCVS.                                                  RL1104.2
028700S    EXIT PROGRAM.                                                RL1104.2
028800STERMINATE-CALL.                                                  RL1104.2
028900     STOP     RUN.                                                RL1104.2
029000 INSPT. MOVE "INSPT" TO P-OR-F. ADD 1 TO INSPECT-COUNTER.         RL1104.2
029100 PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.           RL1104.2
029200 FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.          RL1104.2
029300 DE-LETE.  MOVE "*****" TO P-OR-F.  ADD 1 TO DELETE-COUNTER.      RL1104.2
029400     MOVE "****TEST DELETED****" TO RE-MARK.                      RL1104.2
029500 PRINT-DETAIL.                                                    RL1104.2
029600     IF REC-CT NOT EQUAL TO ZERO                                  RL1104.2
029700             MOVE "." TO PARDOT-X                                 RL1104.2
029800             MOVE REC-CT TO DOTVALUE.                             RL1104.2
029900     MOVE     TEST-RESULTS TO PRINT-REC. PERFORM WRITE-LINE.      RL1104.2
030000     IF P-OR-F EQUAL TO "FAIL*"  PERFORM WRITE-LINE               RL1104.2
030100        PERFORM FAIL-ROUTINE THRU FAIL-ROUTINE-EX                 RL1104.2
030200          ELSE PERFORM BAIL-OUT THRU BAIL-OUT-EX.                 RL1104.2
030300     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              RL1104.2
030400     MOVE SPACE TO CORRECT-X.                                     RL1104.2
030500     IF     REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.         RL1104.2
030600     MOVE     SPACE TO RE-MARK.                                   RL1104.2
030700 HEAD-ROUTINE.                                                    RL1104.2
030800     MOVE CCVS-H-1  TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  RL1104.2
030900     MOVE CCVS-H-2A TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  RL1104.2
031000     MOVE CCVS-H-2B TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  RL1104.2
031100     MOVE CCVS-H-3  TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  RL1104.2
031200 COLUMN-NAMES-ROUTINE.                                            RL1104.2
031300     MOVE CCVS-C-1 TO DUMMY-RECORD. PERFORM WRITE-LINE.           RL1104.2
031400     MOVE CCVS-C-2 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL1104.2
031500     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE.        RL1104.2
031600 END-ROUTINE.                                                     RL1104.2
031700     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE 5 TIMES.RL1104.2
031800 END-RTN-EXIT.                                                    RL1104.2
031900     MOVE CCVS-E-1 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL1104.2
032000 END-ROUTINE-1.                                                   RL1104.2
032100      ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO      RL1104.2
032200      ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.               RL1104.2
032300      ADD PASS-COUNTER TO ERROR-HOLD.                             RL1104.2
032400*     IF PASS-COUNTER EQUAL TO ERROR-HOLD GO TO END-ROUTINE-12.   RL1104.2
032500      MOVE PASS-COUNTER TO CCVS-E-4-1.                            RL1104.2
032600      MOVE ERROR-HOLD TO CCVS-E-4-2.                              RL1104.2
032700      MOVE CCVS-E-4 TO CCVS-E-2-2.                                RL1104.2
032800      MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM WRITE-LINE.           RL1104.2
032900  END-ROUTINE-12.                                                 RL1104.2
033000      MOVE "TEST(S) FAILED" TO ENDER-DESC.                        RL1104.2
033100     IF       ERROR-COUNTER IS EQUAL TO ZERO                      RL1104.2
033200         MOVE "NO " TO ERROR-TOTAL                                RL1104.2
033300         ELSE                                                     RL1104.2
033400         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       RL1104.2
033500     MOVE     CCVS-E-2 TO DUMMY-RECORD.                           RL1104.2
033600     PERFORM WRITE-LINE.                                          RL1104.2
033700 END-ROUTINE-13.                                                  RL1104.2
033800     IF DELETE-COUNTER IS EQUAL TO ZERO                           RL1104.2
033900         MOVE "NO " TO ERROR-TOTAL  ELSE                          RL1104.2
034000         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      RL1104.2
034100     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   RL1104.2
034200     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.           RL1104.2
034300      IF   INSPECT-COUNTER EQUAL TO ZERO                          RL1104.2
034400          MOVE "NO " TO ERROR-TOTAL                               RL1104.2
034500      ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                   RL1104.2
034600      MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.            RL1104.2
034700      MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.          RL1104.2
034800     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM WRITE-LINE.           RL1104.2
034900 WRITE-LINE.                                                      RL1104.2
035000     ADD 1 TO RECORD-COUNT.                                       RL1104.2
035100Y    IF RECORD-COUNT GREATER 50                                   RL1104.2
035200Y        MOVE DUMMY-RECORD TO DUMMY-HOLD                          RL1104.2
035300Y        MOVE SPACE TO DUMMY-RECORD                               RL1104.2
035400Y        WRITE DUMMY-RECORD AFTER ADVANCING PAGE                  RL1104.2
035500Y        MOVE CCVS-C-1 TO DUMMY-RECORD PERFORM WRT-LN             RL1104.2
035600Y        MOVE CCVS-C-2 TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES     RL1104.2
035700Y        MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM WRT-LN          RL1104.2
035800Y        MOVE DUMMY-HOLD TO DUMMY-RECORD                          RL1104.2
035900Y        MOVE ZERO TO RECORD-COUNT.                               RL1104.2
036000     PERFORM WRT-LN.                                              RL1104.2
036100 WRT-LN.                                                          RL1104.2
036200     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               RL1104.2
036300     MOVE SPACE TO DUMMY-RECORD.                                  RL1104.2
036400 BLANK-LINE-PRINT.                                                RL1104.2
036500     PERFORM WRT-LN.                                              RL1104.2
036600 FAIL-ROUTINE.                                                    RL1104.2
036700     IF   COMPUTED-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE. RL1104.2
036800     IF     CORRECT-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE.RL1104.2
036900     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 RL1104.2
037000     MOVE  "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.   RL1104.2
037100     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL1104.2
037200     MOVE   SPACES TO INF-ANSI-REFERENCE.                         RL1104.2
037300     GO TO  FAIL-ROUTINE-EX.                                      RL1104.2
037400 FAIL-ROUTINE-WRITE.                                              RL1104.2
037500     MOVE   TEST-COMPUTED TO PRINT-REC PERFORM WRITE-LINE         RL1104.2
037600     MOVE   ANSI-REFERENCE TO COR-ANSI-REFERENCE.                 RL1104.2
037700     MOVE   TEST-CORRECT TO PRINT-REC PERFORM WRITE-LINE 2 TIMES. RL1104.2
037800     MOVE   SPACES TO COR-ANSI-REFERENCE.                         RL1104.2
037900 FAIL-ROUTINE-EX. EXIT.                                           RL1104.2
038000 BAIL-OUT.                                                        RL1104.2
038100     IF     COMPUTED-A NOT EQUAL TO SPACE GO TO BAIL-OUT-WRITE.   RL1104.2
038200     IF     CORRECT-A EQUAL TO SPACE GO TO BAIL-OUT-EX.           RL1104.2
038300 BAIL-OUT-WRITE.                                                  RL1104.2
038400     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  RL1104.2
038500     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 RL1104.2
038600     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL1104.2
038700     MOVE   SPACES TO INF-ANSI-REFERENCE.                         RL1104.2
038800 BAIL-OUT-EX. EXIT.                                               RL1104.2
038900 CCVS1-EXIT.                                                      RL1104.2
039000     EXIT.                                                        RL1104.2
039100 SECT-RL110A-001 SECTION.                                         RL1104.2
039200 REL-INIT-006.                                                    RL1104.2
039300     MOVE  99 TO RL-FS1-KEY.                                      RL1104.2
039400*    THIS FILE "RL-FS1" IS ACCESSED SEQUENTIALLY AND HAS          RL1104.2
039500*    ASSOCIATED WITH IT A RELATIVE KEY WHICH AT ALL TIMES SHOULD  RL1104.2
039600*    CONTAIN THE NUMBER OF THE RECORD PREVIOUSLY READ.            RL1104.2
039700     OPEN INPUT RL-FS1.                                           RL1104.2
039800     MOVE   "REL-TEST-006" TO   PAR-NAME.                         RL1104.2
039900     MOVE   "VII-26 4.5.4" TO ANSI-REFERENCE.                     RL1104.2
040000     MOVE    ZERO TO             WRK-CS-09V00-006.                RL1104.2
040100     MOVE    ZERO TO             WRK-CS-09V00-007.                RL1104.2
040200     MOVE    ZERO TO             WRK-CS-09V00-008.                RL1104.2
040300     MOVE    ZERO TO             WRK-CS-09V00-009.                RL1104.2
040400     MOVE    ZERO TO             WRK-CS-09V00-010.                RL1104.2
040500     MOVE    ZERO TO             WRK-CS-09V00-011.                RL1104.2
040600     MOVE    SPACE TO  FILE-RECORD-INFO-P1-120 (1).               RL1104.2
040700     MOVE    RL-FS1-KEY TO WRK-CS-09V00-011.                      RL1104.2
040800     MOVE    01 TO REC-CT.                                        RL1104.2
040900     MOVE   "READ SEQUENTIAL"  TO FEATURE.                        RL1104.2
041000 REL-TEST-006-R.                                                  RL1104.2
041100     ADD     1 TO WRK-CS-09V00-006.                               RL1104.2
041200     READ    RL-FS1                                               RL1104.2
041300                END GO TO REL-TEST-006-3                          RL1104.2
041400         NOT AT END GO TO REL-TEST-006-A                          RL1104.2
041500     END-READ.                                                    RL1104.2
041600 REL-TEST-006-A.                                                  RL1104.2
041700     MOVE     RL-FS1R1-F-G-120    TO FILE-RECORD-INFO-P1-120 (1). RL1104.2
041800     IF       UPDATE-NUMBER (1) EQUAL TO 00                       RL1104.2
041900             ADD 1 TO WRK-CS-09V00-007                            RL1104.2
042000             GO TO   REL-TEST-006-2.                              RL1104.2
042100     IF      UPDATE-NUMBER (1) EQUAL TO 01                        RL1104.2
042200             ADD 1 TO WRK-CS-09V00-008                            RL1104.2
042300             GO TO   REL-TEST-006-2.                              RL1104.2
042400     ADD     1 TO WRK-CS-09V00-009.                               RL1104.2
042500 REL-TEST-006-2.                                                  RL1104.2
042600     IF      RL-FS1-KEY NOT EQUAL TO XRECORD-NUMBER (1)           RL1104.2
042700             ADD 1 TO  WRK-CS-09V00-010.                          RL1104.2
042800     IF      WRK-CS-09V00-006  GREATER 501                        RL1104.2
042900             GO TO   REL-TEST-006-3.                              RL1104.2
043000     GO TO   REL-TEST-006-R.                                      RL1104.2
043100 REL-TEST-006-3.                                                  RL1104.2
043200     IF      WRK-CS-09V00-006 NOT EQUAL TO 501                    RL1104.2
043300             MOVE "INCORRECT RECORD COUNT"  TO RE-MARK            RL1104.2
043400             MOVE  WRK-CS-09V00-006 TO COMPUTED-18V0              RL1104.2
043500             MOVE  501  TO             CORRECT-18V0               RL1104.2
043600             PERFORM FAIL                                         RL1104.2
043700     ELSE                                                         RL1104.2
043800             PERFORM PASS.                                        RL1104.2
043900     PERFORM PRINT-DETAIL.                                        RL1104.2
044000*    .01                                                          RL1104.2
044100     ADD     1 TO REC-CT.                                         RL1104.2
044200     IF      WRK-CS-09V00-007 EQUAL TO 400                        RL1104.2
044300             PERFORM PASS                                         RL1104.2
044400     ELSE                                                         RL1104.2
044500             MOVE "NON-UPDATED RECORDS" TO COMPUTED-A             RL1104.2
044600             MOVE  WRK-CS-09V00-007 TO CORRECT-18V0               RL1104.2
044700             MOVE "SHOULD BE 400" TO RE-MARK                      RL1104.2
044800             PERFORM FAIL.                                        RL1104.2
044900     PERFORM PRINT-DETAIL.                                        RL1104.2
045000     ADD     1 TO REC-CT.                                         RL1104.2
045100*    .02                                                          RL1104.2
045200     IF      WRK-CS-09V00-008 EQUAL TO 100                        RL1104.2
045300             PERFORM PASS                                         RL1104.2
045400     ELSE                                                         RL1104.2
045500             MOVE WRK-CS-09V00-008 TO COMPUTED-18V0               RL1104.2
045600             MOVE 100             TO  CORRECT-18V0                RL1104.2
045700             MOVE "UPDATED RECORDS" TO RE-MARK                    RL1104.2
045800             PERFORM FAIL.                                        RL1104.2
045900     PERFORM PRINT-DETAIL.                                        RL1104.2
046000     ADD     1 TO REC-CT.                                         RL1104.2
046100*    .03                                                          RL1104.2
046200     IF      WRK-CS-09V00-009 EQUAL TO ZERO                       RL1104.2
046300             PERFORM PASS                                         RL1104.2
046400     ELSE                                                         RL1104.2
046500             MOVE WRK-CS-09V00-009 TO COMPUTED-18V0               RL1104.2
046600             MOVE  ZERO            TO CORRECT-18V0                RL1104.2
046700             MOVE "BAD-UPDATES" TO RE-MARK                        RL1104.2
046800             PERFORM FAIL.                                        RL1104.2
046900     PERFORM PRINT-DETAIL.                                        RL1104.2
047000     ADD     01 TO REC-CT.                                        RL1104.2
047100*    .04                                                          RL1104.2
047200     IF      WRK-CS-09V00-010 EQUAL TO ZERO                       RL1104.2
047300             PERFORM PASS                                         RL1104.2
047400     ELSE                                                         RL1104.2
047500             MOVE WRK-CS-09V00-010 TO COMPUTED-18V0               RL1104.2
047600             MOVE ZERO             TO CORRECT-18V0                RL1104.2
047700             MOVE "KEY VS RECORD" TO RE-MARK                      RL1104.2
047800             PERFORM FAIL.                                        RL1104.2
047900     PERFORM PRINT-DETAIL.                                        RL1104.2
048000     ADD     01 TO REC-CT.                                        RL1104.2
048100*    .05                                                          RL1104.2
048200     MOVE    WRK-CS-09V00-011 TO RL-FS1-KEY.                      RL1104.2
048300     MOVE  RL-FS1-KEY TO COMPUTED-18V0.                           RL1104.2
048400     MOVE    "INFORMATION" TO CORRECT-A.                          RL1104.2
048500     MOVE    "STATUS AFTER OPEN" TO RE-MARK.                      RL1104.2
048600     PERFORM PRINT-DETAIL.                                        RL1104.2
048700     ADD     01 TO REC-CT.                                        RL1104.2
048800*    .06                                                          RL1104.2
048900     CLOSE    RL-FS1.                                             RL1104.2
049000 REL-INIT-007.                                                    RL1104.2
049100     MOVE   "REL-TEST-007" TO PAR-NAME                            RL1104.2
049200     MOVE   "VII-26 4.5.4" TO ANSI-REFERENCE.                     RL1104.2
049300     OPEN    I-O RL-FS1.                                          RL1104.2
049400     MOVE    ZERO TO WRK-CS-09V00-006                             RL1104.2
049500     MOVE    ZERO TO WRK-CS-09V00-007                             RL1104.2
049600     MOVE    ZERO TO WRK-CS-09V00-008                             RL1104.2
049700     MOVE    ZERO TO WRK-CS-09V00-009                             RL1104.2
049800     MOVE    ZERO TO WRK-CS-09V00-010                             RL1104.2
049900     MOVE    ZERO TO WRK-CS-09V00-011                             RL1104.2
050000     MOVE    01 TO REC-CT.                                        RL1104.2
050100     MOVE    SPACE TO  FILE-RECORD-INFO-P1-120 (1).               RL1104.2
050200     MOVE   "DELETE"  TO FEATURE.                                 RL1104.2
050300 REL-TEST-007-R.                                                  RL1104.2
050400     ADD     1 TO WRK-CS-09V00-006                                RL1104.2
050500     ADD     1 TO WRK-CS-09V00-007.                               RL1104.2
050600     READ    RL-FS1                                               RL1104.2
050700             AT END                                               RL1104.2
050800             MOVE "AT END PATH TAKEN " TO RE-MARK                 RL1104.2
050900             GO TO  REL-TEST-007-3                                RL1104.2
051000         NOT AT END                                               RL1104.2
051100             GO TO  REL-TEST-007-A                                RL1104.2
051200     END-READ.                                                    RL1104.2
051300 REL-TEST-007-A.                                                  RL1104.2
051400     MOVE    RL-FS1R1-F-G-120 TO FILE-RECORD-INFO-P1-120 (1).     RL1104.2
051500     IF      WRK-CS-09V00-007  EQUAL TO 4                         RL1104.2
051600             GO TO REL-TEST-007-2.                                RL1104.2
051700     IF      WRK-CS-09V00-006 GREATER 501                         RL1104.2
051800             MOVE  "AT END NOT TAKEN"  TO RE-MARK                 RL1104.2
051900             GO TO REL-TEST-007-3.                                RL1104.2
052000     GO TO   REL-TEST-007-R.                                      RL1104.2
052100 REL-TEST-007-2.                                                  RL1104.2
052200     MOVE   "VII-19 4.3.4" TO ANSI-REFERENCE.                     RL1104.2
052300     MOVE    CCVS-PGM-ID   TO XPROGRAM-NAME (1).                  RL1104.2
052400     MOVE    99 TO UPDATE-NUMBER (1).                             RL1104.2
052500     MOVE    FILE-RECORD-INFO-P1-120 (1) TO RL-FS1R1-F-G-120.     RL1104.2
052600     DELETE  RL-FS1                                               RL1104.2
052700     END-DELETE.                                                  RL1104.2
052800 REL-TEST-007-2-A.                                                RL1104.2
052900     MOVE    ZERO TO  WRK-CS-09V00-007.                           RL1104.2
053000     ADD     1 TO  WRK-CS-09V00-008.                              RL1104.2
053100     GO TO   REL-TEST-007-R.                                      RL1104.2
053200 REL-TEST-007-3.                                                  RL1104.2
053300     IF      WRK-CS-09V00-006 NOT EQUAL TO 501                    RL1104.2
053400             MOVE WRK-CS-09V00-006 TO COMPUTED-18V0               RL1104.2
053500             MOVE              501 TO CORRECT-18V0                RL1104.2
053600             PERFORM FAIL                                         RL1104.2
053700             ELSE                                                 RL1104.2
053800             PERFORM PASS.                                        RL1104.2
053900     PERFORM PRINT-DETAIL.                                        RL1104.2
054000     ADD     01 TO REC-CT.                                        RL1104.2
054100     CLOSE   RL-FS1.                                              RL1104.2
054200 REL-INIT-008.                                                    RL1104.2
054300     MOVE   "REL-TEST-008" TO PAR-NAME.                           RL1104.2
054400     MOVE   "VII-26 4.5.4" TO ANSI-REFERENCE.                     RL1104.2
054500     MOVE    ZERO TO   WRK-CS-09V00-006                           RL1104.2
054600     MOVE    ZERO TO   WRK-CS-09V00-007                           RL1104.2
054700     MOVE    ZERO TO   WRK-CS-09V00-008                           RL1104.2
054800     MOVE    ZERO TO   WRK-CS-09V00-009                           RL1104.2
054900     MOVE    ZERO TO   WRK-CS-09V00-010                           RL1104.2
055000     MOVE    ZERO TO   WRK-CS-09V00-011                           RL1104.2
055100     MOVE    01 TO REC-CT.                                        RL1104.2
055200     MOVE    SPACE  TO  FILE-RECORD-INFO-P1-120 (1).              RL1104.2
055300     MOVE    ZERO TO RL-FS1-KEY.                                  RL1104.2
055400     OPEN    INPUT  RL-FS1.                                       RL1104.2
055500     MOVE   "READ UPDATED FILE"  TO FEATURE.                      RL1104.2
055600 REL-TEST-008-R.                                                  RL1104.2
055700     ADD     1 TO WRK-CS-09V00-006.                               RL1104.2
055800     ADD     1 TO WRK-CS-09V00-007.                               RL1104.2
055900     ADD     1 TO WRK-CS-09V00-008.                               RL1104.2
056000     READ    RL-FS1                                               RL1104.2
056100             AT END  GO TO REL-TEST-008-3                         RL1104.2
056200         NOT AT END  GO TO REL-TEST-008-A                         RL1104.2
056300     END-READ.                                                    RL1104.2
056400 REL-TEST-008-A.                                                  RL1104.2
056500     MOVE    RL-FS1R1-F-G-120 TO FILE-RECORD-INFO-P1-120 (1).     RL1104.2
056600     IF      UPDATE-NUMBER (1) EQUAL TO 99                        RL1104.2
056700             ADD  1 TO WRK-CS-09V00-009.                          RL1104.2
056800     IF      WRK-CS-09V00-007  EQUAL TO 4                         RL1104.2
056900             MOVE 01 TO WRK-CS-09V00-007                          RL1104.2
057000             ADD 1 TO WRK-CS-09V00-008.                           RL1104.2
057100     IF      RL-FS1-KEY EQUAL TO  XRECORD-NUMBER (1)              RL1104.2
057200             ADD 1 TO  WRK-CS-09V00-010.                          RL1104.2
057300     IF      XRECORD-NUMBER (1) EQUAL TO  WRK-CS-09V00-008        RL1104.2
057400             ADD 1 TO  WRK-CS-09V00-011.                          RL1104.2
057500     IF      WRK-CS-09V00-006 GREATER  501                        RL1104.2
057600             GO TO REL-TEST-008-3.                                RL1104.2
057700     GO TO   REL-TEST-008-R.                                      RL1104.2
057800 REL-TEST-008-3.                                                  RL1104.2
057900     IF      WRK-CS-09V00-006 NOT EQUAL TO 376                    RL1104.2
058000             MOVE "INCORRECT RECORD COUNT"  TO RE-MARK            RL1104.2
058100             MOVE WRK-CS-09V00-006 TO COMPUTED-18V0               RL1104.2
058200             MOVE 376 TO CORRECT-18V0                             RL1104.2
058300             PERFORM  FAIL                                        RL1104.2
058400     ELSE                                                         RL1104.2
058500             PERFORM  PASS.                                       RL1104.2
058600     PERFORM PRINT-DETAIL.                                        RL1104.2
058700     ADD     01 TO REC-CT.                                        RL1104.2
058800*    .01                                                          RL1104.2
058900     IF      WRK-CS-09V00-009 NOT EQUAL TO ZERO                   RL1104.2
059000             MOVE WRK-CS-09V00-009 TO COMPUTED-18V0               RL1104.2
059100             MOVE   ZERO TO CORRECT-18V0                          RL1104.2
059200             MOVE "DELETED RECORDS" TO RE-MARK                    RL1104.2
059300             PERFORM FAIL                                         RL1104.2
059400     ELSE                                                         RL1104.2
059500             PERFORM PASS.                                        RL1104.2
059600     PERFORM PRINT-DETAIL.                                        RL1104.2
059700     ADD     01  TO  REC-CT.                                      RL1104.2
059800*    .02                                                          RL1104.2
059900     IF      WRK-CS-09V00-010 NOT EQUAL TO 375                    RL1104.2
060000             MOVE "KEY MISMATCH" TO RE-MARK                       RL1104.2
060100             MOVE 375 TO CORRECT-18V0                             RL1104.2
060200             MOVE WRK-CS-09V00-010 TO COMPUTED-18V0               RL1104.2
060300             PERFORM FAIL                                         RL1104.2
060400     ELSE                                                         RL1104.2
060500             PERFORM PASS.                                        RL1104.2
060600     PERFORM PRINT-DETAIL.                                        RL1104.2
060700     ADD     01 TO REC-CT.                                        RL1104.2
060800*    .03                                                          RL1104.2
060900     IF      WRK-CS-09V00-011  NOT EQUAL TO 375                   RL1104.2
061000             MOVE   375  TO CORRECT-18V0                          RL1104.2
061100             MOVE  "INCORRECT RECORD FOUND"  TO RE-MARK           RL1104.2
061200             MOVE   WRK-CS-09V00-011 TO COMPUTED-18V0             RL1104.2
061300             PERFORM   FAIL                                       RL1104.2
061400     ELSE                                                         RL1104.2
061500             PERFORM  PASS.                                       RL1104.2
061600     PERFORM PRINT-DETAIL.                                        RL1104.2
061700     ADD     01 TO REC-CT.                                        RL1104.2
061800*04                                                               RL1104.2
061900     CLOSE   RL-FS1.                                              RL1104.2
062000 CCVS-EXIT SECTION.                                               RL1104.2
062100 CCVS-999999.                                                     RL1104.2
062200     GO TO CLOSE-FILES.                                           RL1104.2
