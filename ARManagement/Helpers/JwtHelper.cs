using Microsoft.IdentityModel.Tokens;
using Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ARManagement.Helpers
{
    public interface IJwtHelper
    {
        JwtToken GenerateToken(Userinfo userinfo);
    }

    public class JwtToken
    {
        public string Token { get; set; } = string.Empty;
        public DateTime EffectiveTime { get; set; }
    }

    public class JwtHelper : IJwtHelper
    {
        private readonly IConfiguration _configuration;

        public JwtHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// 產生Token
        /// </summary>
        /// <param name="userInfo"></param>
        /// <returns></returns>
        public JwtToken GenerateToken(Userinfo userinfo)
        {
            var issuer = _configuration.GetValue<string>("JwtSettings:Issuer");
            var signKey = _configuration.GetValue<string>("JwtSettings:SignKey");
            var lifeHour = _configuration.GetValue<double>("JwtSettings:LifeHour");

            var now = DateTime.Now;
            var expires = DateTime.Now.AddHours(lifeHour);

            // 1. 定義需要使用到的Claims
            var claims = new List<Claim>();
            claims.Add(new Claim(JwtRegisteredClaimNames.Iss, issuer));
            claims.Add(new Claim("UserId", userinfo.UserId.ToString()));
            claims.Add(new Claim("UserName", userinfo.UserName));
            claims.Add(new Claim("UserAccount", userinfo.UserAccount));
            claims.Add(new Claim("Exp", expires.ToString()));

            // 2. 建立一組對稱式加密的金鑰，主要用於 JWT 簽章之用
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signKey));

            // 3. 選擇加密演算法
            var algorithm = SecurityAlgorithms.HmacSha512;

            // 4. 生成Credentials
            var signingCredentials = new SigningCredentials(securityKey, algorithm);

            // 5. 根據以上，生成token
            var jwtSecurityToken = new JwtSecurityToken(
                issuer: issuer,//Token釋出者
                               //audience: _options.Value.Audience,//Token接受者
                claims: claims,//攜帶的負載
                notBefore: now,//當前時間token生成時間
                expires: expires,//過期時間
                signingCredentials: signingCredentials
            );

            // 6. 將token變為string
            JwtToken jwtToken = new JwtToken()
            {
                Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken),
                EffectiveTime = expires
            };

            return jwtToken;
        }

        /// <summary>
        /// 驗證Token是否失效
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public bool ValidateToken(string token, out ClaimsPrincipal claims)
        {
            bool result = false;
            claims = null;
            try
            {
                SecurityToken securityToken;
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                TokenValidationParameters validationParameters = GetValidationParameters();

                claims = tokenHandler.ValidateToken(token, validationParameters, out securityToken);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private TokenValidationParameters GetValidationParameters()
        {
            var issuer = _configuration.GetValue<string>("JwtSettings:Issuer");
            var signKey = _configuration.GetValue<string>("JwtSettings:SignKey");
            var lifeHour = _configuration.GetValue<double>("JwtSettings:LifeHour");

            return new TokenValidationParameters()
            {
                ValidateLifetime = false,
                ValidateAudience = false,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signKey)) // The same key as the one that generate the token
            };
        }
    }
}
