# InternetID

# Upgrading

```sh
dotnet tool install --global dotnet-outdated-tool
dotnet tool update --global dotnet-outdated-tool

cd [solution-folder]
dotnet-outdated -inc Microsoft. -inc System. -inc EntityFramework -inc EFCore -exc Npgsql -vl Major -u
dotnet-outdated -exc Microsoft. -exc System. -exc EntityFramework -exc EFCore -exc Npgsql -u
dotnet-outdated -inc OpenIddict -u
```