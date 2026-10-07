#!/bin/bash
# usage: gate.sh <label>   (build + wave-local gate per golden-lander-brief rule 4, adapted)
L=/tmp/gl/gate-$1; mkdir -p $L; cd /home/user/CobolSharp
dotnet build Cobol.Net.sln -c Debug > $L/build.log 2>&1; echo "BUILD-EXIT=$?" >> $L/build.log
dotnet test tests/Cobol.Net.Tests.Conformance --no-build --filter "FullyQualifiedName~Corpus|FullyQualifiedName~Negative|FullyQualifiedName~Intrinsic|FullyQualifiedName~VersionMatrix" --logger "trx;LogFileName=$L/conf.trx" > $L/conf.log 2>&1; echo "CONF-EXIT=$?" >> $L/conf.log
dotnet test tests/Cobol.Net.Tests.Unit --no-build --filter "FullyQualifiedName~SpecTraceabilityInventory|FullyQualifiedName~DefectiveRowCoverage|FullyQualifiedName~Manifest|FullyQualifiedName~AnnexA1Register" --logger "trx;LogFileName=$L/unit.trx" > $L/unit.log 2>&1; echo "UNIT-EXIT=$?" >> $L/unit.log
echo GATE-DONE > $L/done
