using ARManagement.BaseRepository.Interface;
using ARManagement.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;
using System.Diagnostics;

namespace ARManagement.Controllers
{
    public class KnowledgeBaseController : MyBaseApiController
    {
        private readonly IBaseRepository _baseRepository;
        private readonly ResponseCodeHelper _responseCodeHelper;
        private string _savePath = string.Empty;

        public KnowledgeBaseController(
            IBaseRepository baseRepository,
            ResponseCodeHelper responseCodeHelper)
        {
            _baseRepository = baseRepository;
            _responseCodeHelper = responseCodeHelper;
            _savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "upload");
        }

        /// <summary>
        /// 取得最新建立的MachineId資訊綁定用以故障說明資訊
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<ApiResult<int>>> GetLatestMachineId()
        {
            ApiResult<int> apiResult = new ApiResult<int>();

            try
            {
                // 使用第二個 GetOneAsync 方法獲取最新的MachineId
                string tableName = "Machine";
                string sWhere = "\"Deleted\" = 0"; // 假設有一個Deleted字段來標記記錄是否被刪除
                string selCol = "\"MachineId\""; // 您想要返回的列
                string sOrderBy = "\"MachineId\" DESC"; // 降序排序以獲取最新的ID
                var latestMachineId = await _baseRepository.GetOneAsync<int>(
                    tableName,
                    sWhere,
                    selCol,
                    param: null,
                    sOrderBy: sOrderBy
                );

                if (latestMachineId != 0)
                {
                    apiResult.Code = "0000";
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(latestMachineId); // 返回最新的MachineId
                }
                else
                {
                    apiResult.Code = "4004"; // 沒有找到有效的機台ID
                    apiResult.Message = "沒有找到有效的機台ID";
                }
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
        /// 依據MachineId取得所有故障說明(包含眼鏡使用)
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<ApiResult<List<KnowledgeBase>>>> GetAllKnowledgeBaseByMachineId([FromForm] PostAllKnowledgeBase post)
        {
            ApiResult<List<KnowledgeBase>> apiResult = new ApiResult<List<KnowledgeBase>>(); /*jwtToken.Token*/

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

                #region 判斷IsCommon
                if (post.IsCommon < 0 || post.IsCommon > 1)
                {
                    apiResult.Code = "2004"; //不合法的欄位
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                #endregion

                #region 判斷機台是否被刪除或失效
                var machineWhere = $@"""MachineId"" = @MachineId";
                var machine = await _baseRepository.GetOneAsync<Machine>("Machine", machineWhere, new { MachineId = post.Id });

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

                var knowledgeBaseWhere = $@"""Deleted"" = 0 AND ""MachineId"" = @MachineId";

                //取得所有KnowledgeBase
                var knowledgeBases = await _baseRepository.GetAllAsync<KnowledgeBase>("KnowledgeBase", knowledgeBaseWhere, new { MachineId = machine.MachineId });

                foreach (var knowledgeBase in knowledgeBases)
                {
                    if (!string.IsNullOrEmpty(knowledgeBase.KnowledgeBaseModelImage))
                    {
                        knowledgeBase.KnowledgeBaseModelImage = $"{baseURL}upload/machine/{machine.MachineId}/knowledgeBase/{knowledgeBase.KnowledgeBaseId}/{knowledgeBase.KnowledgeBaseModelImage}";
                    }

                    if (!string.IsNullOrEmpty(knowledgeBase.KnowledgeBaseToolsImage))
                    {
                        knowledgeBase.KnowledgeBaseToolsImage = $"{baseURL}upload/machine/{machine.MachineId}/knowledgeBase/{knowledgeBase.KnowledgeBaseId}/{knowledgeBase.KnowledgeBaseToolsImage}";
                    }

                    if (!string.IsNullOrEmpty(knowledgeBase.KnowledgeBasePositionImage))
                    {
                        knowledgeBase.KnowledgeBasePositionImage = $"{baseURL}upload/machineAdd/{machine.MachineId}/knowledgeBase/{knowledgeBase.KnowledgeBaseId}/{knowledgeBase.KnowledgeBasePositionImage}";
                    }

                }

                apiResult.Code = "0000";
                apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                apiResult.Result = knowledgeBases;
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
        /// 26. KnowledgeBase頁面儲存設定
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        [DisableRequestSizeLimit]
        [Consumes("multipart/form-data")]
        [HttpPut]
        public async Task<ActionResult<ApiResult<int>>> SaveKnowledgeBase([FromForm] PostSaveKnowledgeBase post)
        {
            ApiResult<int> apiResult = new ApiResult<int>(); /*jwtToken.Token*/

            try
            {
                // 检查 post.KnowledgeBases 是否为 null
                if (post.KnowledgeBases == null)
                {
                    apiResult.Code = "錯誤代碼"; // 設置適當的錯誤代碼
                    apiResult.Message = "KnowledgeBases 為空";
                    return Ok(apiResult);
                }

                if (post.MachineId <= 0)
                {
                    apiResult.Code = "错误代码"; // 自定义或使用适当的错误代码
                    apiResult.Message = "MachineId must be greater than 0.";
                    return BadRequest(apiResult);
                }

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
                foreach (var knowledgeBase in post.KnowledgeBases)
                {
                    if ((knowledgeBase.KnowledgeBaseDeviceType != null && knowledgeBase.KnowledgeBaseDeviceType.Length > 100) ||
                        (knowledgeBase.KnowledgeBaseDeviceParts != null && knowledgeBase.KnowledgeBaseDeviceParts.Length > 100) ||
                        (knowledgeBase.KnowledgeBaseRepairItems != null && knowledgeBase.KnowledgeBaseRepairItems.Length > 100) ||
                        (knowledgeBase.KnowledgeBaseRepairType != null && knowledgeBase.KnowledgeBaseRepairType.Length > 100) ||
                        (knowledgeBase.KnowledgeBaseFileNo != null && knowledgeBase.KnowledgeBaseFileNo.Length > 50) ||
                        (knowledgeBase.KnowledgeBaseAlarmCode != null && knowledgeBase.KnowledgeBaseAlarmCode.Length > 50) ||
                        (knowledgeBase.KnowledgeBaseSpec != null && knowledgeBase.KnowledgeBaseSpec.Length > 100) ||
                        (knowledgeBase.KnowledgeBaseSystem != null && knowledgeBase.KnowledgeBaseSystem.Length > 100) ||
                        (knowledgeBase.KnowledgeBaseProductName != null && knowledgeBase.KnowledgeBaseProductName.Length > 100) ||
                        (knowledgeBase.KnowledgeBaseAlarmCause != null && knowledgeBase.KnowledgeBaseAlarmCause.Length > 1000) ||
                        (knowledgeBase.KnowledgeBaseAlarmDesc != null && knowledgeBase.KnowledgeBaseAlarmDesc.Length > 1000) ||
                        (knowledgeBase.KnowledgeBaseAlarmOccasion != null && knowledgeBase.KnowledgeBaseAlarmOccasion.Length > 1000))
                    {
                        apiResult.Code = "2005"; //輸入文字字數不符合長度規範
                        apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                        return Ok(apiResult);
                    }
                }
                #endregion

                #region 判斷圖片附檔名
                var validImageEx = new List<string>() { "png", "jpg", "jpeg" }; //合法圖片附檔名
                foreach (var knowledgeBase in post.KnowledgeBases)
                {
                    if (knowledgeBase.KnowledgeBaseModelImageObj != null)
                    {
                        var validImageSplit = knowledgeBase.KnowledgeBaseModelImageObj.FileName.Split(".");
                        var tempImageNameEx = validImageSplit[validImageSplit.Length - 1]; //副檔名

                        if (!validImageEx.Contains(tempImageNameEx.ToLower()))
                        {
                            apiResult.Code = "2004"; //不合法的欄位
                            apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                            return Ok(apiResult);
                        }
                    }

                    if (knowledgeBase.KnowledgeBaseToolsImageObj != null)
                    {
                        var validToolsImageSplit = knowledgeBase.KnowledgeBaseToolsImageObj.FileName.Split(".");
                        var tempToolsImageNameEx = validToolsImageSplit[validToolsImageSplit.Length - 1]; //副檔名

                        if (!validImageEx.Contains(tempToolsImageNameEx.ToLower()))
                        {
                            apiResult.Code = "2004"; //不合法的欄位
                            apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                            return Ok(apiResult);
                        }
                    }

                    if (knowledgeBase.KnowledgeBasePositionImageObj != null)
                    {
                        var validPositionImageSplit = knowledgeBase.KnowledgeBasePositionImageObj.FileName.Split(".");
                        var tempPositionImageNameEx = validPositionImageSplit[validPositionImageSplit.Length - 1]; //副檔名

                        if (!validImageEx.Contains(tempPositionImageNameEx.ToLower()))
                        {
                            apiResult.Code = "2004"; //不合法的欄位
                            apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                            return Ok(apiResult);
                        }
                    }
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

                FolderFunction folderFunction = new FolderFunction();
                var knowledgeBaseRootPath = Path.Combine(_savePath, "machine", machine.MachineId.ToString(), "knowledgeBase");

                #region 找出要新增的KnowledgeBase
                var addKnowledgeBases = post.KnowledgeBases.Where(x => x.KnowledgeBaseId == 0).ToList();
                foreach (var addKnowledgeBase in addKnowledgeBases)
                {
                    string? knowledgeBaseModelImageName = null;
                    string? knowledgeBaseToolsImageName = null;
                    string? knowledgeBasePositionImageName = null;

                    if (addKnowledgeBase.KnowledgeBaseModelImageObj != null)
                    {
                        knowledgeBaseModelImageName = folderFunction.FileProduceName(addKnowledgeBase.KnowledgeBaseModelImageObj);
                    }

                    if (addKnowledgeBase.KnowledgeBaseToolsImageObj != null)
                    {
                        knowledgeBaseToolsImageName = folderFunction.FileProduceName(addKnowledgeBase.KnowledgeBaseToolsImageObj);
                    }

                    if (addKnowledgeBase.KnowledgeBasePositionImageObj != null)
                    {
                        knowledgeBasePositionImageName = folderFunction.FileProduceName(addKnowledgeBase.KnowledgeBasePositionImageObj);
                    }

                    Dictionary<string, object> addKnowledgeBase_Dict = new Dictionary<string, object>()
                    {
                        { "@MachineId", post.MachineId},
                        { "@KnowledgeBaseDeviceType", addKnowledgeBase.KnowledgeBaseDeviceType },
                        { "@KnowledgeBaseDeviceParts", addKnowledgeBase.KnowledgeBaseDeviceParts },
                        { "@KnowledgeBaseRepairItems", addKnowledgeBase.KnowledgeBaseRepairItems },
                        { "@KnowledgeBaseRepairType", addKnowledgeBase.KnowledgeBaseRepairType },
                        { "@KnowledgeBaseFileNo", addKnowledgeBase.KnowledgeBaseFileNo },
                        { "@KnowledgeBaseAlarmCode", addKnowledgeBase.KnowledgeBaseAlarmCode },
                        { "@KnowledgeBaseSpec", addKnowledgeBase.KnowledgeBaseSpec },
                        { "@KnowledgeBaseSystem", addKnowledgeBase.KnowledgeBaseSystem },
                        { "@KnowledgeBaseProductName", addKnowledgeBase.KnowledgeBaseProductName },
                        { "@KnowledgeBaseAlarmCause", addKnowledgeBase.KnowledgeBaseAlarmCause },
                        { "@KnowledgeBaseAlarmDesc", addKnowledgeBase.KnowledgeBaseAlarmDesc },
                        { "@KnowledgeBaseAlarmOccasion", addKnowledgeBase.KnowledgeBaseAlarmOccasion },
                        { "@KnowledgeBaseModelImage", knowledgeBaseModelImageName },
                        { "@KnowledgeBaseToolsImage", knowledgeBaseToolsImageName },
                        { "@KnowledgeBasePositionImage", knowledgeBasePositionImageName },
                        { "@Creator", 1 },
                        //{ "@Creator", myUser.UserId },
                    };

                    // 建立新Table
                    var knowledgeBaseId = await _baseRepository.AddOneByCustomTable(addKnowledgeBase_Dict, "KnowledgeBase", "KnowledgeBaseId");

                    // 判斷KnowledgeBase資料夾是否存在
                    var knowledgeBasePath = Path.Combine(knowledgeBaseRootPath, knowledgeBaseId.ToString());
                    folderFunction.CreateFolder(knowledgeBasePath, 0);

                    // 處理圖片儲存
                    if (!string.IsNullOrEmpty(knowledgeBaseModelImageName))
                    {
                        folderFunction.SavePathFile(addKnowledgeBase.KnowledgeBaseModelImageObj, knowledgeBasePath, knowledgeBaseModelImageName);
                    }

                    if (!string.IsNullOrEmpty(knowledgeBaseToolsImageName))
                    {
                        folderFunction.SavePathFile(addKnowledgeBase.KnowledgeBaseToolsImageObj, knowledgeBasePath, knowledgeBaseToolsImageName);
                    }

                    if (!string.IsNullOrEmpty(knowledgeBasePositionImageName))
                    {
                        folderFunction.SavePathFile(addKnowledgeBase.KnowledgeBasePositionImageObj, knowledgeBasePath, knowledgeBasePositionImageName);
                    }

                }
                #endregion

                #region 找出要刪除的KnowledgeBase
                var deleteKnowledgeBases = post.KnowledgeBases.Where(x => x.Deleted == 1).ToList();
                List<Dictionary<string, object>> deleteKnowledgeBase_Dicts = new List<Dictionary<string, object>>();
                foreach (var deleteKnowledgeBase in deleteKnowledgeBases)
                {
                    Dictionary<string, object> deleteKnowledgeBase_Dict = new Dictionary<string, object>()
                    {
                        { "KnowledgeBaseId", deleteKnowledgeBase.KnowledgeBaseId},
                        { "@Deleted", DeletedDataEnum.True},
                        { "@Updater", myUser.UserId},
                        { "@UpdateTime", DateTime.Now}
                    };

                    deleteKnowledgeBase_Dicts.Add(deleteKnowledgeBase_Dict);
                }
                if (deleteKnowledgeBase_Dicts.Count > 0)
                {
                    //刪除KnowledgeBase
                    await _baseRepository.UpdateMutiByCustomTable(deleteKnowledgeBase_Dicts, "KnowledgeBase", "\"KnowledgeBaseId\" = @KnowledgeBaseId");
                }

                //刪除KnowledgeBase資料底下所有子資料夾、檔案
                foreach (var deleteKnowledgeBase in deleteKnowledgeBases)
                {
                    var tempKnowledgeBasePath = Path.Combine(knowledgeBaseRootPath, deleteKnowledgeBase.KnowledgeBaseId.ToString());

                    DirectoryInfo directoryInfo = new DirectoryInfo(tempKnowledgeBasePath);
                    if (directoryInfo.Exists)
                    {
                        directoryInfo.Delete(true);
                    }
                }

                #endregion

                #region 找出要修改的KnowledgeBase
                var updateKnowledgeBases = post.KnowledgeBases.Where(x => x.KnowledgeBaseId != 0 && x.Deleted == 0).ToList();

                //取得所有KnowledgeBase資料
                var knowledgeBaseWhere = $@"""Deleted"" = 0 AND ""KnowledgeBaseId"" = ANY (@KnowledgeBaseIds)";
                var tempKnowledgeBases = await _baseRepository.GetAllAsync<KnowledgeBase>("KnowledgeBase", knowledgeBaseWhere, new { KnowledgeBaseIds = updateKnowledgeBases.Select(x => x.KnowledgeBaseId).ToList() });
                foreach (var updateKnowledgeBase in updateKnowledgeBases)
                {
                    // 圖片
                    string? knowledgeBaseModelImageName = null;
                    string? knowledgeBaseToolsImageName = null;
                    string? knowledgeBasePositionImageName = null;

                    if (updateKnowledgeBase.KnowledgeBaseModelImageObj != null)
                    {
                        knowledgeBaseModelImageName = folderFunction.FileProduceName(updateKnowledgeBase.KnowledgeBaseModelImageObj);
                    }

                    if (updateKnowledgeBase.KnowledgeBaseToolsImageObj != null)
                    {
                        knowledgeBaseToolsImageName = folderFunction.FileProduceName(updateKnowledgeBase.KnowledgeBaseToolsImageObj);
                    }

                    if (updateKnowledgeBase.KnowledgeBasePositionImageObj != null)
                    {
                        knowledgeBasePositionImageName = folderFunction.FileProduceName(updateKnowledgeBase.KnowledgeBasePositionImageObj);
                    }

                    Dictionary<string, object> updateKnowledgeBase_Dict = new Dictionary<string, object>()
                    {
                        { "@KnowledgeBaseId", updateKnowledgeBase.KnowledgeBaseId},
                        { "@KnowledgeBaseDeviceType", updateKnowledgeBase.KnowledgeBaseDeviceType },
                        { "@KnowledgeBaseDeviceParts", updateKnowledgeBase.KnowledgeBaseDeviceParts },
                        { "@KnowledgeBaseRepairItems", updateKnowledgeBase.KnowledgeBaseRepairItems },
                        { "@KnowledgeBaseRepairType", updateKnowledgeBase.KnowledgeBaseRepairType },
                        { "@KnowledgeBaseFileNo", updateKnowledgeBase.KnowledgeBaseFileNo },
                        { "@KnowledgeBaseAlarmCode", updateKnowledgeBase.KnowledgeBaseAlarmCode },
                        { "@KnowledgeBaseSpec", updateKnowledgeBase.KnowledgeBaseSpec },
                        { "@KnowledgeBaseSystem", updateKnowledgeBase.KnowledgeBaseSystem },
                        { "@KnowledgeBaseProductName", updateKnowledgeBase.KnowledgeBaseProductName },
                        { "@KnowledgeBaseAlarmCause", updateKnowledgeBase.KnowledgeBaseAlarmCause },
                        { "@KnowledgeBaseAlarmDesc", updateKnowledgeBase.KnowledgeBaseAlarmDesc },
                        { "@KnowledgeBaseAlarmOccasion", updateKnowledgeBase.KnowledgeBaseAlarmOccasion },
                        { "@Updater", myUser.UserId},
                        { "@UpdateTime", DateTime.Now}
                    };

                    // 處理圖片儲存
                    if (!string.IsNullOrEmpty(knowledgeBaseModelImageName))
                    {
                        updateKnowledgeBase_Dict.Add("@KnowledgeBaseModelImage", knowledgeBaseModelImageName);
                    }
                    else
                    {
                        if (updateKnowledgeBase.IsDeletedKnowledgeBaseModelImage)
                        {
                            updateKnowledgeBase_Dict.Add("@KnowledgeBaseModelImage", null);
                        }
                    }

                    if (!string.IsNullOrEmpty(knowledgeBaseToolsImageName))
                    {
                        updateKnowledgeBase_Dict.Add("@KnowledgeBaseToolsImage", knowledgeBaseToolsImageName);
                    }
                    else
                    {
                        if (updateKnowledgeBase.IsDeletedKnowledgeBaseToolsImage)
                        {
                            updateKnowledgeBase_Dict.Add("@KnowledgeBaseToolsImage", null);
                        }
                    }

                    if (!string.IsNullOrEmpty(knowledgeBasePositionImageName))
                    {
                        updateKnowledgeBase_Dict.Add("@KnowledgeBasePositionImage", knowledgeBasePositionImageName);
                    }
                    else
                    {
                        if (updateKnowledgeBase.IsDeletedKnowledgeBasePositionImage)
                        {
                            updateKnowledgeBase_Dict.Add("@KnowledgeBasePositionImage", null);
                        }
                    }

                    await _baseRepository.UpdateOneByCustomTable(updateKnowledgeBase_Dict, "KnowledgeBase", "\"KnowledgeBaseId\" = @KnowledgeBaseId");

                    var tempSelectKnowledgeBase = tempKnowledgeBases.Where(x => x.KnowledgeBaseId == updateKnowledgeBase.KnowledgeBaseId).FirstOrDefault();

                    if (updateKnowledgeBase.IsDeletedKnowledgeBaseModelImage && !string.IsNullOrEmpty(tempSelectKnowledgeBase.KnowledgeBaseModelImage))
                    {
                        folderFunction.DeleteFile(Path.Combine(knowledgeBaseRootPath, tempSelectKnowledgeBase.KnowledgeBaseId.ToString(), tempSelectKnowledgeBase.KnowledgeBaseModelImage));
                    }

                    if (updateKnowledgeBase.IsDeletedKnowledgeBaseToolsImage && !string.IsNullOrEmpty(tempSelectKnowledgeBase.KnowledgeBaseToolsImage))
                    {
                        folderFunction.DeleteFile(Path.Combine(knowledgeBaseRootPath, tempSelectKnowledgeBase.KnowledgeBaseId.ToString(), tempSelectKnowledgeBase.KnowledgeBaseToolsImage));
                    }

                    if (updateKnowledgeBase.IsDeletedKnowledgeBasePositionImage && !string.IsNullOrEmpty(tempSelectKnowledgeBase.KnowledgeBasePositionImage))
                    {
                        folderFunction.DeleteFile(Path.Combine(knowledgeBaseRootPath, tempSelectKnowledgeBase.KnowledgeBaseId.ToString(), tempSelectKnowledgeBase.KnowledgeBasePositionImage));
                    }

                    if (!string.IsNullOrEmpty(knowledgeBaseModelImageName))
                    {
                        folderFunction.SavePathFile(updateKnowledgeBase.KnowledgeBaseModelImageObj, Path.Combine(knowledgeBaseRootPath, tempSelectKnowledgeBase.KnowledgeBaseId.ToString()), knowledgeBaseModelImageName);
                    }

                    if (!string.IsNullOrEmpty(knowledgeBaseToolsImageName))
                    {
                        folderFunction.SavePathFile(updateKnowledgeBase.KnowledgeBaseToolsImageObj, Path.Combine(knowledgeBaseRootPath, tempSelectKnowledgeBase.KnowledgeBaseId.ToString()), knowledgeBaseToolsImageName);
                    }

                    if (!string.IsNullOrEmpty(knowledgeBasePositionImageName))
                    {
                        folderFunction.SavePathFile(updateKnowledgeBase.KnowledgeBasePositionImageObj, Path.Combine(knowledgeBaseRootPath, tempSelectKnowledgeBase.KnowledgeBaseId.ToString()), knowledgeBasePositionImageName);
                    }
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
#endregion