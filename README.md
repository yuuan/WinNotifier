# WinNotifier

LAN 内の Linux / Mac から Windows 11 へ通知を送るツール。

タスクトレイに常駐し、HTTP API サーバーを立てる。通知送信 API が叩かれると Windows 11 ネイティブのトースト通知として表示する。

## セットアップ

`WinNotifier.exe` を起動するとタスクトレイに常駐し、ポート 8080 (デフォルト) で HTTP リクエストを待ち受ける。

初回起動時に `config.json` が exe と同じディレクトリに自動生成される。

```json
{
  "port": 8080
}
```

ポート番号を変更したい場合はこのファイルを編集してアプリを再起動する。

## API

```
POST /notify
Content-Type: application/json
```

### パラメータ

| 名前 | 必須 | 説明 |
|------|------|------|
| `title` | Yes | 通知のタイトル |
| `message` | Yes | 通知の本文 |
| `from` | No | 送信元アプリケーション名 (通知の末尾に小さく表示) |
| `icon` | No | 絵文字アイコン (Twemoji で表示。`:rocket:`、`rocket`、`🚀` のいずれかの形式) |

### レスポンス

- `200 OK` — `{"status":"sent"}`
- `400 Bad Request` — バリデーションエラー

## 通知の送り方

### Mac / Linux から (notify スクリプト)

`tools/notify-sh/winnotify` を PATH の通った場所にコピーまたはシンボリックリンクを作成する。

```bash
cp tools/notify-sh/winnotify ~/.local/bin/winnotify
```

エンドポイントを設定する。環境変数か設定ファイルのどちらかで指定する。

```bash
# 環境変数 (.zshrc / .bashrc に追加)
export WINNOTIFIER_ENDPOINT=http://<WindowsのIP>:8080

# または設定ファイル
mkdir -p ~/.config/winnotifier
echo "WINNOTIFIER_ENDPOINT=http://<WindowsのIP>:8080" > ~/.config/winnotifier/config
```

環境変数が既に設定されている場合は設定ファイルを読み込まない。

```bash
winnotify -t <title> -m <message> [-i <icon>] [-f <from>]
```

```bash
# 基本
winnotify -t "ビルド完了" -m "成功しました"

# アイコンと送信元を指定
winnotify --title "デプロイ完了" --message "本番環境へのデプロイが成功しました" --icon rocket --from "GitHub Actions"
```

### Mac / Linux から (curl)

```bash
# JSON 形式
curl -X POST http://<WindowsのIP>:8080/notify \
  -H "Content-Type: application/json" \
  -d '{"title":"デプロイ完了","message":"成功しました","icon":"rocket","from":"GitHub Actions"}'

# フォーム形式
curl -X POST http://<WindowsのIP>:8080/notify \
  -d title=デプロイ完了 -d message=成功しました -d icon=rocket
```

### Windows から (Notify.exe)

`Notify.exe` を `WinNotifier.exe` と同じディレクトリに配置する。`config.json` のポート番号を参照して localhost に通知を送る。

```
Notify.exe -t <title> -m <message> [-i <icon>] [-f <from>]
```

```powershell
# 基本
Notify.exe -t "ビルド完了" -m "成功しました"

# アイコンと送信元を指定
Notify.exe --title "デプロイ完了" --message "本番環境へのデプロイが成功しました" --icon rocket --from "GitHub Actions"
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
curl -X POST http://localhost:8080/notify `
  -H "Content-Type: application/x-www-form-urlencoded; charset=shift_jis" `
  -d "title=テスト" -d "message=こんにちは"
```

JSON 形式であればこの問題は発生しない。Windows からの送信には `Notify.exe` の利用を推奨する。
