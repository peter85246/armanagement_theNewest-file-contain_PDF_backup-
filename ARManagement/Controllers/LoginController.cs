using ARManagement.BaseRepository.Interface;
using ARManagement.Helpers;
using Microsoft.AspNetCore.Mvc;
using Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ARManagement.Controllers
{

    public class LoginController : MyBaseApiController
    {
        private readonly JwtHelper _jwtHelper;
        private readonly IBaseRepository _baseRepository;
        private readonly ResponseCodeHelper _responseCodeHelper;

        public LoginController(
            JwtHelper jwtHelper,
           IBaseRepository baseRepository,
           ResponseCodeHelper responseCodeHelper)
        {
            _jwtHelper = jwtHelper;
            _baseRepository = baseRepository;
            _responseCodeHelper = responseCodeHelper;
        }

        /// <summary>
        /// 1. 登入
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<ApiResult<string>>> SignIn(PostSignIn post)
        {
            ApiResult<string> apiResult = new ApiResult<string>();

            try
            {
                #region 欄位驗證

                #region 判斷必填欄位
                if (string.IsNullOrEmpty(post.Account) ||  //帳號
                    string.IsNullOrEmpty(post.Paw) //密碼
                    )
                {
                    apiResult.Code = "2003"; //有必填欄位尚未填寫
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 密碼是否合法
                //判斷是否有不合法的特殊符號
                var tempMailTooken = post.Paw;

                //取代字串
                tempMailTooken = Regex.Replace(tempMailTooken, "\\d", "");
                tempMailTooken = Regex.Replace(tempMailTooken, "[A-Z]", "");
                tempMailTooken = Regex.Replace(tempMailTooken, "[a-z]", "");
                tempMailTooken = Regex.Replace(tempMailTooken, "[,.~!@#$%^&*_+\\-=]", "");

                if (tempMailTooken.Length > 0)
                {
                    apiResult.Code = "2004"; //不合法的欄位
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #endregion

                #region 判斷帳號是否存在
                var where = $@"""Deleted"" = 0 AND ""UserAccount"" = @UserAccount ";

                var userinfo = await _baseRepository.GetOneAsync<Userinfo>("Userinfo", where, new { UserAccount = post.Account });

                if (userinfo == null)
                {
                    apiResult.Code = "4003"; //該帳號不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷密碼是否正確
                EDFunction edFunction = new EDFunction();
                var userPaw = edFunction.GetSHA256Encryption(post.Paw);

                if (userPaw != userinfo.UserPassword)
                {
                    apiResult.Code = "1004"; //帳號或密碼錯誤
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                jwtToken = _jwtHelper.GenerateToken(userinfo);

                apiResult.Code = "0000";
                apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                apiResult.Result = jwtToken.Token;
            }
            catch (Exception ex)
            {
                apiResult.Code = "9999";
                apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                exceptionMsg = ex.ToString();
                stackTrace = new StackTrace(ex);
            }

            return Ok(apiResult);
        }
    }
}
