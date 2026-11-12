# 约局 SportMate

奥克兰球类约局网站。原型里的活动列表、报名、找场馆、发布活动和创建俱乐部都已经接上。

- 前端：TypeScript、React、Vite
- 后端：C#、ASP.NET Core
- 数据库：Docker 里的 PostgreSQL
- 支付：配置了 Stripe 密钥就走 Stripe Checkout 和 Connect；没配置就用演示支付，只记在数据库里，不会扣款

## 启动

先打开 Docker Desktop，然后在项目根目录：

```powershell
docker compose up -d
dotnet run --project backend/SportMate.Api --launch-profile http
```

另开一个终端：

```powershell
npm install --prefix frontend
npm run dev --prefix frontend
```

浏览器打开 http://localhost:5173 。API 在 http://localhost:5080 。

第一次启动会自动建表并写入奥克兰的演示数据。

## 演示账号

打开「我的」可以切换：

- 林可：用来报名，水平 L3
- 小郑：Epsom 周末羽球会的组织者
- Nick：水平 L6，适合试候补
- Bella：已经在周日畅打名单里

默认进入的是林可。

## 接上 Stripe

启动 API 前设置环境变量：

```text
Stripe__SecretKey=sk_test_...
Stripe__WebhookSecret=whsec_...
Stripe__PublishableKey=pk_test_...
FrontendUrl=http://localhost:5173
```

俱乐部页的「去连接」会打开 Stripe Express 开户。报名会跳到 Stripe Checkout。本地收不到 webhook 时，支付成功跳回网站也会向 API 确认订单。

清空数据库：

```powershell
docker compose down -v
```
