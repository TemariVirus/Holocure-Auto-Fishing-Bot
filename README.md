# HoloCure Auto Fishing Bot

I decided to make this bot after getting fed up with not even encountering a shiny in my first few thousand catches.
I've also seen some comments about people having bad luck like me or just being not so good at the game, so hopefully this bot will help them as well.
Currently the bot is practically 100% accurate, and uses very little CPU resources.

## How to use / 使い方

1. Download Holocure.Auto.Fishing.Bot.zip from the [releases](https://github.com/Zemogus/Holocure-Auto-Fishing-Bot/releases/latest) page. / [リリース](https://github.com/Zemogus/Holocure-Auto-Fishing-Bot/releases/latest)から Holocure.Auto.Fishing.Bot.zip をダウンロードしてください。
2. Unzip the downloaded file / ダウンロードしたファイルを解凍してください。
3. Run HoloCure. / HoloCure を起動してください。
4. Stand next to the pond so that you can fish. / 池のすぐそばに立ってください。
5. Double click the "Holocure Auto Fishing Bot" file to run the executable. / 「Holocure Auto Fishing Bot」を二回クリックして実行ファイルを起動してください。
7. If you get a warning from Windows Defender, click on "More info" and then "Run anyway". / Windows Defender から警告が出たら、「詳細情報」をクリックしてから「実行」をクリックしてください。
8. Happy fishing! / 楽しい釣りを！

## Dependencies

This project only requires the .NET 4.8 runtime, which has come shipped with Windows since... a long time ago. If you don't have it for some reason, you can download it [here](https://dotnet.microsoft.com/en-us/download/dotnet-framework/thank-you/net48-web-installer). If HoloCure becomes cross-platform I'll look into possibly changing the .NET version so that the bot is cross-platform as well.

このプロジェクトは .NET 4.8 ランタイムのみを必要とします。.NET 4.8 ランタイムはかなり前から Windows に付属しています。もし何らかの理由でインストールされていない場合は、[ここ](https://dotnet.microsoft.com/ja-jp/download/dotnet-framework/thank-you/net48-web-installer)からダウンロードできます。もし HoloCure がクロスプラットフォームになったら、ボットもクロスプラットフォームにするかもしれません。

## FAQ

### The bot is hitting notes, but gets a lot of 'ok's and 'bad's

Go to the bot's terminal window and press 'c'. This should open the config dialogue. If the bot is hitting notes too late, you'll want to decrease the offsets. Likewise, if the bot is hitting notes too early, you'll want to increase the offsets. You can only use whole numbers (positive or negative) for the offsets.

If you don't have the bot running, you can also edit the config in `config.txt` and then start up the bot.

### Will there be a mining bot?

I have no plans to make one currently. The mining minigame is a lot less dependent on luck, and from my own experience you should get the hololite prism in around 3 hours or less.
