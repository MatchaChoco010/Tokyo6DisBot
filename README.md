# Tokyo6DisBot

DiscordでCeVIO AI小樽組によるテキスト読み上げを行うbotプログラムです。

## 動作環境

.NET、CsVIO AI、そして小春六花トークボイス、夏色花梨トークボイスのいずれかのインストールされたPCが必要です。 CeVIO AIがインストールされたPC上でBotプログラムを走らせます。

## 導入方法

### Botの作成

まずはBotを作成します。

DiscordのDeveloper Portal(https://discord.com/developers/applications)にアクセスし、「New Application」を選択します。

![screenshot](screenshot/screenshot-0.png)

名前をつけて「Create」します。

![screenshot](screenshot/screenshot-1.png)

アイコンと名前を設定します。

![screenshot](screenshot/screenshot-2.png)
![screenshot](screenshot/screenshot-3.png)

「Bot」から「Add Bot」を選択し「Yes」を選択します。

![screenshot](screenshot/screenshot-4.png)
![screenshot](screenshot/screenshot-5.png)

「Public Bot」のチェックを外しておきます。

![screenshot](screenshot/screenshot-6.png)

### Botをサーバーに追加する

「OAuth2」を選択し「URL Generator」を選択します。

![screenshot](screenshot/screenshot-7.png)

SCOPESを「bot」として、BOT PERMISSIONSに「Send Messages」「Connect」「Speak」を選択し、ページ下部のURLをコピーします。

![screenshot](screenshot/screenshot-8.png)
![screenshot](screenshot/screenshot-9.png)

コピーしたURLへブラウザでアクセスし、サーバーを選んでjoinさせます。

![screenshot](screenshot/screenshot-10.png)
![screenshot](screenshot/screenshot-11.png)

### tokenを取得する

「Bot」から「Reset Token」を選択します。

![screenshot](screenshot/screenshot-12.png)

Tokenを「Copy」します。

![screenshot](screenshot/screenshot-12.png)

「karin-token.txt」というファイルをREADME.mdと同じディレクトリに作成し、Tokenの内容をペーストします。

同様にして、Botをもう一つ作成し「rikka-token.txt」というファイルも作成します。

### .NETをインストールする

.NETがインストールされていない場合、.NET Runtimeを次のページからダウンロードし、インストールします。

https://dotnet.microsoft.com/ja-jp/download

### Botを起動する

README.mdと同じディレクトリで次のコマンドを実行します。

```
$ dotnet run
```

プログラムが起動し、CeVIO AIが起動してBotがアクティブになります。

各Botのコマンドについては次のコマンドをDiscordで入力して確認してください。

```
rikka-chan help
```

```
karin-senpai help
```
