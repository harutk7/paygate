var builder = DistributedApplication.CreateBuilder(args);

// SQL Server
var sqlServer = builder.AddSqlServer("sql", port: 18060)
    .AddDatabase("paymentdb");

// API Services — modify auto-created endpoints to set fixed ports
var identityApi = builder.AddProject<Projects.PaymentGateway_Identity_Api>("identity-api")
    .WithEndpoint("http", e => e.Port = 18011)
    .WithEndpoint("https", e => e.Port = 18010)
    .WithReference(sqlServer)
    .WaitFor(sqlServer);

var billingApi = builder.AddProject<Projects.PaymentGateway_Billing_Api>("billing-api")
    .WithEndpoint("http", e => e.Port = 18021)
    .WithEndpoint("https", e => e.Port = 18020)
    .WithReference(sqlServer)
    .WaitFor(sqlServer);

var gatewayApi = builder.AddProject<Projects.PaymentGateway_Gateway_Api>("gateway-api")
    .WithEndpoint("http", e => e.Port = 18031)
    .WithEndpoint("https", e => e.Port = 18030)
    .WithReference(sqlServer)
    .WaitFor(sqlServer);

var backofficeApi = builder.AddProject<Projects.PaymentGateway_Backoffice_Api>("backoffice-api")
    .WithEndpoint("http", e => e.Port = 18041)
    .WithEndpoint("https", e => e.Port = 18040)
    .WithReference(sqlServer)
    .WaitFor(sqlServer);

// React frontends (Vite + pnpm) — modify auto-created http endpoint
var landing = builder.AddViteApp("landing", "../../frontends/apps/landing")
    .WithPnpm()
    .WithEndpoint("http", e => e.Port = 18050)
    .WithReference(identityApi)
    .WithReference(billingApi);

var portal = builder.AddViteApp("portal", "../../frontends/apps/portal")
    .WithPnpm()
    .WithEndpoint("http", e => e.Port = 18051)
    .WithReference(identityApi)
    .WithReference(billingApi)
    .WithReference(gatewayApi);

var backofficeUi = builder.AddViteApp("backoffice-ui", "../../frontends/apps/backoffice")
    .WithPnpm()
    .WithEndpoint("http", e => e.Port = 18052)
    .WithReference(identityApi)
    .WithReference(backofficeApi);

builder.Build().Run();
