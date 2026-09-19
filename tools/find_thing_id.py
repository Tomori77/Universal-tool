"""按名称（中/英/日）模糊搜索 Elin 物品 id。

用法: python find_thing_id.py 名称 [名称2 ...]
数据源: 游戏内 _Lang_Chinese 的 Thing.xlsx（id + 中英日名）
"""
import sys
import warnings

import openpyxl

XLSX = r"D:\Steam\steamapps\common\Elin\Package\_Lang_Chinese\Lang\CN\Game\Thing.xlsx"


def search(keyword: str) -> list[tuple]:
    warnings.filterwarnings("ignore")
    wb = openpyxl.load_workbook(XLSX)
    ws = wb[wb.sheetnames[0]]
    kw = keyword.lower()
    hits = []
    for r in ws.iter_rows(min_row=3, values_only=True):
        if not r or not r[0]:
            continue
        name, name_en = r[3] or "", r[4] or ""
        if kw in str(name).lower() or kw in str(name_en).lower():
            hits.append((r[0], name, name_en))
    return hits


if __name__ == "__main__":
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    for kw in sys.argv[1:]:
        print(f"=== {kw} ===")
        results = search(kw)
        if not results:
            print("(无结果)")
        for row in results[:40]:
            print("  {:<30} {} / {}".format(*row))
        if len(results) > 40:
            print(f"  ...共 {len(results)} 条，仅显示前 40")
