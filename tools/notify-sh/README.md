# winnotify client scripts

[WinNotifier](https://github.com/yuuan/WinNotifier) の Mac/Linux 向けクライアントスクリプトです。

- **winnotify** — WinNotifier へ通知を送信するコマンド
- **winnotify-send** — `notify-send` 互換のラッパー (内部で winnotify を使用)

## Install

ワンライナーでインストール:

```sh
curl -fsSL https://raw.githubusercontent.com/yuuan/WinNotifier/main/tools/notify-sh/install.sh | sh
```

`notify-send` としても使えるようにする場合:

```sh
curl -fsSL https://raw.githubusercontent.com/yuuan/WinNotifier/main/tools/notify-sh/install.sh | sh -s -- --notify-send
```

`~/.local/bin` にインストールされます。`PATH` 内で `/usr/bin` より前にあることを確認してください。

## Update

```sh
winnotify --self-update
```

同じディレクトリに winnotify-send がある場合、一緒に更新されます。

## Uninstall

```sh
winnotify-send --uninstall  # notify-send シンボリックリンクがあれば削除
rm ~/.local/bin/winnotify ~/.local/bin/winnotify-send
rm -r "${XDG_CONFIG_HOME:-$HOME/.config}/winnotifier"
```

## winnotify

WinNotifier の API を呼び出して Windows に通知を送ります。

### Usage

```
winnotify -t <title> -m <message> [-i <icon>] [-f <from>]
```

| オプション | 説明 |
|---|---|
| `-t, --title` | 通知タイトル (必須) |
| `-m, --message` | 通知メッセージ (必須) |
| `-i, --icon` | 絵文字アイコン (例: `rocket`, `:bell:`, `🚀`) |
| `-f, --from` | 送信者名 |
| `--init` | サンプルの設定ファイルを作成 |
| `--configure` | 設定ファイルをエディタで開く |
| `--icons` | 使用可能な絵文字アイコンを一覧表示 |
| `--version` | インストール済みバージョン (コミットハッシュ) を表示 |
| `--self-update` | GitHub から最新版に更新 |

### Configuration

設定ファイル (`~/.config/winnotifier/config`) または環境変数で接続先を指定します:

```sh
# ~/.config/winnotifier/config
endpoint=http://<windows-ip>:8080
token=<your-token>
```

環境変数 `WINNOTIFIER_ENDPOINT`, `WINNOTIFIER_TOKEN` で上書きできます。

## winnotify-send

`notify-send` と同じ引数を受け付け、内部で winnotify に変換して通知を送ります。

### Usage

```
winnotify-send [OPTIONS] SUMMARY [BODY]
```

`notify-send` として使うにはシンボリックリンクを作成します:

```sh
winnotify-send --install    # notify-send シンボリックリンクを作成
winnotify-send --uninstall  # notify-send シンボリックリンクを削除
```

### 引数のマッピング

| notify-send | winnotify | 説明 |
|---|---|---|
| `SUMMARY` (位置引数1) | `-t` | 通知タイトル |
| `BODY` (位置引数2) | `-m` | 通知メッセージ (省略時は SUMMARY を使用) |
| `-a, --app-name` | `-f` | 送信者名 |
| `-i, --icon` | `-i` | アイコン (そのまま winnotify に渡される) |

その他のオプション (`-u`, `-t`, `-c`, `-e`, `-w`, `-h`, `-A`, `-r`, `-n`, `-p` など) は受け取りますが無視します。
