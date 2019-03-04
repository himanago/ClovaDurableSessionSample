# ClovaDurableSessionSample
Durable Functions＋Clovaでの時間のかかる処理の応用例（セッション無限待機）

## 概要
時間のかかる処理をClovaで実行した場合、スキルがタイムアウトしてしまうため、完了まで待つことはできません。
そこでClovaのAudioPlayer機能の応用（Durable FunctionsのGetStatusAsyncの戻り値が完了であることを終了条件とした無音無限ループ）により疑似的な永続セッションをつくり、処理の完了を待機します。

※無音無限ループについては[「ClovaとLINE Botの一味違う２つの連携手法～タイミング同期＆無限セッション～」](https://qiita.com/himarin269/items/675c6619bfb32acfd9c3)の②を参照

## 参考
以下の記事・ソースコードを参考にしています。
こちらは「結果を教えて」というインテントリクエストを繰り返すことによりセッションをキープする手法です。

* blog: [スマートスピーカーで時間のかかる処理を呼び出す方法と実装方法](https://blog.okazuki.jp/entry/2018/09/13/171933)
* repos: [runceel/clova-longtime-process-sample-using-durable-functions](https://github.com/runceel/clova-longtime-process-sample-using-durable-functions)
