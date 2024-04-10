using ARManagement.BaseRepository.Interface;
using ARManagement.Helpers;

using Microsoft.AspNetCore.Mvc;

using Models;
using System.Diagnostics;

namespace ARManagement.Controllers
{
    public class IOTController : MyBaseApiController
    {
        private readonly IBaseRepository _baseRepository;
        private readonly ResponseCodeHelper _responseCodeHelper;

        public IOTController(
           IBaseRepository baseRepository,
           ResponseCodeHelper responseCodeHelper)
        {
            _baseRepository = baseRepository;
            _responseCodeHelper = responseCodeHelper;
        }

        /// <summary>
        /// 14. IOT列表
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<ApiResult<List<IOT>>>> IOTOverview(PostIOTFilter post)
        {
            ApiResult<List<IOT>> apiResult = new ApiResult<List<IOT>>();

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

                #region 機台是否存在
                var where = $@"""MachineId"" = @MachineId";
                Machine machineinfo = await _baseRepository.GetOneAsync<Machine>("Machine", where, new { MachineId = post.MachineId });

                if (machineinfo == null)
                {
                    apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machineinfo.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                string conditionSQL = @" ""Deleted"" = 0 AND ""MachineId"" = @MachineId";

                if (!string.IsNullOrEmpty(post.Keyword))
                {
                    conditionSQL += @" AND (""MachineIOTMQTTBroker"" LIKE CONCAT('%', @Keyword ,'%') OR ""MachineIOTClientId"" LIKE CONCAT('%', @Keyword ,'%') )";
                }

                conditionSQL += $@" ORDER BY ""MachineIOTId"" ASC";

                List<IOT> iots = await _baseRepository.GetAllAsync<IOT>("MachineIOT", conditionSQL, new { MachineId = post.MachineId, Keyword = post.Keyword });

                apiResult.Code = "0000";
                apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                apiResult.Result = iots;
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
        /// 15. 取得單一IOT資訊
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<ApiResult<IOT>>> GetOneMachineIOT(PostId post)
        {
            ApiResult<IOT> apiResult = new ApiResult<IOT>(jwtToken.Token);

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

                var where = $@"""MachineIOTId"" = @MachineIOTId";

                var iot = await _baseRepository.GetOneAsync<IOT>("MachineIOT", where, new { MachineIOTId = post.Id });

                if (iot == null)
                {
                    apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (iot.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4002"; //此資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                #region 機台是否存在
                var machineWhere = $@"""MachineId"" = @MachineId";
                Machine machineinfo = await _baseRepository.GetOneAsync<Machine>("Machine", machineWhere, new { MachineId = iot.MachineId });

                if (machineinfo == null)
                {
                    apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machineinfo.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                //密碼解密
                EDFunction edFunction = new EDFunction();
                iot.MachineIOTPassword = edFunction.AESDecrypt(iot.MachineIOTPassword);

                //取得topic
                var topicWhere = $@"""Deleted"" = 0 AND ""MachineIOTId"" = @MachineIOTId";
                iot.MachineIOTTopics = await _baseRepository.GetAllAsync<MachineIOTTopic>("MachineIOTTopic", topicWhere, new { MachineIOTId = iot.MachineIOTId });

                apiResult.Code = "0000";
                apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                apiResult.Result = iot;
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
        /// 16. 新增/編輯IOT
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<ActionResult<ApiResult<int>>> SaveMachineIOT(IOT post)
        {
            ApiResult<int> apiResult = new ApiResult<int>();

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
                if (string.IsNullOrEmpty(post.MachineIOTDeviceName) ||
                    string.IsNullOrEmpty(post.MachineIOTMQTTBroker) ||
                    string.IsNullOrEmpty(post.MachineIOTClientId) ||
                    string.IsNullOrEmpty(post.MachineIOTUserName) ||
                    string.IsNullOrEmpty(post.MachineIOTPassword))
                {
                    apiResult.Code = "2003"; //有必填欄位尚未填寫
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 欄位長度
                if (post.MachineIOTDeviceName.Length > 1000 ||
                    post.MachineIOTMQTTBroker.Length > 1000 ||
                    post.MachineIOTClientId.Length > 1000 ||
                    post.MachineIOTUserName.Length > 50 ||
                    post.MachineIOTPassword.Length > 50)
                {
                    apiResult.Code = "2005"; //輸入文字字數不符合長度規範
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                foreach(var topics in post.MachineIOTTopics)
                {
                    if(topics.TopicValue.Length > 50)
                    {
                        apiResult.Code = "2005"; //輸入文字字數不符合長度規範
                        apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                        return Ok(apiResult);
                    }
                }
                #endregion

                #region 同一Server是否有ClientId相同
                var brokerWhere = $@"""Deleted"" = 0 AND ""MachineIOTMQTTBroker"" = @MachineIOTMQTTBroker AND ""MachineIOTClientId"" = @MachineIOTClientId";
                var tempIOT = await _baseRepository.GetOneAsync<IOT>("MachineIOT", brokerWhere, post);

                if (tempIOT != null)
                {
                    if (tempIOT.MachineIOTId != post.MachineIOTId)
                    {
                        apiResult.Code = "2014"; //Client ID已重複
                        apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                        return Ok(apiResult);
                    }
                }
                #endregion

                #endregion

                if (post.MachineIOTId != 0)
                {
                    #region IOT是否存在
                    var where = $@"""MachineIOTId"" = @MachineIOTId";
                    var machineIOT = await _baseRepository.GetOneAsync<IOT>("MachineIOT", where, new { MachineIOTId = post.MachineIOTId });

                    if (machineIOT == null)
                    {
                        apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                        apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                        return Ok(apiResult);
                    }

                    if (machineIOT.Deleted == (byte)DeletedDataEnum.True)
                    {
                        apiResult.Code = "4002"; //此資料已被刪除
                        apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                        return Ok(apiResult);
                    }
                    #endregion

                    #region 機台是否存在
                    var machineWhere = $@"""MachineId"" = @MachineId";
                    Machine machineinfo = await _baseRepository.GetOneAsync<Machine>("Machine", machineWhere, new { MachineId = machineIOT.MachineId });

                    if (machineinfo == null)
                    {
                        apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                        apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                        return Ok(apiResult);
                    }

                    if (machineinfo.Deleted == (byte)DeletedDataEnum.True)
                    {
                        apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                        apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                        return Ok(apiResult);
                    }
                    #endregion
                }

                //密碼加密
                EDFunction edFunction = new EDFunction();
                var paw = edFunction.AESEncrypt(post.MachineIOTPassword);

                var machineIOTId = 0;
                if (post.MachineIOTId == 0) //新增
                {
                    Dictionary<string, object> addMachineIOT_Dict = new Dictionary<string, object>()
                    {
                        { "@MachineId", post.MachineId},
                        { "@MachineIOTDeviceName", post.MachineIOTDeviceName},
                        { "@MachineIOTMQTTBroker", post.MachineIOTMQTTBroker},
                        { "@MachineIOTClientId", post.MachineIOTClientId},
                        { "@MachineIOTUserName", post.MachineIOTUserName},
                        { "@MachineIOTPassword", paw},
                        { "@Creator", myUser.UserId},
                    };

                    machineIOTId = await _baseRepository.AddOneByCustomTable(addMachineIOT_Dict, "MachineIOT", "MachineIOTId");

                    List<Dictionary<string, object>> addMachineIOTTopic_Dicts = new List<Dictionary<string, object>>();

                    foreach (var topic in post.MachineIOTTopics)
                    {
                        Dictionary<string, object> addMachineIOTTopic_Dict = new Dictionary<string, object>()
                        {
                            { "@MachineIOTId", machineIOTId},
                            { "@TopicValue", topic.TopicValue},
                            { "@Creator", myUser.UserId},
                        };

                        addMachineIOTTopic_Dicts.Add(addMachineIOTTopic_Dict);
                    }

                    if (addMachineIOTTopic_Dicts.Count > 0)
                    {
                        await _baseRepository.AddMutiByCustomTable(addMachineIOTTopic_Dicts, "MachineIOTTopic");
                    }
                }
                else //編輯
                {
                    machineIOTId = post.MachineIOTId;

                    Dictionary<string, object> updateMachineIOT_Dict = new Dictionary<string, object>()
                     {
                        { "MachineIOTId", post.MachineIOTId},
                        { "@MachineIOTDeviceName", post.MachineIOTDeviceName},
                        { "@MachineIOTMQTTBroker", post.MachineIOTMQTTBroker},
                        { "@MachineIOTClientId", post.MachineIOTClientId},
                        { "@MachineIOTUserName", post.MachineIOTUserName},
                        { "@MachineIOTPassword", paw},
                        { "@Updater", myUser.UserId},
                        { "@UpdateTime", DateTime.Now}
                    };

                    await _baseRepository.UpdateOneByCustomTable(updateMachineIOT_Dict, "MachineIOT", "\"MachineIOTId\" = @MachineIOTId");

                    #region 找出要新增的topic
                    var addTopics = post.MachineIOTTopics.Where(x => x.TopicId == 0).ToList();

                    List<Dictionary<string, object>> addMachineIOTTopic_Dicts = new List<Dictionary<string, object>>();
                    foreach (var addTopic in addTopics)
                    {
                        Dictionary<string, object> addMachineIOTTopic_Dict = new Dictionary<string, object>()
                        {
                            { "@MachineIOTId", post.MachineIOTId},
                            { "@TopicValue", addTopic.TopicValue},
                            { "@Creator", myUser.UserId},
                        };

                        addMachineIOTTopic_Dicts.Add(addMachineIOTTopic_Dict);
                    }

                    if (addMachineIOTTopic_Dicts.Count > 0)
                    {
                        await _baseRepository.AddMutiByCustomTable(addMachineIOTTopic_Dicts, "MachineIOTTopic");
                    }
                    #endregion

                    #region 找出要刪除的topic
                    var deleteTopics = post.MachineIOTTopics.Where(x => x.Deleted == 1).ToList();

                    List<Dictionary<string, object>> deleteMachineIOTTopic_Dicts = new List<Dictionary<string, object>>();
                    foreach (var deleteTopic in deleteTopics)
                    {
                        Dictionary<string, object> deleteMachineIOTTopic_Dict = new Dictionary<string, object>()
                        {
                            { "TopicId", deleteTopic.TopicId},
                            { "@Deleted", DeletedDataEnum.True},
                            { "@Updater", myUser.UserId},
                            { "@UpdateTime", DateTime.Now}
                        };

                        deleteMachineIOTTopic_Dicts.Add(deleteMachineIOTTopic_Dict);
                    }
                    if (deleteMachineIOTTopic_Dicts.Count > 0)
                    {
                        await _baseRepository.UpdateMutiByCustomTable(deleteMachineIOTTopic_Dicts, "MachineIOTTopic", "\"TopicId\" = @TopicId");
                    }
                    #endregion

                    #region 找出要修改的topic
                    var updateTopics = post.MachineIOTTopics.Where(x => x.TopicId != 0 && x.Deleted == 0).ToList();

                    List<Dictionary<string, object>> updateMachineIOTTopic_Dicts = new List<Dictionary<string, object>>();
                    foreach (var updateTopic in updateTopics)
                    {
                        Dictionary<string, object> updateMachineIOTTopic_Dict = new Dictionary<string, object>()
                        {
                            { "TopicId", updateTopic.TopicId},
                            { "@TopicValue", updateTopic.TopicValue},
                            { "@Updater", myUser.UserId},
                            { "@UpdateTime", DateTime.Now}
                        };

                        updateMachineIOTTopic_Dicts.Add(updateMachineIOTTopic_Dict);
                    }
                    if (updateMachineIOTTopic_Dicts.Count > 0)
                    {
                        await _baseRepository.UpdateMutiByCustomTable(updateMachineIOTTopic_Dicts, "MachineIOTTopic", "\"TopicId\" = @TopicId");
                    }
                    #endregion
                }

                apiResult.Code = "0000";
                apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                apiResult.Result = machineIOTId;
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
        /// 17. 刪除IOT
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<ActionResult<ApiResult<int>>> DeleteMachineIOT(PostId post)
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

                #region IOT是否存在
                var where = $@"""MachineIOTId"" = @MachineIOTId";

                var machineIOT = await _baseRepository.GetOneAsync<IOT>("MachineIOT", where, new { MachineIOTId = post.Id });

                if (machineIOT == null)
                {
                    apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machineIOT.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4002"; //此資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 機台是否存在
                var machineWhere = $@"""MachineId"" = @MachineId";
                Machine machineinfo = await _baseRepository.GetOneAsync<Machine>("Machine", machineWhere, new { MachineId = machineIOT.MachineId });

                if (machineinfo == null)
                {
                    apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machineinfo.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #endregion

                await _baseRepository.DeleteOne(machineIOT.MachineIOTId, "MachineIOT", "\"MachineIOTId\"", myUser.UserId);

                //刪除該IOT底下所有topic
                await _baseRepository.DeleteOne(machineIOT.MachineIOTId, "MachineIOTTopic", "\"MachineIOTId\"", myUser.UserId);

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
