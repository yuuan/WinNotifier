# WinNotifier

LAN 内の Linux / Mac から Windows 11 へ通知を送るツール。

タスクトレイに常駐し、HTTP API サーバーを立てる。通知送信 API が叩かれると Windows 11 ネイティブのトースト通知として表示する。

## セットアップ

### Windows (サーバー)

`WinNotifier.exe` を起動するとタスクトレイに常駐し、ポート 8080 (デフォルト) で HTTP リクエストを待ち受ける。

初回起動時に `config.json` が exe と同じディレクトリに自動生成される。`token` にはランダムな UUID が設定される。

```json
{
  "port": 8080,
  "token": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
}
```

- `port` — 待ち受けポート番号
- `token` — Basic 認証トークン。設定すると認証が有効になる。`null` または省略で認証なし

### Mac / Linux (クライアント)

```bash
curl -fsSL https://raw.githubusercontent.com/yuuan/WinNotifier/main/tools/notify-sh/winnotify -o ~/.local/bin/winnotify && chmod +x ~/.local/bin/winnotify
```

初期設定:

```bash
winnotify --init
winnotify --configure
```

`--init` で `~/.config/winnotifier/config` にサンプル設定ファイルが作成される。`--configure` でエディタが開くので、Windows の IP アドレスと `config.json` に表示されたトークンを設定する。

```sh
# WinNotifier config
endpoint=http://<WindowsのIP>:8080
token=<config.json の token の値>
```

環境変数 `WINNOTIFIER_ENDPOINT` と `WINNOTIFIER_TOKEN` でも設定可能。設定されている場合はファイルより優先される。

### Windows (クライアント)

`Notify.exe` を `WinNotifier.exe` と同じディレクトリに配置する。`config.json` のポート番号とトークンを自動で参照する。

PowerShell 用の `winnotify.ps1` も `tools/notify-sh/` に用意されている。

## API

```
POST /notify
Content-Type: application/json
```

`token` が設定されている場合は Basic 認証が必要。ユーザー名は任意 (空でも可)、パスワードにトークンを指定する。

### パラメータ

| 名前 | 必須 | 説明 |
|------|------|------|
| `title` | Yes | 通知のタイトル |
| `message` | Yes | 通知の本文 |
| `from` | No | 送信元アプリケーション名 (通知の下部に小さく表示) |
| `icon` | No | 絵文字アイコン (Twemoji で表示。`:rocket:`、`rocket`、`🚀` のいずれかの形式) |

### レスポンス

- `200 OK` — `{"status":"sent"}`
- `400 Bad Request` — バリデーションエラー
- `401 Unauthorized` — 認証失敗

## 通知の送り方

### Mac / Linux から (winnotify)

```bash
winnotify -t <title> -m <message> [-i <icon>] [-f <from>]
```

```bash
# 基本
winnotify -t "ビルド完了" -m "成功しました"

# アイコンと送信元を指定
winnotify --title "デプロイ完了" --message "本番環境へのデプロイが成功しました" --icon rocket --from "GitHub Actions"
```

### Windows から (Notify.exe)

```
Notify.exe -t <title> -m <message> [-i <icon>] [-f <from>]
```

```powershell
# 基本
Notify.exe -t "ビルド完了" -m "成功しました"

# アイコンと送信元を指定
Notify.exe --title "デプロイ完了" --message "本番環境へのデプロイが成功しました" --icon rocket --from "GitHub Actions"
```

### Mac / Linux から (curl)

```bash
# JSON 形式
curl -u :<token> -X POST http://<WindowsのIP>:8080/notify \
  -H "Content-Type: application/json" \
  -d '{"title":"デプロイ完了","message":"成功しました","icon":"rocket","from":"GitHub Actions"}'

# フォーム形式
curl -u :<token> -X POST http://<WindowsのIP>:8080/notify \
  -d title=デプロイ完了 -d message=成功しました -d icon=rocket
```

## 絵文字アイコン

`icon` パラメータに以下のいずれかの形式で絵文字を指定できる。

- GitHub ショートコード: `:rocket:`
- ショートコード (コロンなし): `rocket`
- Unicode 絵文字: `🚀`

[jdecked/twemoji](https://github.com/jdecked/twemoji) の PNG 画像を CDN からダウンロードして表示する。ダウンロードした画像は exe と同じディレクトリの `emoji-cache/` にキャッシュされる。

ショートコードの一覧は [GitHub Emoji Cheat Sheet](https://github.com/ikatyang/emoji-cheat-sheet) を参照。

## 補足: Windows の curl で日本語が文字化けする場合

Windows の curl はコンソールのコードページ (日本語環境では Shift_JIS) でデータを送信する。フォーム形式で日本語を送る場合は Content-Type ヘッダーで文字コードを明示する。

```powershell
curl -u :<token> -X POST http://localhost:8080/notify `
  -H "Content-Type: application/x-www-form-urlencoded; charset=shift_jis" `
  -d "title=テスト" -d "message=こんにちは"
```

JSON 形式であればこの問題は発生しない。Windows からの送信には `Notify.exe` の利用を推奨する。
