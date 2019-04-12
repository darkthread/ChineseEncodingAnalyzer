# 中文編碼解析工具

處理中文編碼問題時的方便小工具，原本寫成 Windows Form，從 2006 年開始發展，已有超過十年歷史。現在改寫成 Web 方式並開源囉~ 

## 發展沿革 ##

* 2006-12-17 [KB-Unicode編碼解析小工具](https://blog.darkthread.net/blog/kb-unicode)
* 2007-01-03 [中文編碼解析工具1.1版](https://blog.darkthread.net/blog/1-1)
* 2007-09-07 [中文編碼解析工具 Ver 1.2](https://blog.darkthread.net/blog/1040)
* 2007-09-16 [中文編碼解析工具 Ver 1.3](https://blog.darkthread.net/blog/1083/)
* 2019-04-13 移植為 Vue.js + TypeScript + ASP.NET Core 版本並開源

## 程式說明 ##

網站使用 ASP.NET Core with Vue SPA 專案範本為基礎修改(參考：[Vue筆記4-Vue.js + TypeScript + ASP.NET Core](https://blog.darkthread.net/blog/vue-notes-4/))。
遇到的首要困難是原本 WinForm 版使用 System.Web.HttpUtility 處理 UrlEncode 及 UrlDecode，
在 .NET Core 已不再建議使用，故需[尋找替代程式庫](https://blog.darkthread.net/blog/urlencode-in-dotnet/)，
但評估過 System.Net.WebUtility、System.Web.Encodings.Web.UrlEncoder、System.Uri.EscapeDataString() 所提供方法多已回歸當代標準，
統一使用 UTF8 編碼，不像 HttpUtility.UrlEncode() 可指定 Encoding 參數，亦不支援 %uxxxx 這種 UrlEncodeUnicode() 過時編碼。
為保有過往規格，我選擇了可恥但有用的做法 -
參考 [HttpUtility 原始碼](https://referencesource.microsoft.com/#System.Web/httpserverutility.cs)，將其中的邏輯搬進專案，復刻成同名元件。

無論如何，算是把原本 WinForm 版的相同邏輯移植成網頁版，並且提供了線上版本：[https://www.darkthread.net/cea](https://www.darkthread.net/cea])，歡迎大家利用。