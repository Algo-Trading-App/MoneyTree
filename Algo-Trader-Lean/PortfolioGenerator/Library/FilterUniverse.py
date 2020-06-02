import pandas as pd
import numpy as np
import ta
import os
from Library.DataClass import *

VOLATILITY_THRESHOLD = 0.25
ADX_THRESHOLD = 25

def calc_volatility(asset: Asset, num_days: int = 252):
    path = os.path.abspath('./Stock_Data')+'/'+asset.ticker+'.csv'
    df = pd.read_csv(path)

    if len(df) <= num_days:
        return None
    
    prices = df['Adj. Close'][-num_days:].values
    prev_prices = df['Adj. Close'][-(num_days+1):-1].values

    log_ret = np.log(prices/prev_prices)
    std = np.std(log_ret)
    volatility = std * np.sqrt(num_days)

    return volatility

def calc_adx(asset: Asset, data_period:int = 252, num_days: int = 14):
    path = os.path.abspath('./Stock_Data')+'/'+asset.ticker+'.csv'
    df = pd.read_csv(path)

    if len(df) <= num_days:
        return None

    df = df.tail(data_period)

    adx = ta.trend.ADXIndicator(high=df['Adj. High'],low=df['Adj. Low'],close=df['Adj. Close'],n=num_days)

    return (adx.adx().values[-1],adx.adx_neg().values[-1],adx.adx_pos().values[-1])

def calc_aroon(asset: Asset, data_period:int = 252, num_days: int = 25):
    path = os.path.abspath('./Stock_Data')+'/'+asset.ticker+'.csv'
    df = pd.read_csv(path)

    if len(df) <= num_days:
        return None

    df = df.tail(data_period)

    aroon = ta.trend.AroonIndicator(close=df['Adj. Close'],n=num_days)

    return (aroon.aroon_down().values[-1],aroon.aroon_up().values[-1])

def filter_universe(universe: Universe):
    filtered_set = []
    count = 0

    for asset in universe.universe_set:
        vol = calc_volatility(asset)
        adx = calc_adx(asset)
        aroon = calc_aroon(asset)

        if vol == None or adx == None or aroon == None:
            continue

        if vol > VOLATILITY_THRESHOLD and adx[0] > ADX_THRESHOLD and adx[1] < adx[2] and aroon[0] < aroon[1]:
            filtered_set.append(asset)
            count += 1
    
    return Universe(count,filtered_set)