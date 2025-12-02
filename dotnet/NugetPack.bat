nuget pack ./KornSW.DocToolkit.nuspec -Build -Symbols -OutputDirectory ".\dist\Packages" -InstallPackageToOutputPath
IF NOT EXIST "..\..\(NuGetRepo)" GOTO NOCOPYTOGLOBALREPO
xcopy ".\dist\Packages\*.nuspec" "..\..\(NuGetRepo)\" /d /r /y /s
xcopy ".\dist\Packages\*.nupkg*" "..\..\(NuGetRepo)\" /d /r /y /s
:NOCOPYTOGLOBALREPO
PAUSE