using CEK.CSharp;
using CEK.CSharp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace ClovaDurableSessionSample
{
    public static class LongTimeFunctions
    {
        [FunctionName(nameof(LongTimeFunction))]
        public static async Task<IActionResult> LongTimeFunction(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequest req,
            [OrchestrationClient] DurableOrchestrationClient client,
            ExecutionContext context,
            ILogger log)
        {
            var cekResponse = new CEKResponse();

            // オーディオ系イベントではリクエスト形式が異なるのでJSONデータを直接確認する
            var reqJson = JObject.Parse(await req.ReadAsStringAsync());
            var reqObj = reqJson["request"];
            if (reqObj["type"].Value<string>() != "EventRequest")
            {
                var clovaClient = new ClovaClient();
                var cekRequest = await clovaClient.GetRequest(req.Headers["SignatureCEK"], req.Body);
                switch (cekRequest.Request.Type)
                {
                    case RequestType.LaunchRequest:
                        {
                            // UserId をインスタンス ID として新しい関数を実行
                            await client.StartNewAsync(nameof(LongTimeOrchestrationFunction), cekRequest.Session.User.UserId, null);
                            cekResponse.AddText("時間のかかる処理を実行しました。少々お待ちください。");

                            // 無音無限ループに入る
                            KeepClovaWaiting(cekResponse);
                            break;
                        }
                    case RequestType.IntentRequest:
                        {
                            // インテントリクエストは特に用意しない
                            cekResponse.AddText("すみません。よくわかりませんでした。");
                            break;
                        }
                    case RequestType.SessionEndedRequest:
                        {
                            // スキル終了の場合は処理もキャンセル
                            await client.TerminateAsync(cekRequest.Session.User.UserId, "Cancel");
                            break;
                        }
                }
            }
            else
            {
                // オーディオイベントの制御
                // Clovaでのオーディオ再生が終わった際に呼び出される
                if (reqObj["event"]["namespace"].Value<string>() == "AudioPlayer")
                {
                    var userId = reqJson["session"]["user"]["userId"].Value<string>();
                    var eventName = reqObj["event"]["name"].Value<string>();

                    if (eventName == "PlayFinished")
                    {
                        var status = await client.GetStatusAsync(userId);

                        // 終わっていなければ無音再生リクエストを繰り返す
                        if (status.RuntimeStatus == OrchestrationRuntimeStatus.ContinuedAsNew ||
                            status.RuntimeStatus == OrchestrationRuntimeStatus.Pending ||
                            status.RuntimeStatus == OrchestrationRuntimeStatus.Running)
                        {
                            KeepClovaWaiting(cekResponse);
                            cekResponse.ShouldEndSession = false;
                        }
                        else if (status.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
                        {
                            // 終了していたら結果をしゃべって終了
                            cekResponse.AddText($"終わりました。結果は{status.Output.ToObject<string>()}です。");
                        }
                    }
                    else if (eventName == "PlayStopped")
                    {
                        await client.TerminateAsync(userId, "Cancel");
                    }
                }
            }

            // AudioPlayer利用時はCEK.CSharpのJSON変換がうまくいかないので自前で変換して返す
            return new OkObjectResult(JsonConvert.SerializeObject(cekResponse));
        }

        /// <summary>
        /// Clovaを無音再生で待機させます。
        /// </summary>
        /// <param name="cekResponse"></param>
        private static void KeepClovaWaiting(CEKResponse cekResponse)
        {
            // 無音mp3の再生指示
            cekResponse.Response.Directives.Add(new Directive()
            {
                Header = new DirectiveHeader()
                {
                    Namespace = DirectiveHeaderNamespace.AudioPlayer,
                    Name = DirectiveHeaderName.Play
                },
                // CEK.CSharpのModelは対応していないため、自前で用意したModelクラスを使用
                Payload = new AudioPayload
                {
                    AudioItem = new AudioItem
                    {
                        AudioItemId = "silent-audio",
                        Title = "待機中",
                        Artist = "Durable Functions",
                        Stream = new Stream
                        {
                            BeginAtInMilliseconds = 0,
                            ProgressReport = new ProgressReport
                            {
                                ProgressReportDelayInMilliseconds = null,
                                ProgressReportIntervalInMilliseconds = null,
                                ProgressReportPositionInMilliseconds = null
                            },
                            Url = Consts.SilentAudioFileUri,
                            UrlPlayable = true
                        }
                    },
                    PlayBehavior = "REPLACE_ALL"
                }
            });
        }

        [FunctionName(nameof(LongTimeOrchestrationFunction))]
        public static async Task<string> LongTimeOrchestrationFunction(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            // 時間のかかる処理を１つ呼び出す
            return await context.CallActivityAsync<string>(nameof(LongTimeActivityFunction), null);
        }

        [FunctionName(nameof(LongTimeActivityFunction))]
        public static async Task<string> LongTimeActivityFunction(
            [ActivityTrigger] DurableActivityContext context)
        {
            // 時間のかかる処理（60秒待つだけ）
            var time = 60000;
            await Task.Delay(time);
            return $"{(time / 1000).ToString()}秒待機成功";
        }
    }
}
