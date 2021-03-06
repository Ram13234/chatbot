﻿using System;
using System.Collections.Generic;

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

using Microsoft.IdentityModel.Protocols;

using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;


namespace Microsoft.Bot.Connector
{
    public class JwtTokenExtractor
    {
        /// <summary>
        /// The endorsements validator delegate.
        /// </summary>
        /// <param name="endorsements"> The endorsements used for validation.</param>
        /// <returns>true if validation passes; false otherwise.</returns>
        public delegate bool EndorsementsValidator(string[] endorsements);

        /// <summary>
        /// Cache for OpenIdConnect configuration managers (one per metadata URL)
        /// </summary>
        private static readonly ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> _openIdMetadataCache =
            new ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>>();

        /// <summary>
        /// Cache for Endorsement configuration managers (one per metadata URL)
        /// </summary>
        private static readonly ConcurrentDictionary<string, ConfigurationManager<IDictionary<string, string[]>>> _endorsementsCache =
            new ConcurrentDictionary<string, ConfigurationManager<IDictionary<string, string[]>>>();

        /// <summary>
        /// Token validation parameters for this instance
        /// </summary>
        private readonly TokenValidationParameters _tokenValidationParameters;

        /// <summary>
        /// OpenIdConnect configuration manager for this instance
        /// </summary>
        private readonly ConfigurationManager<OpenIdConnectConfiguration> _openIdMetadata;

        /// <summary>
        /// Endorsements configuration manager for this instance
        /// </summary>
        private readonly ConfigurationManager<IDictionary<string, string[]>> _endorsementsData;

        /// <summary>
        /// Allowed signing algorithms
        /// </summary>
        private readonly string[] _allowedSigningAlgorithms;

        /// <summary>
        /// Delegate for validating endorsements extracted from JwtToken
        /// </summary>
        private readonly EndorsementsValidator _validator;

        public JwtTokenExtractor(TokenValidationParameters tokenValidationParameters, string metadataUrl, string[] allowedSigningAlgorithms, EndorsementsValidator validator)
        {
            // Make our own copy so we can edit it
            _tokenValidationParameters = tokenValidationParameters.Clone();
            _tokenValidationParameters.RequireSignedTokens = true;
            _allowedSigningAlgorithms = allowedSigningAlgorithms;
            _validator = validator;

            _openIdMetadata = _openIdMetadataCache.GetOrAdd(metadataUrl, key =>
            {

                return new ConfigurationManager<OpenIdConnectConfiguration>(metadataUrl, new OpenIdConnectConfigurationRetriever());
            });

            _endorsementsData = _endorsementsCache.GetOrAdd(metadataUrl, key =>
            {
                var retriever = new EndorsementsRetriever();
                return new ConfigurationManager<IDictionary<string, string[]>>(metadataUrl, retriever, retriever);
            }); ;
        }

        public async Task<ClaimsIdentity> GetIdentityAsync(HttpRequestMessage request)
        {
            if (request.Headers.Authorization != null)
                return await GetIdentityAsync(request.Headers.Authorization.Scheme, request.Headers.Authorization.Parameter).ConfigureAwait(false);
            return null;
        }

        public async Task<ClaimsIdentity> GetIdentityAsync(string authorizationHeader)
        {
            if (authorizationHeader == null)
                return null;

            string[] parts = authorizationHeader?.Split(' ');
            if (parts.Length == 2)
                return await GetIdentityAsync(parts[0], parts[1]).ConfigureAwait(false);
            return null;
        }

        public async Task<ClaimsIdentity> GetIdentityAsync(string scheme, string parameter)
        {
            // No header in correct scheme or no token
            if (scheme != "Bearer" || string.IsNullOrEmpty(parameter))
                return null;

            // Issuer isn't allowed? No need to check signature
            if (!HasAllowedIssuer(parameter))
                return null;

            try
            {
                ClaimsPrincipal claimsPrincipal = await ValidateTokenAsync(parameter).ConfigureAwait(false);
                return claimsPrincipal.Identities.OfType<ClaimsIdentity>().FirstOrDefault();
            }
            catch (Exception e)
            {
                IdentityModel.Logging.LogHelper.LogException<Exception>($"Invalid token. {e.ToString()}");
                return null;
            }
        }

        private bool HasAllowedIssuer(string jwtToken)
        {
            JwtSecurityToken token = new JwtSecurityToken(jwtToken);
            if (_tokenValidationParameters.ValidIssuer != null && _tokenValidationParameters.ValidIssuer == token.Issuer)
                return true;

            if ((_tokenValidationParameters.ValidIssuers ?? Enumerable.Empty<string>()).Contains(token.Issuer))
                return true;

            return false;
        }



        public string GetAppIdFromClaimsIdentity(ClaimsIdentity identity)
        {
            if (identity == null)
                return null;

            Claim botClaim = identity.Claims.FirstOrDefault(c => _tokenValidationParameters.ValidIssuers.Contains(c.Issuer) && c.Type == "aud");
            return botClaim?.Value;
        }

        public string GetAppIdFromEmulatorClaimsIdentity(ClaimsIdentity identity)
        {
            if (identity == null)
                return null;

            Claim versionClaim = identity.Claims.FirstOrDefault(c => c.Type == "ver");

            Claim appIdClaim = identity.Claims.FirstOrDefault(c => _tokenValidationParameters.ValidIssuers.Contains(c.Issuer) &&
                ((versionClaim != null && versionClaim.Value == "2.0" && c.Type == "azp") || c.Type == "appid"));
            if (appIdClaim == null)
                return null;

            // v3.1 or v3.2 emulator token
            if (identity.Claims.Any(c => c.Type == "aud" && c.Value == appIdClaim.Value))
                return appIdClaim.Value;

            // v3.0 emulator token -- allow this
            if (identity.Claims.Any(c => c.Type == "aud" && c.Value == "https://graph.microsoft.com"))
                return appIdClaim.Value;

            return null;
        }

        private async Task<ClaimsPrincipal> ValidateTokenAsync(string jwtToken)
        {
            // _openIdMetadata only does a full refresh when the cache expires every 5 days
            OpenIdConnectConfiguration config = null;
            try
            {
                config = await _openIdMetadata.GetConfigurationAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                IdentityModel.Logging.LogHelper.LogException<Exception>($"Error refreshing OpenId configuration: {e}");

                // No config? We can't continue
                if (config == null)
                    throw;
            }

            // Update the signing tokens from the last refresh
            _tokenValidationParameters.IssuerSigningKeys = config.SigningKeys;


            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                SecurityToken parsedToken;
                ClaimsPrincipal principal = tokenHandler.ValidateToken(jwtToken, _tokenValidationParameters, out parsedToken);
                var parsedJwtToken = parsedToken as JwtSecurityToken;

                if (_validator != null)
                {
                    string keyId = (string)parsedJwtToken?.Header?["kid"];
                    var endorsements = await _endorsementsData.GetConfigurationAsync();
                    if (!string.IsNullOrEmpty(keyId) && endorsements.ContainsKey(keyId))
                    {
                        if (!_validator(endorsements[keyId]))
                        {
                            throw new ArgumentException($"Could not validate endorsement for key: {keyId} with endorsements: {string.Join(",", endorsements[keyId])}");
                        }
                    }
                }

                if (_allowedSigningAlgorithms != null)
                {
                    string algorithm = parsedJwtToken?.Header?.Alg;
                    if (!_allowedSigningAlgorithms.Contains(algorithm))
                    {
                        throw new ArgumentException($"Token signing algorithm '{algorithm}' not in allowed list");
                    }
                }
                return principal;
            }
            catch (SecurityTokenSignatureKeyNotFoundException)
            {
                string keys = string.Join(", ", ((config?.SigningKeys) ?? Enumerable.Empty<SecurityKey>()).Select(t => t.KeyId));
                IdentityModel.Logging.LogHelper.LogException<SecurityTokenSignatureKeyNotFoundException>("Error finding key for token.Available keys: " + keys);
                throw;
            }
        }
    }

    public sealed class EndorsementsRetriever : IDocumentRetriever, IConfigurationRetriever<IDictionary<string, string[]>>
    {
        private static HttpClient g_httpClient = new HttpClient();

        public async Task<IDictionary<string, string[]>> GetConfigurationAsync(string address, IDocumentRetriever retriever, CancellationToken cancel)
        {
            var res = await retriever.GetDocumentAsync(address, cancel);
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(res);
            if (obj != null && obj.HasValues && obj["keys"] != null)
            {
                var keys = obj.SelectToken("keys").Value<JArray>();
                var endorsements = keys.Where(key => key["endorsements"] != null).Select(key => Tuple.Create(key.SelectToken("kid").Value<string>(), key.SelectToken("endorsements").Values<string>()));
                return endorsements.Distinct(new EndorsementsComparer()).ToDictionary(item => item.Item1, item => item.Item2.ToArray());
            }
            else
            {
                return new Dictionary<string, string[]>();
            }
        }

        public async Task<string> GetDocumentAsync(string address, CancellationToken cancel)
        {
            using (var response = await g_httpClient.GetAsync(address, cancel))
            {
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                JObject obj = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(json);
                if (obj != null && obj.HasValues && obj["jwks_uri"] != null)
                {
                    var keysUrl = obj.SelectToken("jwks_uri").Value<string>();
                    using (var keysResponse = await g_httpClient.GetAsync(keysUrl, cancel))
                    {
                        keysResponse.EnsureSuccessStatusCode();
                        return await keysResponse.Content.ReadAsStringAsync();
                    }
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        private class EndorsementsComparer : IEqualityComparer<Tuple<string, IEnumerable<string>>>
        {
            public bool Equals(Tuple<string, IEnumerable<string>> x, Tuple<string, IEnumerable<string>> y)
            {
                return x.Item1 == y.Item1;
            }

            public int GetHashCode(Tuple<string, IEnumerable<string>> obj)
            {
                return obj.Item1.GetHashCode();
            }
        }

    }
}