
using System.Text.Json;

namespace ARManagement.Helpers
{
    public interface IResponseCodeHelper
    {
        public string GetResponseCodeString(string code);
    }

    public class ResponseCodeHelper : IResponseCodeHelper
    {
        private readonly string _defaultLanguage = "zh-TW";
        private string _language = string.Empty;
        private Dictionary<string, string> _responseCode;
        public ResponseCodeHelper()
        {
            GetResponseCodeDictionary();
        }

        public void SetLanguage(string language)
        {
            _language = language;
            GetResponseCodeDictionary();
        }

        public string GetResponseCodeString(string code)
        {
            string result = string.Empty;

            try
            {
                if (_responseCode != null)
                {
                    _responseCode.TryGetValue(code, out result);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }

        private void GetResponseCodeDictionary()
        {
            //讀取檔案
            string filePath = string.Empty;
            if (string.IsNullOrEmpty(_language))
            {
                filePath = Path.Combine(Directory.GetCurrentDirectory() + "/wwwroot/", "resources", "ResponseCode", string.Format("ResponseCode.{0}.json", this._defaultLanguage));
            }
            else
            {
                filePath = Path.Combine(Directory.GetCurrentDirectory() + "/wwwroot/", "resources", "ResponseCode", string.Format("ResponseCode.{0}.json", this._language));
            }

            using (StreamReader r = new StreamReader(filePath))
            {
                string json = r.ReadToEnd();
                _responseCode = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }
        }
    }
}
