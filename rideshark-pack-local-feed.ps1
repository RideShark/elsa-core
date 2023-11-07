$projects = Get-ChildItem -Recurse -Filter *.csproj

foreach ($project in $projects)
{
    dotnet pack $project.FullName -o ./packages -p:PackageVersion=20.5.0
}

Get-ChildItem -Path ./packages -Filter *.nupkg | ForEach-Object {dotnet nuget push $_.FullName --api-key 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 -s https://nuget.pkg.github.com/RideShark/index.json --skip-duplicate}