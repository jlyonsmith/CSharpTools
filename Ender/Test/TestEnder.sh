mcs GenEnderTestFiles.cs
mono GenEnderTestFiles.exe
ENDER="../Ender.exe"
mono $ENDER -?
mono $ENDER cr.txt
mono $ENDER lf.txt
mono $ENDER crlf.txt
mono $ENDER mixed1.txt
mono $ENDER mixed2.txt
mono $ENDER mixed3.txt
mono $ENDER mixed4.txt
mono $ENDER -m:lf -o:cr2lf.txt cr.txt
mono $ENDER -m:cr -o:lf2cr.txt lf.txt
mono $ENDER -m:lf -o:crlf2lf.txt crlf.txt
mono $ENDER -m:cr -o:crlf2cr.txt crlf.txt
mono $ENDER -m:auto mixed1.txt
mono $ENDER -m:auto mixed2.txt
mono $ENDER -m:auto mixed3.txt
mono $ENDER -m:auto mixed4.txt
#rm GenEnderTestFiles.exe
#rm *.txt