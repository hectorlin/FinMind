from __future__ import annotations

import datetime
import os
import time
from pathlib import Path

import pandas as pd
from loguru import logger
from FinMind.data import DataLoader

ROOT = Path(__file__).resolve().parent
DEFAULT_TOKEN = ""

def _load_dotenv() -> None:
    path = ROOT / ".env"
    if not path.is_file():
        return
    for raw in path.read_text(encoding="utf-8").splitlines():
        line = raw.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, _, value = line.partition("=")
        key = key.strip()
        if not key or key in os.environ:
            continue
        value = value.strip().strip("'").strip('"')
        os.environ[key] = value


STOCK_ID_LIST = [
    "00878",
    "00919",
    "0056",
    "0050",
]
MAX_RUN_RETRIES = 10
RETRY_DELAY_SECONDS = 3


def _resolve_token() -> str:
    return os.getenv("FINMIND_TOKEN", DEFAULT_TOKEN).strip()


def main() -> None:
    _load_dotenv()
    token = _resolve_token()
    if not token:
        raise SystemExit("Set FINMIND_TOKEN (e.g. in .env) or DEFAULT_TOKEN before running.")

    data_loader = DataLoader()
    data_loader.login_by_token(token)

    start_date = "2025-01-01"
    end_date = "2025-12-31"
    cal = data_loader.taiwan_stock_trading_date(
        start_date=start_date,
        end_date=end_date,
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

    non_empty = [df for df in dfs if df is not None and not df.empty]
    if not non_empty:
        logger.warning("no kbar rows; skip writing csv")
        return

    combined = pd.concat(non_empty, ignore_index=True).drop_duplicates()
    symbol_col = "stock_id"
    if symbol_col not in combined.columns:
        raise SystemExit(f"expected column {symbol_col!r}, got {list(combined.columns)}")

    done = 0
    for symbol, part in combined.groupby(symbol_col, sort=False):
        out = part.copy()
        if symbol_col in out.columns:
            out = out.drop(columns=[symbol_col])
        sort_keys = [c for c in ("date", "minute") if c in out.columns]
        if sort_keys:
            out = out.sort_values(sort_keys, kind="mergesort").reset_index(drop=True)

        safe = str(symbol).strip().replace("/", "_")
        path = ROOT / f"output_{safe}_{start_date}_{end_date}.csv"
        out.to_csv(path, index=False, encoding="utf-8-sig")
        logger.info("wrote {} rows -> {}", len(out), path)
        done += 1

    logger.info("done: {} symbols", done)


def run_with_retry() -> None:
    last_error: Exception | None = None
    for attempt in range(1, MAX_RUN_RETRIES + 1):
        try:
            logger.info("run attempt {}/{}", attempt, MAX_RUN_RETRIES)
            main()
            return
        except Exception as exc:
            last_error = exc
            logger.warning("run failed ({}/{}): {}", attempt, MAX_RUN_RETRIES, exc)
            if attempt < MAX_RUN_RETRIES:
                time.sleep(RETRY_DELAY_SECONDS)
    raise SystemExit(f"run failed after {MAX_RUN_RETRIES} retries: {last_error}")


if __name__ == "__main__":
    run_with_retry()
