echo $TargetDir
echo $ProjectName
copy /Y "$(ProjectDir)${OutDir}$(ProjectName).dll" "C:\Users\disro\AppData\Roaming\r2modmanPlus-local\RiskOfRain2\profiles\ListEclipseLevelsMod\BepInEx\plugins\$(ProjectName).dll"
copy /Y "$(ProjectDir)${OutDir}$(ProjectName).pdb" "C:\Users\disro\AppData\Roaming\r2modmanPlus-local\RiskOfRain2\profiles\ListEclipseLevelsMod\BepInEx\plugins\$(ProjectName).pdb"