using ARManagement.BaseRepository.Interface;
using ARManagement.Helpers;
using Microsoft.AspNetCore.Mvc;
using Models;
using System.Diagnostics;

namespace ARManagement.Controllers
{
    public class SOPController : MyBaseApiController
    {
        private readonly IBaseRepository _baseRepository;
        private readonly ResponseCodeHelper _responseCodeHelper;
        private string _savePath = string.Empty;

        public SOPController(
            IBaseRepository baseRepository,
            ResponseCodeHelper responseCodeHelper)
        {
            _baseRepository = baseRepository;
            _responseCodeHelper = responseCodeHelper;
            _savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "upload");
        }

        /// <summary>
        /// 25. 依據AlarmId取得所有SOP(包含眼鏡使用)
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<ApiResult<List<SOP>>>> GetAllSOPByMachineAlarmId(PostAllSOP post)
        {
            ApiResult<List<SOP>> apiResult = new ApiResult<List<SOP>>(jwtToken.Token);

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

                #region 判斷IsCommon
                if (post.IsCommon < 0 || post.IsCommon > 1)
                {
                    apiResult.Code = "2004"; //不合法的欄位
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #endregion

                #region 判斷該筆alarm是否被刪除或失效
                var machineAlarmWhere = $@"""MachineAlarmId"" = @MachineAlarmId";
                var machineAlarm = await _baseRepository.GetOneAsync<MachineAlarm>("MachineAlarm", machineAlarmWhere, new { MachineAlarmId = post.Id });

                if (machineAlarm == null)
                {
                    apiResult.Code = "4005"; //該機台Alarm不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machineAlarm.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4005"; //該機台Alarm不存在資料庫或是資料已被刪除
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

                var sopWhere = $@"""Deleted"" = 0 AND ""MachineAlarmId"" = @MachineAlarmId";

                //取得所有SOP
                var sops = await _baseRepository.GetAllAsync<SOP>("SOP", sopWhere, new { MachineAlarmId = machineAlarm.MachineAlarmId }, "\"SOPStep\" ASC");

                //取得所有model
                var sopIds = sops.Select(x => x.SOPId).ToList();
                var sopModelWhere = $@"""Deleted"" = 0 AND ""SOPId"" = ANY (@SOPIds)";

                if (post.IsCommon == 0)
                {
                    sopModelWhere += $@" AND ""IsCommon"" = @IsCommon";
                }

                var sopModels = await _baseRepository.GetAllAsync<SOPModel>("SOPModel", sopModelWhere, new { SOPIds = sopIds, IsCommon = post.IsCommon });

                foreach (var sopModel in sopModels)
                {
                    if (!string.IsNullOrEmpty(sopModel.SOPModelImage))
                    {
                        sopModel.SOPModelImage = $"{baseURL}upload/machine/{machine.MachineId}/sop/{sopModel.SOPId}/model/{sopModel.SOPModelImage}";
                    }
                    if (!string.IsNullOrEmpty(sopModel.SOPModelImage))
                    {
                        sopModel.SOPModelFile = $"{baseURL}upload/machine/{machine.MachineId}/sop/{sopModel.SOPId}/model/{sopModel.SOPModelFile}";
                    }
                }

                foreach (var sop in sops)
                {
                    if (!string.IsNullOrEmpty(sop.SOPImage))
                    {
                        sop.SOPImage = $"{baseURL}upload/machine/{machine.MachineId}/sop/{sop.SOPId}/{sop.SOPImage}";
                    }

                    if (!string.IsNullOrEmpty(sop.SOPVideo))
                    {
                        sop.SOPVideo = $"{baseURL}upload/machine/{machine.MachineId}/sop/{sop.SOPId}/{sop.SOPVideo}";
                    }

                    if (!string.IsNullOrEmpty(sop.SOPRemarksImage))
                    {
                        sop.SOPRemarksImage = $"{baseURL}upload/machine/{machine.MachineId}/sop/{sop.SOPId}/{sop.SOPRemarksImage}";
                    }

                    sop.SOPModels = sopModels.Where(x => x.SOPId == sop.SOPId).ToList();
                }

                apiResult.Code = "0000";
                apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                apiResult.Result = sops;
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
        /// 依據過濾條件取得單一SOP (未使用)
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<ApiResult<SOP>>> GetOneSOPByFilter(PostSOPFilter post)
        {
            ApiResult<SOP> apiResult = new ApiResult<SOP>(jwtToken.Token);

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

                #region 判斷帳號是否被刪除或失效
                if (myUser == null)
                {
                    apiResult.Code = "1003"; //該帳號已被刪除或失效
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷帳號是否為系統管理員
                if (myUser.UserLevel != (byte)UserLevelEnum.Admin)
                {
                    apiResult.Code = "3001"; //您不具有瀏覽的權限
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷該筆alarm是否被刪除或失效
                var machineAlarmWhere = $@"""MachineAlarmId"" = @MachineAlarmId";
                var machineAlarm = await _baseRepository.GetOneAsync<MachineAlarm>("MachineAlarm", machineAlarmWhere, new { MachineAlarmId = post.MachineAlarmId });

                if (machineAlarm == null)
                {
                    apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machineAlarm.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4002"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷機台是否被刪除或失效
                var machineWhere = $@"""MachineId"" = @MachineId";
                var machine = await _baseRepository.GetOneAsync<Machine>("Machine", machineWhere, new { MachineId = machineAlarm.MachineId });

                if (machine == null)
                {
                    apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machine.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                var sopWhere = $@"""Deleted"" = 0 AND ""MachineAlarmId"" = @MachineAlarmId AND ""SOPStep"" = @SOPStep";

                var sop = await _baseRepository.GetOneAsync<SOP>("SOP", sopWhere, new { MachineAlarmId = machineAlarm.MachineAlarmId, SOPStep = post.SOPStep });

                var sopModelWhere = $@"""Deleted"" = 0 AND ""SOPId"" = @SOPId";

                var sopModels = await _baseRepository.GetAllAsync<SOPModel>("SOPModel", sopModelWhere, new { SOPId = sop.SOPId }, "\"SOPModelId\" ASC");

                sop.SOPModels = sopModels;

                apiResult.Code = "0000";
                apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                apiResult.Result = sop;
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
        /// 新增單一SOP步驟 (未使用)
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<ApiResult<int>>> AddSOPStep(PostAddSOPStep post)
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

                #region 判斷帳號是否被刪除或失效
                if (myUser == null)
                {
                    apiResult.Code = "1003"; //該帳號已被刪除或失效
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷帳號是否為系統管理員
                if (myUser.UserLevel != (byte)UserLevelEnum.Admin)
                {
                    apiResult.Code = "3002"; //您不具有新增的權限
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷該筆alarm是否被刪除或失效
                var machineAlarmWhere = $@"""MachineAlarmId"" = @MachineAlarmId";
                var machineAlarm = await _baseRepository.GetOneAsync<MachineAlarm>("MachineAlarm", machineAlarmWhere, new { MachineAlarmId = post.MachineAlarmId });

                if (machineAlarm == null)
                {
                    apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machineAlarm.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4002"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷機台是否被刪除或失效
                var machineWhere = $@"""MachineId"" = @MachineId";
                var machine = await _baseRepository.GetOneAsync<Machine>("Machine", machineWhere, new { MachineId = machineAlarm.MachineId });

                if (machine == null)
                {
                    apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machine.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                //找出當前最後的號碼
                var sopWhere = $@"""Deleted"" = 0 AND ""MachineAlarmId"" = @MachineAlarmId";
                var sop = await _baseRepository.GetOneAsync<SOP>("SOP", sopWhere, new { MachineAlarmId = machineAlarm.MachineAlarmId }, "\"SOPStep\" DESC");

                //新增SOP
                Dictionary<string, object> addSOP_Dict = new Dictionary<string, object>()
                {
                    { "@MachineAlarmId", machine.MachineId},
                    { "@SOPStep", sop.SOPStep + 1},
                    //{ "@Creator", myUser.UserId},
                };

                var sopId = await _baseRepository.AddOneByCustomTable(addSOP_Dict, "SOP", "SOPId");

                apiResult.Code = "0000";
                apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                apiResult.Result = sopId;
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
        /// 更換SOP步驟順序 (未使用)
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<ApiResult<int>>> EditSOPStepPriority(PostEditSOPStepPriority post)
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

                #region 判斷帳號是否被刪除或失效
                if (myUser == null)
                {
                    apiResult.Code = "1003"; //該帳號已被刪除或失效
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷帳號是否為系統管理員
                if (myUser.UserLevel != (byte)UserLevelEnum.Admin)
                {
                    apiResult.Code = "3003"; //您不具有修改的權限
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷該筆alarm是否被刪除或失效
                var machineAlarmWhere = $@"""MachineAlarmId"" = @MachineAlarmId";
                var machineAlarm = await _baseRepository.GetOneAsync<MachineAlarm>("MachineAlarm", machineAlarmWhere, new { MachineAlarmId = post.MachineAlarmId });

                if (machineAlarm == null)
                {
                    apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machineAlarm.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4002"; //資料庫或是實體檔案不存在
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

                List<Dictionary<string, object>> updateSOPStep_Dicts = new List<Dictionary<string, object>>();
                var step = 1;
                foreach (var sopId in post.SOPIds)
                {
                    Dictionary<string, object> updateSOPStep_Dict = new Dictionary<string, object>()
                    {
                        { "SOPId", sopId},
                        { "@SOPStep", step},
                    };

                    step++;
                    updateSOPStep_Dicts.Add(updateSOPStep_Dict);
                }

                await _baseRepository.UpdateMutiByCustomTable(updateSOPStep_Dicts, "SOP", "\"SOPId\" = @SOPId");

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
        /// 刪除單一SOP步驟 (未使用)
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<ActionResult<ApiResult<int>>> DeleteSOPStep(PostId post)
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

                #region 判斷帳號是否被刪除或失效
                if (myUser == null)
                {
                    apiResult.Code = "1003"; //該帳號已被刪除或失效
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷帳號是否為系統管理員
                if (myUser.UserLevel != (byte)UserLevelEnum.Admin)
                {
                    apiResult.Code = "3004"; //您不具有刪除的權限
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 欄位驗證

                #region 判斷該筆SOP是否被刪除或失效
                var sopWhere = $@"""SOPId"" = @SOPId";
                var sop = await _baseRepository.GetOneAsync<SOP>("SOP", sopWhere, new { SOPId = post.Id });

                if (sop == null)
                {
                    apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (sop.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4002"; //此資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷該筆alarm是否被刪除或失效
                var machineAlarmWhere = $@"""MachineAlarmId"" = @MachineAlarmId";
                var machineAlarm = await _baseRepository.GetOneAsync<MachineAlarm>("MachineAlarm", machineAlarmWhere, new { MachineAlarmId = sop.MachineAlarmId });

                if (machineAlarm == null)
                {
                    apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machineAlarm.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4002"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷機台是否被刪除或失效
                var machineWhere = $@"""MachineId"" = @MachineId";
                var machine = await _baseRepository.GetOneAsync<Machine>("Machine", machineWhere, new { MachineId = machineAlarm.MachineId });

                if (machine == null)
                {
                    apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machine.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #endregion

                //刪除SOP
                await _baseRepository.DeleteOne(sop.SOPId, "SOP", "\"SOPId\"");

                //刪除SOP Model
                await _baseRepository.DeleteOne(sop.SOPId, "SOPModel", "\"SOPId\"");

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
        /// 修改單一SOP步驟內容 (未使用)
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<ActionResult<ApiResult<int>>> EditSOP(PostSOP post)
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

                #region 判斷帳號是否被刪除或失效
                if (myUser == null)
                {
                    apiResult.Code = "1003"; //該帳號已被刪除或失效
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷帳號是否為系統管理員
                if (myUser.UserLevel != (byte)UserLevelEnum.Admin)
                {
                    apiResult.Code = "3003"; //您不具有修改的權限
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 欄位驗證

                #region 欄位長度
                if (post.SOPMessage.Length > 1000 || post.SOPRemarksMessage.Length > 1000)
                {
                    apiResult.Code = "2005"; //輸入文字字數不符合長度規範
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷圖片附檔名
                //var validImageEx = new List<string>() { "png", "jpg", "jpeg" }; //合法圖片附檔名
                //if (post.SOPImage != null)
                //{
                //    var validImageSplit = post.SOPImage.FileName.Split(".");
                //    var tempImageNameEx = validImageSplit[validImageSplit.Length - 1]; //副檔名
                //    if (!validImageEx.Contains(tempImageNameEx.ToLower()))
                //    {
                //        apiResult.Code = "2007"; //不合法的欄位
                //        apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                //        return Ok(apiResult);
                //    }
                //}
                #endregion

                #region 判斷影片附檔名
                //var validVideoEx = new List<string>() { "mp4" }; //合法影片附檔名
                //if (post.SOPVideo != null)
                //{
                //    var validVideoSplit = post.SOPVideo.FileName.Split(".");
                //    var tempVideoNameEx = validVideoSplit[validVideoSplit.Length - 1]; //副檔名
                //    if (!validVideoEx.Contains(tempVideoNameEx.ToLower()))
                //    {
                //        apiResult.Code = "2007"; //不合法的欄位
                //        apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                //        return Ok(apiResult);
                //    }
                //}
                #endregion

                #endregion

                #region 判斷該筆SOP是否被刪除或失效
                var sopWhere = $@"""SOPId"" = @SOPId";
                var sop = await _baseRepository.GetOneAsync<SOP>("SOP", sopWhere, new { SOPId = post.SOPId });

                if (sop == null)
                {
                    apiResult.Code = "4001"; //資料庫或是實體檔案不存在
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (sop.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4002"; //此資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #region 判斷該筆alarm是否被刪除或失效
                var where = $@"""MachineAlarmId"" = @MachineAlarmId";
                var machineAlarm = await _baseRepository.GetOneAsync<MachineAlarm>("MachineAlarm", where, new { MachineAlarmId = sop.MachineAlarmId });

                if (machineAlarm == null)
                {
                    apiResult.Code = "4005"; //該機台Alarm不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machineAlarm.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4005"; //該機台Alarm不存在資料庫或是資料已被刪除
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

                //更新SOP
                Dictionary<string, object> updateSOP_Dict = new Dictionary<string, object>()
                {
                    { "@SOPMessage", post.SOPMessage},
                    { "@SOPRemarksMessage", post.SOPRemarksMessage },
                    { "@SOPPLC1", post.SOPPLC1},
                    { "@SOPPLC2", post.SOPPLC2},
                    { "@SOPPLC3", post.SOPPLC3},
                    { "@SOPPLC4", post.SOPPLC4},
                    { "@Updater", myUser.UserId},
                    { "@UpdateTime", DateTime.Now},
                };

                #region 判斷是否有圖片
                //if (post.SOPImage != null)
                //{
                //    var filePath = Path.Combine(_savePath, "machine", machine.MachineId.ToString(), "sop", "image");
                //    DirectoryInfo directoryInfo = new DirectoryInfo(filePath);
                //    if (!directoryInfo.Exists)
                //    {
                //        directoryInfo.Create();
                //    }

                //    var imageGuid = Guid.NewGuid().ToString("N");

                //    var imageSplit = post.SOPImage.FileName.Split(".");
                //    var imageEx = imageSplit[imageSplit.Length - 1];
                //    var imageFileName = $@"{imageGuid}.{imageEx}";

                //    var fullPath = Path.Combine(filePath, imageFileName);
                //    using (var stream = new FileStream(fullPath, FileMode.Create))
                //    {
                //        post.SOPImage.CopyTo(stream);
                //    }

                //    updateSOP_Dict.Add("@SOPImage", imageFileName);
                //}
                #endregion

                #region 判斷是否有影片
                //if (post.SOPVideo != null)
                //{
                //    var filePath = Path.Combine(_savePath, "machine", machine.MachineId.ToString(), "sop", "video");
                //    DirectoryInfo directoryInfo = new DirectoryInfo(filePath);
                //    if (!directoryInfo.Exists)
                //    {
                //        directoryInfo.Create();
                //    }

                //    var videoGuid = Guid.NewGuid();

                //    var videoSplit = post.SOPVideo.FileName.Split(".");
                //    var videoEx = videoSplit[videoSplit.Length - 1];
                //    var videoFileName = $@"{videoGuid}.{videoEx}";

                //    var fullPath = Path.Combine(filePath, videoFileName);
                //    using (var stream = new FileStream(fullPath, FileMode.Create))
                //    {
                //        post.SOPVideo.CopyTo(stream);
                //    }

                //    updateSOP_Dict.Add("@SOPVideo", videoFileName);
                //}
                #endregion

                await _baseRepository.UpdateOneByCustomTable(updateSOP_Dict, "SOP", "\"SOPId\" = @SOPId");

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
        /// 26. SOP頁面儲存設定
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        [DisableRequestSizeLimit]
        [Consumes("multipart/form-data")]
        [HttpPut]
        public async Task<ActionResult<ApiResult<int>>> SaveSOP([FromForm] PostSaveSOP post)
        {
            ApiResult<int> apiResult = new ApiResult<int>(jwtToken.Token);

            try
            {
                #region 判斷Token是否過期或無效
                //if (tokenExpired)
                //{
                //    apiResult.Code = "1001"; //Token過期或無效
                //    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                //    return Ok(apiResult);
                //}
                #endregion

                #region 欄位驗證

                #region 判斷長度
                foreach (var sop in post.SOPs)
                {
                    if ((sop.SOPMessage != null && sop.SOPMessage.Length > 1000) ||
                        (sop.SOPRemarksMessage != null && sop.SOPRemarksMessage.Length > 1000) ||
                        (sop.SOPPLC1 != null && sop.SOPPLC1.Length > 10) ||
                        (sop.SOPPLC2 != null && sop.SOPPLC2.Length > 10) ||
                        (sop.SOPPLC3 != null && sop.SOPPLC3.Length > 10) ||
                        (sop.SOPPLC4 != null && sop.SOPPLC4.Length > 10))
                    {
                        apiResult.Code = "2005"; //輸入文字字數不符合長度規範
                        apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                        return Ok(apiResult);
                    }
                }
                #endregion

                #region 判斷圖片附檔名
                var validImageEx = new List<string>() { "png", "jpg", "jpeg" }; //合法圖片附檔名
                foreach (var sop in post.SOPs)
                {
                    if (sop.SOPImageObj != null)
                    {
                        var validImageSplit = sop.SOPImageObj.FileName.Split(".");
                        var tempImageNameEx = validImageSplit[validImageSplit.Length - 1]; //副檔名
                        if (!validImageEx.Contains(tempImageNameEx.ToLower()))
                        {
                            apiResult.Code = "2004"; //不合法的欄位
                            apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                            return Ok(apiResult);
                        }
                    }

                    // 檢查SOPRemarksImageObj
                    if (sop.SOPRemarksImageObj != null)
                    {
                        var validRemarksImageSplit = sop.SOPRemarksImageObj.FileName.Split(".");
                        var tempRemarksImageNameEx = validRemarksImageSplit[validRemarksImageSplit.Length - 1]; //副檔名
                        if (!validImageEx.Contains(tempRemarksImageNameEx.ToLower()))
                        {
                            apiResult.Code = "2004"; //不合法的欄位
                            apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                            return Ok(apiResult);
                        }
                    }
                }
                #endregion

                #region 判斷影片附檔名
                var validVideoEx = new List<string>() { "mp4" }; //合法影片附檔名
                foreach (var sop in post.SOPs)
                {
                    if (sop.SOPVideoObj != null)
                    {
                        var validVideoSplit = sop.SOPVideoObj.FileName.Split(".");
                        var tempVideoNameEx = validVideoSplit[validVideoSplit.Length - 1]; //副檔名
                        if (!validVideoEx.Contains(tempVideoNameEx.ToLower()))
                        {
                            apiResult.Code = "2004"; //不合法的欄位
                            apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                            return Ok(apiResult);
                        }
                    }
                }
                #endregion

                #region 判斷3D Model 圖片及檔案附檔名
                var validFileEx = new List<string>() { "zip" }; //合法檔案附檔名
                foreach (var sop in post.SOPs)
                {
                    if (sop.SOPModels != null && sop.SOPModels.Count > 0)
                    {
                        foreach (var sopModel in sop.SOPModels)
                        {
                            #region 判斷3D Model 圖片附檔名
                            if (sopModel.SOPModelImageObj != null)
                            {
                                var validImageSplit = sopModel.SOPModelImageObj.FileName.Split(".");
                                var tempImageNameEx = validImageSplit[validImageSplit.Length - 1]; //副檔名
                                if (!validImageEx.Contains(tempImageNameEx.ToLower()))
                                {
                                    apiResult.Code = "2004"; //不合法的欄位
                                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                                    return Ok(apiResult);
                                }
                            }
                            #endregion

                            #region 判斷3D Model 檔案附檔名
                            if (sopModel.SOPModelFileObj != null)
                            {
                                var validFileSplit = sopModel.SOPModelFileObj.FileName.Split(".");
                                var tempFileNameEx = validFileSplit[validFileSplit.Length - 1]; //副檔名
                                if (!validFileEx.Contains(tempFileNameEx.ToLower()))
                                {
                                    apiResult.Code = "2004"; //不合法的欄位
                                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                                    return Ok(apiResult);
                                }
                            }
                            #endregion
                        }
                    }
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

                FolderFunction folderFunction = new FolderFunction();
                var sopRootPath = Path.Combine(_savePath, "machine", machine.MachineId.ToString(), "sop");

                #region 找出要新增的SOP
                var addSOPs = post.SOPs.Where(x => x.SOPId == 0).ToList();
                foreach (var addSOP in addSOPs)
                {
                    //步驟圖片
                    string? imageName = null;
                    if (addSOP.SOPImageObj != null)
                    {
                        imageName = folderFunction.FileProduceName(addSOP.SOPImageObj);
                    }

                    //步驟影片
                    string? videoName = null;
                    if (addSOP.SOPVideoObj != null)
                    {
                        videoName = folderFunction.FileProduceName(addSOP.SOPVideoObj);
                    }

                    //備註圖片
                    string? remarksImageName = null;
                    if (addSOP.SOPRemarksImageObj != null)
                    {
                        remarksImageName = folderFunction.FileProduceName(addSOP.SOPRemarksImageObj);
                    }

                    Dictionary<string, object> addSOP_Dict = new Dictionary<string, object>()
                    {
                        { "@MachineAlarmId", post.MachineAlarmId},
                        { "@SOPStep", addSOP.SOPStep},
                        { "@SOPMessage", addSOP.SOPMessage},
                        { "@SOPImage", imageName},
                        { "@SOPVideo", videoName},
                        { "@SOPRemarksMessage", addSOP.SOPRemarksMessage},
                        { "@SOPRemarksImage", remarksImageName},
                        { "@SOPPLC1", addSOP.SOPPLC1},
                        { "@SOPPLC2", addSOP.SOPPLC2},
                        { "@SOPPLC3", addSOP.SOPPLC3},
                        { "@SOPPLC4", addSOP.SOPPLC4},
                        { "@Creator", 1},
                        //{ "@Creator", myUser.UserId},
                    };

                    var sopId = await _baseRepository.AddOneByCustomTable(addSOP_Dict, "SOP", "SOPId");

                    //判斷SOP資料夾是否存在
                    var sopPath = Path.Combine(sopRootPath, sopId.ToString());
                    folderFunction.CreateFolder(sopPath, 0);

                    if (!string.IsNullOrEmpty(imageName))
                    {
                        folderFunction.SavePathFile(addSOP.SOPImageObj, sopPath, imageName);
                    }

                    if (!string.IsNullOrEmpty(videoName))
                    {
                        folderFunction.SavePathFile(addSOP.SOPVideoObj, sopPath, videoName);
                    }

                    if (!string.IsNullOrEmpty(remarksImageName))
                    {
                        folderFunction.SavePathFile(addSOP.SOPRemarksImageObj, sopPath, remarksImageName);
                    }

                    #region 新增Models
                    if (addSOP.SOPModels != null)
                    {
                        List<Dictionary<string, object>> addModel_Dicts = new List<Dictionary<string, object>>();
                        List<IFormFile> modelImageFiles = new List<IFormFile>(); //要儲存的圖片檔案
                        List<string> modelImageFileNames = new List<string>(); //要儲存的圖片名稱
                        List<IFormFile> modelFiles = new List<IFormFile>(); //要儲存的檔案
                        List<string> modelFileNames = new List<string>(); //要儲存的檔案名稱
                        foreach (var sopModel in addSOP.SOPModels)
                        {
                            //圖片
                            string? modelImageName = null;
                            if (sopModel.SOPModelImageObj != null)
                            {
                                modelImageName = folderFunction.FileProduceName(sopModel.SOPModelImageObj);

                                modelImageFiles.Add(sopModel.SOPModelImageObj);
                                modelImageFileNames.Add(modelImageName);
                            }

                            //檔案
                            string? modelFileName = null;
                            if (sopModel.SOPModelFileObj != null)
                            {
                                modelFileName = folderFunction.FileProduceName(sopModel.SOPModelFileObj);

                                modelFiles.Add(sopModel.SOPModelFileObj);
                                modelFileNames.Add(modelFileName);
                            }

                            Dictionary<string, object> addModel_Dict = new Dictionary<string, object>()
                            {
                                { "@SOPId", sopId},
                                { "@SOPModelImage", modelImageName},
                                { "@SOPModelFile", modelFileName},
                                { "@Creator", myUser.UserId},
                            };

                            addModel_Dicts.Add(addModel_Dict);
                        }

                        if (addModel_Dicts.Count > 0)
                        {
                            await _baseRepository.AddMutiByCustomTable(addModel_Dicts, "SOPModel");
                        }

                        var modelPath = Path.Combine(sopRootPath, sopId.ToString(), "model");
                        if (modelImageFiles.Count > 0)
                        {
                            folderFunction.SavePathFile(modelImageFiles, modelPath, modelImageFileNames);
                        }

                        if (modelFiles.Count > 0)
                        {
                            folderFunction.SavePathFile(modelFiles, modelPath, modelFileNames);
                        }
                    }
                    #endregion
                }
                #endregion

                #region 找出要刪除的SOP
                var deleteSOPs = post.SOPs.Where(x => x.Deleted == 1).ToList();
                List<Dictionary<string, object>> deleteSOP_Dicts = new List<Dictionary<string, object>>();
                foreach (var deleteSOP in deleteSOPs)
                {
                    Dictionary<string, object> deleteSOP_Dict = new Dictionary<string, object>()
                    {
                        { "SOPId", deleteSOP.SOPId},
                        { "@Deleted", DeletedDataEnum.True},
                        { "@Updater", myUser.UserId},
                        { "@UpdateTime", DateTime.Now}
                    };

                    deleteSOP_Dicts.Add(deleteSOP_Dict);
                }
                if (deleteSOP_Dicts.Count > 0)
                {
                    //刪除SOP
                    await _baseRepository.UpdateMutiByCustomTable(deleteSOP_Dicts, "SOP", "\"SOPId\" = @SOPId");
                    //刪除SOP Model
                    await _baseRepository.UpdateMutiByCustomTable(deleteSOP_Dicts, "SOPModel", "\"SOPId\" = @SOPId");
                }

                //刪除SOP資料底下所有子資料夾、檔案
                foreach (var deleteSOP in deleteSOPs)
                {
                    var tempSOPPath = Path.Combine(sopRootPath, deleteSOP.SOPId.ToString());

                    DirectoryInfo directoryInfo = new DirectoryInfo(tempSOPPath);
                    if (directoryInfo.Exists)
                    {
                        directoryInfo.Delete(true);
                    }
                }

                #endregion

                #region 找出要修改的SOP
                var updateSOPs = post.SOPs.Where(x => x.SOPId != 0 && x.Deleted == 0).ToList();

                //取得所有SOP資料
                var sopWhere = $@"""Deleted"" = 0 AND ""SOPId"" = ANY (@SOPIds)";
                var tempSops = await _baseRepository.GetAllAsync<SOP>("SOP", sopWhere, new { SOPIds = updateSOPs.Select(x => x.SOPId).ToList() });
                foreach (var updateSOP in updateSOPs)
                {
                    //步驟圖片
                    string? imageName = null;
                    if (updateSOP.SOPImageObj != null)
                    {
                        imageName = folderFunction.FileProduceName(updateSOP.SOPImageObj);
                    }

                    //步驟影片
                    string? videoName = null;
                    if (updateSOP.SOPVideoObj != null)
                    {
                        videoName = folderFunction.FileProduceName(updateSOP.SOPVideoObj);
                    }

                    //備註圖片
                    string? remarksImageName = null;
                    if (updateSOP.SOPRemarksImageObj != null)
                    {
                        remarksImageName = folderFunction.FileProduceName(updateSOP.SOPRemarksImageObj);
                    }

                    Dictionary<string, object> updateSOP_Dict = new Dictionary<string, object>()
                    {
                        { "SOPId", updateSOP.SOPId},
                        { "@SOPStep", updateSOP.SOPStep},
                        { "@SOPMessage", updateSOP.SOPMessage},
                        { "@SOPRemarksMessage", updateSOP.SOPRemarksMessage},
                        { "@SOPPLC1", updateSOP.SOPPLC1},
                        { "@SOPPLC2", updateSOP.SOPPLC2},
                        { "@SOPPLC3", updateSOP.SOPPLC3},
                        { "@SOPPLC4", updateSOP.SOPPLC4},
                        { "@Updater", myUser.UserId},
                        { "@UpdateTime", DateTime.Now}
                    };

                    if (!string.IsNullOrEmpty(imageName))
                    {
                        updateSOP_Dict.Add("@SOPImage", imageName);
                    }
                    else
                    {
                        if (updateSOP.IsDeletedSOPImage)
                        {
                            updateSOP_Dict.Add("@SOPImage", null);
                        }
                    }

                    if (!string.IsNullOrEmpty(videoName))
                    {
                        updateSOP_Dict.Add("@SOPVideo", videoName);
                    }
                    else
                    {
                        if (updateSOP.IsDeletedSOPVideo)
                        {
                            updateSOP_Dict.Add("@SOPVideo", null);
                        }
                    }

                    if (!string.IsNullOrEmpty(remarksImageName))
                    {
                        updateSOP_Dict.Add("@SOPRemarksImage", remarksImageName);
                    }
                    else
                    {
                        if (updateSOP.IsDeletedSOPRemarksImage)
                        {
                            updateSOP_Dict.Add("@SOPRemarksImage", null);
                        }
                    }

                    await _baseRepository.UpdateOneByCustomTable(updateSOP_Dict, "SOP", "\"SOPId\" = @SOPId");

                    var tempSelectSOP = tempSops.Where(x => x.SOPId == updateSOP.SOPId).FirstOrDefault();
                    if (updateSOP.IsDeletedSOPImage && !string.IsNullOrEmpty(tempSelectSOP.SOPImage))
                    {
                        folderFunction.DeleteFile(Path.Combine(sopRootPath, tempSelectSOP.SOPId.ToString(), tempSelectSOP.SOPImage));
                    }

                    if (updateSOP.IsDeletedSOPVideo && !string.IsNullOrEmpty(tempSelectSOP.SOPVideo))
                    {
                        folderFunction.DeleteFile(Path.Combine(sopRootPath, tempSelectSOP.SOPId.ToString(), tempSelectSOP.SOPVideo));
                    }

                    if (updateSOP.IsDeletedSOPRemarksImage && !string.IsNullOrEmpty(tempSelectSOP.SOPRemarksImage))
                    {
                        folderFunction.DeleteFile(Path.Combine(sopRootPath, tempSelectSOP.SOPId.ToString(), tempSelectSOP.SOPRemarksImage));
                    }

                    if (!string.IsNullOrEmpty(imageName))
                    {
                        folderFunction.SavePathFile(updateSOP.SOPImageObj, Path.Combine(sopRootPath, tempSelectSOP.SOPId.ToString()), imageName);
                    }

                    if (!string.IsNullOrEmpty(videoName))
                    {
                        folderFunction.SavePathFile(updateSOP.SOPVideoObj, Path.Combine(sopRootPath, tempSelectSOP.SOPId.ToString()), videoName);
                    }

                    if (!string.IsNullOrEmpty(remarksImageName))
                    {
                        folderFunction.SavePathFile(updateSOP.SOPRemarksImageObj, Path.Combine(sopRootPath, tempSelectSOP.SOPId.ToString()), remarksImageName);
                    }
                }

                #region 修改的SOP底下所有Model情況
                foreach (var updateSOP in updateSOPs)
                {
                    if (updateSOP.SOPModels != null)
                    {
                        var addSOPModels = updateSOP.SOPModels.Where(x => x.SOPModelId == 0).ToList();
                        var deleteSOPModels = updateSOP.SOPModels.Where(x => x.SOPModelId != 0 && x.Deleted == 1).ToList();

                        #region 新增SOP Model
                        if (addSOPModels.Count > 0)
                        {
                            List<Dictionary<string, object>> addSOPModel_Dicts = new List<Dictionary<string, object>>();
                            List<IFormFile> modelImageFiles = new List<IFormFile>(); //要儲存的圖片檔案
                            List<string> modelImageFileNames = new List<string>(); //要儲存的圖片名稱
                            List<IFormFile> modelFiles = new List<IFormFile>(); //要儲存的zip檔案
                            List<string> modelFileNames = new List<string>(); //要儲存的zip檔案名稱
                            foreach (var addSOPModel in addSOPModels)
                            {
                                //圖片
                                string? modelImageName = null;
                                if (addSOPModel.SOPModelImageObj != null)
                                {
                                    modelImageName = folderFunction.FileProduceName(addSOPModel.SOPModelImageObj);

                                    modelImageFiles.Add(addSOPModel.SOPModelImageObj);
                                    modelImageFileNames.Add(modelImageName);
                                }

                                //檔案
                                string? modelFileName = null;
                                if (addSOPModel.SOPModelFileObj != null)
                                {
                                    modelFileName = folderFunction.FileProduceName(addSOPModel.SOPModelFileObj);

                                    modelFiles.Add(addSOPModel.SOPModelFileObj);
                                    modelFileNames.Add(modelFileName);
                                }

                                Dictionary<string, object> addSOPModel_Dict = new Dictionary<string, object>()
                                {
                                    { "@SOPId", addSOPModel.SOPId},
                                    { "@SOPModelImage", modelImageName},
                                    { "@SOPModelFile", modelFileName},
                                    { "@Creator", myUser.UserId},
                                };

                                addSOPModel_Dicts.Add(addSOPModel_Dict);
                            }

                            if (addSOPModel_Dicts.Count > 0)
                            {
                                await _baseRepository.AddMutiByCustomTable(addSOPModel_Dicts, "SOPModel");
                            }

                            var modelPath = Path.Combine(sopRootPath, updateSOP.SOPId.ToString(), "model");
                            if (modelImageFiles.Count > 0)
                            {
                                folderFunction.SavePathFile(modelImageFiles, modelPath, modelImageFileNames);
                            }

                            if (modelFiles.Count > 0)
                            {
                                folderFunction.SavePathFile(modelFiles, modelPath, modelFileNames);
                            }
                        }
                        #endregion

                        #region 刪除SOP Model
                        if (deleteSOPModels.Count > 0)
                        {
                            List<Dictionary<string, object>> deleteSOPModel_Dicts = new List<Dictionary<string, object>>();
                            foreach (var deleteSOPModel in deleteSOPModels)
                            {
                                Dictionary<string, object> deleteSOPModel_Dict = new Dictionary<string, object>()
                                {
                                    { "SOPModelId", deleteSOPModel.SOPModelId},
                                    { "@Deleted", DeletedDataEnum.True},
                                    { "@Updater", myUser.UserId},
                                    { "@UpdateTime", DateTime.Now}
                                };

                                deleteSOPModel_Dicts.Add(deleteSOPModel_Dict);
                            }
                            if (deleteSOPModel_Dicts.Count > 0)
                            {
                                await _baseRepository.UpdateMutiByCustomTable(deleteSOPModel_Dicts, "SOPModel", "\"SOPModelId\" = @SOPModelId");
                            }

                            //刪除SOP Model的圖片跟檔案
                            foreach (var deleteSOPModel in deleteSOPModels)
                            {
                                var tempSOPModelImageFullPath = Path.Combine(sopRootPath, updateSOP.SOPId.ToString(), "model", deleteSOPModel.SOPModelImage);
                                folderFunction.DeleteFile(tempSOPModelImageFullPath);

                                var tempSOPModelFileFullPath = Path.Combine(sopRootPath, updateSOP.SOPId.ToString(), "model", deleteSOPModel.SOPModelFile);
                                folderFunction.DeleteFile(tempSOPModelFileFullPath);
                            }
                        }
                        #endregion
                    }
                }
                #endregion

                #endregion

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
        /// 27. 儲存SOP Model 眼鏡
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<ApiResult<int>>> SaveSOPModelGlasses(PostSaveSOPModelGlasses post)
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
                var machineAlarm = await _baseRepository.GetOneAsync<MachineAlarm>("MachineAlarm", where, new { MachineAlarmId = post.MachineAlarmId });

                if (machineAlarm == null)
                {
                    apiResult.Code = "4005"; //該機台Alarm不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machineAlarm.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4005"; //該機台Alarm不存在資料庫或是資料已被刪除
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

                FolderFunction folderFunction = new FolderFunction();
                var sopRootPath = Path.Combine(_savePath, "machine", machine.MachineId.ToString(), "sop");

                #region 找出要新增的SOP Model
                var addSOPModels = post.SOPModelGlasses.Where(x => x.SOPModelId == 0).ToList();

                //找到來源的SOP Model
                var sourceSOPModelIds = addSOPModels.Select(x => x.SourceSOPModelId).ToList();
                var sopModelWhere = $@"""SOPModelId"" = ANY (@SOPModelIds)";
                var sopModels = await _baseRepository.GetAllAsync<SOPModel>("SOPModel", sopModelWhere, new { SOPModelIds = sourceSOPModelIds });

                List<Dictionary<string, object>> addSOPModel_Dicts = new List<Dictionary<string, object>>();
                List<string> sourceSOPModelImageFiles = new List<string>(); //要儲存的圖片檔案來源路徑
                List<string> targetSOPModelImageFiles = new List<string>(); //要儲存的圖片檔案目標路徑
                List<string> sourceSOPModelFiles = new List<string>(); //要儲存的檔案來源路徑
                List<string> targetSOPModelFiles = new List<string>(); //要儲存的檔案目標路徑
                foreach (var addSOPModel in addSOPModels)
                {
                    var sourceSOPModel = sopModels.Where(x => x.SOPModelId == addSOPModel.SourceSOPModelId).FirstOrDefault();

                    var modelPath = Path.Combine(sopRootPath, addSOPModel.SOPId.ToString(), "model");

                    //圖片
                    string? modelImageName = null;
                    if (sourceSOPModel == null)
                    {   //表示是從眼鏡新增的model
                        modelImageName = addSOPModel.SOPModelImage;
                    }
                    else if (!string.IsNullOrEmpty(sourceSOPModel.SOPModelImage))
                    {
                        var split = sourceSOPModel.SOPModelImage.Split(".");
                        var fileNameEx = split[split.Length - 1]; //副檔名

                        string guidFile = Guid.NewGuid().ToString("N");

                        modelImageName = $@"{guidFile}.{fileNameEx}";

                        var sourceSOPModelPath = Path.Combine(modelPath, sourceSOPModel.SOPModelImage);
                        sourceSOPModelImageFiles.Add(sourceSOPModelPath);

                        var targetSOPModelPath = Path.Combine(modelPath, modelImageName);
                        targetSOPModelImageFiles.Add(targetSOPModelPath);
                    }

                    //檔案
                    string? modelFileName = null;
                    if (sourceSOPModel == null)
                    {   //表示是從眼鏡新增的model
                        modelFileName = addSOPModel.SOPModelFile;
                    }
                    else if (!string.IsNullOrEmpty(sourceSOPModel.SOPModelFile))
                    {
                        var split = sourceSOPModel.SOPModelFile.Split(".");
                        var fileNameEx = split[split.Length - 1]; //副檔名

                        string guidFile = Guid.NewGuid().ToString("N");

                        modelFileName = $@"{guidFile}.{fileNameEx}";

                        var sourceSOPModelPath = Path.Combine(modelPath, sourceSOPModel.SOPModelFile);
                        sourceSOPModelFiles.Add(sourceSOPModelPath);

                        var targetSOPModelPath = Path.Combine(modelPath, modelFileName);
                        targetSOPModelFiles.Add(targetSOPModelPath);
                    }

                    Dictionary<string, object> addSOPModel_Dict = new Dictionary<string, object>()
                    {
                        { "@SOPId", addSOPModel.SOPId},
                        { "@SOPModelImage", modelImageName},
                        { "@SOPModelFile", modelFileName},
                        { "@SOPModelPX", addSOPModel.SOPModelPX},
                        { "@SOPModelPY", addSOPModel.SOPModelPY},
                        { "@SOPModelPZ", addSOPModel.SOPModelPZ},
                        { "@SOPModelRX", addSOPModel.SOPModelRX},
                        { "@SOPModelRY", addSOPModel.SOPModelRY},
                        { "@SOPModelRZ", addSOPModel.SOPModelRZ},
                        { "@SOPModelSX", addSOPModel.SOPModelSX},
                        { "@SOPModelSY", addSOPModel.SOPModelSY},
                        { "@SOPModelSZ", addSOPModel.SOPModelSZ},
                        { "@IsCommon", 1},
                        { "@Creator", myUser.UserId},
                    };
                    addSOPModel_Dicts.Add(addSOPModel_Dict);
                }

                if (addSOPModel_Dicts.Count > 0)
                {
                    await _baseRepository.AddMutiByCustomTable(addSOPModel_Dicts, "SOPModel");
                }

                for (var i = 0; i < sourceSOPModelImageFiles.Count; i++)
                {
                    System.IO.File.Copy(sourceSOPModelImageFiles[i], targetSOPModelImageFiles[i], true);
                }

                for (var i = 0; i < sourceSOPModelFiles.Count; i++)
                {
                    System.IO.File.Copy(sourceSOPModelFiles[i], targetSOPModelFiles[i], true);
                }
                #endregion

                #region 找出要刪除的SOP Model
                var deleteSOPModels = post.SOPModelGlasses.Where(x => x.Deleted == 1).ToList();

                List<Dictionary<string, object>> deleteSOPModel_Dicts = new List<Dictionary<string, object>>();
                foreach (var deleteSOPModel in deleteSOPModels)
                {
                    Dictionary<string, object> deleteSOPModel_Dict = new Dictionary<string, object>()
                    {
                        { "SOPModelId", deleteSOPModel.SOPModelId},
                        { "@Deleted", DeletedDataEnum.True},
                        { "@Updater", myUser.UserId},
                        { "@UpdateTime", DateTime.Now}
                    };

                    deleteSOPModel_Dicts.Add(deleteSOPModel_Dict);
                }
                if (deleteSOPModel_Dicts.Count > 0)
                {
                    //刪除SOP Model
                    await _baseRepository.UpdateMutiByCustomTable(deleteSOPModel_Dicts, "SOPModel", "\"SOPModelId\" = @SOPModelId");
                }

                //刪除SOP Model的圖片跟檔案
                foreach (var deleteSOPModel in deleteSOPModels)
                {
                    var tempSOPModelImageFullPath = Path.Combine(sopRootPath, deleteSOPModel.SOPId.ToString(), "model", deleteSOPModel.SOPModelImage);
                    folderFunction.DeleteFile(tempSOPModelImageFullPath);

                    var tempSOPModelFileFullPath = Path.Combine(sopRootPath, deleteSOPModel.SOPId.ToString(), "model", deleteSOPModel.SOPModelFile);
                    folderFunction.DeleteFile(tempSOPModelFileFullPath);
                }
                #endregion

                #region 找出要修改的SOP Model
                var updateSOPModels = post.SOPModelGlasses.Where(x => x.SOPModelId != 0 && x.Deleted == 0).ToList();
                List<Dictionary<string, object>> updateSOPModel_Dicts = new List<Dictionary<string, object>>();
                foreach (var updateSOPModel in updateSOPModels)
                {
                    Dictionary<string, object> updateModel_Dict = new Dictionary<string, object>()
                    {
                        { "SOPModelId", updateSOPModel.SOPModelId},
                        { "@SOPModelPX", updateSOPModel.SOPModelPX},
                        { "@SOPModelPY", updateSOPModel.SOPModelPY},
                        { "@SOPModelPZ", updateSOPModel.SOPModelPZ},
                        { "@SOPModelRX", updateSOPModel.SOPModelRX},
                        { "@SOPModelRY", updateSOPModel.SOPModelRY},
                        { "@SOPModelRZ", updateSOPModel.SOPModelRZ},
                        { "@SOPModelSX", updateSOPModel.SOPModelSX},
                        { "@SOPModelSY", updateSOPModel.SOPModelSY},
                        { "@SOPModelSZ", updateSOPModel.SOPModelSZ},
                        { "@Updater", myUser.UserId},
                    };
                    updateSOPModel_Dicts.Add(updateModel_Dict);
                }

                if (updateSOPModel_Dicts.Count > 0)
                {
                    await _baseRepository.UpdateMutiByCustomTable(updateSOPModel_Dicts, "SOPModel", "\"SOPModelId\" = @SOPModelId");
                }
                #endregion

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
