# ICanPay介绍
---

ICanPay是一个提供了多个支付网关支付处理的类库，使用ICanPay可以简化订单的创建、查询跟接收网关返回的支付通知。

ICanPay目前实现了4家支付网关，你可以通过实现抽象基类来支持更多的网关。

目前支持的支付网关有：支付宝(Alipay)、微信支付(Wechatpay)、财付通(Tenpay)、易宝(Yeepay)。


# Package
---

Package  | NuGet 
-------- | :------------ 
ICanPay.Core		| [![NuGet](https://img.shields.io/nuget/v/ICanPay.Core.svg)](https://www.nuget.org/packages/ICanPay.Core)
ICanPay.Alipay		| [![NuGet](https://img.shields.io/nuget/v/ICanPay.Alipay.svg)](https://www.nuget.org/packages/ICanPay.Alipay)
ICanPay.Wechatpay	| [![NuGet](https://img.shields.io/nuget/v/ICanPay.Wechatpay.svg)](https://www.nuget.org/packages/ICanPay.Wechatpay)
ICanPay.Tenpay		| [![NuGet](https://img.shields.io/nuget/v/ICanPay.Tenpay.svg)](https://www.nuget.org/packages/ICanPay.Tenpay)
ICanPay.Yeepay		| [![NuGet](https://img.shields.io/nuget/v/ICanPay.Yeepay.svg)](https://www.nuget.org/packages/ICanPay.Yeepay)

# 如何使用

首先在Startup文件中添加如下方法：

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddICanPay(a =>
            {
				var gateways = new List<GatewayBase>();
				
                // 设置商户数据
                var alipayMerchant = new Alipay.Merchant
                {
                    AppId = "123456",
                    NotifyUrl = "http://localhost:61337/Notify",
                    ReturnUrl = "http://localhost:61337/Return",
                    AlipayPublicKey = "Varorbc",
                    Privatekey = "Varorbc"
                };
				
                var alipayGateway = new AlipayGateway(alipayMerchant)
                {
                    GatewayTradeType = GatewayTradeType.Web
                };

                gateways.Add(alipayGateway);
            });
        }

		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{	
			app.UseICanPay();
		}
    
然后创建支付控制类：

        using ICanPay.Alipay;
        using ICanPay.Core;
        using Microsoft.AspNetCore.Mvc;
        using System.Collections.Generic;

        namespace ICanPay.Demo.Controllers
        {
			public class PaymentController : Controller
			{
				private ICollection<GatewayBase> gatewayList;
				public PaymentController(ICollection<GatewayBase> gatewayList)
				{
					this.gatewayList = gatewayList;
				}

				public IActionResult Index()
				{

					string content = CreateAlipayOrder();

					return Content(content);
				}

				/// <summary>
				/// 创建支付宝的支付订单
				/// </summary>
				private string CreateAlipayOrder()
				{
					Alipay.Order order = new Alipay.Order()
					{
						Amount = 0.01,
						OutTradeNo = "35",
						Subject = "测测看支付宝",
						Body = "1234",
						ExtendParams = new ExtendParam()
						{
							HbFqNum = "3"
						},
						GoodsDetail = new Goods[] {
							new Goods()
							{
								Id = "12"
							}
						}
					};

					var gateway = gatewayList.GetGateway(GatewayType.Alipay);
					gateway.GatewayTradeType = GatewayTradeType.App;

					PaymentSetting paymentSetting = new PaymentSetting(gateway, order);
					return paymentSetting.Payment();
				}	
			}
		}

再创建通知控制类：

        using ICanPay.Alipay;
        using ICanPay.Core;
        using Microsoft.AspNetCore.Mvc;
        using System.Collections.Generic;
        using System.Threading.Tasks;

        namespace ICanPay.Demo.Controllers
        {
			public class NotifyController : Controller
			{
				private ICollection<GatewayBase> gatewayList;
				public NotifyController(ICollection<GatewayBase> gatewayList)
				{
					this.gatewayList = gatewayList;
				}

				public async Task Index()
				{
					// 订阅支付通知事件
					PaymentNotify notify = new PaymentNotify(gatewayList);
					notify.PaymentSucceed += Notify_PaymentSucceed;
					notify.PaymentFailed += Notify_PaymentFailed;
					notify.UnknownGateway += Notify_UnknownGateway;

					// 接收并处理支付通知
					await notify.ReceivedAsync();
				}

				private void Notify_PaymentSucceed(object sender, PaymentSucceedEventArgs e)
				{
					// 支付成功时时的处理代码
					if (e.GatewayType == GatewayType.Alipay)
					{
						var alipayNotify = (Notify)e.Notify;
					}
				}

				private void Notify_PaymentFailed(object sender, PaymentFailedEventArgs e)
				{
					// 支付失败时的处理代码
				}

				private void Notify_UnknownGateway(object sender, UnknownGatewayEventArgs e)
				{
					// 无法识别支付网关时的处理代码
				}
			}
		}

# Wiki
---

支付宝支付文档：

https://docs.open.alipay.com/203/105288/

https://docs.open.alipay.com/204/105051/

https://docs.open.alipay.com/270/105898/

https://docs.open.alipay.com/api_1/alipay.trade.pay

微信支付文档：

https://pay.weixin.qq.com/wiki/doc/api/index.html
