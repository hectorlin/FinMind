from __future__ import annotations

import datetime
import os
from pathlib import Path

import pandas as pd
from loguru import logger
from FinMind.data import DataLoader

ROOT = Path(__file__).resolve().parent
OUTPUT_DIR = ROOT / "kbar_by_symbol"

DEFAULT_TOKEN = ""

STOCK_ID_LIST = [
    "2330",
    "0050",
    "2002",
    "2317",
    "1101",
    "0056",
    "2890",
    "00878",
    "00713",
]


def _resolve_token() -> str:
    return os.getenv("FINMIND_TOKEN", DEFAULT_TOKEN).strip()


def main() -> None:
    token = _resolve_token()
    if not token:
        raise SystemExit("Set FINMIND_TOKEN or DEFAULT_TOKEN before running.")

    data_loader = DataLoader()
    data_loader.login_by_token(token)

    cal = data_loader.taiwan_stock_trading_date(
        start_date="2023-01-01",
        end_date="2023-01-31",
    )
    trading_date_list = cal["date"].astype(str).tolist()

    start = datetime.datetime.now()
    dfs = [
        data_loader.taiwan_stock_kbar(
            stock_id_list=STOCK_ID_LIST,
            date=date,
            use_async=True,
        )
        for date in trading_date_list
    ]
    elapsed = datetime.datetime.now() - start
    logger.info("fetch kbar: {}", elapsed)

    combined = pd.concat(dfs, ignore_index=True)
    if combined.empty:
        logger.warning("no kbar rows; skip writing csv")
        return

    symbol_col = "stock_id"
    if symbol_col not in combined.columns:
        raise SystemExit(f"expected column {symbol_col!r}, got {list(combined.columns)}")

    sort_keys = [c for c in ("date", "minute") if c in combined.columns]
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    for symbol, part in combined.groupby(symbol_col, sort=False):
        out = part.sort_values(sort_keys, kind="mergesort") if sort_keys else part
        safe = str(symbol).strip().replace("/", "_")
        path = OUTPUT_DIR / f"{safe}.csv"
        out.to_csv(path, index=False)
        logger.info("wrote {} rows -> {}", len(out), path)

    logger.info("done: {} symbols -> {}", combined[symbol_col].nunique(), OUTPUT_DIR)


if __name__ == "__main__":
    main()
