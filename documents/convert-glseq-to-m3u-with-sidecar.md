# GamingLooper の Sequence ファイルを m3u 形式のプレイリストとサイドカーファイルに変換する

[GamingLooper](https://github.com/YoshimiKudo/GamingLooper) を使用すると高速にループ位置を検出できます。
また、検出したループ位置を Sequence ファイル (*.glseq) に保存することができます。

以下の PowerShell スクリプトを使用すると、Sequence ファイルに含まれているトラックリストとループ位置情報を
m3u 形式のプレイリストファイルと SoundTrackPlayer のサイドカーファイルに変換できます。

## 前提

- PowerShell は .NET Core ベースの新しいものを使用してください(OS 付属のものでは動きません)。
- このスクリプトは以下のバージョンで動作確認をしました。異なるバージョンでは正常に動作しない可能性があります。
    - GamingLooper v0.1.0 beta 1
    - SoundTrackPlayer 1.4.0
    - PowerShell 7.4.18


## スクリプト

以下スクリプトをファイルに保存するか、PowerShell に直接貼り付けて実行します。

ファイルから直接実行する場合は、[実行ポリシーの設定](https://learn.microsoft.com/ja-jp/powershell/module/microsoft.powershell.security/set-executionpolicy)が必要です。


実行後、以下を入力してください。
- glseq filepath
    - Sequence ファイル (*.glseq) のフルパスを入力します。
- playlist directory
    - プレイリストファイル (*.m3u8) の出力先ディレクトリを入力します。プレイリストファイルに記載される楽曲ファイルのパスは、このディレクトリからの相対パスになります。

正常に実行が完了すると、`playlist_dir` に Sequence ファイルと同じ名前でプレイリストファイルが作成されます。
また、Sequence ファイルに含まれていた楽曲ファイルと同じディレクトリに SoundTrackPlayer のサイドカーファイルが作成されます。

PowerShell に直接貼り付けた場合は、最後の一文まで実行済みであることを確認してください。

```PowerShell
$glseq_filepath = Read-Host -Prompt "glseq filepath"
$playlist_dir = Read-Host -Prompt "playlist directory"

if ([String]::IsNullOrEmpty($glseq_filepath) -or [String]::IsNullOrEmpty($playlist_dir)) {
    exit 1
}

$glseq_raw = Get-Content -Raw -Encoding UTF8 -LiteralPath $glseq_filepath
$glseq = ConvertFrom-Json $glseq_raw

$playlist_filename = [System.IO.Path]::GetFileNameWithoutExtension($glseq_filepath) + ".m3u8"
$playlist_filepath = [System.IO.Path]::Combine($playlist_dir, $playlist_filename)

$tracks = $glseq.tracks
$items = $glseq.playlist.items

$m3u_content = @()

$items | % {
    $tid = $_.trackId
    $t = ($tracks | ? { $_.id -eq $tid })[0]
    
    $full_filepath = $t.filePath
    $relative_filepath = [System.IO.Path]::GetRelativePath($playlist_dir, $full_filepath)

    $m3u_content += $relative_filepath

    $sidecar_filename = $t.fileName + ".toml"
    $sidecar_dir = [System.IO.Path]::GetDirectoryName($full_filepath)
    $sidecar_filepath = [System.IO.Path]::Combine($sidecar_dir, $sidecar_filename)

    $sample_rate = $t.sampleRate
    $loop_begin = $t.loop.startSample / $sample_rate
    $loop_end = $t.loop.endSample / $sample_rate
    $loop_count = $_.rule.loopCount

    $sidecar_content = @"
loop_position_source = "Custom"
loop_begin = $loop_begin
loop_end = $loop_end
default_loop_mode = "Limited"
loop_count = $loop_count
"@ -replace "`n","`r`n"
    $sidecar_content | Set-Content -LiteralPath $sidecar_filepath -Encoding UTF8
}

$m3u_content | Set-Content -LiteralPath $playlist_filepath -Encoding UTF8

```
