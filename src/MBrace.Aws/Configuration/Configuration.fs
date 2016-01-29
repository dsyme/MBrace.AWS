﻿namespace MBrace.AWS

open System
open System.Runtime.Serialization

open Amazon
open Amazon.Runtime

open MBrace.AWS.Runtime

/// Serializable wrapper for AWS RegionEndpoint
[<Sealed; DataContract>]
type AWSRegion (region : RegionEndpoint) =
    static let mk r = new AWSRegion(r)
    [<DataMember(Name = "SystemName")>]
    let name = region.SystemName
    member __.SystemName = name
    member internal __.RegionEndpoint = RegionEndpoint.GetBySystemName name

    override __.GetHashCode() = hash name
    override __.Equals y =
        match y with
        | :? AWSRegion as r -> name = r.SystemName
        | _ -> false

    interface IComparable with
        member __.CompareTo y =
            match y with
            | :? AWSRegion as r -> compare name r.SystemName
            | _ -> invalidArg "y" "invalid comparand"

    /// The Asia Pacific (Tokyo) endpoint.
    static member APNortheast1 = mk RegionEndpoint.APNortheast1
    /// The Asia Pacific (Seoul) endpoint.
    static member APNortheast2 = mk RegionEndpoint.APNortheast2
    /// The Asia Pacific (Singapore) endpoint.
    static member APSoutheast1 = mk RegionEndpoint.APSoutheast1
    /// The Asia Pacific (Sydney) endpoint.
    static member APSoutheast2 = mk RegionEndpoint.APSoutheast2
    /// The China (Beijing) endpoint.
    static member CNNorth1 = mk RegionEndpoint.CNNorth1
    static member EUCentral1 = mk RegionEndpoint.EUCentral1
    /// The South America (Sao Paulo) endpoint.
    static member SAEast1 = mk RegionEndpoint.SAEast1
    /// The US East (Virginia) endpoint.
    static member USEast1 = mk RegionEndpoint.USEast1
    /// The EU West (Ireland) endpoint.
    static member EUWest1 = mk RegionEndpoint.EUWest1
    /// The US GovCloud West (Oregon) endpoint.
    static member USGovCloudWest1 = mk RegionEndpoint.USGovCloudWest1
    /// The US West (N. California) endpoint.
    static member USWest1 = mk RegionEndpoint.USWest1
    /// The US West (Oregon) endpoint.
    static member USWest2 = mk RegionEndpoint.USWest2

/// Serializable AWS credentials record
[<NoEquality; NoComparison>]
type AWSCredentials = 
    {
        /// AWS account Access Key
        AccessKey :string
        /// AWS account Secret Key
        SecretKey : string
    }
with
    member internal __.Credentials = new BasicAWSCredentials(__.AccessKey, __.SecretKey) :> Amazon.Runtime.AWSCredentials

    static member FromCredentialStore(?profileName : string) =
        let profileName = defaultArg profileName "default"
        let creds = Amazon.Util.ProfileManager.GetAWSCredentials(profileName).GetCredentials()
        { AccessKey = creds.AccessKey ; SecretKey = creds.SecretKey }


/// Azure Configuration Builder. Used to specify MBrace.AWS cluster storage configuration.
[<AutoSerializable(true); Sealed; NoEquality; NoComparison>]
type Configuration(region : AWSRegion, credentials : AWSCredentials) =

    // Default Service Bus Configuration
    let mutable workItemQueue        = "MBraceWorkItemQueue"
    let mutable workItemTopic        = "MBraceWorkItemTopic"

    // Default Blob Storage Containers
    let mutable runtimeBucket    = "mbraceruntimedata"
    let mutable userDataBucket   = "mbraceuserdata"

    // Default Table Storage tables
    let mutable userDataTable       = "MBraceUserData"
    let mutable runtimeTable        = "MBraceRuntimeData"
    let mutable runtimeLogsTable    = "MBraceRuntimeLogs"

    /// AWS S3 Account credentials
    member val S3Credentials = credentials with get, set

    /// AWS DynamoDB Account credentials
    member val DynamoDBCredentials = credentials with get, set

    /// AWS SQS Account credentials
    member val SQSCredentials = credentials with get, set

    /// AWS Region
    member val Region = region with get, set

    /// Append version to given configuration e.g. $RuntimeQueue$Version. Defaults to true.
    member val UseVersionSuffix    = true with get, set

    /// Runtime identifier, used for runtime isolation when using the same storage/servicebus accounts. Defaults to 0.
    member val SuffixId             = 0us with get, set

    /// Append runtime id to given configuration e.g. $RuntimeQueue$Version$Id. Defaults to false.
    member val UseSuffixId          = false with get, set

    /// Specifies wether the cluster should optimize closure serialization. Defaults to true.
    member val OptimizeClosureSerialization = true with get, set

    /// Service Bus work item queue used by the runtime.
    member __.WorkItemQueue
        with get () = workItemQueue
        and set rq = workItemQueue <- rq

    /// Service Bus work item topic used by the runtime.
    member __.WorkItemTopic
        with get () = workItemTopic
        and set rt = workItemTopic <- rt

    /// Azure Storage container used by the runtime.
    member __.RuntimeBucket
        with get () = runtimeBucket
        and set rc = Validate.bucketName rc ; runtimeBucket <- rc

    /// Azure Storage container used for user data.
    member __.UserDataBucket
        with get () = userDataBucket
        and set udb = Validate.bucketName udb ; userDataBucket <- udb

    /// Azure Storage table used by the runtime.
    member __.RuntimeTable
        with get () = runtimeTable
        and set rt = Validate.tableName rt; runtimeTable <- rt

    /// Azure Storage table used by the runtime for storing logs.
    member __.RuntimeLogsTable
        with get () = runtimeLogsTable
        and set rlt = Validate.tableName rlt ; runtimeLogsTable <- rlt

    /// Azure Storage table used for user data.
    member __.UserDataTable
        with get () = userDataTable
        and set udt = Validate.tableName udt ; userDataTable <- udt

    /// Create a configuration object by reading credentials from the local store
    static member FromCredentialsStore(region : AWSRegion, ?profileName : string) =
        let credentials = AWSCredentials.FromCredentialStore(?profileName = profileName)
        new Configuration(region, credentials)