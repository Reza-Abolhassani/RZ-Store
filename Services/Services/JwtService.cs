using Common;

using Entities;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class JwtService : IJwtService, IScopedDependency
    {
        private readonly SiteSettings _siteSetting;
        private readonly SignInManager<User> signInManager;
        private readonly UserManager<User> userManager;
        private const int RefreshTokenDays = 14;
        private const string RefreshLoginProvider = "JWT";
        private const string RefreshTokenName = "RefreshToken";

        public JwtService(IOptionsSnapshot<SiteSettings> settings, SignInManager<User> signInManager, UserManager<User> userManager)
        {
            _siteSetting = settings.Value;
            this.signInManager = signInManager;
            this.userManager = userManager;
        }

        public async Task<AccessToken> GenerateAsync(User user)
        {
            var secretKey = Encoding.UTF8.GetBytes(_siteSetting.JwtSettings.SecretKey); // longer that 16 character
            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature);

            var encryptionkey = Encoding.UTF8.GetBytes(_siteSetting.JwtSettings.EncryptKey); //must be 16 character
            var encryptingCredentials = new EncryptingCredentials(new SymmetricSecurityKey(encryptionkey), SecurityAlgorithms.Aes128KW, SecurityAlgorithms.Aes128CbcHmacSha256);

            var claims = await _getClaimsAsync(user);

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _siteSetting.JwtSettings.Issuer,
                Audience = _siteSetting.JwtSettings.Audience,
                IssuedAt = DateTime.UtcNow,
                NotBefore = DateTime.UtcNow.AddMinutes(_siteSetting.JwtSettings.NotBeforeMinutes),
                Expires = DateTime.UtcNow.AddMinutes(_siteSetting.JwtSettings.ExpirationMinutes),
                SigningCredentials = signingCredentials,
                EncryptingCredentials = encryptingCredentials,
                Subject = new ClaimsIdentity(claims)
            };

            //JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            //JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
            //JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateJwtSecurityToken(descriptor);
            var Token = new AccessToken(securityToken);

            //check roles
            var roles = await userManager.GetRolesAsync(user);
            foreach (var item in roles)
            {
                Token.Role += item + ",";
            }
            if (Token.Role.EndsWith(','))
            {
                Token.Role = Token.Role.TrimEnd(',');
            }
            //refresh token
            var (refreshToken, refreshExpiresUtc) = await IssueRefreshTokenAsync(user);
            Token.refresh_token = refreshToken;
            return Token;
        }


        private async Task<(string token, DateTime expiresUtc)> IssueRefreshTokenAsync(User user)
        {
            // strong randomizer
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            var randomRaw = Base64UrlEncoder.Encode(randomBytes);//safe and shorter url

            var expiresUtc = DateTime.UtcNow.AddDays(RefreshTokenDays);

            // userid for finding user faster
            var outwardToken = $"{user.Id}.{randomRaw}";

            // only randoms
            var randomHashHex = ToSha256Hex(randomRaw);

            // value = hash|expiresIso
            var storeValue = $"{randomHashHex}|{expiresUtc:o}";
            await userManager.SetAuthenticationTokenAsync(user, RefreshLoginProvider, RefreshTokenName, storeValue);

            return (outwardToken, expiresUtc);
        }

        private static string ToSha256Hex(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        private static bool FixedTimeEqualsHex(string aHex, string bHex)
        {
            var a = Convert.FromHexString(aHex);
            var b = Convert.FromHexString(bHex);
            return CryptographicOperations.FixedTimeEquals(a, b);
        }

        private async Task<IEnumerable<Claim>> _getClaimsAsync(User user)
        {
            var result = await signInManager.ClaimsFactory.CreateAsync(user);
            //add custom claims
            var list = new List<Claim>(result.Claims);
            list.Add(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? "-"));
            var fullName = $"{user.FirstName ?? string.Empty} {user.LastName ?? string.Empty}".Trim();
            list.Add(new Claim(ClaimTypes.Name, string.IsNullOrWhiteSpace(fullName) ? "-" : fullName));

            //JwtRegisteredClaimNames.Sub
            //var securityStampClaimType = new ClaimsIdentityOptions().SecurityStampClaimType;

            //var list = new List<Claim>
            //{
            //    new Claim(ClaimTypes.Name, user.UserName),
            //    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            //    //new Claim(ClaimTypes.MobilePhone, "09123456987"),
            //    //new Claim(securityStampClaimType, user.SecurityStamp.ToString())
            //};

            //var roles = new Role[] { new Role { Name = "Admin" } };
            //foreach (var role in roles)
            //    list.Add(new Claim(ClaimTypes.Role, role.Name));

            return list;
        }
    }
}
