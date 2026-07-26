"""
把同資料夾內所有 .m4a 轉成 .mp3
需要安裝 ffmpeg：sudo apt install ffmpeg
"""
import subprocess
from pathlib import Path

folder = Path(__file__).parent

m4a_files = list(folder.rglob("*.m4a"))

if not m4a_files:
    print("找不到任何 .m4a 檔案")
else:
    for src in m4a_files:
        dst = src.with_suffix(".mp3")
        print(f"轉換：{src.name} → {dst.name}")
        subprocess.run(["ffmpeg", "-y", "-i", str(src), str(dst)], check=True)
    print(f"\n完成！共轉換 {len(m4a_files)} 個檔案")
