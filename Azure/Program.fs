open System
open Farmer
open Farmer.Builders

let audience = Environment.GetEnvironmentVariable  "AUTH0_AUDIENCE"
let issuer = Environment.GetEnvironmentVariable  "AUTH0_ISSUER"

let myAppInsights = appInsights {
    name "sswrules-staging"
}

// 1. Create a Functions App
printfn "Creating Functions App"
let myFunctions = functions {
    name "sswrules-functions"
    service_plan_name "sswrules-serviceplan"
    setting "audience" audience
    setting "issuer" issuer
    link_to_app_insights myAppInsights.Name
}

// 2. Create a cosmos db
printfn "Creating CosmosDb"
let myCosmosDb = cosmosDb {
    name "sswrules-cosmosdb"
    account_name "sswrulescosmosdb"
    throughput 400<CosmosDb.RU>
    failover_policy CosmosDb.NoFailover
    consistency_policy (CosmosDb.BoundedStaleness(500, 1000))
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
