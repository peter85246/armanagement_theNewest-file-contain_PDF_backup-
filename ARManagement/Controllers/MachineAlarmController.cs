using ARManagement.BaseRepository.Interface;
using ARManagement.Helpers;
using Microsoft.AspNetCore.Mvc;
using Models;
using System.Diagnostics;

namespace ARManagement.Controllers
{
    public class MachineAlarmController : MyBaseApiController
    {
        private readonly IBaseRepository _baseRepository;
        private readonly ResponseCodeHelper _responseCodeHelper;
        public MachineAlarmController(
            IBaseRepository baseRepository,
            ResponseCodeHelper responseCodeHelper)
        {
            _baseRepository = baseRepository;
            _responseCodeHelper = responseCodeHelper;
        }

        /// <summary>
        /// 20. 依據條件取得指定機台的所有Alarm
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<ApiResult<List<MachineAlarm>>>> GetAllMachineAlarmByFilter(PostAlarmFilter post)
        {
            ApiResult<List<MachineAlarm>> apiResult = new ApiResult<List<MachineAlarm>>(jwtToken.Token);

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

                #region 判斷機台是否被刪除或失效
                var machineWhere = $@"""MachineId"" = @MachineId";
                var machine = await _baseRepository.GetOneAsync<Machine>("Machine", machineWhere, new { MachineId = post.MachineId });

                if (machine == null)
                {
                    apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machine.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                var where = $@"""Deleted"" = 0 AND ""MachineId"" = @MachineId";

                if (!string.IsNullOrEmpty(post.Keyword))
                {
                    where += @" AND (""MachineAlarmCode"" LIKE CONCAT('%', @Keyword ,'%') OR ""MachineAlarmAbstract"" LIKE CONCAT('%', @Keyword ,'%') )";
                }

                var machineAlarms = await _baseRepository.GetAllAsync<MachineAlarm>("MachineAlarm", where, new { MachineId = post.MachineId, Keyword = post.Keyword }, "\"MachineAlarmId\" ASC");

                apiResult.Code = "0000";
                apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                apiResult.Result = machineAlarms;
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
        /// 21. 取得指定機台的單一Alarm
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<ApiResult<MachineAlarm>>> GetOneMachineAlarm(PostId post)
        {
            ApiResult<MachineAlarm> apiResult = new ApiResult<MachineAlarm>(jwtToken.Token);

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

                #region 判斷該筆alarm是否被刪除或失效
                var where = $@"""MachineAlarmId"" = @MachineAlarmId";
                var machineAlarm = await _baseRepository.GetOneAsync<MachineAlarm>("MachineAlarm", where, new { MachineAlarmId = post.Id });

                if (machineAlarm == null)
                {
                    apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machineAlarm.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4002"; //此資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷機台是否被刪除或失效
                var machineWhere = $@"""MachineId"" = @MachineId";
                var machine = await _baseRepository.GetOneAsync<Machine>("Machine", machineWhere, new { MachineId = machineAlarm.MachineId });

                if (machine == null)
                {
                    apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machine.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                apiResult.Code = "0000";
                apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                apiResult.Result = machineAlarm;
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
        /// 22. 新增指定機台的Alarm
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<ActionResult<ApiResult<int>>> AddMachineAlarm(PostAddMachineAlarm post)
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
                if (string.IsNullOrEmpty(post.MachineAlarmCode) ||
                    string.IsNullOrEmpty(post.MachineAlarmAbstract))
                {
                    apiResult.Code = "2003"; //有必填欄位尚未填寫
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 欄位長度
                if (post.MachineAlarmCode.Length > 50)
                {
                    apiResult.Code = "2005"; //輸入文字字數不符合長度規範
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (post.MachineAlarmAbstract.Length > 1000)
                {
                    apiResult.Code = "2005"; //輸入文字字數不符合長度規範
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #endregion

                #region 判斷機台是否被刪除或失效
                var machineWhere = $@"""MachineId"" = @MachineId";
                var machine = await _baseRepository.GetOneAsync<Machine>("Machine", machineWhere, new { MachineId = post.MachineId });

                if (machine == null)
                {
                    apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machine.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷該機台是否以存在相同的AlarmCode
                var machineAlarmWhere = $@"""Deleted"" = 0 AND ""MachineAlarmCode"" = @MachineAlarmCode AND ""MachineId"" = @MachineId";
                var machineAlarmRepeat = await _baseRepository.GetOneAsync<MachineAlarm>("MachineAlarm", machineAlarmWhere, new { MachineAlarmCode = post.MachineAlarmCode, MachineId = machine.MachineId });
                
                if(machineAlarmRepeat != null)
                {
                    apiResult.Code = "2015"; //故障代碼已重複
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                //新增Alarm
                Dictionary<string, object> addAlarm_Dict = new Dictionary<string, object>()
                {
                    { "@MachineId", machine.MachineId},
                    { "@MachineAlarmCode", post.MachineAlarmCode},
                    { "@MachineAlarmAbstract", post.MachineAlarmAbstract},
                    { "@Creator", myUser.UserId},
                };

                var machineAlarmId = await _baseRepository.AddOneByCustomTable(addAlarm_Dict, "MachineAlarm", "MachineAlarmId");

                apiResult.Code = "0000";
                apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                apiResult.Result = machineAlarmId;
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
        /// 23. 修改指定機台的Alarm
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<ActionResult<ApiResult<int>>> EditMachineAlarm(PostEditMachineAlarm post)
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
                if (string.IsNullOrEmpty(post.MachineAlarmCode) ||
                    string.IsNullOrEmpty(post.MachineAlarmAbstract))
                {
                    apiResult.Code = "2003"; //有必填欄位尚未填寫
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 欄位長度
                if (post.MachineAlarmCode.Length > 50)
                {
                    apiResult.Code = "2005"; //輸入文字字數不符合長度規範
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (post.MachineAlarmAbstract.Length > 1000)
                {
                    apiResult.Code = "2005"; //輸入文字字數不符合長度規範
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #endregion

                #region 判斷該筆alarm是否被刪除或失效
                var where = $@"""MachineAlarmId"" = @MachineAlarmId";
                var machineAlarm = await _baseRepository.GetOneAsync<MachineAlarm>("MachineAlarm", where, new { MachineAlarmId = post.MachineAlarmId });

                if (machineAlarm == null)
                {
                    apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machineAlarm.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4002"; //此資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷機台是否被刪除或失效
                var machineWhere = $@"""MachineId"" = @MachineId";
                var machine = await _baseRepository.GetOneAsync<Machine>("Machine", machineWhere, new { MachineId = machineAlarm.MachineId });

                if (machine == null)
                {
                    apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machine.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷該機台是否以存在相同的AlarmCode
                var machineAlarmWhere = $@"""Deleted"" = 0 AND ""MachineAlarmId"" != @MachineAlarmId AND ""MachineAlarmCode"" = @MachineAlarmCode AND ""MachineId"" = @MachineId";
                var machineAlarmRepeat = await _baseRepository.GetOneAsync<MachineAlarm>("MachineAlarm", machineAlarmWhere, new { MachineAlarmId = machineAlarm.MachineAlarmId, MachineAlarmCode = post.MachineAlarmCode, MachineId = machine.MachineId });

                if (machineAlarmRepeat != null)
                {
                    apiResult.Code = "2015"; //故障代碼已重複
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                //更新alarm
                Dictionary<string, object> updateAlarm_Dict = new Dictionary<string, object>()
                {
                    { "MachineAlarmId", machineAlarm.MachineAlarmId},
                    { "@MachineAlarmCode", post.MachineAlarmCode},
                    { "@MachineAlarmAbstract", post.MachineAlarmAbstract},
                    { "@Updater", myUser.UserId},
                    { "@UpdateTime", DateTime.Now},
                };

                await _baseRepository.UpdateOneByCustomTable(updateAlarm_Dict, "MachineAlarm", "\"MachineAlarmId\" = @MachineAlarmId");

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
        /// 24. 刪除指定機台的Alarm
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<ActionResult<ApiResult<int>>> DeleteMachineAlarm(PostId post)
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

                #region 判斷該筆alarm是否被刪除或失效
                var where = $@"""MachineAlarmId"" = @MachineAlarmId";
                var machineAlarm = await _baseRepository.GetOneAsync<MachineAlarm>("MachineAlarm", where, new { MachineAlarmId = post.Id });

                if (machineAlarm == null)
                {
                    apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machineAlarm.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4002"; //此資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷機台是否被刪除或失效
                var machineWhere = $@"""MachineId"" = @MachineId";
                var machine = await _baseRepository.GetOneAsync<Machine>("Machine", machineWhere, new { MachineId = machineAlarm.MachineId });

                if (machine == null)
                {
                    apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machine.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                //刪除MachineAlarm
                await _baseRepository.DeleteOne(machineAlarm.MachineAlarmId, "MachineAlarm", "\"MachineAlarmId\"", myUser.UserId);

                #region 刪除SOP
                //找出所有SOP ID
                var sopWhere = $@"""Deleted"" = 0 AND ""MachineAlarmId"" = @MachineAlarmId";
                var sops = await _baseRepository.GetAllAsync<SOP>("SOP", sopWhere, new { MachineAlarmId = machineAlarm.MachineAlarmId });
                var sopIds = sops.Select(x => x.SOPId).ToList();

                //刪除SOP
                await _baseRepository.DeleteOne(machineAlarm.MachineAlarmId, "SOP", "\"MachineAlarmId\"", myUser.UserId);
                #endregion

                //刪除SOP Model
                if (sopIds.Count > 0)
                {
                    var deleteSOPModelSql = $@"UPDATE public.""SOPModel"" SET
                                                ""Deleted"" = 1,
                                                ""Updater"" = @Updater,
                                                ""UpdateTime"" = @UpdateTime WHERE ""SOPId"" = ANY (@Ids)";
                    await _baseRepository.ExecuteSql(deleteSOPModelSql, new { Ids = sopIds, Updater = myUser.UserId, UpdateTime = DateTime.Now });
                }

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
