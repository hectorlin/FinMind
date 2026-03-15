# Taiwan stock minute K-bar (分K) via FinMind
# Ref: https://finmind.github.io/tutor/TaiwanMarket/Technical/#k-taiwanstockkbar-sponsor
# Single request = one day only (per stock). use_async returns empty here, so we use per-stock loop.

from datetime import datetime, timedelta
import time
import pandas as pd
from FinMind.data import DataLoader
from tqdm import tqdm

api = DataLoader()
token = 'eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJkYXRlIjoiMjAyNi0wMy0xMyAyMjowODoxMyIsInVzZXJfaWQiOiJ3ZWlzZW5saW4yIiwiZW1haWwiOiJzaWx2ZXJsaW4yQG1zbi5jb20iLCJpcCI6IjYwLjI0OC4xODQuMTM5In0.oBbVN--fbv2ElVdF0jDbDo1to4hAsO9DSC5rA7sMMxQ'
api.login_by_token(api_token=token)

with open("date.txt", "r", encoding="utf-8") as f:
    date_lines = [line.strip() for line in f if line.strip()]

start_parts = [int(part.strip()) for part in date_lines[0].split(",")]
end_parts = [int(part.strip()) for part in date_lines[1].split(",")]

start_date = datetime(*start_parts)
end_date = datetime(*end_parts)
start_str = start_date.strftime("%Y-%m-%d")
end_str = end_date.strftime("%Y-%m-%d")

with open("stock_id.txt", "r", encoding="utf-8") as f:
    stock_ids = [line.strip() for line in f if line.strip()]

# Fetch trading days only (doc: 資料更新時間 星期一至五)
try:
    trading_df = api.taiwan_stock_trading_date()
    trading_dates = set(trading_df["date"].astype(str).tolist())
except Exception:
    trading_dates = None  # fallback: request every day

all_data = {sid: [] for sid in stock_ids}
date_list = []
current = start_date
while current <= end_date:
    date_list.append(current.strftime("%Y-%m-%d"))
    current += timedelta(days=1)

if trading_dates is not None:
    date_list = [d for d in date_list if d in trading_dates]

MAX_RETRIES = 3
RETRY_DELAY = 2

for date_str in tqdm(date_list, desc="Days"):
    for stock_id in stock_ids:
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

for stock_id, df_list in all_data.items():
    if not df_list:
        print(f"No data for {stock_id}, skip.")
        continue
    full_df = pd.concat(df_list, ignore_index=True)
    # Schema: date, minute, stock_id, open, high, low, close, volume
    if "stock_id" in full_df.columns:
        full_df = full_df.drop(columns=["stock_id"])
    # Sort by date, minute for correct raw order
    sort_cols = [c for c in ["date", "minute"] if c in full_df.columns]
    if sort_cols:
        full_df = full_df.sort_values(sort_cols).reset_index(drop=True)
    output_name = f"output_{stock_id}_{start_str}_{end_str}.csv"
    full_df.to_csv(output_name, index=False, encoding="utf-8-sig")
    print(f"Saved {output_name}")
