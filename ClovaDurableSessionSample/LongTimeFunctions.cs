using CEK.CSharp;
using CEK.CSharp.Models;
using DurableTask.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
            var clovaClient = new ClovaClient();
            var cekRequest = await clovaClient.GetRequest(req.Headers["SignatureCEK"], req.Body);
            var userId = cekRequest.Session.User.UserId;

            switch (cekRequest.Request.Type)
            {
                case RequestType.LaunchRequest:
                    {
                        // sessionId をインスタンス ID として新しい関数を実行
                        await client.StartNewAsync(nameof(LongTimeOrchestrationFunction), userId, null);
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
                case RequestType.EventRequest:
                    {
                        // オーディオイベントの制御
                        // Clovaでのオーディオ再生が終わった際に呼び出される
                        if (cekRequest.Request.Event.Namespace == "AudioPlayer")
                        {
                            if (cekRequest.Request.Event.Name == "PlayFinished")
                            {
                                var status = await client.GetStatusAsync(userId);

                                log.LogInformation(status.RuntimeStatus.ToString());

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
                                else if (status.RuntimeStatus == OrchestrationRuntimeStatus.Failed)
                                {
                                    // 失敗していたら結果をしゃべって終了
                                    cekResponse.AddText("失敗しました。");
                                }
                            }
                            else if (cekRequest.Request.Event.Name == "PlayStopped")
                            {
                                await client.TerminateAsync(userId, "Cancel");
                            }
                        }
                        break;
                    }
                case RequestType.SessionEndedRequest:
                    {
                        // スキル終了の場合は処理もキャンセル
                        await client.TerminateAsync(userId, "Cancel");
                        break;
                    }
            }

            return new OkObjectResult(cekResponse);
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
                Payload = new AudioPlayPayload
                {
                    AudioItem = new AudioItem
                    {
                        AudioItemId = "silent-audio",
                        TitleText = "Durable Session Sample",
                        TitleSubText1 = "Azure Functions",
                        TitleSubText2 = "Durable Functions",
                        Stream = new AudioStreamInfoObject
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
                    PlayBehavior = AudioPlayBehavior.REPLACE_ALL,
                    Source = new Source
                    {
                        Name = "Microsoft Azure"
                    }
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
            // 時間のかかる処理（20秒待つだけ）
            var time = 20000;
            await Task.Delay(time);
            return $"{(time / 1000).ToString()}秒待機成功";
        }

        /// <summary>
        /// 実行履歴を削除するタイマー関数。1日1回、午前12時に実行されます。
        /// </summary>
        /// <param name="client"></param>
        /// <param name="myTimer"></param>
        /// <returns></returns>
        [FunctionName(nameof(HistoryCleanerFunction))]
        public static Task HistoryCleanerFunction(
            [OrchestrationClient] DurableOrchestrationClient client,
            [TimerTrigger("0 0 12 * * *")]TimerInfo myTimer)
        {
            return client.PurgeInstanceHistoryAsync(
                DateTime.MinValue,
                DateTime.UtcNow.AddDays(-1),
                new List<OrchestrationStatus>
                {
                    OrchestrationStatus.Completed
                });
        }
    }
}
