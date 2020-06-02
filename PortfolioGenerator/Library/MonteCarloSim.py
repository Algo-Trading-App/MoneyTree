import numpy as np
import pandas as pd
from typing import List

# geometric brownian motion simulation
# s0: initial price
# mu: mean
# sigma: standard deviation
# period: total number of days to be simulated in days
# delta_t: number of increments per period in days
# n_sim: number of monte carlo simulations
def gbm(s0:float, mu:float, sigma:float, period:int, n_inc:int, n_sim:int, asset: str):
    # brownian motion
    dt = period/n_inc  # size of time step
    w = np.random.normal(0, 1, [n_sim,n_inc+1]) * np.sqrt(dt)  # random increments n_sim by n_inc array

    # simulation setup
    dist = pd.DataFrame()
    t = np.linspace(0,period,n_inc+1)

    for i in range(0,n_sim):  # for number of simulations
        # reset simulation
        s = []
        s.append(s0)

        # GBM simulation
        for k in range(1,n_inc+1):
            delta = (mu-.5*sigma**2)*t[k] + sigma*w[i][k-1]  #gbm formula
            s_k = s[k-1]*np.exp(delta)                       #gbm formula
            s.append(s_k)

        # append simulation results as a new column to dataframe
        col = asset + 'sim' + str(i+1)
        dist[col] = s

    # reformat index
    dist.index = t
    dist.index.name = 'Day'

    return dist


# Monte Carlo simulation for a given data frame and constraints
# period: total number of days to be simulated in days
# n_inc: number of increments in period
# n_sim: number of monte carlo simulations
# type - specific monte carlo type of simulation
# data - historical price data frame for asset(s)
# asset - specifies ticker of assets to apply monte carlo simulation to
def monte_carlo(period:int, n_inc:int, n_sim:int, type: str, data: pd.DataFrame, assets: List[str]):
    df = pd.DataFrame()
    returns = data.pct_change()
    returns_mean = returns.mean()  # daily mean
    returns_std = returns.std()  # daily std

    for name in assets:
        # retrieve asset mean and std
        try:
            asset_mean = returns_mean[name]
            asset_std = returns_std[name]
            asset_last_price = data[name][-1]
        except:
            print('asset not in data frame')
            return

        #switch for monte carlo simulations
        if type is 'GBM':
            temp = gbm(asset_last_price, asset_mean, asset_std, period, n_inc, n_sim, name)

        # append asset simulation to final data frame
        if df.empty:
            df = temp
        else:
            df = df.join(temp)

    return df

