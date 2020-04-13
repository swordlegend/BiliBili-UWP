﻿using BiliBili_Lib.Enums;
using BiliBili_Lib.Models.BiliBili;
using BiliBili_Lib.Models.Others;
using BiliBili_Lib.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace BiliBili_Lib.Service
{
    public class AccountService
    {
        public string _accessToken;
        private string _refreshToken;
        private string _userId;
        public int _expiry;
        public Me Me;
        public AccountService(TokenPackage p)
        {
            InitToken(p);
        }
        public event EventHandler<TokenPackage> TokenChanged;
        private void InitToken(TokenPackage p)
        {
            BiliTool._accessToken = _accessToken = p.AccessToken;
            _refreshToken = p.RefreshToken;
            _userId = p.UserId;
            _expiry = Convert.ToInt32(p.Expiry);
        }
        /// <summary>
        /// 获取验证码
        /// </summary>
        /// <returns></returns>
        public async Task<BitmapImage> GetCaptchaAsync()
        {
            var stream = await BiliTool.GetStreamFromWebAsync($"{Api.PASSPORT_CAPTCHA}?ts=${AppTool.GetNowSeconds()}");
            if (stream != null)
            {
                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
                return bitmap;
            }
            return new BitmapImage(new Uri("ms-appx:///Assets/captcha_refresh.png"));
        }
        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="captcha">验证码</param>
        /// <returns></returns>
        public async Task<LoginCallback> LoginAsync(string userName, string password, string captcha = "")
        {
            string param = $"appkey={BiliTool.AndroidKey.Appkey}&build={BiliTool.BuildNumber}&mobi_app=android&password={Uri.EscapeDataString(await EncryptedPasswordAsync(password))}&platform=android&ts={AppTool.GetNowSeconds()}&username={Uri.EscapeDataString(userName)}";
            if (!string.IsNullOrEmpty(captcha))
            {
                param += "&captcha=" + captcha;
            }
            param += "&sign=" + BiliTool.GetSign(param);
            string response = await BiliTool.PostContentToWebAsync(Api.PASSPORT_LOGIN, param);
            var result = new LoginCallback();
            result.Status = LoginResultType.Error;
            if (!string.IsNullOrEmpty(response))
            {
                var jobj = JObject.Parse(response);
                int code = Convert.ToInt32(jobj["code"]);
                if (code == 0)
                {
                    var data = JsonConvert.DeserializeObject<LoginResult>(jobj["data"].ToString());
                    var package = new TokenPackage(data.token_info.access_token, data.token_info.refresh_token, data.token_info.mid.ToString(), AppTool.DateToTimeStamp(DateTime.Now.AddSeconds(data.token_info.expires_in)));
                    InitToken(package);
                    TokenChanged?.Invoke(this, package);
                    result.Status = LoginResultType.Success;
                    await SSO();
                }
                else if (code == -2100)
                {
                    result.Status = LoginResultType.NeedValidate;
                    result.Url = jobj["url"].ToString();
                }
                else if (code == -105)
                    result.Status = LoginResultType.NeedCaptcha;
                else if (code == -449)
                    result.Status = LoginResultType.Busy;
                else
                    result.Status = LoginResultType.Fail;
            }
            return result;
        }
        /// <summary>
        /// 加密密码
        /// </summary>
        /// <param name="password">密码</param>
        /// <returns></returns>
        private async Task<string> EncryptedPasswordAsync(string password)
        {
            string base64String;
            try
            {
                string param = BiliTool.UrlContact("").TrimStart('?');
                string content = await BiliTool.PostContentToWebAsync(Api.PASSPORT_KEY_ENCRYPT, param);
                JObject jobj = JObject.Parse(content);
                string str = jobj["data"]["hash"].ToString();
                string str1 = jobj["data"]["key"].ToString();
                string str2 = string.Concat(str, password);
                string str3 = Regex.Match(str1, "BEGIN PUBLIC KEY-----(?<key>[\\s\\S]+)-----END PUBLIC KEY").Groups["key"].Value.Trim();
                byte[] numArray = Convert.FromBase64String(str3);
                AsymmetricKeyAlgorithmProvider asymmetricKeyAlgorithmProvider = AsymmetricKeyAlgorithmProvider.OpenAlgorithm(AsymmetricAlgorithmNames.RsaPkcs1);
                CryptographicKey cryptographicKey = asymmetricKeyAlgorithmProvider.ImportPublicKey(WindowsRuntimeBufferExtensions.AsBuffer(numArray), 0);
                IBuffer buffer = CryptographicEngine.Encrypt(cryptographicKey, WindowsRuntimeBufferExtensions.AsBuffer(Encoding.UTF8.GetBytes(str2)), null);
                base64String = Convert.ToBase64String(WindowsRuntimeBufferExtensions.ToArray(buffer));
            }
            catch (Exception)
            {
                base64String = password;
            }
            return base64String;
        }
        /// <summary>
        /// 登陆成功后设置令牌状态
        /// </summary>
        /// <param name="accessToken">令牌</param>
        /// <param name="mid">用户ID</param>
        /// <returns></returns>
        public async Task<bool> SetLoginStatusAsync(string accessToken, string mid, string refreshToken = "", int expiry = 0)
        {
            try
            {
                string refe = string.IsNullOrEmpty(refreshToken) ? accessToken : refreshToken;
                var package = new TokenPackage(accessToken, refe, mid, AppTool.DateToTimeStamp(DateTime.Now.AddSeconds(expiry == 0 ? 7200 : expiry)));
                InitToken(package);
                TokenChanged?.Invoke(this, package);
                await SSO();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        /// <summary>
        /// 刷新令牌
        /// </summary>
        /// <returns></returns>
        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                var param = $"access_token={_accessToken}&refresh_token={_refreshToken}&appkey={BiliTool.AndroidKey.Appkey}&ts={AppTool.GetNowSeconds()}";
                param += "&sign=" + BiliTool.GetSign(param);
                var content = await BiliTool.PostContentToWebAsync(Api.PASSPORT_REFRESH_TOKEN, param);
                var obj = JObject.Parse(content);
                if (Convert.ToInt32(obj["code"]) == 0)
                {
                    var data = JsonConvert.DeserializeObject<TokenInfo>(obj["data"].ToString());
                    var package = new TokenPackage(data.access_token, data.refresh_token, data.mid.ToString(), AppTool.DateToTimeStamp(DateTime.Now.AddSeconds(data.expires_in)));
                    InitToken(package);
                    TokenChanged?.Invoke(this, package);
                    await SSO();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        /// <summary>
        /// 检查令牌状态
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CheckTokenStatusAsync()
        {
            try
            {
                var param = new Dictionary<string, string>();
                param.Add("access_token", _accessToken);
                var url = BiliTool.UrlContact(Api.PASSPORT_CHECK_TOKEN, param);
                var content = await BiliTool.GetTextFromWebAsync(url, true);
                var obj = JObject.Parse(content);
                if (Convert.ToInt32(obj["code"]) == 0)
                {
                    return true;
                }
            }
            catch (Exception)
            {
            }
            return false;
        }
        /// <summary>
        /// Cookie转化处理
        /// </summary>
        /// <returns></returns>
        public async Task SSO()
        {
            try
            {
                var url = BiliTool.UrlContact(Api.PASSPORT_SSO, hasAccessKey: true);
                await BiliTool.GetTextFromWebAsync(url, true);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 获取我的个人信息
        /// </summary>
        /// <returns></returns>
        public async Task<Me> GetMeAsync()
        {
            var url = BiliTool.UrlContact(Api.ACCOUNT_MINE, hasAccessKey: true);
            var data = await BiliTool.ConvertEntityFromWebAsync<Me>(url);
            Me = data;
            return data;
        }
        /// <summary>
        /// 关注用户
        /// </summary>
        /// <param name="uid">用户ID</param>
        /// <returns></returns>
        public async Task<bool> FollowUser(int uid)
        {
            var param = new Dictionary<string, string>();
            param.Add("uid", Me.mid.ToString());
            param.Add("follow", uid.ToString());
            var data = BiliTool.UrlContact("", param, true);
            string response = await BiliTool.PostContentToWebAsync(Api.ACCOUNT_FOLLOW_USER, data);
            if (!string.IsNullOrEmpty(response))
            {
                var jobj = JObject.Parse(response);
                return jobj["code"].ToString() == "0";
            }
            return false;
        }
        /// <summary>
        /// 取消关注用户
        /// </summary>
        /// <param name="uid">用户ID</param>
        /// <returns></returns>
        public async Task<bool> UnfollowUser(int uid)
        {
            var param = new Dictionary<string, string>();
            param.Add("uid", Me.mid.ToString());
            param.Add("follow", uid.ToString());
            var data = BiliTool.UrlContact("", param, true);
            string response = await BiliTool.PostContentToWebAsync(Api.ACCOUNT_UNFOLLOW_USER, data);
            if (!string.IsNullOrEmpty(response))
            {
                var jobj = JObject.Parse(response);
                return jobj["code"].ToString() == "0";
            }
            return false;
        }
    }
}
