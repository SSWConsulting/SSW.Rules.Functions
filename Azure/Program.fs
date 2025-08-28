open System
open Farmer
open Farmer.Builders

let audience = Environment.GetEnvironmentVariable  "AUTH0_AUDIENCE"
let apiAudience = Environment.GetEnvironmentVariable "AUTH0_APIAUDIENCE"
let issuer = Environment.GetEnvironmentVariable  "AUTH0_ISSUER"
let mutable namePrefix = Environment.GetEnvironmentVariable "AZURE_RG_PREFIX"
let gitHubToken = Environment.GetEnvironmentVariable "GITHUB_TOKEN" 
let cmsOAuthClientId = Environment.GetEnvironmentVariable "CMS_OAUTH_CLIENT_ID"
let cmsOAuthClientSecret = Environment.GetEnvironmentVariable "CMS_OAUTH_CLIENT_SECRET"
let corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS")
                    |> fun origins -> origins.Split(',') |> Array.toList

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
    use_runtime DotNetIsolated
    name (namePrefix + "-functions")
    setting "GitHub:Token" gitHubToken
    setting "CMS_OAUTH_CLIENT_ID" cmsOAuthClientId
    setting "CMS_OAUTH_CLIENT_SECRET" cmsOAuthClientSecret
    setting "OidcApiAuthorizationSettings:Audience" audience
    setting "OidcApiAuthorizationSettings:ApiAudience" apiAudience
    setting "OidcApiAuthorizationSettings:IssuerUrl" issuer
    setting "CosmosDb:Account" myCosmosDb.Endpoint
    setting "CosmosDb:Key" myCosmosDb.PrimaryKey
    setting "CosmosDb:DatabaseName" myCosmosDb.DbName

    enable_cors corsOrigins
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
