
if(!(Test-Path -Path NugetOut ))
{
    mkdir NugetOut
}

$dirs = dir src -Directory
cd src
foreach($dir in $dirs)
{
    $name = $dir.Name
    if($name -notmatch "artifacts")
    {
		echo $name
		cd $name
		dotnet pack -c Release
		cp ./bin/Release/*-alpha1.nupkg ../../NugetOut
		cd ..
        #if(!(Test-Path -Path "NugetOut/$name" ))
        #{
        #    mkdir "NugetOut/$name"
        #}

        #cp -r -Force "artifacts/bin/$name/Debug/*" "NugetOut/$name/"
    }
}
cd ..
