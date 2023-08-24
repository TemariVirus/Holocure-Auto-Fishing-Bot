# HoloCure Auto Fishing Bot

I decided to make this bot after getting fed up with not even encountering a shiny in my first few thousand catches.
I've also seen some comments about people having bad luck like me or just being not so good at the game, so hopefully this bot will help them as well.
Currently the bot is practically 100% accurate, and uses very little CPU resources.

## How to use / 使い方

1. Download Holocure.Auto.Fishing.Bot.zip from the [releases](https://github.com/Zemogus/Holocure-Auto-Fishing-Bot/releases/latest) page. / [リリース](https://github.com/Zemogus/Holocure-Auto-Fishing-Bot/releases/latest)から Holocure.Auto.Fishing.Bot.zip をダウンロードしてください。
2. Unzip the downloaded file / ダウンロードしたファイルを解凍してください。
3. Run HoloCure. / HoloCure を起動してください。
4. Stand next to the pond so that you can fish. / 池のすぐそばに立ってください。
5. Run the executable. / 実行ファイルを起動してください。
6. If you get a warning from Windows Defender, click on "More info" and then "Run anyway". / Windows Defender から警告が出たら、「詳細情報」をクリックしてから「実行」をクリックしてください。
7. Happy fishing! / 楽しい釣りを！

## FAQ

### The bot only presses ENTER and misses everything. / ボットは ENTER キーしか押さず、何も釣れません。

This is likely a problem with DirectX hardware acceleration. As a workaround, open settings, and navigate to System > Display > Graphics. Then, disable "Optimizations for windowed games", "Variable refresh rate" and "Auto HDR". I'm currently working on a fix for this. ([More information about "Optimizations for windowed games"](https://support.microsoft.com/en-us/windows/optimizations-for-windowed-games-in-windows-11-3f006843-2c7e-4ed0-9a5e-f9389e535952))

これは DirectX のハードウェアアクセラレーションに関する問題はず。回避策として、設定で、システム > 表示 > グラフィックス > 既定のグラフィックス設定を変更する を選択してください。そして、「ウィンドウゲームの最適化」、「可変リフレッシュレート」、「自動 HDR」をオフにしてください。現在は、修正に取り組んでいます。お待ちしてくれたら幸いです。（[「ウィンドウゲームの最適化」の詳細情報](https://support.microsoft.com/ja-jp/windows/windows-11でのウィンドウ-ゲームの最適化-3f006843-2c7e-4ed0-9a5e-f9389e535952)）

## Dependencies

This project only requires the .NET 4.8 runtime, which has come shipped with Windows since... a long time ago. If you don't have it for some reason, you can download it [here](https://dotnet.microsoft.com/en-us/download/dotnet-framework/thank-you/net48-web-installer). If HoloCure becomes cross-platform I'll look into possibly changing the .NET version so that the bot is cross-platform as well.

このプロジェクトは .NET 4.8 ランタイムのみを必要とします。.NET 4.8 ランタイムはかなり前から Windows に付属しています。もし何らかの理由でインストールされていない場合は、[ここ](https://dotnet.microsoft.com/en-us/download/dotnet-framework/thank-you/net48-web-installer)からダウンロードできます。もし HoloCure がクロスプラットフォームになったら、ボットもクロスプラットフォームにするかもしれません。
