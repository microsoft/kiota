﻿using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Kiota.Builder.Caching.Tests;

public class UpdateServiceTests
{
    private const string ResponsePayload = @"[    {
        ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/108014494"",
        ""assets_url"": ""https://api.github.com/repos/microsoft/kiota/releases/108014494/assets"",
        ""upload_url"": ""https://uploads.github.com/repos/microsoft/kiota/releases/108014494/assets{?name,label}"",
        ""html_url"": ""https://github.com/microsoft/kiota/releases/tag/v1.3.0"",
        ""id"": 108014494,
        ""author"": {
            ""login"": ""baywet"",
            ""id"": 7905502,
            ""node_id"": ""MDQ6VXNlcjc5MDU1MDI="",
            ""avatar_url"": ""https://avatars.githubusercontent.com/u/7905502?v=4"",
            ""gravatar_id"": """",
            ""url"": ""https://api.github.com/users/baywet"",
            ""html_url"": ""https://github.com/baywet"",
            ""followers_url"": ""https://api.github.com/users/baywet/followers"",
            ""following_url"": ""https://api.github.com/users/baywet/following{/other_user}"",
            ""gists_url"": ""https://api.github.com/users/baywet/gists{/gist_id}"",
            ""starred_url"": ""https://api.github.com/users/baywet/starred{/owner}{/repo}"",
            ""subscriptions_url"": ""https://api.github.com/users/baywet/subscriptions"",
            ""organizations_url"": ""https://api.github.com/users/baywet/orgs"",
            ""repos_url"": ""https://api.github.com/users/baywet/repos"",
            ""events_url"": ""https://api.github.com/users/baywet/events{/privacy}"",
            ""received_events_url"": ""https://api.github.com/users/baywet/received_events"",
            ""type"": ""User"",
            ""site_admin"": false
        },
        ""node_id"": ""RE_kwDOE0q91s4GcCue"",
        ""tag_name"": ""v1.3.0"",
        ""target_commitish"": ""main"",
        ""name"": ""v1.3.0"",
        ""draft"": false,
        ""prerelease"": false,
        ""created_at"": ""2023-06-09T16:35:09Z"",
        ""published_at"": ""2023-06-09T17:38:14Z"",
        ""assets"": [
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/112053226"",
                ""id"": 112053226,
                ""node_id"": ""RA_kwDOE0q91s4Grcvq"",
                ""name"": ""kiota-1.3.0.vsix"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""baywet"",
                    ""id"": 7905502,
                    ""node_id"": ""MDQ6VXNlcjc5MDU1MDI="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/7905502?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/baywet"",
                    ""html_url"": ""https://github.com/baywet"",
                    ""followers_url"": ""https://api.github.com/users/baywet/followers"",
                    ""following_url"": ""https://api.github.com/users/baywet/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/baywet/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/baywet/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/baywet/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/baywet/orgs"",
                    ""repos_url"": ""https://api.github.com/users/baywet/repos"",
                    ""events_url"": ""https://api.github.com/users/baywet/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/baywet/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/octet-stream"",
                ""state"": ""uploaded"",
                ""size"": 418147,
                ""download_count"": 2,
                ""created_at"": ""2023-06-09T17:36:38Z"",
                ""updated_at"": ""2023-06-09T17:36:39Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.3.0/kiota-1.3.0.vsix""
            },
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/112053360"",
                ""id"": 112053360,
                ""node_id"": ""RA_kwDOE0q91s4Grcxw"",
                ""name"": ""linux-x64.zip"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""baywet"",
                    ""id"": 7905502,
                    ""node_id"": ""MDQ6VXNlcjc5MDU1MDI="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/7905502?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/baywet"",
                    ""html_url"": ""https://github.com/baywet"",
                    ""followers_url"": ""https://api.github.com/users/baywet/followers"",
                    ""following_url"": ""https://api.github.com/users/baywet/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/baywet/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/baywet/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/baywet/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/baywet/orgs"",
                    ""repos_url"": ""https://api.github.com/users/baywet/repos"",
                    ""events_url"": ""https://api.github.com/users/baywet/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/baywet/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/x-zip-compressed"",
                ""state"": ""uploaded"",
                ""size"": 32039545,
                ""download_count"": 199,
                ""created_at"": ""2023-06-09T17:37:47Z"",
                ""updated_at"": ""2023-06-09T17:37:59Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.3.0/linux-x64.zip""
            },
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/112053267"",
                ""id"": 112053267,
                ""node_id"": ""RA_kwDOE0q91s4GrcwT"",
                ""name"": ""Microsoft.OpenApi.Kiota.1.3.0.nupkg"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""baywet"",
                    ""id"": 7905502,
                    ""node_id"": ""MDQ6VXNlcjc5MDU1MDI="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/7905502?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/baywet"",
                    ""html_url"": ""https://github.com/baywet"",
                    ""followers_url"": ""https://api.github.com/users/baywet/followers"",
                    ""following_url"": ""https://api.github.com/users/baywet/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/baywet/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/baywet/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/baywet/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/baywet/orgs"",
                    ""repos_url"": ""https://api.github.com/users/baywet/repos"",
                    ""events_url"": ""https://api.github.com/users/baywet/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/baywet/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/octet-stream"",
                ""state"": ""uploaded"",
                ""size"": 3115492,
                ""download_count"": 0,
                ""created_at"": ""2023-06-09T17:36:58Z"",
                ""updated_at"": ""2023-06-09T17:36:59Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.3.0/Microsoft.OpenApi.Kiota.1.3.0.nupkg""
            },
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/112053270"",
                ""id"": 112053270,
                ""node_id"": ""RA_kwDOE0q91s4GrcwW"",
                ""name"": ""Microsoft.OpenApi.Kiota.1.3.0.snupkg"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""baywet"",
                    ""id"": 7905502,
                    ""node_id"": ""MDQ6VXNlcjc5MDU1MDI="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/7905502?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/baywet"",
                    ""html_url"": ""https://github.com/baywet"",
                    ""followers_url"": ""https://api.github.com/users/baywet/followers"",
                    ""following_url"": ""https://api.github.com/users/baywet/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/baywet/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/baywet/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/baywet/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/baywet/orgs"",
                    ""repos_url"": ""https://api.github.com/users/baywet/repos"",
                    ""events_url"": ""https://api.github.com/users/baywet/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/baywet/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/octet-stream"",
                ""state"": ""uploaded"",
                ""size"": 223210,
                ""download_count"": 0,
                ""created_at"": ""2023-06-09T17:36:59Z"",
                ""updated_at"": ""2023-06-09T17:36:59Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.3.0/Microsoft.OpenApi.Kiota.1.3.0.snupkg""
            },
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/112053271"",
                ""id"": 112053271,
                ""node_id"": ""RA_kwDOE0q91s4GrcwX"",
                ""name"": ""Microsoft.OpenApi.Kiota.ApiDescription.Client.0.5.0-preview2.nupkg"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""baywet"",
                    ""id"": 7905502,
                    ""node_id"": ""MDQ6VXNlcjc5MDU1MDI="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/7905502?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/baywet"",
                    ""html_url"": ""https://github.com/baywet"",
                    ""followers_url"": ""https://api.github.com/users/baywet/followers"",
                    ""following_url"": ""https://api.github.com/users/baywet/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/baywet/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/baywet/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/baywet/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/baywet/orgs"",
                    ""repos_url"": ""https://api.github.com/users/baywet/repos"",
                    ""events_url"": ""https://api.github.com/users/baywet/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/baywet/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/octet-stream"",
                ""state"": ""uploaded"",
                ""size"": 14614,
                ""download_count"": 0,
                ""created_at"": ""2023-06-09T17:36:59Z"",
                ""updated_at"": ""2023-06-09T17:37:00Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.3.0/Microsoft.OpenApi.Kiota.ApiDescription.Client.0.5.0-preview2.nupkg""
            },
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/112053272"",
                ""id"": 112053272,
                ""node_id"": ""RA_kwDOE0q91s4GrcwY"",
                ""name"": ""Microsoft.OpenApi.Kiota.Builder.1.3.0.nupkg"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""baywet"",
                    ""id"": 7905502,
                    ""node_id"": ""MDQ6VXNlcjc5MDU1MDI="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/7905502?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/baywet"",
                    ""html_url"": ""https://github.com/baywet"",
                    ""followers_url"": ""https://api.github.com/users/baywet/followers"",
                    ""following_url"": ""https://api.github.com/users/baywet/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/baywet/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/baywet/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/baywet/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/baywet/orgs"",
                    ""repos_url"": ""https://api.github.com/users/baywet/repos"",
                    ""events_url"": ""https://api.github.com/users/baywet/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/baywet/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/octet-stream"",
                ""state"": ""uploaded"",
                ""size"": 375417,
                ""download_count"": 0,
                ""created_at"": ""2023-06-09T17:37:00Z"",
                ""updated_at"": ""2023-06-09T17:37:00Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.3.0/Microsoft.OpenApi.Kiota.Builder.1.3.0.nupkg""
            },
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/112053274"",
                ""id"": 112053274,
                ""node_id"": ""RA_kwDOE0q91s4Grcwa"",
                ""name"": ""Microsoft.OpenApi.Kiota.Builder.1.3.0.snupkg"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""baywet"",
                    ""id"": 7905502,
                    ""node_id"": ""MDQ6VXNlcjc5MDU1MDI="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/7905502?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/baywet"",
                    ""html_url"": ""https://github.com/baywet"",
                    ""followers_url"": ""https://api.github.com/users/baywet/followers"",
                    ""following_url"": ""https://api.github.com/users/baywet/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/baywet/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/baywet/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/baywet/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/baywet/orgs"",
                    ""repos_url"": ""https://api.github.com/users/baywet/repos"",
                    ""events_url"": ""https://api.github.com/users/baywet/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/baywet/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/octet-stream"",
                ""state"": ""uploaded"",
                ""size"": 165029,
                ""download_count"": 0,
                ""created_at"": ""2023-06-09T17:37:00Z"",
                ""updated_at"": ""2023-06-09T17:37:01Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.3.0/Microsoft.OpenApi.Kiota.Builder.1.3.0.snupkg""
            },
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/112053370"",
                ""id"": 112053370,
                ""node_id"": ""RA_kwDOE0q91s4Grcx6"",
                ""name"": ""osx-arm64.zip"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""baywet"",
                    ""id"": 7905502,
                    ""node_id"": ""MDQ6VXNlcjc5MDU1MDI="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/7905502?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/baywet"",
                    ""html_url"": ""https://github.com/baywet"",
                    ""followers_url"": ""https://api.github.com/users/baywet/followers"",
                    ""following_url"": ""https://api.github.com/users/baywet/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/baywet/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/baywet/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/baywet/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/baywet/orgs"",
                    ""repos_url"": ""https://api.github.com/users/baywet/repos"",
                    ""events_url"": ""https://api.github.com/users/baywet/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/baywet/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/x-zip-compressed"",
                ""state"": ""uploaded"",
                ""size"": 31025738,
                ""download_count"": 189,
                ""created_at"": ""2023-06-09T17:37:59Z"",
                ""updated_at"": ""2023-06-09T17:38:11Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.3.0/osx-arm64.zip""
            },
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/112053298"",
                ""id"": 112053298,
                ""node_id"": ""RA_kwDOE0q91s4Grcwy"",
                ""name"": ""osx-x64.zip"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""baywet"",
                    ""id"": 7905502,
                    ""node_id"": ""MDQ6VXNlcjc5MDU1MDI="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/7905502?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/baywet"",
                    ""html_url"": ""https://github.com/baywet"",
                    ""followers_url"": ""https://api.github.com/users/baywet/followers"",
                    ""following_url"": ""https://api.github.com/users/baywet/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/baywet/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/baywet/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/baywet/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/baywet/orgs"",
                    ""repos_url"": ""https://api.github.com/users/baywet/repos"",
                    ""events_url"": ""https://api.github.com/users/baywet/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/baywet/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/x-zip-compressed"",
                ""state"": ""uploaded"",
                ""size"": 32395654,
                ""download_count"": 151,
                ""created_at"": ""2023-06-09T17:37:11Z"",
                ""updated_at"": ""2023-06-09T17:37:24Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.3.0/osx-x64.zip""
            },
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/112053323"",
                ""id"": 112053323,
                ""node_id"": ""RA_kwDOE0q91s4GrcxL"",
                ""name"": ""win-x64.zip"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""baywet"",
                    ""id"": 7905502,
                    ""node_id"": ""MDQ6VXNlcjc5MDU1MDI="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/7905502?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/baywet"",
                    ""html_url"": ""https://github.com/baywet"",
                    ""followers_url"": ""https://api.github.com/users/baywet/followers"",
                    ""following_url"": ""https://api.github.com/users/baywet/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/baywet/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/baywet/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/baywet/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/baywet/orgs"",
                    ""repos_url"": ""https://api.github.com/users/baywet/repos"",
                    ""events_url"": ""https://api.github.com/users/baywet/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/baywet/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/x-zip-compressed"",
                ""state"": ""uploaded"",
                ""size"": 32210339,
                ""download_count"": 467,
                ""created_at"": ""2023-06-09T17:37:24Z"",
                ""updated_at"": ""2023-06-09T17:37:36Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.3.0/win-x64.zip""
            },
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/112053344"",
                ""id"": 112053344,
                ""node_id"": ""RA_kwDOE0q91s4Grcxg"",
                ""name"": ""win-x86.zip"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""baywet"",
                    ""id"": 7905502,
                    ""node_id"": ""MDQ6VXNlcjc5MDU1MDI="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/7905502?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/baywet"",
                    ""html_url"": ""https://github.com/baywet"",
                    ""followers_url"": ""https://api.github.com/users/baywet/followers"",
                    ""following_url"": ""https://api.github.com/users/baywet/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/baywet/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/baywet/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/baywet/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/baywet/orgs"",
                    ""repos_url"": ""https://api.github.com/users/baywet/repos"",
                    ""events_url"": ""https://api.github.com/users/baywet/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/baywet/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/x-zip-compressed"",
                ""state"": ""uploaded"",
                ""size"": 29766398,
                ""download_count"": 128,
                ""created_at"": ""2023-06-09T17:37:36Z"",
                ""updated_at"": ""2023-06-09T17:37:47Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.3.0/win-x86.zip""
            }
        ],
        ""tarball_url"": ""https://api.github.com/repos/microsoft/kiota/tarball/v1.3.0"",
        ""zipball_url"": ""https://api.github.com/repos/microsoft/kiota/zipball/v1.3.0"",
        ""body"": ""### Changed\r\n\r\n- Changed python model classes to dataclasses. [#2684](https://github.com/microsoft/kiota/issues/2684)\r\n- Fix issue with command conflicts causing CLI crashes. (Shell)\r\n- Fix build error by splitting the ambiguous `--file` option into `--input-file` and `--output-file`. (Shell)\r\n- Fixed including unused imports in Go [#2699](https://github.com/microsoft/kiota/pull/2410)\r\n- Fixed a bug where error response type with primitive types would cause compile errors in dotnet [#2651](https://github.com/microsoft/kiota/issues/2693)\r\n- Fixed a bug where CSharp generation would fail if the input openApi contained schemas named 'TimeOnly' or 'DateOnly' [2671](https://github.com/microsoft/kiota/issues/2671)\r\n- Updated the reserved types for CSharp to include 'Stream' and 'Date' should be reserved names in CSharp [2369](https://github.com/microsoft/kiota/issues/2369)\r\n- Fix issue with request builders with parameters being excluded from commands output. (Shell)\r\n- Fixed a bug in setting default enum values fails if the symbol has been sanitized and the symbol only contains special characters [2360](https://github.com/microsoft/kiota/issues/2360)\r\n- Fixed issue where duplicate query parameter names per path were added to the URL template. Now only distinct query parameter names are added. [2725](https://github.com/microsoft/kiota/issues/2725)""
    },
    {
        ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/103269038"",
        ""assets_url"": ""https://api.github.com/repos/microsoft/kiota/releases/103269038/assets"",
        ""upload_url"": ""https://uploads.github.com/repos/microsoft/kiota/releases/103269038/assets{?name,label}"",
        ""html_url"": ""https://github.com/microsoft/kiota/releases/tag/v1.2.1"",
        ""id"": 103269038,
        ""author"": {
            ""login"": ""andrueastman"",
            ""id"": 6464005,
            ""node_id"": ""MDQ6VXNlcjY0NjQwMDU="",
            ""avatar_url"": ""https://avatars.githubusercontent.com/u/6464005?v=4"",
            ""gravatar_id"": """",
            ""url"": ""https://api.github.com/users/andrueastman"",
            ""html_url"": ""https://github.com/andrueastman"",
            ""followers_url"": ""https://api.github.com/users/andrueastman/followers"",
            ""following_url"": ""https://api.github.com/users/andrueastman/following{/other_user}"",
            ""gists_url"": ""https://api.github.com/users/andrueastman/gists{/gist_id}"",
            ""starred_url"": ""https://api.github.com/users/andrueastman/starred{/owner}{/repo}"",
            ""subscriptions_url"": ""https://api.github.com/users/andrueastman/subscriptions"",
            ""organizations_url"": ""https://api.github.com/users/andrueastman/orgs"",
            ""repos_url"": ""https://api.github.com/users/andrueastman/repos"",
            ""events_url"": ""https://api.github.com/users/andrueastman/events{/privacy}"",
            ""received_events_url"": ""https://api.github.com/users/andrueastman/received_events"",
            ""type"": ""User"",
            ""site_admin"": false
        },
        ""node_id"": ""RE_kwDOE0q91s4GJ8Ku"",
        ""tag_name"": ""v1.2.1"",
        ""target_commitish"": ""main"",
        ""name"": ""v1.2.1"",
        ""draft"": false,
        ""prerelease"": false,
        ""created_at"": ""2023-05-17T06:31:30Z"",
        ""published_at"": ""2023-05-17T07:36:35Z"",
        ""assets"": [
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/108527136"",
                ""id"": 108527136,
                ""node_id"": ""RA_kwDOE0q91s4Gd_4g"",
                ""name"": ""kiota-1.2.1.vsix"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""andrueastman"",
                    ""id"": 6464005,
                    ""node_id"": ""MDQ6VXNlcjY0NjQwMDU="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/6464005?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/andrueastman"",
                    ""html_url"": ""https://github.com/andrueastman"",
                    ""followers_url"": ""https://api.github.com/users/andrueastman/followers"",
                    ""following_url"": ""https://api.github.com/users/andrueastman/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/andrueastman/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/andrueastman/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/andrueastman/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/andrueastman/orgs"",
                    ""repos_url"": ""https://api.github.com/users/andrueastman/repos"",
                    ""events_url"": ""https://api.github.com/users/andrueastman/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/andrueastman/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/vsix"",
                ""state"": ""uploaded"",
                ""size"": 418150,
                ""download_count"": 5,
                ""created_at"": ""2023-05-17T07:30:51Z"",
                ""updated_at"": ""2023-05-17T07:30:55Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.2.1/kiota-1.2.1.vsix""
            },
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/108527349"",
                ""id"": 108527349,
                ""node_id"": ""RA_kwDOE0q91s4Gd_71"",
                ""name"": ""linux-x64.zip"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""andrueastman"",
                    ""id"": 6464005,
                    ""node_id"": ""MDQ6VXNlcjY0NjQwMDU="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/6464005?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/andrueastman"",
                    ""html_url"": ""https://github.com/andrueastman"",
                    ""followers_url"": ""https://api.github.com/users/andrueastman/followers"",
                    ""following_url"": ""https://api.github.com/users/andrueastman/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/andrueastman/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/andrueastman/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/andrueastman/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/andrueastman/orgs"",
                    ""repos_url"": ""https://api.github.com/users/andrueastman/repos"",
                    ""events_url"": ""https://api.github.com/users/andrueastman/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/andrueastman/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/x-zip-compressed"",
                ""state"": ""uploaded"",
                ""size"": 31948693,
                ""download_count"": 247,
                ""created_at"": ""2023-05-17T07:32:46Z"",
                ""updated_at"": ""2023-05-17T07:32:53Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.2.1/linux-x64.zip""
            },
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/108527224"",
                ""id"": 108527224,
                ""node_id"": ""RA_kwDOE0q91s4Gd_54"",
                ""name"": ""Microsoft.OpenApi.Kiota.1.2.1.nupkg"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""andrueastman"",
                    ""id"": 6464005,
                    ""node_id"": ""MDQ6VXNlcjY0NjQwMDU="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/6464005?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/andrueastman"",
                    ""html_url"": ""https://github.com/andrueastman"",
                    ""followers_url"": ""https://api.github.com/users/andrueastman/followers"",
                    ""following_url"": ""https://api.github.com/users/andrueastman/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/andrueastman/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/andrueastman/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/andrueastman/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/andrueastman/orgs"",
                    ""repos_url"": ""https://api.github.com/users/andrueastman/repos"",
                    ""events_url"": ""https://api.github.com/users/andrueastman/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/andrueastman/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/octet-stream"",
                ""state"": ""uploaded"",
                ""size"": 3030900,
                ""download_count"": 4,
                ""created_at"": ""2023-05-17T07:31:54Z"",
                ""updated_at"": ""2023-05-17T07:31:56Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.2.1/Microsoft.OpenApi.Kiota.1.2.1.nupkg""
            },
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/108527231"",
                ""id"": 108527231,
                ""node_id"": ""RA_kwDOE0q91s4Gd_5_"",
                ""name"": ""Microsoft.OpenApi.Kiota.1.2.1.snupkg"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""andrueastman"",
                    ""id"": 6464005,
                    ""node_id"": ""MDQ6VXNlcjY0NjQwMDU="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/6464005?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/andrueastman"",
                    ""html_url"": ""https://github.com/andrueastman"",
                    ""followers_url"": ""https://api.github.com/users/andrueastman/followers"",
                    ""following_url"": ""https://api.github.com/users/andrueastman/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/andrueastman/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/andrueastman/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/andrueastman/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/andrueastman/orgs"",
                    ""repos_url"": ""https://api.github.com/users/andrueastman/repos"",
                    ""events_url"": ""https://api.github.com/users/andrueastman/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/andrueastman/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/octet-stream"",
                ""state"": ""uploaded"",
                ""size"": 222781,
                ""download_count"": 4,
                ""created_at"": ""2023-05-17T07:31:56Z"",
                ""updated_at"": ""2023-05-17T07:31:57Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.2.1/Microsoft.OpenApi.Kiota.1.2.1.snupkg""
            },
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/108527235"",
                ""id"": 108527235,
                ""node_id"": ""RA_kwDOE0q91s4Gd_6D"",
                ""name"": ""Microsoft.OpenApi.Kiota.ApiDescription.Client.0.5.0-preview2.nupkg"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""andrueastman"",
                    ""id"": 6464005,
                    ""node_id"": ""MDQ6VXNlcjY0NjQwMDU="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/6464005?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/andrueastman"",
                    ""html_url"": ""https://github.com/andrueastman"",
                    ""followers_url"": ""https://api.github.com/users/andrueastman/followers"",
                    ""following_url"": ""https://api.github.com/users/andrueastman/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/andrueastman/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/andrueastman/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/andrueastman/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/andrueastman/orgs"",
                    ""repos_url"": ""https://api.github.com/users/andrueastman/repos"",
                    ""events_url"": ""https://api.github.com/users/andrueastman/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/andrueastman/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/octet-stream"",
                ""state"": ""uploaded"",
                ""size"": 14615,
                ""download_count"": 4,
                ""created_at"": ""2023-05-17T07:31:57Z"",
                ""updated_at"": ""2023-05-17T07:31:57Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.2.1/Microsoft.OpenApi.Kiota.ApiDescription.Client.0.5.0-preview2.nupkg""
            },
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/108527237"",
                ""id"": 108527237,
                ""node_id"": ""RA_kwDOE0q91s4Gd_6F"",
                ""name"": ""Microsoft.OpenApi.Kiota.Builder.1.2.1.nupkg"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""andrueastman"",
                    ""id"": 6464005,
                    ""node_id"": ""MDQ6VXNlcjY0NjQwMDU="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/6464005?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/andrueastman"",
                    ""html_url"": ""https://github.com/andrueastman"",
                    ""followers_url"": ""https://api.github.com/users/andrueastman/followers"",
                    ""following_url"": ""https://api.github.com/users/andrueastman/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/andrueastman/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/andrueastman/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/andrueastman/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/andrueastman/orgs"",
                    ""repos_url"": ""https://api.github.com/users/andrueastman/repos"",
                    ""events_url"": ""https://api.github.com/users/andrueastman/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/andrueastman/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/octet-stream"",
                ""state"": ""uploaded"",
                ""size"": 373700,
                ""download_count"": 4,
                ""created_at"": ""2023-05-17T07:31:57Z"",
                ""updated_at"": ""2023-05-17T07:31:58Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.2.1/Microsoft.OpenApi.Kiota.Builder.1.2.1.nupkg""
            },
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/108527240"",
                ""id"": 108527240,
                ""node_id"": ""RA_kwDOE0q91s4Gd_6I"",
                ""name"": ""Microsoft.OpenApi.Kiota.Builder.1.2.1.snupkg"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""andrueastman"",
                    ""id"": 6464005,
                    ""node_id"": ""MDQ6VXNlcjY0NjQwMDU="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/6464005?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/andrueastman"",
                    ""html_url"": ""https://github.com/andrueastman"",
                    ""followers_url"": ""https://api.github.com/users/andrueastman/followers"",
                    ""following_url"": ""https://api.github.com/users/andrueastman/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/andrueastman/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/andrueastman/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/andrueastman/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/andrueastman/orgs"",
                    ""repos_url"": ""https://api.github.com/users/andrueastman/repos"",
                    ""events_url"": ""https://api.github.com/users/andrueastman/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/andrueastman/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/octet-stream"",
                ""state"": ""uploaded"",
                ""size"": 164602,
                ""download_count"": 4,
                ""created_at"": ""2023-05-17T07:31:58Z"",
                ""updated_at"": ""2023-05-17T07:31:59Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.2.1/Microsoft.OpenApi.Kiota.Builder.1.2.1.snupkg""
            },
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/108527362"",
                ""id"": 108527362,
                ""node_id"": ""RA_kwDOE0q91s4Gd_8C"",
                ""name"": ""osx-arm64.zip"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""andrueastman"",
                    ""id"": 6464005,
                    ""node_id"": ""MDQ6VXNlcjY0NjQwMDU="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/6464005?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/andrueastman"",
                    ""html_url"": ""https://github.com/andrueastman"",
                    ""followers_url"": ""https://api.github.com/users/andrueastman/followers"",
                    ""following_url"": ""https://api.github.com/users/andrueastman/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/andrueastman/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/andrueastman/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/andrueastman/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/andrueastman/orgs"",
                    ""repos_url"": ""https://api.github.com/users/andrueastman/repos"",
                    ""events_url"": ""https://api.github.com/users/andrueastman/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/andrueastman/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/x-zip-compressed"",
                ""state"": ""uploaded"",
                ""size"": 30937498,
                ""download_count"": 237,
                ""created_at"": ""2023-05-17T07:32:53Z"",
                ""updated_at"": ""2023-05-17T07:33:02Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.2.1/osx-arm64.zip""
            },
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/108527369"",
                ""id"": 108527369,
                ""node_id"": ""RA_kwDOE0q91s4Gd_8J"",
                ""name"": ""osx-x64.zip"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""andrueastman"",
                    ""id"": 6464005,
                    ""node_id"": ""MDQ6VXNlcjY0NjQwMDU="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/6464005?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/andrueastman"",
                    ""html_url"": ""https://github.com/andrueastman"",
                    ""followers_url"": ""https://api.github.com/users/andrueastman/followers"",
                    ""following_url"": ""https://api.github.com/users/andrueastman/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/andrueastman/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/andrueastman/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/andrueastman/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/andrueastman/orgs"",
                    ""repos_url"": ""https://api.github.com/users/andrueastman/repos"",
                    ""events_url"": ""https://api.github.com/users/andrueastman/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/andrueastman/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/x-zip-compressed"",
                ""state"": ""uploaded"",
                ""size"": 32301050,
                ""download_count"": 195,
                ""created_at"": ""2023-05-17T07:33:02Z"",
                ""updated_at"": ""2023-05-17T07:33:13Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.2.1/osx-x64.zip""
            },
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/108527391"",
                ""id"": 108527391,
                ""node_id"": ""RA_kwDOE0q91s4Gd_8f"",
                ""name"": ""win-x64.zip"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""andrueastman"",
                    ""id"": 6464005,
                    ""node_id"": ""MDQ6VXNlcjY0NjQwMDU="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/6464005?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/andrueastman"",
                    ""html_url"": ""https://github.com/andrueastman"",
                    ""followers_url"": ""https://api.github.com/users/andrueastman/followers"",
                    ""following_url"": ""https://api.github.com/users/andrueastman/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/andrueastman/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/andrueastman/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/andrueastman/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/andrueastman/orgs"",
                    ""repos_url"": ""https://api.github.com/users/andrueastman/repos"",
                    ""events_url"": ""https://api.github.com/users/andrueastman/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/andrueastman/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/x-zip-compressed"",
                ""state"": ""uploaded"",
                ""size"": 32123198,
                ""download_count"": 602,
                ""created_at"": ""2023-05-17T07:33:13Z"",
                ""updated_at"": ""2023-05-17T07:33:24Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.2.1/win-x64.zip""
            },
            {
                ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/assets/108527408"",
                ""id"": 108527408,
                ""node_id"": ""RA_kwDOE0q91s4Gd_8w"",
                ""name"": ""win-x86.zip"",
                ""label"": null,
                ""uploader"": {
                    ""login"": ""andrueastman"",
                    ""id"": 6464005,
                    ""node_id"": ""MDQ6VXNlcjY0NjQwMDU="",
                    ""avatar_url"": ""https://avatars.githubusercontent.com/u/6464005?v=4"",
                    ""gravatar_id"": """",
                    ""url"": ""https://api.github.com/users/andrueastman"",
                    ""html_url"": ""https://github.com/andrueastman"",
                    ""followers_url"": ""https://api.github.com/users/andrueastman/followers"",
                    ""following_url"": ""https://api.github.com/users/andrueastman/following{/other_user}"",
                    ""gists_url"": ""https://api.github.com/users/andrueastman/gists{/gist_id}"",
                    ""starred_url"": ""https://api.github.com/users/andrueastman/starred{/owner}{/repo}"",
                    ""subscriptions_url"": ""https://api.github.com/users/andrueastman/subscriptions"",
                    ""organizations_url"": ""https://api.github.com/users/andrueastman/orgs"",
                    ""repos_url"": ""https://api.github.com/users/andrueastman/repos"",
                    ""events_url"": ""https://api.github.com/users/andrueastman/events{/privacy}"",
                    ""received_events_url"": ""https://api.github.com/users/andrueastman/received_events"",
                    ""type"": ""User"",
                    ""site_admin"": false
                },
                ""content_type"": ""application/x-zip-compressed"",
                ""state"": ""uploaded"",
                ""size"": 29678843,
                ""download_count"": 175,
                ""created_at"": ""2023-05-17T07:33:24Z"",
                ""updated_at"": ""2023-05-17T07:33:34Z"",
                ""browser_download_url"": ""https://github.com/microsoft/kiota/releases/download/v1.2.1/win-x86.zip""
            }
        ],
        ""tarball_url"": ""https://api.github.com/repos/microsoft/kiota/tarball/v1.2.1"",
        ""zipball_url"": ""https://api.github.com/repos/microsoft/kiota/zipball/v1.2.1"",
        ""body"": ""## Changed\r\n\r\n- Fixed a bug where Operation filters would be greedy and exclude non operation filters. [#2651](https://github.com/microsoft/kiota/issues/2651)\r\n- Shorten Go File names to a max of 252\r\n- Fixed a bug where clean output option would fail because of the log file. [#2645](https://github.com/microsoft/kiota/issues/2645)\r\n- Fixed a bug in the extension where selection in multiple indexers would fail. [#2666](https://github.com/microsoft/kiota/issues/2666)"",
        ""reactions"": {
            ""url"": ""https://api.github.com/repos/microsoft/kiota/releases/103269038/reactions"",
            ""total_count"": 3,
            ""+1"": 3,
            ""-1"": 0,
            ""laugh"": 0,
            ""hooray"": 0,
            ""confused"": 0,
            ""heart"": 0,
            ""rocket"": 0,
            ""eyes"": 0
        }
    }]";
    private static readonly Lazy<HttpClient> HttpClientInstance = new(() =>
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
        .Protected()
        // Setup the PROTECTED method to mock
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
        // prepare the expected response of the mocked http call
        .ReturnsAsync(() => new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(ResponsePayload, new MediaTypeHeaderValue("application/json")),
        })
        .Verifiable();
        return new HttpClient(handlerMock.Object);
    });
    private static readonly Lazy<HttpClient> HttpClientInstanceNoData = new(() =>
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
        .Protected()
        // Setup the PROTECTED method to mock
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
        // prepare the expected response of the mocked http call
        .ReturnsAsync(() => new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("[]", new MediaTypeHeaderValue("application/json")),
        })
        .Verifiable();
        return new HttpClient(handlerMock.Object);
    });
    private static readonly Lazy<HttpClient> HttpClientInstanceFailed = new(() =>
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
        .Protected()
        // Setup the PROTECTED method to mock
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
        // prepare the expected response of the mocked http call
        .ReturnsAsync(() => new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Content = new StringContent("", new MediaTypeHeaderValue("text/plain")),
        })
        .Verifiable();
        return new HttpClient(handlerMock.Object);
    });
    [Fact]
    public async Task DoesntEmitMessageOnAlreadyUpToDateAsync()
    {
        var client = HttpClientInstance.Value; //not disposed on purpose
        var mockLogger = new Mock<ILogger>().Object;
        var configuration = new UpdateConfiguration();
        var service = new UpdateService(client, mockLogger, configuration);
        UpdateService.ClearVerificationDate();
        var result = await service.GetUpdateMessageAsync("1000000.0.0", CancellationToken.None);
        Assert.Empty(result);
    }
    [Fact]
    public async Task EmitsASingleMessageWhenOutdatedAsync()
    {
        var client = HttpClientInstance.Value; //not disposed on purpose
        var mockLogger = new Mock<ILogger>().Object;
        var configuration = new UpdateConfiguration();
        var service = new UpdateService(client, mockLogger, configuration);
        UpdateService.ClearVerificationDate();
        var result = await service.GetUpdateMessageAsync("1.0.0", CancellationToken.None);
        Assert.NotEmpty(result);
        result = await service.GetUpdateMessageAsync("1.0.0", CancellationToken.None);
        Assert.Empty(result);
    }
    [Fact]
    public async Task ReturnsNoMessageOnEmptyVersionAsync()
    {
        var client = HttpClientInstance.Value; //not disposed on purpose
        var mockLogger = new Mock<ILogger>().Object;
        var configuration = new UpdateConfiguration();
        var service = new UpdateService(client, mockLogger, configuration);
        UpdateService.ClearVerificationDate();
        var result = await service.GetUpdateMessageAsync(string.Empty, CancellationToken.None);
        Assert.Empty(result);
    }
    [Fact]
    public async Task ReturnsNoMessageOnNoReleaseInformationAsync()
    {
        var client = HttpClientInstanceNoData.Value; //not disposed on purpose
        var mockLogger = new Mock<ILogger>().Object;
        var configuration = new UpdateConfiguration();
        var service = new UpdateService(client, mockLogger, configuration);
        UpdateService.ClearVerificationDate();
        var result = await service.GetUpdateMessageAsync("1000000.0.0", CancellationToken.None);
        Assert.Empty(result);
    }
    [Fact]
    public async Task ReturnsNoMessageOnFailedRequestAsync()
    {
        var client = HttpClientInstanceFailed.Value; //not disposed on purpose
        var mockLogger = new Mock<ILogger>().Object;
        var configuration = new UpdateConfiguration();
        var service = new UpdateService(client, mockLogger, configuration);
        UpdateService.ClearVerificationDate();
        var result = await service.GetUpdateMessageAsync("1000000.0.0", CancellationToken.None);
        Assert.Empty(result);
    }
}
