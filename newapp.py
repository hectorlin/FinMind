from __future__ import annotations

import argparse
import os
import re
import sys
import time
from datetime import datetime, timedelta
from pathlib import Path

import pandas as pd
from FinMind.data import DataLoader
from tqdm import tqdm

DEFAULT_TOKEN = ""
MAX_RETRIES = 3
RETRY_DELAY = 2
ROOT = Path(__file__).resolve().parent


def parse_ymd(value: str) -> datetime:
    return datetime.strptime(value, "%Y-%m-%d")


def parse_legacy_date_line(value: str) -> datetime:
    parts = [int(part.strip()) for part in value.split(",")]
    return datetime(*parts)


def parse_symbol_list(symbols_text: str) -> list[str]:
    return [part.strip() for part in re.split(r"[\s,]+", symbols_text.strip()) if part.strip()]


def load_inputs(args: argparse.Namespace) -> tuple[list[str], datetime, datetime]:
    has_dates = bool(args.start_date and args.end_date)
    symbols_from_cli: list[str] = []
    if getattr(args, "symbols", None) and str(args.symbols).strip():
        symbols_from_cli = parse_symbol_list(str(args.symbols))
    elif args.symbol and str(args.symbol).strip():
        symbols_from_cli = [args.symbol.strip()]

    if has_dates and symbols_from_cli:
        return symbols_from_cli, parse_ymd(args.start_date), parse_ymd(args.end_date)

    date_path = ROOT / "date.txt"
    stock_path = ROOT / "stock_id.txt"
    with date_path.open("r", encoding="utf-8") as file:
        date_lines = [line.strip() for line in file if line.strip()]
    with stock_path.open("r", encoding="utf-8") as file:
        stock_ids = [line.strip() for line in file if line.strip()]

    if len(date_lines) < 2:
        raise ValueError("date.txt needs two lines: start and end date parts.")
    return stock_ids, parse_legacy_date_line(date_lines[0]), parse_legacy_date_line(date_lines[1])


def build_dates(start_date: datetime, end_date: datetime, api: DataLoader) -> list[str]:
    date_list: list[str] = []
    current = start_date
    while current <= end_date:
        date_list.append(current.strftime("%Y-%m-%d"))
        current += timedelta(days=1)

    try:
        trading_df = api.taiwan_stock_trading_date()
        trading_dates = set(trading_df["date"].astype(str).tolist())
        return [date_value for date_value in date_list if date_value in trading_dates]
    except Exception:
        return date_list


def run(symbols: list[str], start_date: datetime, end_date: datetime) -> int:
    if start_date > end_date:
        print("ERROR: start_date must be <= end_date.")
        return 1

    token = os.getenv("FINMIND_TOKEN", DEFAULT_TOKEN).strip()
    if not token:
        print("ERROR: FINMIND_TOKEN is required. Set it in your environment before running.")
        return 1
    api = DataLoader()
    api.login_by_token(api_token=token)

    start_str = start_date.strftime("%Y-%m-%d")
    end_str = end_date.strftime("%Y-%m-%d")
    all_data: dict[str, list[pd.DataFrame]] = {sid: [] for sid in symbols}
    date_list = build_dates(start_date, end_date, api)

    for date_str in tqdm(date_list, desc="Days"):
        for stock_id in symbols:
            for attempt in range(MAX_RETRIES):
                try:
                    df = api.taiwan_stock_kbar(stock_id=stock_id, date=date_str)
                    break
                except Exception:
                    if attempt < MAX_RETRIES - 1:
                        time.sleep(RETRY_DELAY)
                    else:
                        df = pd.DataFrame()
                        break
            if df is not None and not df.empty:
                all_data[stock_id].append(df)

    has_data = False
    for stock_id, df_list in all_data.items():
        if not df_list:
            print(f"No data for {stock_id}, skip.")
            continue

        has_data = True
        full_df = pd.concat(df_list, ignore_index=True)
        if "stock_id" in full_df.columns:
            full_df = full_df.drop(columns=["stock_id"])
        sort_cols = [column for column in ["date", "minute"] if column in full_df.columns]
        if sort_cols:
            full_df = full_df.sort_values(sort_cols).reset_index(drop=True)

        output_name = f"output_{stock_id}_{start_str}_{end_str}.csv"
        output_path = ROOT / output_name
        full_df.to_csv(output_path, index=False, encoding="utf-8-sig")
        print(f"Saved {output_path}")

    return 0 if has_data else 2


def main() -> int:
    parser = argparse.ArgumentParser(description="Fetch Taiwan stock minute K-bar data from FinMind.")
    parser.add_argument("--symbol", help="Single stock symbol, e.g. 2330")
    parser.add_argument(
        "--symbols",
        help="Multiple symbols: comma or whitespace separated, e.g. '2330,2317 2454'",
    )
    parser.add_argument("--start-date", help="Start date in YYYY-MM-DD")
    parser.add_argument("--end-date", help="End date in YYYY-MM-DD")
    args = parser.parse_args()

    try:
        symbols, start_date, end_date = load_inputs(args)
        return run(symbols, start_date, end_date)
    except Exception as exc:
        print(f"ERROR: {exc}")
        return 1


if __name__ == "__main__":
    sys.exit(main())
