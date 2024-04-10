using Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ARManagement.BaseRepository.Interface
{
    public interface IBaseRepository
    {
        public IDbConnection GetDbConnection();
        /// <summary>
        /// 新增table一筆資料
        /// dictionary ex:{"@guid", Guid.NewGuid()}
        /// </summary>
        /// <param name="dict">新增資料庫名稱以及值</param>
        /// <param name="Table_name">資料表名稱</param>
        /// <returns>
        /// </returns>
        Task<int> AddOneByCustomTable(Dictionary<string, object> dict, string Table_name, string idColumn = "");
        /// <summary>
        /// 透過guid，軟刪除單一筆資料
        /// <para>UPDATE {tableName} SET deleted = 1 WHERE {idName} = @Guid</para>
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="tableName"></param>
        /// <param name="idName"></param>
        /// <returns></returns>
        Task DeleteOne(int id, string tableName, string idName);
        /// <summary>
        /// 透過guid，軟刪除單一筆資料
        /// <para>UPDATE {tableName} SET deleted = 1, updater = {updater}, UpdateTime = DateTime.now WHERE {idName} = @Guid</para>
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="tableName"></param>
        /// <param name="idName"></param>
        /// <returns></returns>
        Task DeleteOne(int id, string tableName, string idName, int updater);
        /// <summary>
        /// 透過guid、db_name、table_name，刪除指定的資料庫之資料表的一筆資料
        /// <para>UPDATE {db_name}.{table_name} SET Deleted = 1 WHERE {idName} = @Guid</para>
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="db_name"></param>
        /// <param name="table_name"></param>
        /// <param name="idName"></param>
        /// <returns></returns>
        Task DeleteOneByGuidWithCustomDBNameAndTable(string guid, string db_name, string table_name, string idName);
        /// <summary>
        /// 根據Where條件進行刪除
        /// <para>DELETE FROM {table_name} WHERE {sWhere}</para>
        /// </summary>
        /// <param name="table_name"></param>
        /// <param name="sWhere"></param>
        /// <param name="param">Ex: new { status = 1}</param>
        /// <returns></returns>
        Task PurgeOneByGuidWithCustomDBNameAndTable(string table_name, string sWhere, object param = null);
        /// <summary>
        /// 指定單一欄位，實際刪除單一筆資料
        /// <para>DELETE FROM {table_name} WHERE {idName} = @Guid</para>
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="table_name"></param>
        /// <param name="idName"></param>
        /// <returns></returns>
        Task PurgeOneAsync(int guid, string table_name, string idName);
        /// <summary>
        /// 取得所有資料(根據條件以及排序) 不需要則填where為""
        /// <para>SELECT * FROM {tableName} WHERE {sWhere} </para>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="tableName"></param>
        /// <param name="sWhere"></param>
        /// <param name="param">Ex: new { status = 1}</param>
        /// <param name="sOrderBy"></param>
        /// <returns></returns>
        Task<List<A>> GetAllAsync<A>(string tableName, string sWhere, object param = null, string sOrderBy = "");
        /// <summary>
        /// 根據SQL語句抓取整包資料
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="sqlString"></param>
        /// <param name="param">Ex: new { status = 1}</param>
        /// <returns></returns>
        Task<List<A>> GetAllAsync<A>(string sqlString, object param = null);
        /// <summary>
        /// 取得單一筆資料(排序),不需OrderBy填""
        /// <para>SELECT * FROM {tableName} WHERE {sWhere} ORDER BY {sOrderBy}</para>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="tableName"></param>
        /// <param name="sWhere"></param>
        /// <param name="param"></param>
        /// <param name="sOrderBy"></param>
        /// <returns></returns>
        Task<A> GetOneAsync<A>(string tableName, string sWhere, object param = null, string sOrderBy = "");
        /// <summary>
        /// 取得單一筆資料某一欄位(排序)，不需sWhere及sOrderBy填""
        /// <para>
        /// SELECT {selCol} FROM {tableName} WHERE {sWhere} ORDER BY {sOrderBy}
        /// </para>
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="sWhere"></param>
        /// <param name="sOrderBy"></param>
        /// <param name="selCol">填放欄位</param>
        /// <returns></returns>
        Task<A> GetOneAsync<A>(string tableName, string sWhere, string selCol, object param = null, string sOrderBy = "");
        /// <summary>
        /// 取得單一筆資料(根據自訂SQL, 自訂參數)
        /// </summary>
        /// <param name="sqlString"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        Task<A> GetOneAsync<A>(string sqlString, object param = null);
        /// <summary>
        /// 取得多筆資料的某個欄位變成列表
        /// <para>SELECT {selCol} FROM {table_name} WHERE {idName} = @Guid</para>
        /// </summary>
        /// <param name="id">流水號</param>
        /// <param name="table_name">資料表名稱</param>
        /// <param name="idName">指定欄位名稱的流水號</param>
        /// <param name="selCol">選擇陣列資料庫欄位名稱</param>
        /// <returns></returns>
        Task<List<object>> GetAllWithCustomDBNameAndTableAsync(int id, string table_name, string idName, string selCol);

        /// <summary>
        /// 新增Table多筆資料
        /// dictionary ex:{"@guid", Guid.NewGuid()}
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="Table_name"></param>
        /// <returns></returns>
        Task AddMutiByCustomTable(List<Dictionary<string, object>> dict, string Table_name);
        /// <summary>
        /// 新增Table多筆資料
        /// dictionary ex:{"@guid", Guid.NewGuid()}
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="Table_name"></param>
        /// <returns></returns>
        Task AddMutiByCustomTable(IDbConnection conn, IDbTransaction trans, List<Dictionary<string, object>> dict, string Table_name);
        /// <summary>
        /// 透過Guid來搜尋此筆資料是否存在
        /// <para>SELECT * FROM {Table_name} WHERE {id_name} = @Guid</para>
        /// </summary>
        /// <param name="id">id值</param>
        /// <param name="Table_name">table名稱</param>
        /// <param name="id_name">id名稱</param>
        /// <returns></returns>
        Task<Boolean> HasExistsWithGuid(int id, string Table_name, string id_name);
        /// <summary>
        /// 根據SQL條件搜尋此筆資料是否存在
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        Task<Boolean> HasExistsWithParam(string sql, object param = null);
        /// <summary>
        /// 更新Table一筆資料
        /// </summary>
        /// <param name="dict">更新資料庫名稱以及值</param>
        /// <param name="Table_name">資料表名稱</param>
        /// <param name="sWhere">Where條件</param>
        /// <returns></returns>
        Task UpdateOneByCustomTable(Dictionary<string, object> dict, string Table_name, string sWhere);
        /// <summary>
        /// 更新Table多筆資料
        /// </summary>
        /// <param name="dicts">更新值</param>
        /// <param name="Table_name">資料表名稱</param>
        /// <param name="sWhere">Where條件</param>
        /// <returns></returns>
        Task UpdateMutiByCustomTable(List<Dictionary<string, object>> dicts, string Table_name, string sWhere);

        Task ExecuteSql(string sql, object param = null);
        /// <summary>
        /// 判斷順序是否重複
        /// </summary>
        /// <param name="priorityVal">順序值</param>
        /// <param name="Table_name">table名稱</param>
        /// <param name="priority_name">順序名稱</param>
        /// <param name="sWhere">排除Where條件</param>
        /// <returns></returns>
        Task<Boolean> PriorityRepeat(int priorityVal, string Table_name, string priority_name, string sWhere);
    }
}
