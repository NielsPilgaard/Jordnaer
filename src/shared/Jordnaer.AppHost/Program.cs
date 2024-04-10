var builder = DistributedApplication.CreateBuilder(args);

var sqlDatabase = builder.AddSqlServer("jordnaer-sqlserver")
						 .WithVolumeMount("VolumeMount.sqlserver.data", "/var/opt/mssql")
						 .AddDatabase("jordnaer");

builder.AddProject<Projects.Jordnaer>("jordnaer-web")
						.WithReference(sqlDatabase);

builder.Build().Run();
