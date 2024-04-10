using ARManagement.BaseRepository.Interface;
using ARManagement.Helpers;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Models;
using System.Diagnostics;
using System.Net;
using System.Security.Claims;

namespace ARManagement.Controllers
{
    [ApiController]
    [Route("api/AREditior/[action]")]
    public class MyBaseApiController : Controller
    {
        private IBaseRepository _baseRepository => HttpContext?.RequestServices.GetService<IBaseRepository>();
        private JwtHelper jwtHelper => HttpContext?.RequestServices.GetService<JwtHelper>();
        public string controllerName = string.Empty;
        public string actionName = string.Empty;
        public IDictionary<string, object> actionParams = new Dictionary<string, object>(); //傳入參數
        private ResponseCodeHelper responseCodeHelper => HttpContext?.RequestServices.GetService<ResponseCodeHelper>();
        public string baseURL => HttpContext?.Request.Scheme + "://" + HttpContext?.Request.Host + "/";
        protected Userinfo myUser;
        public JwtToken jwtToken = new JwtToken() { Token = string.Empty, EffectiveTime = DateTime.MinValue };
        public bool tokenExpired = false; //token是否過期或無效，true: 是；false: 否

        public string exceptionMsg = string.Empty; //Exception 訊息
        public StackTrace stackTrace; //追蹤Exception
        public int CompanyId = 1; //暫時公司流水號(ToDo)

        public override async void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                controllerName = ControllerContext.RouteData.Values["controller"].ToString();   //controller名稱
                actionName = ControllerContext.RouteData.Values["action"].ToString();   //action名稱

                actionParams = filterContext.ActionArguments;

                responseCodeHelper.SetLanguage("zh-TW");

                var ignoreControllerActions = new List<IgnoreControllerAction>()
                {
                    new IgnoreControllerAction() { Controller="Login", Action="SignIn" },
                };

                var isIgnore = ignoreControllerActions.Where(x => x.Controller == controllerName && x.Action == actionName).FirstOrDefault();
                var needVerift = true;

                if (isIgnore != null)
                {   //可以忽略的，需再判斷Method
                    if (!string.IsNullOrEmpty(isIgnore.Method))
                    {
                        if (isIgnore.Method == filterContext.HttpContext.Request.Method.ToLower())
                        {
                            needVerift = false;
                        }
                    }
                    else
                    {
                        needVerift = false;
                    }
                }

                if (needVerift)
                {   //需進行token驗證
                    string authHeader = filterContext.HttpContext.Request.Headers["Authorization"];
                    if (authHeader != null)
                    {
                        var token = authHeader.Replace("Bearer ", "");

                        ClaimsPrincipal outClaims;
                        var validate = jwtHelper.ValidateToken(token, out outClaims);
                        if (validate)
                        {
                            var claims = outClaims.Claims.Select(x => new { Type = x.Type, Value = x.Value }).ToList();

                            var tempExp = claims.Where(x => x.Type == "exp").Select(x => x.Value).FirstOrDefault();
                            DateTime expiration = new DateTime(1970, 1, 1).AddHours(8).AddSeconds(Convert.ToDouble(tempExp));

                            //判斷該token是否已過期 
                            if (expiration < DateTime.Now)
                            {
                                tokenExpired = true;
                            }
                            else
                            {
                                var userId = claims.Where(x => x.Type == "UserId").Select(x => x.Value).FirstOrDefault();

                                if (!tokenExpired)
                                {
                                    //取得用戶資訊
                                    if (!string.IsNullOrEmpty(userId))
                                    {
                                        var where = $@" ""UserId"" = @UserId";
                                        myUser = _baseRepository.GetOneAsync<Userinfo>("Userinfo", where, new { UserId = int.Parse(userId) }).Result;
                                        if (myUser == null)
                                        {
                                            tokenExpired = true;
                                        }
                                        else
                                        {
                                        }
                                    }
                                    else
                                    {
                                        tokenExpired = true;
                                    }
                                }

                                if (!tokenExpired)
                                {
                                    //判斷是否需產生新token(過期前2小時)
                                    if (DateTime.Now >= expiration.AddHours(-2) && DateTime.Now <= expiration)
                                    {
                                        jwtToken = jwtHelper.GenerateToken(myUser);
                                    }
                                }
                            }
                        }
                        else
                        {
                            tokenExpired = true;
                        }
                    }
                    else
                    {
                        tokenExpired = true;
                    }
                }

                //myUser = new Userinfo()
                //{
                //    UserId = 6,
                //    CompanyId = 1,
                //    UserName = "最高管理員",
                //    UserAccount = "Admin",
                //    UserPassword = "123456",
                //    UserLevel = 1
                //};

                //tokenExpired = false;
            }
            catch (Exception ex)
            {
                exceptionMsg = ex.ToString();
            }
        }

        public override async void OnActionExecuted(ActionExecutedContext context)
        {
            object outputData = null;

            EDFunction edFunction = new EDFunction();
            var result = context.Result;
            if (result is ObjectResult json)
            {
                outputData = json.Value;
            }

            if (myUser != null && !string.IsNullOrEmpty(myUser.UserId.ToString()))
            {
                await Write2ApiLog(myUser.UserId, actionParams, outputData, exceptionMsg);
            }
            else
            {
                await Write2ApiLog(0, actionParams, outputData, exceptionMsg);
            }
        }

        /// <summary>
        /// ApiLog記錄
        /// </summary>
        protected async Task Write2ApiLog(int userId, object inputData, object outputData, string exceptionMsg = null)
        {
            try
            {
                // Backend write APILog information
                int line = -1;
                if (stackTrace != null)
                {
                    line = stackTrace.GetFrame(0).GetFileLineNumber();
                }

                var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
                var initAddress = IPAddress.Parse(ipAddress);
                ipAddress = initAddress.MapToIPv4().ToString();

                Dictionary<string, object> apiLog_Dict = new Dictionary<string, object>()
                {
                    { "@Apiurl",UriHelper.GetDisplayUrl(Request)},
                    { "@ApiMathod", Request.Method},
                    { "@UserId", userId},
                    { "@RequestData",inputData != null ? System.Text.Json.JsonSerializer.Serialize(inputData) : null},
                    { "@ResponseData",outputData != null ? System.Text.Json.JsonSerializer.Serialize(outputData) : null},
                    { "@IPAddress", ipAddress},
                    { "@Line", line > 0 ? line : null},
                    { "@ExceptionMsg", exceptionMsg}
                };

                await _baseRepository.AddOneByCustomTable(apiLog_Dict, "Apilog");
            }
            catch (Exception ex)
            {
                exceptionMsg = ex.ToString();
            }
        }
    }
}
