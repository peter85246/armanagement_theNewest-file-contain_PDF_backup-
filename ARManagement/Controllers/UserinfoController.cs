using ARManagement.BaseRepository.Interface;
using ARManagement.Helpers;
using Microsoft.AspNetCore.Mvc;
using Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ARManagement.Controllers
{
    public class UserinfoController : MyBaseApiController
    {
        private readonly IBaseRepository _baseRepository;
        private readonly ResponseCodeHelper _responseCodeHelper;
        public UserinfoController(
            IBaseRepository baseRepository,
            ResponseCodeHelper responseCodeHelper)
        {
            _baseRepository = baseRepository;
            _responseCodeHelper = responseCodeHelper;
        }

        /// <summary>
        /// 2. 取得我的個人用戶資訊
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<ApiResult<Userinfo>>> MyUserData()
        {
            ApiResult<Userinfo> apiResult = new ApiResult<Userinfo>(jwtToken.Token);

            try
            {
                #region 判斷Token是否過期或無效
                if (tokenExpired)
                {
                    apiResult.Code = "1001"; //Token過期或無效
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                //將密碼移除
                myUser.UserPassword = string.Empty;

                apiResult.Code = "0000";
                apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                apiResult.Result = myUser;
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

        /// <summary>
        /// 3. 依據條件取得所有用戶列表
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<ApiResult<List<Userinfo>>>> GetAllUserinfoByFilter(PostUserinfoFilter post)
        {
            ApiResult<List<Userinfo>> apiResult = new ApiResult<List<Userinfo>>(jwtToken.Token);

            try
            {
                #region 判斷Token是否過期或無效
                if (tokenExpired)
                {
                    apiResult.Code = "1001"; //Token過期或無效
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷帳號是否為系統管理員
                if ((myUser.UserLevel & (byte)UserLevelEnum.Admin) == 0)
                {
                    apiResult.Code = "3001"; //您不具有瀏覽的權限
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                var where = $@"""Deleted"" = 0";

                if (!string.IsNullOrEmpty(post.Keyword))
                {
                    where += @" AND (""UserName"" LIKE CONCAT('%', @Keyword ,'%') OR ""UserAccount"" LIKE CONCAT('%', @Keyword ,'%') )";
                }

                var userinfos = await _baseRepository.GetAllAsync<Userinfo>("Userinfo", where, new { Keyword = post.Keyword }, "\"UserId\" ASC");

                apiResult.Code = "0000";
                apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                apiResult.Result = userinfos;
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

        /// <summary>
        /// 4. 取得單一用戶資訊
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<ApiResult<Userinfo>>> GetOneUserinfo(PostId post)
        {
            ApiResult<Userinfo> apiResult = new ApiResult<Userinfo>(jwtToken.Token);

            try
            {
                #region 判斷Token是否過期或無效
                if (tokenExpired)
                {
                    apiResult.Code = "1001"; //Token過期或無效
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷帳號是否為系統管理員
                if ((myUser.UserLevel & (byte)UserLevelEnum.Admin) == 0)
                {
                    apiResult.Code = "3001"; //您不具有瀏覽的權限
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                var where = $@"""UserId"" = @UserId";

                var userinfo = await _baseRepository.GetOneAsync<Userinfo>("Userinfo", where, new { UserId = post.Id });

                if (userinfo == null)
                {
                    apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (userinfo.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4002"; //此資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                apiResult.Code = "0000";
                apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                apiResult.Result = userinfo;
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

        /// <summary>
        /// 5. 新增使用者資訊
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<ActionResult<ApiResult<int>>> AddUserinfo(PostAddUserinfo post)
        {
            ApiResult<int> apiResult = new ApiResult<int>(jwtToken.Token);

            try
            {
                #region 判斷Token是否過期或無效
                if (tokenExpired)
                {
                    apiResult.Code = "1001"; //Token過期或無效
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷帳號是否為系統管理員
                if ((myUser.UserLevel & (byte)UserLevelEnum.Admin) == 0)
                {
                    apiResult.Code = "3002"; //您不具有新增的權限
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 欄位驗證

                #region 必填欄位
                if (string.IsNullOrEmpty(post.UserName) ||
                    string.IsNullOrEmpty(post.UserAccount) ||
                    string.IsNullOrEmpty(post.UserPaw) ||
                    string.IsNullOrEmpty(post.UserAgainPaw))
                {
                    apiResult.Code = "2003"; //有必填欄位尚未填寫
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 欄位長度
                if (post.UserName.Length > 50)
                {
                    apiResult.Code = "2005"; //輸入文字字數不符合長度規範
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (post.UserAccount.Length > 50)
                {
                    apiResult.Code = "2005"; //輸入文字字數不符合長度規範
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 合法欄位
                if (!Enum.IsDefined(typeof(UserLevelEnum), post.UserLevel))
                {
                    apiResult.Code = "2004"; //不合法的欄位
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 2次密碼是否相同
                if (post.UserPaw != post.UserAgainPaw)
                {
                    apiResult.Code = "2002"; //密碼與確認密碼不一致
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 帳號是否存在
                var where = $@"""UserAccount"" = @UserAccount";

                var userinfo = await _baseRepository.GetOneAsync<Userinfo>("Userinfo", where, new { UserAccount = post.UserAccount });

                if (userinfo != null)
                {
                    apiResult.Code = "2001"; //該帳號已存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #endregion

                //密碼加密
                EDFunction edFunction = new EDFunction();
                var paw = edFunction.GetSHA256Encryption(post.UserPaw);

                //新增用戶
                Dictionary<string, object> addUserinfo_Dict = new Dictionary<string, object>()
                {
                    { "@CompanyId", 1},
                    { "@UserName", post.UserName},
                    { "@UserAccount", post.UserAccount},
                    { "@UserPassword", paw},
                    { "@UserLevel", post.UserLevel},
                    { "@Creator", myUser.UserId},
                };

                var userId = await _baseRepository.AddOneByCustomTable(addUserinfo_Dict, "Userinfo", "UserId");

                apiResult.Code = "0000";
                apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                apiResult.Result = userId;
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

        /// <summary>
        /// 6. 修改使用者資訊
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<ActionResult<ApiResult<int>>> EditUserinfo(PostEditUserinfo post)
        {
            ApiResult<int> apiResult = new ApiResult<int>(jwtToken.Token);

            try
            {
                #region 判斷Token是否過期或無效
                if (tokenExpired)
                {
                    apiResult.Code = "1001"; //Token過期或無效
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷帳號是否為系統管理員
                if ((myUser.UserLevel & (byte)UserLevelEnum.Admin) == 0)
                {
                    apiResult.Code = "3003"; //您不具有修改的權限
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 欄位驗證

                #region 必填欄位
                if (string.IsNullOrEmpty(post.UserName))
                {
                    apiResult.Code = "2003"; //有必填欄位尚未填寫
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 欄位長度
                if (post.UserName.Length > 50)
                {
                    apiResult.Code = "2005"; //輸入文字字數不符合長度規範
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 合法欄位
                if (!Enum.IsDefined(typeof(UserLevelEnum), post.UserLevel))
                {
                    apiResult.Code = "2004"; //不合法的欄位
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 使用者是否存在
                var where = $@"""UserId"" = @UserId";

                var userinfo = await _baseRepository.GetOneAsync<Userinfo>("Userinfo", where, new { UserId = post.UserId });

                if (userinfo == null)
                {
                    apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (userinfo.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4002"; //此資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #endregion

                //更新用戶
                Dictionary<string, object> updateUserinfo_Dict = new Dictionary<string, object>()
                {
                    { "UserId", userinfo.UserId},
                    { "@UserName", post.UserName},
                    { "@UserLevel", post.UserLevel},
                    { "@Updater", myUser.UserId},
                    { "@UpdateTime", DateTime.Now},
                };

                await _baseRepository.UpdateOneByCustomTable(updateUserinfo_Dict, "Userinfo", "\"UserId\" = @UserId");

                apiResult.Code = "0000";
                apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
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

        /// <summary>
        /// 7. 使用者修改密碼
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<ActionResult<ApiResult<int>>> UserinfoChangePaw(PostUserinfoChangePaw post)
        {
            ApiResult<int> apiResult = new ApiResult<int>(jwtToken.Token);

            try
            {
                #region 判斷Token是否過期或無效
                if (tokenExpired)
                {
                    apiResult.Code = "1001"; //Token過期或無效
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 欄位驗證

                #region 必填欄位
                if (string.IsNullOrEmpty(post.NewPaw) ||
                    string.IsNullOrEmpty(post.AgainPaw))
                {
                    apiResult.Code = "2003"; //有必填欄位尚未填寫
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 密碼長度是否合法
                if (post.NewPaw.Length < 6 || post.NewPaw.Length > 30)
                {
                    apiResult.Code = "2005"; //輸入文字字數不符合長度規範
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 密碼是否合法
                //判斷是否有不合法的特殊符號
                var tempMailTooken = post.NewPaw;

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

                #region 2次密碼是否相同
                if (post.NewPaw != post.AgainPaw)
                {
                    apiResult.Code = "2002"; //密碼與確認密碼不一致
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 使用者是否存在
                var where = $@"""UserId"" = @UserId";

                var userinfo = await _baseRepository.GetOneAsync<Userinfo>("Userinfo", where, new { UserId = post.UserId });

                if (userinfo == null)
                {
                    apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (userinfo.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4002"; //此資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #endregion

                //修改密碼
                EDFunction edFunction = new EDFunction();
                var paw = edFunction.GetSHA256Encryption(post.NewPaw);

                //更新用戶
                Dictionary<string, object> updateUserinfo_Dict = new Dictionary<string, object>()
                {
                    { "UserId", userinfo.UserId},
                    { "@UserPassword", paw},
                    { "@Updater", myUser.UserId},
                    { "@UpdateTime", DateTime.Now},
                };

                await _baseRepository.UpdateOneByCustomTable(updateUserinfo_Dict, "Userinfo", "\"UserId\" = @UserId");

                apiResult.Code = "0000";
                apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
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

        /// <summary>
        /// 8. 刪除使用者資訊
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<ActionResult<ApiResult<int>>> DeleteUserinfo(PostId post)
        {
            ApiResult<int> apiResult = new ApiResult<int>(jwtToken.Token);

            try
            {
                #region 判斷Token是否過期或無效
                if (tokenExpired)
                {
                    apiResult.Code = "1001"; //Token過期或無效
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷帳號是否為系統管理員
                if ((myUser.UserLevel & (byte)UserLevelEnum.Admin) == 0)
                {
                    apiResult.Code = "3004"; //您不具有刪除的權限
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 使用者是否存在
                var where = $@"""UserId"" = @UserId";

                var userinfo = await _baseRepository.GetOneAsync<Userinfo>("Userinfo", where, new { UserId = post.Id });

                if (userinfo == null)
                {
                    apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (userinfo.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4002"; //此資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 不可以刪除自己
                if (userinfo.UserId == myUser.UserId)
                {
                    apiResult.Code = "1005"; //無權限刪除自己帳號
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                await _baseRepository.DeleteOne(userinfo.UserId, "Userinfo", "\"UserId\"", myUser.UserId);

                apiResult.Code = "0000";
                apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
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

        /// <summary>
        /// 9. 變更密碼
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<ActionResult<ApiResult<int>>> ChangePaw(PostChangePaw post)
        {
            ApiResult<int> apiResult = new ApiResult<int>(jwtToken.Token);

            try
            {
                #region 判斷Token是否過期或無效
                if (tokenExpired)
                {
                    apiResult.Code = "1001"; //Token過期或無效
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 欄位驗證

                #region 必填欄位
                if (string.IsNullOrEmpty(post.OldPaw) ||
                    string.IsNullOrEmpty(post.NewPaw) ||
                    string.IsNullOrEmpty(post.AgainPaw))
                {
                    apiResult.Code = "2003"; //有必填欄位尚未填寫
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷舊密碼是否正確
                EDFunction edFunction = new EDFunction();
                var userPaw = edFunction.GetSHA256Encryption(post.OldPaw);

                if (userPaw != myUser.UserPassword)
                {
                    apiResult.Code = "1006"; //舊密碼錯誤
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 密碼長度是否合法
                if (post.NewPaw.Length < 6 || post.NewPaw.Length > 30)
                {
                    apiResult.Code = "2005"; //輸入文字字數不符合長度規範
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 密碼是否合法
                //判斷是否有不合法的特殊符號
                var tempMailTooken = post.NewPaw;

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

                #region 2次密碼是否相同
                if (post.NewPaw != post.AgainPaw)
                {
                    apiResult.Code = "2002"; //密碼與確認密碼不一致
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #endregion

                var paw = edFunction.GetSHA256Encryption(post.NewPaw);

                //更新用戶
                Dictionary<string, object> updateUserinfo_Dict = new Dictionary<string, object>()
                {
                    { "UserId", myUser.UserId},
                    { "@UserPassword", paw},
                    { "@Updater", myUser.UserId},
                    { "@UpdateTime", DateTime.Now},
                };

                await _baseRepository.UpdateOneByCustomTable(updateUserinfo_Dict, "Userinfo", "\"UserId\" = @UserId");

                apiResult.Code = "0000";
                apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
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
