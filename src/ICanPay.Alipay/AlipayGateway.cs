﻿using ICanPay.Core;
using System.Threading.Tasks;

namespace ICanPay.Alipay
{
    /// <summary>
    /// 支付宝网关
    /// </summary>
    public sealed class AlipayGateway : GatewayBase, IPaymentForm, IPaymentUrl, IPaymentApp
    {

        #region 私有字段

        private Merchant merchant;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化支付宝网关
        /// </summary>
        /// <param name="merchant">商户数据</param>
        public AlipayGateway(Merchant merchant)
            : base(merchant)
        {
            this.merchant = merchant;
        }

        #endregion

        #region 属性

        public override GatewayType GatewayType => GatewayType.Alipay;

        public override string GatewayUrl { get; set; } = "https://openapi.alipay.com/gateway.do";

        public new Merchant Merchant => merchant;

        public new Order Order => (Order)base.Order;

        public new Notify Notify => (Notify)base.Notify;

        #endregion

        #region 方法

        public string BuildPaymentForm()
        {
            InitOrderParameter();

            return GatewayData.ToForm(GatewayUrl, Merchant.Charset);
        }

        public string BuildPaymentUrl()
        {
            InitOrderParameter();

            return $"{GatewayUrl}?{GetPaymentQueryString()}";
        }

        public string BuildPaymentApp()
        {
            InitOrderParameter();

            return GetPaymentQueryString();
        }

        protected override async Task<bool> CheckNotifyDataAsync()
        {
            ReadNotify<Notify>();
            if (await IsSuccessResultAsync())
            {
                return true;
            }

            return false;
        }

        protected override void SupplementaryAppParameter()
        {
            Merchant.Method = Constant.APP;
            Order.ProductCode = Constant.QUICK_MSECURITY_PAY;
        }

        protected override void SupplementaryWebParameter()
        {
            if (!string.IsNullOrEmpty(Merchant.ReturnUrl))
            {
                GatewayData.Add(Constant.RETURN_URL, Merchant.ReturnUrl);
            }

            Merchant.Method = Constant.WEB;
            Order.ProductCode = Constant.FAST_INSTANT_TRADE_PAY;
        }

        protected override void SupplementaryWapParameter()
        {
            if (!string.IsNullOrEmpty(Merchant.ReturnUrl))
            {
                GatewayData.Add(Constant.RETURN_URL, Merchant.ReturnUrl);
            }

            Merchant.Method = Constant.WAP;
            Order.ProductCode = Constant.QUICK_WAP_WAY;
        }

        protected override void SupplementaryScanParameter()
        {
            if (!string.IsNullOrEmpty(Merchant.AppAuthToken))
            {
                GatewayData.Add(Constant.APP_AUTH_TOKEN, Merchant.AppAuthToken);
            }

            Merchant.Method = Constant.SCAN;
            Order.ProductCode = Constant.FACE_TO_FACE_PAYMENT;
        }

        /// <summary>
        /// 初始化订单参数
        /// </summary>
        protected override void InitOrderParameter()
        {
            base.InitOrderParameter();
            Merchant.BizContent = Util.SerializeObject(Order);
            GatewayData.Add(Constant.APP_ID, Merchant.AppId);
            GatewayData.Add(Constant.METHOD, Merchant.Method);
            GatewayData.Add(Constant.FORMAT, Merchant.Format);
            GatewayData.Add(Constant.CHARSET, Merchant.Charset);
            GatewayData.Add(Constant.SIGN_TYPE, Merchant.SignType);
            GatewayData.Add(Constant.TIMESTAMP, Merchant.Timestamp.ToString(TIME_FORMAT));
            GatewayData.Add(Constant.VERSION, Merchant.Version);
            GatewayData.Add(Constant.NOTIFY_URL, Merchant.NotifyUrl);
            GatewayData.Add(Constant.BIZ_CONTENT, Merchant.BizContent);
            Merchant.Sign = EncryptUtil.RSA2(GatewayData.ToUrl(), Merchant.Privatekey);
            GatewayData.Add(Constant.SIGN, Merchant.Sign);
        }

        private string GetPaymentQueryString()
        {
            return GatewayData.ToUrlEncode();
        }

        /// <summary>
        /// 是否是已成功支付的支付通知
        /// </summary>
        /// <returns></returns>
        private async Task<bool> IsSuccessResultAsync()
        {
            if (ValidateNotifyParameter() && ValidateNotifySign() && await ValidateNotifyIdAsync())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查支付通知，是否支付成功，签名是否正确。
        /// </summary>
        /// <returns></returns>
        private bool ValidateNotifyParameter()
        {
            // 支付状态是否为成功。
            if (Notify.TradeStatus == Constant.TRADE_SUCCESS)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 验证支付宝通知的签名
        /// </summary>
        private bool ValidateNotifySign()
        {
            Merchant.Sign = EncryptUtil.RSA2(GatewayData.ToUrl(Constant.SIGN, Constant.SIGN_TYPE), Merchant.Privatekey);
            // 验证通知的签名
            if (Notify.Sign == Merchant.Sign)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 验证网关的通知Id是否有效
        /// </summary>
        private bool ValidateNotifyId()
        {
            string data = HttpUtil.ReadPage(GetValidateNotifyUrl());
            GatewayData.FromXml(data);
            // 服务器异步通知的通知Id则会在输出标志成功接收到通知的success字符串后失效。
            if (GatewayData.GetStringValue("is_success") == "T")
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 异步验证网关的通知Id是否有效
        /// </summary>
        private async Task<bool> ValidateNotifyIdAsync()
        {
            return await Task.Run(() => ValidateNotifyId());
        }

        /// <summary>
        /// 获得验证支付宝通知的Url
        /// </summary>
        private string GetValidateNotifyUrl()
        {
            return $"{GatewayUrl}?service=notify_verify&partner={Merchant.AppId}&notify_id={Notify.NotifyId}";
        }

        #endregion

    }
}