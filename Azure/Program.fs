open System
open Farmer
open Farmer.Builders

let audience = Environment.GetEnvironmentVariable  "AUTH0_AUDIENCE"
let issuer = Environment.GetEnvironmentVariable  "AUTH0_ISSUER"

// 1. Create a cosmos db
printfn "Creating CosmosDb"
let myCosmosDb = cosmosDb {
    name "sswrules-cosmosdb-staging"
    account_name "sswrulescosmosdb-staging"
    throughput 400<CosmosDb.RU>
    failover_policy CosmosDb.NoFailover
    consistency_policy (CosmosDb.Session)
}

// 2. Create a Functions App
printfn "Creating Functions App"
let myFunctions = functions {
    name "sswrules-functions-staging"
    zip_deploy "myFunctionsFolder"
    setting "OidcApiAuthorizationSettings:Audience" audience
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
