var builder = DistributedApplication.CreateBuilder(args);

// To have a persistent volume across container instances, it must be named.
var sqlDatabase = builder.AddSqlServer("jordnaer-sqlserver")
						 .WithVolumeMount("VolumeMount.sqlserver.data", "/var/opt/mssql")
						 .AddDatabase("jordnaer");

builder.AddProject<Projects.Jordnaer>("jordnaer-web")
						.WithReference(sqlDatabase);

builder.Build().Run();
