var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.DIBA_School_Portal_Server>("diba-school-portal-server");

builder.Build().Run();
