from Library.GeneratePortfolio import *
from Library.RiskCalc import calc_bollinger_lower_indicator, calc_bollinger_upper_indicator
import datetime
import math

TRANING_START = '2019-08-01'
TRANING_END = '2020-02-03'
TESTING_START = '2020-02-04'
TESTING_END = '2020-04-17'
NUM_PORTFS_GEN = 3

def calc_portfolio_value(asset_value, asset_shares:dict):
    portf_val = 0
    for key in asset_shares.keys():
        portf_val = portf_val + asset_value[key] * math.trunc(asset_shares[key])
    return portf_val

def create_porfolios(training_start: str, training_end: str, num_portf, buying_power):
    print("generating portfolios")
    # universe = ReadUniverse(training_end)
    # filtered_universe = filter_universe_trend(universe, training_start, training_end)
    # if filtered_universe != 0:
    return generate_portfolios(buying_power, 1, training_start, training_end, num_portf)
    # else:
    #     return None

def update_portf_stats(portfs: [Portfolio]):
    test_data = []
    start_values = []
    bollinger_data = []

    for portf in portfs:
        data = portfolio_get_data(portf,TESTING_START,TESTING_END, "Adj_Close")[0]
        test_data.append(data)
        start_values.append(data.iloc[0])
        data_bol = bollinger_series_data(portf.assets, portf.AssetShares, "Close", TESTING_START, TESTING_END)
        bollinger_data.append(data_bol)

    return test_data , start_values , bollinger_data

def backtest_bollinger(portfs: [Portfolio]):

    # generate portfolios for test if none provided
    if len(portfs) == 0:
        portfs = create_porfolios(TRANING_START, TRANING_END, NUM_PORTFS_GEN, 10000)

    # all of length NUM_PORTFS_GEN
    testing_data = []
    starting_asset_values = []
    bollinger_portfs_data = []

    # get all portfolio data and statistics for testing
    testing_data , starting_asset_values, bollinger_portfs_data = update_portf_stats(portfs)

    # significant variables
    starting_investment = 10000
    current_investment = starting_investment
    portfolio_active = False
    portfolio_loc = 0 # by default
    current_month = int(datetime.datetime.strptime(testing_data[portfolio_loc].index[0], '%Y-%m-%d').date().month)
    num_testing_days = testing_data[portfolio_loc].shape[0]

    # for each day in testing date
    for i in range(1,num_testing_days):
        # new day calculations
        current_date = datetime.datetime.strptime(testing_data[portfolio_loc].index[i], '%Y-%m-%d').date()
        start_bollinger_date = current_date + datetime.timedelta(days=-14)
        month = int(current_date.month)
        current_portf_val = calc_portfolio_value(testing_data[portfolio_loc].iloc[i], portfs[portfolio_loc].AssetShares)
        starting_portf_val = calc_portfolio_value(starting_asset_values[portfolio_loc], portfs[portfolio_loc].AssetShares)
        current_return = current_portf_val - starting_portf_val
        print(str(current_date)+":")

        # sell portfolio at the end of testing range if portfolio is still active
        if i == num_testing_days - 1 and portfolio_active:
            current_investment = current_investment + current_return
            print("sold portfolio at {} end test".format(current_portf_val))
            break

        # generate new portfolio at beginning of new month
        if  month > current_month:
            if portfolio_active:
                current_investment = current_investment + current_return
                print("sold portfolio at {} new month".format(current_portf_val))

            print("new month")
            portfs = create_porfolios(TRANING_START, testing_data[portfolio_loc].index[i], NUM_PORTFS_GEN, current_investment)
            if portfs != None:
                # update asset starting prices, current month, active portf
                testing_data , starting_asset_values, bollinger_portfs_data = update_portf_stats(portfs)

            portfolio_active = False
            current_month = month

        # check for stop loss condition if portfolio is active
        elif portfolio_active:
            # sell portfolio if hit sell signal. lower band hit.
            data = bollinger_portfs_data[portfolio_loc][str(start_bollinger_date):str(current_date)]
            if calc_bollinger_lower_indicator(data, len(data)):
                current_investment = current_investment + current_return
                portfolio_active = False
                print("sold portfolio {} at {} due to bollinger band".format(portfolio_loc, current_portf_val))

        # check buy signal
        elif not portfolio_active:
            # check each potential portfolio
            for k in range(0,len(portfs)):
                data = bollinger_portfs_data[k][str(start_bollinger_date):str(current_date)]
                portf_val = calc_portfolio_value(testing_data[k].iloc[i], portfs[k].AssetShares)
                if calc_bollinger_upper_indicator(data, len(data)) and not portfolio_active and portf_val <= current_investment:
                    portfolio_active = True
                    portfolio_loc = k
                    starting_asset_values[k] = testing_data[portfolio_loc].iloc[i]
                    print("bought portfolio {} at {}".format(k, portf_val))
                    break

    print(current_investment/starting_investment)

def backtest(por: Portfolio):
    portf = por
    stoploss = portfolio_var(portf,TRANING_START,TRANING_END,21)
    testing_data= portfolio_get_data(portf,TESTING_START,TESTING_END, "Adj_Close")[0]

    # starting values
    starting_asset_values = testing_data.iloc[0]
    current_month = int(datetime.datetime.strptime(testing_data.index[0], '%Y-%m-%d').date().month)
    lifetime_returns = 1
    portfolio_active = True
    testing_days = testing_data.shape[0]
    offset = 0

    # for each day in testing date
    for i in range(1,testing_days):
        date = datetime.datetime.strptime(testing_data.index[i-offset], '%Y-%m-%d').date()
        month = int(date.month)
        print(date)

        # buy new portfolio at beginning of new month
        if  month > current_month:
            if portfolio_active:
                asset_returns = testing_data.iloc[i - offset].sum() / starting_asset_values.sum()
                total_returns = asset_returns
                lifetime_returns = lifetime_returns * total_returns
                print(total_returns)
                print("sold portfolio new month")

            print("new month generating portf..")
            # universe = ReadUniverse()
            # filtered_universe = filter_universe_trend(universe, TRANING_START, testing_data.index[i-offset])
            # if filtered_universe != 0:
            portf = generate_portfolio(filtered_universe, 10000, 1)
            if (portf != None):
                # update stoploss, asset starting prices, current month, active portf
                stoploss = portfolio_var(portf, TRANING_START, testing_data.index[i-offset], 21)
                testing_data = portfolio_get_data(portf, testing_data.index[i-offset], TESTING_END, "Adj_Close")[0]
                starting_asset_values = testing_data.iloc[0]
                offset = i
                portfolio_active = True
            else:
                portfolio_active = False
            current_month = month

        # check for stoploss condition if portfolio is active
        elif portfolio_active:
            # returns for day
            asset_returns = testing_data.iloc[i-offset].sum()/starting_asset_values.sum()
            total_returns = asset_returns

            # sell portfolio if hit stop loss
            if total_returns <= (1+stoploss):
                lifetime_returns = lifetime_returns*total_returns
                portfolio_active = False
                print(total_returns)
                print("sold portfolio stoploss")

        # sell portfolio at the end of testing range if portfolio is still active
        if i == testing_days-1 and portfolio_active:
            asset_returns = testing_data.iloc[i - offset].sum() / starting_asset_values.sum()
            total_returns = asset_returns
            lifetime_returns = lifetime_returns*total_returns
            print(total_returns)
            print("sold portfolio end test")

    print(lifetime_returns)

