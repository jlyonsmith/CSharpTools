mcs GenSpacerTestFiles.cs
mono GenSpacerTestFiles.exe
SPACER="../Spacer.exe"
mono $SPACER -?
echo Test.cs:
hexdump Test.cs
mono $SPACER Test.cs -m:t -o:TestTabs.cs
mono $SPACER TestTabs.cs
echo TestTabs.cs:
hexdump TestTabs.cs
mono $SPACER Test.cs -m:s -o:TestSpaces.cs
mono $SPACER TestSpaces.cs
echo TestSpaces.cs:
hexdump TestSpaces.cs
echo -----------------
echo Test.tson:
hexdump Test.tson
mono $SPACER Test.tson -m:t -o:TestTabs.tson
mono $SPACER TestTabs.tson
echo TestTabs.tson:
hexdump TestTabs.tson
mono $SPACER Test.tson -m:s -o:TestSpaces.tson
mono $SPACER TestSpaces.tson
echo TestSpaces.tson:
hexdump TestSpaces.tson
# rm GenSpacerTestFiles.exe
# rm *.txt
