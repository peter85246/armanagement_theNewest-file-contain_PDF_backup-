using ARManagement.BaseRepository.Interface;
using ARManagement.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;
using System.Diagnostics;

namespace ARManagement.Controllers
{
    public class SOP2Controller : MyBaseApiController
    {
        private readonly IBaseRepository _baseRepository;
        private readonly ResponseCodeHelper _responseCodeHelper;
        private string _savePath = string.Empty;

        public SOP2Controller(
            IBaseRepository baseRepository,
            ResponseCodeHelper responseCodeHelper)
        {
            _baseRepository = baseRepository;
            _responseCodeHelper = responseCodeHelper;
            _savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "upload");
        }

        /// <summary>
        /// 依據MachineAddId取得所有SOP(包含眼鏡使用)
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<ApiResult<List<SOP2>>>> GetAllSOPByMachineAddId(PostAllSOP2 post)
        {
            ApiResult<List<SOP2>> apiResult = new ApiResult<List<SOP2>>(); /*jwtToken.Token*/

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
                var machineAddWhere = $@"""MachineAddId"" = @MachineAddId";
                var machineAdd = await _baseRepository.GetOneAsync<MachineAdd>("MachineAdd", machineAddWhere, new { MachineAddId = post.Id });

                if (machineAdd == null)
                {
                    apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machineAdd.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                var sopWhere = $@"""Deleted"" = 0 AND ""MachineAddId"" = @MachineAddId";

                //取得所有SOP
                var sop2s = await _baseRepository.GetAllAsync<SOP2>("SOP2", sopWhere, new { MachineAddId = machineAdd.MachineAddId }, "\"SOP2Step\" ASC");

                foreach (var sop2 in sop2s)
                {
                    if (!string.IsNullOrEmpty(sop2.SOP2Image))
                    {
                        sop2.SOP2Image = $"{baseURL}upload/machineAdd/{machineAdd.MachineAddId}/sop2/{sop2.SOP2Id}/{sop2.SOP2Image}";
                    }

                    if (!string.IsNullOrEmpty(sop2.SOP2RemarkImage))
                    {
                        sop2.SOP2RemarkImage = $"{baseURL}upload/machineAdd/{machineAdd.MachineAddId}/sop2/{sop2.SOP2Id}/{sop2.SOP2RemarkImage}";
                    }
                    
                }

                apiResult.Code = "0000";
                apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                apiResult.Result = sop2s;
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
        public async Task<ActionResult<ApiResult<int>>> SaveSOP2([FromForm] PostSaveSOP2 post)
        {
            ApiResult<int> apiResult = new ApiResult<int>(); /*jwtToken.Token*/

            try
            {
                //    return Ok(apiResult);
                //}
                #endregion

                #region 欄位驗證

                #region 判斷長度
                foreach (var sop2 in post.SOP2s)
                {
                    if ((sop2.SOP2Message != null && sop2.SOP2Message.Length > 1000))
                    {
                        apiResult.Code = "2005"; //輸入文字字數不符合長度規範
                        apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                        return Ok(apiResult);
                    }
                }
                #endregion

                #region 判斷圖片附檔名
                var validImageEx = new List<string>() { "png", "jpg", "jpeg" }; //合法圖片附檔名
                foreach (var sop2 in post.SOP2s)
                {
                    if (sop2.SOP2ImageObj != null)
                    {
                        var validImageSplit = sop2.SOP2ImageObj.FileName.Split(".");
                        var tempImageNameEx = validImageSplit[validImageSplit.Length - 1]; //副檔名

                        if (!validImageEx.Contains(tempImageNameEx.ToLower()))
                        {
                            apiResult.Code = "2004"; //不合法的欄位
                            apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                            return Ok(apiResult);
                        }
                    }

                    if (sop2.SOP2RemarkImageObj != null)
                    {
                        var validImageSplitRemark = sop2.SOP2RemarkImageObj.FileName.Split(".");
                        var tempImageNameExRemark = validImageSplitRemark[validImageSplitRemark.Length - 1]; //副檔名

                        if (!validImageEx.Contains(tempImageNameExRemark.ToLower()))
                        {
                            apiResult.Code = "2004"; //不合法的欄位
                            apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                            return Ok(apiResult);
                        }
                    }
                }
                #endregion


                #region 判斷機台是否被刪除或失效
                var machineAddWhere = $@"""MachineAddId"" = @MachineAddId";
                var machineAdd = await _baseRepository.GetOneAsync<MachineAdd>("MachineAdd", machineAddWhere, new { MachineAddId = post.MachineAddId });

                if (machineAdd == null)
                {
                    apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }

                if (machineAdd.Deleted == (byte)DeletedDataEnum.True)
                {
                    apiResult.Code = "4004"; //該機台不存在資料庫或是資料已被刪除
                    apiResult.Message = _responseCodeHelper.GetResponseCodeString(apiResult.Code);
                    return Ok(apiResult);
                }
                #endregion

                FolderFunction folderFunction = new FolderFunction();
                var sopRootPath = Path.Combine(_savePath, "machineAdd", machineAdd.MachineAddId.ToString(), "sop2");

                #region 找出要新增的SOP
                var addSOPs = post.SOP2s.Where(x => x.SOP2Id == 0).ToList();
                foreach (var addSOP in addSOPs)
                {
                    string? sopImageName = null;
                    string? remarkImageName = null;

                    if (addSOP.SOP2ImageObj != null)
                    {
                        sopImageName = folderFunction.FileProduceName(addSOP.SOP2ImageObj);
                    }

                    if (addSOP.SOP2RemarkImageObj != null)
                    {
                        remarkImageName = folderFunction.FileProduceName(addSOP.SOP2RemarkImageObj);
                    }

                    Dictionary<string, object> addSOP_Dict = new Dictionary<string, object>()
                    {
                        { "@MachineAddId", post.MachineAddId},
                        { "@SOP2Name", addSOP.SOP2Name },
                        { "@SOP2Step", addSOP.SOP2Step },
                        { "@SOP2Message", addSOP.SOP2Message },
                        { "@SOP2Image", sopImageName },
                        { "@SOP2Remark", addSOP.SOP2Remark },
                        { "@SOP2RemarkImage", remarkImageName },
                        //{ "@Creator", myUser.UserId },
                    };

                    var sopId = await _baseRepository.AddOneByCustomTable(addSOP_Dict, "SOP2", "SOP2Id");
                    var sopPath = Path.Combine(sopRootPath, sopId.ToString());
                    folderFunction.CreateFolder(sopPath, 0);

                    if (!string.IsNullOrEmpty(sopImageName))
                    {
                        folderFunction.SavePathFile(addSOP.SOP2ImageObj, sopPath, sopImageName);
                    }

                    if (!string.IsNullOrEmpty(remarkImageName))
                    {
                        folderFunction.SavePathFile(addSOP.SOP2RemarkImageObj, sopPath, remarkImageName);
                    }
                }
                #endregion

                #region 找出要刪除的SOP
                var deleteSOPs = post.SOP2s.Where(x => x.Deleted == 1).ToList();
                List<Dictionary<string, object>> deleteSOP_Dicts = new List<Dictionary<string, object>>();
                foreach (var deleteSOP in deleteSOPs)
                {
                    Dictionary<string, object> deleteSOP_Dict = new Dictionary<string, object>()
                    {
                        { "SOP2Id", deleteSOP.SOP2Id},
                        { "@Deleted", DeletedDataEnum.True},
                        //{ "@Updater", myUser.UserId},
                        { "@Updater", 1},
                        { "@UpdateTime", DateTime.Now}
                    };

                    deleteSOP_Dicts.Add(deleteSOP_Dict);
                }
                if (deleteSOP_Dicts.Count > 0)
                {
                    await _baseRepository.UpdateMutiByCustomTable(deleteSOP_Dicts, "SOP2", "\"SOP2Id\" = @SOP2Id");
                }

                foreach (var deleteSOP in deleteSOPs)
                {
                    var tempSOPPath = Path.Combine(sopRootPath, deleteSOP.SOP2Id.ToString());

                    DirectoryInfo directoryInfo = new DirectoryInfo(tempSOPPath);
                    if (directoryInfo.Exists)
                    {
                        directoryInfo.Delete(true);
                    }
                }
                #endregion

                #region 找出要修改的SOP
                var updateSOPs = post.SOP2s.Where(x => x.SOP2Id != 0 && x.Deleted == 0).ToList();
                var sopWhere = $@"""Deleted"" = 0 AND ""SOP2Id"" = ANY (@SOP2Ids)";
                var tempSops = await _baseRepository.GetAllAsync<SOP2>("SOP2", sopWhere, new { SOP2Ids = updateSOPs.Select(x => x.SOP2Id).ToList() });
                foreach (var updateSOP in updateSOPs)
                {
                    string? sopImageName = null;
                    string? remarkImageName = null;

                    if (updateSOP.SOP2ImageObj != null)
                    {
                        sopImageName = folderFunction.FileProduceName(updateSOP.SOP2ImageObj);
                    }

                    if (updateSOP.SOP2RemarkImageObj != null)
                    {
                        remarkImageName = folderFunction.FileProduceName(updateSOP.SOP2RemarkImageObj);
                    }

                    Dictionary<string, object> updateSOP_Dict = new Dictionary<string, object>()
                    {
                        { "@SOP2Id", updateSOP.SOP2Id },
                        { "@SOP2Name", updateSOP.SOP2Name },
                        { "@SOP2Step", updateSOP.SOP2Step },
                        { "@SOP2Message", updateSOP.SOP2Message },
                        { "@SOP2Remark", updateSOP.SOP2Remark },
                        //{ "@Updater", myUser.UserId},
                        { "@Updater", 1},
                        { "@UpdateTime", DateTime.Now}
                    };

                    if (!string.IsNullOrEmpty(sopImageName))
                    {
                        updateSOP_Dict.Add("@SOP2Image", sopImageName);
                    }
                    else
                    {
                        if (updateSOP.IsDeletedSOP2Image)
                        {
                            updateSOP_Dict.Add("@SOP2Image", null);
                        }

                    if (!string.IsNullOrEmpty(remarkImageName))
                    {
                        updateSOP_Dict.Add("@SOP2RemarkImage", remarkImageName);
                    }
                    else
                    {
                        if (updateSOP.IsDeletedSOP2RemarkImage)
                        {
                            updateSOP_Dict.Add("@SOP2RemarkImage", null);
                        }

                    await _baseRepository.UpdateOneByCustomTable(updateSOP_Dict, "SOP2", "\"SOP2Id\" = @SOP2Id");

                    var tempSelectSOP = tempSops.Where(x => x.SOP2Id == updateSOP.SOP2Id).FirstOrDefault();

                    if (updateSOP.IsDeletedSOP2Image && !string.IsNullOrEmpty(tempSelectSOP.SOP2Image))
                    {
                        folderFunction.DeleteFile(Path.Combine(sopRootPath, tempSelectSOP.SOP2Id.ToString(), tempSelectSOP.SOP2Image));
                    }

                    if (updateSOP.IsDeletedSOP2RemarkImage && !string.IsNullOrEmpty(tempSelectSOP.SOP2RemarkImage))
                    {
                        folderFunction.DeleteFile(Path.Combine(sopRootPath, tempSelectSOP.SOP2Id.ToString(), tempSelectSOP.SOP2RemarkImage));
                    }

                    if (!string.IsNullOrEmpty(sopImageName))
                    {
                        folderFunction.SavePathFile(updateSOP.SOP2ImageObj, Path.Combine(sopRootPath, tempSelectSOP.SOP2Id.ToString()), sopImageName);
                    }

                    if (!string.IsNullOrEmpty(remarkImageName))
                    {
                        folderFunction.SavePathFile(updateSOP.SOP2RemarkImageObj, Path.Combine(sopRootPath, tempSelectSOP.SOP2Id.ToString()), remarkImageName);
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