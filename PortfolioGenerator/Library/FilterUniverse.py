import pandas as pd
import numpy as np
import os
from Library.DataClass import *
from Library.RiskCalc import calc_volatility, calc_adx, calc_aroon

# Function that filters the entire universe for promising assets to be evaluated by
# genetic algorithm.
def filter_universe_adx(universe: Universe, start_date: str, end_date: str, VOLATILITY_THRESHOLD: float = 0.2, ADX_THRESHOLD: float = 10):
    filtered_set = []
    count = 0

    # for each asset in the universe, read in it's data from the local cache and if
    # the data exists for the specified range, calculate volalitlity, adx, and aroon.
    for asset in universe.UniverseSet:
        df = asset.AssetData
        vol = None
        adx = None
        aroon = None
        if not df.empty:
            if df.index[-1] == end_date:
                vol = calc_volatility(df)
                adx = calc_adx(df)
                aroon = calc_aroon(df)

        # If values could not be calculated due to lack of data, don't add it.
        if vol == None or adx == None or aroon == None:
            continue

        # If the asset meets the volatility and adx thresholds, and showns an uptrend,
        # add it to the filtered universe.
        if vol > VOLATILITY_THRESHOLD and adx[0] > ADX_THRESHOLD and adx[1] < adx[2] and aroon[0] < aroon[1]:
            filtered_set.append(asset)
            count += 1

    # If the filtered universe contains less than, lower the thresholds and filter again
    if count < 20:
        # If the thresholds get too low, return 0 signaling not enough promising assets
        if VOLATILITY_THRESHOLD * 0.9 > .1 and ADX_THRESHOLD * 0.9 > 5:
            return filter_universe_adx(universe, start_date, end_date, VOLATILITY_THRESHOLD*0.9, ADX_THRESHOLD*0.9)
        else:
            return None
    else:
        return Universe(count,filtered_set, start_date, end_date)

# less strict conditions for filter universe, used for back testing
def filter_universe_trend(universe: Universe, start_date: str, end_date: str):
    filtered_set = []
    max_filter1_size = 100
    max_filter2_size = 30
    filter_df = pd.DataFrame(columns=('ticker', 'mid_returns', 'end_returns'))

    # new dataframe with returns of all assests at points of intrests.
    for asset in universe.UniverseSet:
        df = asset.AssetData
        df = df['Adj_Close']
        if not df.empty:
            if df.index[-1] == end_date:
                size = df.shape[0]
                start_value = df.iloc[0]
                mid_value = df.iloc[round(size / 2)]
                end_value = df.iloc[-1]
                mid_rtns = np.log(mid_value/start_value)
                end_rtns = np.log(end_value/start_value)
                new_row = {'ticker': asset.Ticker, 'mid_returns': mid_rtns, 'end_returns': end_rtns}
                filter_df = filter_df.append(new_row, ignore_index=True)

    # first filter_df by returns halfway through trainning data
    filter_df = filter_df.sort_values(by=['mid_returns'],ascending=False).reset_index(drop=True)
    filter_df = filter_df.iloc[0:max_filter1_size]

    # second filter_df by returns over start to end of trainning data
    filter_df = filter_df.sort_values(by=['end_returns'], ascending=False).reset_index(drop=True)
    filter_df = filter_df.iloc[0:max_filter2_size]

    filtered_assets = filter_df['ticker'].to_numpy()
    for asset in universe.UniverseSet:
        if asset.Ticker in filtered_assets:
            filtered_set.append(asset)

    return Universe(len(filtered_set), filtered_set, start_date, end_date)



