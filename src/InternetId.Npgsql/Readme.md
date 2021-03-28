# InternetId.Npgsql Readme

```
cd ./to-solution-folder

# This is required once after cloning the repo
dotnet tool restore

# Add a migration
dotnet ef migrations add 1 -c UsersDbContext -p InternetId.Npgsql -s InternetId.Server

# Apply migrations
dotnet ef database update -c UsersDbContext -s InternetId.Server

# Revert all migrations
dotnet ef database update 0 -c UsersDbContext -s InternetId.Server

# Remove last migration, the migration cannot be applied to the database
dotnet ef migrations remove -c UsersDbContext -p Users.Data.PostgreSQL -s InternetId.Server

# Remove all migrations
rmdir .\Users.Data.PostgreSQL\Migrations\ -Recurse

# Start over: DELETE all migrations, add the Init migration and apply start over
dotnet ef database update 0 -c UsersDbContext -s InternetId.Server; dotnet ef migrations add 1 -c UsersDbContext -p Users.Data.PostgreSQL -s InternetId.Server; dotnet ef database update -c UsersDbContext -s InternetId.Server
```
