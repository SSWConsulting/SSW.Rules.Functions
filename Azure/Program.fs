open System
open Farmer
open Farmer.Builders

let audience = Environment.GetEnvironmentVariable  "OAUTH_AUDIENCE"
let issuer = Environment.GetEnvironmentVariable  "OAUTH_ISSUER"
let openIdConfig = Environment.GetEnvironmentVariable  "OAUTH_OPENIDCONFIG"
let mutable namePrefix = Environment.GetEnvironmentVariable "AZURE_RG_PREFIX"

if isNull namePrefix then
    namePrefix <- "sswrules-local"

// 1. Create a cosmos db
printfn "Creating CosmosDb"
let myCosmosDb = cosmosDb {
    name (namePrefix + "-cosmosdb")
    account_name (namePrefix + "-cosmosaccount")
    throughput 400<CosmosDb.RU>
    failover_policy CosmosDb.NoFailover
    consistency_policy (CosmosDb.Session)
}

// 2. Create a Functions App
printfn "Creating Functions App"
let myFunctions = functions {
    name (namePrefix + "-functions")
    setting "OidcApiAuthorizationSettings:Audience" audience
    setting "OidcApiAuthorizationSettings:OpenIdConfigUrl" openIdConfig
    setting "OidcApiAuthorizationSettings:IssuerUrl" issuer
    setting "CosmosDb:Account" myCosmosDb.Endpoint
    setting "CosmosDb:Key" myCosmosDb.PrimaryKey
    setting "CosmosDb:DatabaseName" myCosmosDb.DbName
}

let deployment = arm {
    location Location.AustraliaEast
    add_resource myFunctions
    add_resource myCosmosDb
}

printfn "Creating ARM template..."
deployment
|> Writer.quickWrite "arm-template"

printfn "All done! The template has been saved to arm-template.json"

let json =
    deployment.Template
    |> Writer.toJson
printfn "%s" json
