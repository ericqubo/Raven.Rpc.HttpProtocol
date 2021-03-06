﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Rpc.HttpProtocol
{
    /// <summary>
    /// 
    /// </summary>
    public class RpcHttpClient2 : IRpcHttpClient, IRpcHttpClientAsync, IDisposable
    //where RT : class, new()
    {
        private string _baseUrl;
        private int _timeout;
        private HttpClient _httpClient;
        private string _mediaType;
        private MediaTypeFormatter _mediaTypeFormatter;
        private MediaTypeFormatter[] _mediaTypeFormatterArray = new MediaTypeFormatter[]
        {
            new FormUrlEncodedMediaTypeFormatter(),
            new XmlMediaTypeFormatter(),
            new JsonMediaTypeFormatter(),
        };

        private MediaTypeWithQualityHeaderValue _mediaTypeWithQualityHeaderValue;

        private static Encoding defaultEncoding = Encoding.UTF8;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <param name="mediaType"></param>
        /// <param name="timeout">超时时间（毫秒）</param>
        public RpcHttpClient2(string baseUrl, string mediaType = MediaType.json, int timeout = 10000)
        {
            this._baseUrl = baseUrl;
            this._timeout = timeout;
            _mediaType = mediaType;
            _mediaTypeFormatter = CreateMediaTypeFormatter(mediaType);
            _mediaTypeWithQualityHeaderValue = new MediaTypeWithQualityHeaderValue(mediaType);
            _httpClient = new HttpClient();
            InitHttpClient(timeout, _httpClient);
        }

        /// <summary>
        /// 创建MediaTypeFormatter
        /// </summary>
        /// <param name="mediaType"></param>
        /// <returns></returns>
        private MediaTypeFormatter CreateMediaTypeFormatter(string mediaType)
        {
            MediaTypeFormatter mediaTypeFormatter = null;
            switch (mediaType)
            {
                case MediaType.form:
                    mediaTypeFormatter = new FormUrlEncodedMediaTypeFormatter();
                    break;
                case MediaType.xml:
                    mediaTypeFormatter = new XmlMediaTypeFormatter();
                    break;
                case MediaType.bson:
                    throw new Exception("bson is not support");
                case MediaType.json:
                default:
                    mediaTypeFormatter = new JsonMediaTypeFormatter();
                    break;
            }

            return mediaTypeFormatter;
        }

        /// <summary>
        /// HttpClient初始化
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="client"></param>
        private void InitHttpClient(int? timeout, HttpClient client)
        {
            if (timeout.HasValue)
            {
                client.Timeout = TimeSpan.FromMilliseconds(timeout.Value);
            }
            else
            {
                client.Timeout = TimeSpan.FromMilliseconds(this._timeout);
            }

            if (!string.IsNullOrWhiteSpace(_baseUrl))
            {
                client.BaseAddress = new Uri(_baseUrl);
            }
            client.DefaultRequestHeaders.Accept.Add(_mediaTypeWithQualityHeaderValue);
            client.DefaultRequestHeaders.Connection.Add("keep-alive");
            DefaultRequestHeadersHandler(client.DefaultRequestHeaders);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="TData"></typeparam>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <param name="httpMethod"></param>
        /// <param name="urlParameters"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public virtual Task<TResult> SendAsync<TResult, TData>(string url, TData data = default(TData), IDictionary<string, string> urlParameters = null, HttpMethod httpMethod = null, int? timeout = null)
            where TResult : class
            where TData : class
        {
            var client = _httpClient;
            string requestUrl = _baseUrl + url;
            CreateUrlParams(urlParameters, ref requestUrl);
            HttpContent content = null;
            using (HttpRequestMessage request = new HttpRequestMessage())
            {
                request.Method = httpMethod;
                if (data != null)
                {
                    content = CreateContent(data);
                    //if (data is string)
                    //{
                    //    content = new StringContent(data.ToString(), Encoding.UTF8);
                    //}
                    //else
                    //{
                    //    content = new ObjectContent<TData>(data, _mediaTypeFormatter);
                    //}
                    request.Content = content;
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue(_mediaType);
                }
                request.RequestUri = new Uri(requestUrl);

                var responseTs = client.SendAsync(request);
                request.Dispose();
                return responseTs.ContinueWith(x =>
                {
                    using (var response = x.Result)
                    {
                        return GetResult<TResult>(response);
                    }
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="TData"></typeparam>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <param name="httpMethod"></param>
        /// <param name="urlParameters"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public virtual TResult Send<TResult, TData>(string url, TData data = default(TData), IDictionary<string, string> urlParameters = null, HttpMethod httpMethod = null, int? timeout = null)
            where TResult : class
            where TData : class
        {
            var client = _httpClient;
            string requestUrl = _baseUrl + url;
            CreateUrlParams(urlParameters, ref requestUrl);
            HttpContent content = null;
            using (HttpRequestMessage request = new HttpRequestMessage())
            {
                request.Method = httpMethod;
                if (data != null)
                {
                    content = CreateContent(data);

                    request.Content = content;
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue(_mediaType);
                }
                request.RequestUri = new Uri(requestUrl);
                using (HttpResponseMessage response = client.SendAsync(request).Result)
                {
                    TResult result = GetResultAsync<TResult>(response).Result;

                    if (content != null)
                    {
                        content.Dispose();
                    }
                    return result as TResult;
                }
            }
        }

        /// <summary>
        /// Get
        /// </summary>
        /// <typeparam name="TResult">返回数据类型</typeparam>
        /// <param name="url">请求Url</param>
        /// <param name="urlParameters">url parameter 数据</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public virtual TResult Get<TResult>(string url, IDictionary<string, string> urlParameters = null, int? timeout = null)
            where TResult : class
        {
            //using (var client = new HttpClient())
            //{
            //InitHttpClient(timeout, client);
            var client = _httpClient;

            string requestUrl = _baseUrl + url;
            CreateUrlParams(urlParameters, ref requestUrl);

            using (HttpResponseMessage response = client.GetAsync(requestUrl).Result)
            {
                TResult result = GetResult<TResult>(response);
                return result as TResult;
            }
            //}
        }

        /// <summary>
        /// Get
        /// </summary>
        /// <typeparam name="TResult">返回数据类型</typeparam>
        /// <param name="url">请求Url</param>
        /// <param name="urlParameters">url parameter 数据</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public virtual Task<TResult> GetAsync<TResult>(string url, IDictionary<string, string> urlParameters = null, int? timeout = null)
            where TResult : class
        {
            //using (var client = new HttpClient())
            //{
            //    InitHttpClient(timeout, client);

            var client = _httpClient;
            string requestUrl = _baseUrl + url;
            CreateUrlParams(urlParameters, ref requestUrl);
            var responseTs = client.GetAsync(requestUrl);
            return responseTs.ContinueWith(x =>
            {
                using (HttpResponseMessage response = x.Result)
                {
                    TResult result = GetResult<TResult>(response);
                    return result as TResult;
                }
            });

            //}
        }

        /// <summary>
        /// Post
        /// </summary>
        /// <typeparam name="TData">提交数据类型</typeparam>
        /// <typeparam name="TResult">返回数据类型</typeparam>
        /// <param name="url">请求Url</param>
        /// <param name="data">数据</param>
        /// <param name="urlParameters">url parameter 数据</param>
        /// <param name="timeout">超时时间</param>
        /// <returns></returns>
        public virtual TResult Post<TData, TResult>(string url, TData data, IDictionary<string, string> urlParameters = null, int? timeout = null)
            where TResult : class
        {
            //using (var client = new HttpClient())
            //{
            //    InitHttpClient(timeout, client);

            var client = _httpClient;

            string requestUrl = _baseUrl + url;
            CreateUrlParams(urlParameters, ref requestUrl);

            using (HttpContent content = CreateContent(data))
            {
                using (HttpResponseMessage response = client.PostAsync(requestUrl, content).Result)
                {
                    TResult result = GetResult<TResult>(response);
                    return result as TResult;
                }
            }
            //}
        }

        /// <summary>
        /// Post
        /// </summary>
        /// <typeparam name="TData">提交数据类型</typeparam>
        /// <typeparam name="TResult">返回数据类型</typeparam>
        /// <param name="url">请求Url</param>
        /// <param name="data">数据</param>
        /// <param name="urlParameters">url parameter 数据</param>
        /// <param name="timeout">超时时间</param>
        /// <returns></returns>
        public virtual Task<TResult> PostAsync<TData, TResult>(string url, TData data, IDictionary<string, string> urlParameters = null, int? timeout = null)
            where TResult : class
        {
            //using (var client = new HttpClient())
            //{
            //    InitHttpClient(timeout, client);

            var client = _httpClient;

            string requestUrl = _baseUrl + url;
            CreateUrlParams(urlParameters, ref requestUrl);

            using (HttpContent content = CreateContent(data))
            {
                var responseTs = client.PostAsync(requestUrl, content);
                return responseTs.ContinueWith(x =>
                {
                    using (HttpResponseMessage response = x.Result)
                    {
                        TResult result = GetResult<TResult>(response);
                        return result as TResult;
                    }
                });
            }
            //}
        }

        /// <summary>
        /// Post
        /// </summary>
        /// <typeparam name="TResult">返回数据类型</typeparam>
        /// <param name="url">请求Url</param>
        /// <param name="data">数据</param>
        /// <param name="urlParameters">url parameter 数据</param>
        /// <param name="timeout">超时时间</param>
        /// <returns></returns>
        public virtual TResult Post<TResult>(string url, byte[] data, IDictionary<string, string> urlParameters = null, int? timeout = null)
            where TResult : class
        {
            return Post<TResult>(url, data, 0, data.Length, urlParameters, timeout);
        }

        /// <summary>
        /// Post
        /// </summary>
        /// <typeparam name="TResult">返回数据类型</typeparam>
        /// <param name="url">请求Url</param>
        /// <param name="data">数据</param>
        /// <param name="urlParameters">url parameter 数据</param>
        /// <param name="timeout">超时时间</param>
        /// <returns></returns>
        public virtual Task<TResult> PostAsync<TResult>(string url, byte[] data, IDictionary<string, string> urlParameters = null, int? timeout = null)
            where TResult : class
        {
            return PostAsync<TResult>(url, data, 0, data.Length, urlParameters, timeout);
        }

        /// <summary>
        /// Post
        /// </summary>
        /// <typeparam name="TResult">返回数据类型</typeparam>
        /// <param name="url">请求Url</param>
        /// <param name="data">数据</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <param name="urlParameters">url parameter 数据</param>
        /// <param name="timeout">超时时间</param>
        /// <returns></returns>
        public virtual TResult Post<TResult>(string url, byte[] data, int offset, int count, IDictionary<string, string> urlParameters = null, int? timeout = null)
            where TResult : class
        {
            //using (var client = new HttpClient())
            //{
            //    InitHttpClient(timeout, client);

            var client = _httpClient;

            string requestUrl = _baseUrl + url;
            CreateUrlParams(urlParameters, ref requestUrl);

            using (HttpContent content = new ByteArrayContent(data, offset, count))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue(MediaType.bytes);
                using (HttpResponseMessage response = client.PostAsync(requestUrl, content).Result)
                {
                    TResult result = GetResult<TResult>(response);
                    return result as TResult;
                }
            }
            //}
        }

        /// <summary>
        /// Post
        /// </summary>
        /// <typeparam name="TResult">返回数据类型</typeparam>
        /// <param name="url">请求Url</param>
        /// <param name="data">数据</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <param name="urlParameters">url parameter 数据</param>
        /// <param name="timeout">超时时间</param>
        /// <returns></returns>
        public virtual Task<TResult> PostAsync<TResult>(string url, byte[] data, int offset, int count, IDictionary<string, string> urlParameters = null, int? timeout = null)
            where TResult : class
        {
            //using (var client = new HttpClient())
            //{
            //    InitHttpClient(timeout, client);

            var client = _httpClient;

            string requestUrl = _baseUrl + url;
            CreateUrlParams(urlParameters, ref requestUrl);

            using (HttpContent content = new ByteArrayContent(data, offset, count))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue(MediaType.bytes);

                var responseTs = client.PostAsync(requestUrl, content);
                return responseTs.ContinueWith(x =>
                {
                    using (HttpResponseMessage response = x.Result)
                    {
                        TResult result = GetResult<TResult>(response);
                        return result as TResult;
                    }
                });
            }
            //}
        }

        /// <summary>
        /// Post
        /// </summary>
        /// <typeparam name="TResult">返回数据类型</typeparam>
        /// <param name="url">请求Url</param>
        /// <param name="data">数据</param>
        /// <param name="urlParameters">url parameter 数据</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public virtual TResult Post<TResult>(string url, IDictionary<string, string> data, IDictionary<string, string> urlParameters = null, int? timeout = null)
            where TResult : class
        {
            //using (var client = new HttpClient())
            //{
            //    InitHttpClient(timeout, client);

            var client = _httpClient;

            string requestUrl = _baseUrl + url;
            CreateUrlParams(urlParameters, ref requestUrl);

            using (HttpContent content = new ObjectContent<IDictionary<string, string>>(data, _mediaTypeFormatter))
            {
                using (HttpResponseMessage response = client.PostAsync(requestUrl, content).Result)
                {
                    TResult result = GetResult<TResult>(response);
                    return result as TResult;
                }
            }
            //}
        }

        /// <summary>
        /// Post
        /// </summary>
        /// <typeparam name="TResult">返回数据类型</typeparam>
        /// <param name="url">请求Url</param>
        /// <param name="data">数据</param>
        /// <param name="urlParameters">url parameter 数据</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public virtual Task<TResult> PostAsync<TResult>(string url, IDictionary<string, string> data, IDictionary<string, string> urlParameters = null, int? timeout = null)
            where TResult : class
        {
            //using (var client = new HttpClient())
            //{
            //    InitHttpClient(timeout, client);

            var client = _httpClient;

            string requestUrl = _baseUrl + url;
            CreateUrlParams(urlParameters, ref requestUrl);

            using (HttpContent content = new ObjectContent<IDictionary<string, string>>(data, _mediaTypeFormatter))
            {
                var responseTs = client.PostAsync(requestUrl, content);
                return responseTs.ContinueWith(x =>
                {
                    using (HttpResponseMessage response = x.Result)
                    {
                        TResult result = GetResult<TResult>(response);
                        return result as TResult;
                    }
                });
            }
            //}
        }

        /// <summary>
        /// Put
        /// </summary>
        /// <typeparam name="TData">提交数据类型</typeparam>
        /// <typeparam name="TResult">返回数据类型</typeparam>
        /// <param name="url">请求Url</param>
        /// <param name="data">数据</param>
        /// <param name="urlParameters">url parameter 数据</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public virtual TResult Put<TData, TResult>(string url, TData data, IDictionary<string, string> urlParameters = null, int? timeout = null)
            where TResult : class
        {
            //using (var client = new HttpClient())
            //{
            //    InitHttpClient(timeout, client);

            var client = _httpClient;

            string requestUrl = _baseUrl + url;
            CreateUrlParams(urlParameters, ref requestUrl);

            using (HttpContent content = CreateContent(data))
            {
                using (HttpResponseMessage response = client.PutAsync(requestUrl, content).Result)
                {
                    TResult result = GetResult<TResult>(response);
                    return result as TResult;
                }
            }
            //}
        }

        /// <summary>
        /// Put
        /// </summary>
        /// <typeparam name="TData">提交数据类型</typeparam>
        /// <typeparam name="TResult">返回数据类型</typeparam>
        /// <param name="url">请求Url</param>
        /// <param name="data">数据</param>
        /// <param name="urlParameters">url parameter 数据</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public virtual Task<TResult> PutAsync<TData, TResult>(string url, TData data, IDictionary<string, string> urlParameters = null, int? timeout = null)
            where TResult : class
        {
            //using (var client = new HttpClient())
            //{
            //    InitHttpClient(timeout, client);

            var client = _httpClient;

            string requestUrl = _baseUrl + url;
            CreateUrlParams(urlParameters, ref requestUrl);

            using (HttpContent content = CreateContent(data))
            {
                var responseTs = client.PostAsync(requestUrl, content);
                return responseTs.ContinueWith(x =>
                {
                    using (HttpResponseMessage response = x.Result)
                    {
                        TResult result = GetResult<TResult>(response);
                        return result as TResult;
                    }
                });
            }
            //}
        }

        /// <summary>
        /// Put
        /// </summary>
        /// <typeparam name="TResult">返回数据类型</typeparam>
        /// <param name="url">请求Url</param>
        /// <param name="data">url parameter 数据</param>
        /// <param name="urlParameters">url parameter 数据</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public virtual TResult Put<TResult>(string url, IDictionary<string, string> data, IDictionary<string, string> urlParameters = null, int? timeout = null)
            where TResult : class
        {
            //using (var client = new HttpClient())
            //{
            //    InitHttpClient(timeout, client);

            var client = _httpClient;

            string requestUrl = _baseUrl + url;
            CreateUrlParams(urlParameters, ref requestUrl);

            using (HttpContent content = CreateContent(data))
            {
                using (HttpResponseMessage response = client.PutAsync(requestUrl, content).Result)
                {
                    TResult result = GetResult<TResult>(response);
                    return result as TResult;
                }
            }
            //}
        }

        /// <summary>
        /// Put
        /// </summary>
        /// <typeparam name="TResult">返回数据类型</typeparam>
        /// <param name="url">请求Url</param>
        /// <param name="data">url parameter 数据</param>
        /// <param name="urlParameters">url parameter 数据</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public virtual Task<TResult> PutAsync<TResult>(string url, IDictionary<string, string> data, IDictionary<string, string> urlParameters = null, int? timeout = null)
            where TResult : class
        {
            //using (var client = new HttpClient())
            //{
            //    InitHttpClient(timeout, client);

            var client = _httpClient;

            string requestUrl = _baseUrl + url;
            CreateUrlParams(urlParameters, ref requestUrl);

            using (HttpContent content = new ObjectContent<IDictionary<string, string>>(data, _mediaTypeFormatter))
            {
                var responseTs = client.PostAsync(requestUrl, content);
                return responseTs.ContinueWith(x =>
                {
                    using (HttpResponseMessage response = x.Result)
                    {
                        TResult result = GetResult<TResult>(response);
                        return result as TResult;
                    }
                });
            }
            //}
        }

        /// <summary>
        /// Delete
        /// </summary>
        /// <typeparam name="TResult">返回数据类型</typeparam>
        /// <param name="url">请求Url</param>
        /// <param name="urlParameters">url parameter 数据</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public virtual TResult Delete<TResult>(string url, IDictionary<string, string> urlParameters = null, int? timeout = null)
            where TResult : class
        {
            //using (var client = new HttpClient())
            //{
            //    InitHttpClient(timeout, client);

            var client = _httpClient;

            string requestUrl = _baseUrl + url;
            CreateUrlParams(urlParameters, ref requestUrl);

            using (HttpResponseMessage response = client.DeleteAsync(requestUrl).Result)
            {
                TResult result = GetResult<TResult>(response);
                return result as TResult;
            }
            //}
        }

        /// <summary>
        /// Delete
        /// </summary>
        /// <typeparam name="TResult">返回数据类型</typeparam>
        /// <param name="url">请求Url</param>
        /// <param name="urlParameters">url parameter 数据</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public virtual Task<TResult> DeleteAsync<TResult>(string url, IDictionary<string, string> urlParameters = null, int? timeout = null)
            where TResult : class
        {
            //using (var client = new HttpClient())
            //{
            //    InitHttpClient(timeout, client);

            var client = _httpClient;

            string requestUrl = _baseUrl + url;
            CreateUrlParams(urlParameters, ref requestUrl);

            var responseTs = client.DeleteAsync(requestUrl);
            return responseTs.ContinueWith(x =>
            {
                using (HttpResponseMessage response = x.Result)
                {
                    TResult result = GetResult<TResult>(response);
                    return result as TResult;
                }
            });
            //}
        }

        /// <summary>
        /// 创建参数
        /// </summary>
        /// <param name="urlParameters"></param>
        /// <param name="baseUrl"></param>
        private void CreateUrlParams(IDictionary<string, string> urlParameters, ref string baseUrl)
        {
            StringBuilder buffer = new StringBuilder();
            AddDefaultUrlParameters(ref urlParameters);

            if (urlParameters != null)
            {
                int i = 0;
                foreach (string key in urlParameters.Keys)
                {
                    if (i == 0)
                    {
                        buffer.AppendFormat("{0}={1}", key, urlParameters[key]);
                        i++;
                    }
                    else
                    {
                        buffer.AppendFormat("&{0}={1}", key, urlParameters[key]);
                    }
                }
            }

            int index = baseUrl.IndexOf("?");
            if (index >= 0)
            {
                if (index < baseUrl.Length - 1)
                {
                    baseUrl += "&" + buffer.ToString();
                }
                else
                {
                    baseUrl += buffer.ToString();
                }
            }
            else
            {
                baseUrl += "?" + buffer.ToString();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TData"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        private HttpContent CreateContent<TData>(TData data)
        {
            HttpContent httpContent = null;
            object contentData = data as object;
            RequestContentDataHandler(ref contentData);
            Type type = contentData.GetType();

            var fullName = type.FullName;
            switch (fullName)
            {
                case "System.String":
                    httpContent = new StringContent(contentData.ToString(), defaultEncoding, _mediaType);
                    break;
                case "System.Byte[]":
                    httpContent = new ByteArrayContent(contentData as byte[]);
                    break;
                default:
                    httpContent = new ObjectContent(type, contentData, _mediaTypeFormatter);
                    break;
            }
            return httpContent;

        }

        /// <summary>
        /// 获取Result对象
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="response"></param>
        /// <returns></returns>
        private TResult GetResult<TResult>(HttpResponseMessage response)
            where TResult : class
        {
            TResult result;
            if (response.IsSuccessStatusCode)
            {
                var fullName = typeof(TResult).FullName;
                switch (fullName)
                {
                    case "System.String":
                        result = response.Content.ReadAsStringAsync().Result as TResult;
                        break;
                    case "System.Byte[]":
                        result = response.Content.ReadAsByteArrayAsync().Result as TResult;
                        break;
                    default:
                        result = response.Content.ReadAsAsync<TResult>(_mediaTypeFormatterArray).Result;
                        break;
                }
                return result;
            }
            else
            {
                result = default(TResult);
                ErrorResponseHandler<TResult>(ref result, response);
                return result;
            }
        }

        /// <summary>
        /// 获取Result对象
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="response"></param>
        /// <returns></returns>
        private Task<TResult> GetResultAsync<TResult>(HttpResponseMessage response)
            where TResult : class
        {
            Task<TResult> resultTs;
            TResult result;
            if (response.IsSuccessStatusCode)
            {
                var fullName = typeof(TResult).FullName;
                switch (fullName)
                {
                    case "System.String":
                        resultTs = response.Content.ReadAsStringAsync().ContinueWith(x => x.Result as TResult);
                        break;
                    case "System.Byte[]":
                        resultTs = response.Content.ReadAsByteArrayAsync().ContinueWith(x => x.Result as TResult);
                        break;
                    default:
                        resultTs = response.Content.ReadAsAsync<TResult>(_mediaTypeFormatterArray);
                        break;
                }
                return resultTs;
                //result = await response.Content.ReadAsAsync<TResult>(_mediaTypeFormatterArray);
                //return result;
            }
            else
            {
                result = default(TResult);
                ErrorResponseHandler<TResult>(ref result, response);

                resultTs = new Task<TResult>(() => result);
                return resultTs;
            }
        }

        /// <summary>
        /// 添加默认参数
        /// </summary>
        /// <param name="urlParameters"></param>
        private void AddDefaultUrlParameters(ref IDictionary<string, string> urlParameters)
        {
            //系统参数
            if (urlParameters == null)
            {
                urlParameters = new Dictionary<string, string>();
            }

            //IDictionary<string, string> dp = null;
            //dp = FurnishDefaultParameters();
            DefaultUrlParametersHandler(ref urlParameters);

            //if (dp != null && dp.Count > 0)
            //{
            //    foreach (var item in dp)
            //    {
            //        if (urlParameters.ContainsKey(item.Key)) continue;
            //        urlParameters.Add(item);
            //    }
            //}
        }

        /// <summary>
        /// 请求前，请求header定义
        /// </summary>
        /// <param name="headers"></param>
        protected virtual void DefaultRequestHeadersHandler(HttpRequestHeaders headers)
        {
        }

        /// <summary>
        /// 异常处理
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="result"></param>
        /// <param name="httpResponse"></param>
        protected virtual void ErrorResponseHandler<TResult>(ref TResult result, HttpResponseMessage httpResponse)
            where TResult : class
        {
        }

        /// <summary>
        /// Url默认参数处理
        /// </summary>
        /// <param name="urlParameters"></param>
        protected virtual void DefaultUrlParametersHandler(ref IDictionary<string, string> urlParameters)
        {
        }

        /// <summary>
        /// 请求数据处理
        /// </summary>
        /// <param name="data"></param>
        protected virtual void RequestContentDataHandler(ref object data)
        {
        }

        #region IDispose

        private bool isDisposed = false;

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    if (_httpClient != null)
                    {
                        _httpClient.Dispose();
                    }
                    _httpClient = null;
                }

                _mediaTypeFormatter = null;
                _mediaTypeFormatterArray = null;
            }
            isDisposed = true;
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        ~RpcHttpClient2()
        {
            Dispose(false);
        }
    }
}
