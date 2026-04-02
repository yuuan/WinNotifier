# WinNotifier

LAN 内の他のマシンから送られた通知を Windows 11 ネイティブのトースト通知として表示するツール。

タスクトレイに常駐し、HTTP API サーバーを立てる。通知送信 API が叩かれると通知を表示する。

## サーバー (Windows)

### セットアップ

`WinNotifier.exe` を起動するとタスクトレイに常駐し、ポート 8080 (デフォルト) で HTTP リクエストを待ち受ける。

初回起動時に `config.json` が exe と同じディレクトリに自動生成される。`token` にはランダムな UUID が設定される。

```json
{
  "port": 8080,
  "token": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "icons": {
    "mappings": ["gemoji"],
    "themes": ["Twemoji"]
  }
}
```

- `port` — 待ち受けポート番号
- `token` — Basic 認証トークン。設定すると認証が有効になる。`null` または省略で認証なし
- `icons.mappings` — マッピングの優先順位。先頭が最優先。リストにないものは最後尾扱い
- `icons.themes` — アイコンセットの優先順位。同上

### API

```
POST /notify
Content-Type: application/json
```

`token` が設定されている場合は Basic 認証が必要。ユーザー名は任意 (空でも可)、パスワードにトークンを指定する。

#### パラメータ

| 名前 | 必須 | 説明 |
|------|------|------|
| `title` | Yes | 通知のタイトル |
| `message` | Yes | 通知の本文 |
| `from` | No | 送信元アプリケーション名 (通知の下部に小さく表示) |
| `icon` | No | アイコン名 (後述) |

#### レスポンス

- `200 OK` — `{"status":"sent"}`
- `400 Bad Request` — バリデーションエラー
- `401 Unauthorized` — 認証失敗

### アイコンのダウンロード

`IconDownloader.exe` を実行するとウィザードが起動し、アイコンセットとマッピングをダウンロードできる。

ダウンロード可能な項目:

| 項目 | 種類 | 説明 |
|------|------|------|
| [Twemoji](https://github.com/jdecked/twemoji) | アイコンセット | Twitter 由来の絵文字 PNG (72x72, ~3800 アイコン) |
| [Yaru](https://github.com/ubuntu/yaru) | アイコンセット | Ubuntu デスクトップアイコン (SVG → PNG 変換) |
| [gemoji](https://github.com/github/gemoji) | マッピング | GitHub ショートコード → Unicode 絵文字の変換テーブル |

ダウンロードしたファイルは `icons/` ディレクトリに配置され、`config.json` の `icons` に自動登録される。

### アイコンの指定

`icon` パラメータに以下のいずれかの形式で指定できる。

- ショートコード: `rocket`, `:rocket:` (gemoji マッピングが必要)
- Unicode 絵文字: `🚀` (対応するアイコンセットが必要)
- テーマのアイコン名: `dialog-warning` (Yaru 等のアイコン名を直接指定)

アイコンの解決順序:
1. マッピングルールでショートコードを変換 (例: `rocket` → `🚀`)
2. アイコンセットの name と aliases から一致するものを検索

ショートコードの一覧は [GitHub Emoji Cheat Sheet](https://github.com/ikatyang/emoji-cheat-sheet) を参照。

## クライアント

### Mac / Linux (winnotify)

```bash
curl -fsSL https://raw.githubusercontent.com/yuuan/WinNotifier/main/tools/notify-sh/winnotify -o ~/.local/bin/winnotify && chmod +x ~/.local/bin/winnotify
winnotify --init
winnotify --configure
```

```bash
winnotify -t "ビルド完了" -m "成功しました" -i rocket -f "GitHub Actions"
```

詳細は [tools/notify-sh/README.md](tools/notify-sh/README.md) を参照。

### 同一マシンから (Notify.exe)

`Notify.exe` を `WinNotifier.exe` と同じディレクトリに配置する。`config.json` のポート番号とトークンを自動で参照する。

```
Notify.exe -t <title> -m <message> [-i <icon>] [-f <from>]
```

### 別の Windows マシンから (winnotify.ps1)

`tools/notify-sh/winnotify.ps1` を送信元の Windows マシンに配置する。設定ファイルの形式は winnotify と同じ。

```powershell
.\winnotify.ps1 -t "ビルド完了" -m "成功しました" -i rocket -f "Build Server"
```

### curl

```bash
curl -u :<token> -X POST http://<WindowsのIP>:8080/notify \
  -H "Content-Type: application/json" \
  -d '{"title":"デプロイ完了","message":"成功しました","icon":"rocket","from":"GitHub Actions"}'
```

## ライセンス

[MIT License](LICENSE)

アイコンセットにはそれぞれ固有のライセンスが適用される。ダウンロードしたアイコンの LICENSE ファイルは `icons/themes/<テーマ名>/LICENSE` に配置される。
