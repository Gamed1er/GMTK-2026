"""
裁掉音效開頭的靜音，輸出 .mp3
用法：把要處理的 .m4a 或 .mp3 放在同資料夾，執行此腳本
"""
import subprocess
from pathlib import Path

folder = Path(__file__).parent

# 支援 m4a 和 mp3
files = list(folder.rglob("*.m4a")) + list(folder.rglob("*.mp3"))

# 排除已經是輸出結果的檔案（避免重複處理）
files = [f for f in files if "_trimmed" not in f.stem]

if not files:
    print("找不到任何音效檔案")
else:
    for src in files:
        dst = src.with_stem(src.stem + "_trimmed").with_suffix(".mp3")
        print(f"處理：{src.name} → {dst.name}")
        subprocess.run([
            "ffmpeg", "-y", "-i", str(src),
            "-af", "silenceremove=start_periods=1:start_silence=0.02:start_threshold=-50dB",
            str(dst)
        ], check=True)
    print(f"\n完成！共處理 {len(files)} 個檔案")
