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

1. Try running the bot as administrator. If that doesn't work, proceed to step 2.
2. Open settings, and navigate to System > Display > Graphics.
3. Disable the following (if the setting can be found):<br/>
   a. Optimizations for windowed games<br/>
   b. Variable refresh rate<br/>
   c. Auto HDR<br/>
4. Restart Holocure and try again. If that doesn't work, proceed to step 5.
5. Download [another version](https://github.com/Zemogus/Holocure-Auto-Fishing-Bot/releases/tag/1.1.0-directx) of the bot. Note that this version requires the HoloCure window to always be on top.

[]()

1. ボットを管理者として実行してみてください。無効の場合は、ステップ２に進んでください。
2. 設定で、システム > 表示 > グラフィックス > 既定のグラフィックス設定を変更する を選択してください。
3. 以下の設定を出来る限りオフにしてください:<br/>
   a. ウィンドウゲームの最適化<br/>
   b. 可変リフレッシュレート<br/>
   c. 自動 HDR<br/>
4. HoloCure を再起動して、もう一度試してみてください。無効の場合は、ステップ５に進んでください
5. [別のバージョン](https://github.com/Zemogus/Holocure-Auto-Fishing-Bot/releases/tag/1.1.0-directx)のボットをダウンロードしてください。このバージョンは HoloCure のウィンドウが被られたら働けないので、注意してください。

## Dependencies

This project only requires the .NET 4.8 runtime, which has come shipped with Windows since... a long time ago. If you don't have it for some reason, you can download it [here](https://dotnet.microsoft.com/en-us/download/dotnet-framework/thank-you/net48-web-installer). If HoloCure becomes cross-platform I'll look into possibly changing the .NET version so that the bot is cross-platform as well.

このプロジェクトは .NET 4.8 ランタイムのみを必要とします。.NET 4.8 ランタイムはかなり前から Windows に付属しています。もし何らかの理由でインストールされていない場合は、[ここ](https://dotnet.microsoft.com/ja-jp/download/dotnet-framework/thank-you/net48-web-installer)からダウンロードできます。もし HoloCure がクロスプラットフォームになったら、ボットもクロスプラットフォームにするかもしれません。
