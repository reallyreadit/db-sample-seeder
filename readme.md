Seeds the database with sample data. 

Depends on the `api` project folder being a sibling to this folder.

To develop, run the VSCode Remote container.

```
dotnet restore
dotnet run
```

For some reason, on the first restore, NuGet does not find the local /packages package referenced by nuget.config in /api. Run dotnet restore 3 times.
