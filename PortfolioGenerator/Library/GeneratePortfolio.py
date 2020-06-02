from platypus import NSGAII, Problem, Integer
from Library.ReadUniverse import *
from Library.FilterUniverse import *
from Library.RiskCalc import *
from Library.RabbitMQProducer import *
from Library.RPC import *
import threading
import ta
from datetime import datetime
from dateutil.relativedelta import relativedelta, FR

class GeneratingThread(threading.Thread):
    def run(self):
        # get last friday date for end date of stock data  and set start date to 6 months prior
        end_date = datetime.now() + relativedelta(weekday=FR(-1))
        start_date = end_date + relativedelta(weekday=FR(-27))
        end_date = str(end_date.date())
        start_date = str(start_date.date())

        # generate portfolio using genetic alg
        portf = generate_portfolio(10000, 1, start_date, end_date)

        # convert from portfolio data class to the appropriate message class needed for the Database to parse
        UDMportf = Portfolio_to_UDMPortfolio(portf)
        request_msg = UDMRequest(None,-1, UDRequestType.Portfolio, UDOperation.Insert, None, [UDMportf], None, None)

        # send generated portfolio to user database through rabbitMQ
        rpc = RPCClient()
        try:
            print("sent")
            response = rpc.call(request_msg.to_json())
        except:
            print("failed to send portfolios to database")
        return

# Setting up the Problem class for genetic algorithm
class OptPortfolio(Problem):

    def __init__(self, universe: Universe, buying_power: float):
        # Initialize Problem parent class with the number of assets as the number
        # of variables, 1 objective function, and 1 contraint respectively.
        super(OptPortfolio, self).__init__(universe.count,1,1)
        self.universe = universe
        self.ticker_set = [asset.Ticker for asset in universe.UniverseSet]
        self.universe_historical_data = gen_universe_hist_data(universe.UniverseSet,'Adj_Close')
        self.buying_power = buying_power
        # Specify variables as an integer from [0,1]
        self.typeInt = Integer(0,1)
        self.types[:] = self.typeInt
        # Specify the contraint equation to equal 0
        self.constraints[:] = "==0"
        self.directions[:] = Problem.MAXIMIZE

    def evaluate(self, solution):
        solution.objectives[:] = gen_Entropy(solution.variables, self.ticker_set, self.universe_historical_data)
        solution.constraints[0] = sum(solution.variables) - 10

# Function to generate a single portfolio, used mostly for testing
def generate_portfolio(buying_power: float, user_id: int, start_date: str, end_date: str):
    # Setup stock universe
    universe = ReadUniverse(start_date,end_date)
    filtered_universe = filter_universe_trend(universe,start_date,end_date)
    if (filtered_universe == None or filtered_universe.count == 0):
        print("Error filtering, please ensure correct start and end dates, and that they're valid trading days.")
        return None
    # Initialize problem class and genetic algorithm to be used, and run for 10000 evolutions
    problem = OptPortfolio(filtered_universe,buying_power)
    algorithm = NSGAII(problem)
    algorithm.run(1000)

    # Only keep solutions that are within constraints
    feasible_solutions = [s for s in algorithm.result if s.feasible]

    # Decode solution back to integers and get the first one
    sol = [problem.typeInt.decode(i) for i in feasible_solutions[0].variables]

    # Create allocation dictionary using ticker name as key, assuming uniform weights
    alloc = {}
    shares = {}
    assets = []
    for i, asset in enumerate(filtered_universe.UniverseSet):
        if (sol[i] == 1):
            alloc[asset.Ticker] = 0.1
            shares[asset.Ticker] = (buying_power * 0.1) / asset.LastPrice
            assets.append(asset)

    portf = Portfolio(UserID=user_id,BuyingPower= buying_power,assets= assets,AssetAlloc = alloc,AssetShares = shares)

    return portf

# Function to generate the requested number of portfolios
def generate_portfolios(buying_power: float, user_id: int, start_date: str, end_date: str, num_portfs: int):
    # Setup stock universe
    universe = ReadUniverse(start_date,end_date)
    filtered_universe = filter_universe_trend(universe,start_date,end_date)
    if (filtered_universe == None or filtered_universe.count == 0):
        print("Error filtering, please ensure correct start and end dates, and that they're valid trading days.")
        return None
    # Initialize problem class and genetic algorithm to be used, and run for 10000 evolutions
    problem = OptPortfolio(filtered_universe,buying_power)
    algorithm = NSGAII(problem)
    algorithm.run(1000)

    # Only keep solutions that are within constraints
    feasible_solutions = [s for s in algorithm.result if s.feasible]
    ports = num_portfs

    # If the number of feasible solutions is less than the number requested, use the length as the number to return
    if len(feasible_solutions) < num_portfs:
        ports = len(feasible_solutions)

    # Decode solutions back to integers for the number of requested portfolios
    solutions = [[problem.typeInt.decode(i) for i in feasible_solutions[j].variables] for j in range(ports)]

    # Create allocation dictionary using ticker name as key, assuming uniform weights, for each solution
    portfolios = []
    for sol in solutions:
        alloc = {}
        shares = {}
        assets = []
        for i, asset in enumerate(filtered_universe.UniverseSet):
            if (sol[i] == 1):
                alloc[asset.Ticker] = 0.1
                shares[asset.Ticker] = (buying_power * 0.1) / asset.LastPrice
                assets.append(asset)

        portf = Portfolio(UserID = user_id,BuyingPower = buying_power,assets=assets,AssetAlloc = alloc,AssetShares = shares)
        portfolios.append(portf)

    return portfolios

### Dataclass Conversions ###
# converts all assets in portfolio to a list of UDMHoldings so that
# it fits the User Database messaging format
def Assets_to_UDMHoldings(portf: Portfolio):
    UDMholdings = []
    for asset in portf.assets:
        quantity = math.trunc((portf.BuyingPower * portf.AssetAlloc[asset.Ticker]) / asset.LastPrice)
        holding = UDMHolding(-1, -1, asset.Name, asset.Ticker, '', quantity)
        UDMholdings.append(holding)
    return UDMholdings

# converts a portfolio to a UDMPortfolio so that
# it fits the User Database messaging format
def Portfolio_to_UDMPortfolio(portf: Portfolio):
    horizion = 21  # number of trading days per month
    end_date = datetime.now() + relativedelta(weekday=FR(-1))
    start_date = end_date + relativedelta(weekday=FR(-27))
    VaR = portfolio_var(portf,str(start_date),str(end_date),horizion) # portfolio_var returns a negative percent
    stopValue = round(portf.BuyingPower * (1+VaR), 2)
    holdings = Assets_to_UDMHoldings(portf)
    # risk is at medium for all portfolios
    UDMportf = UDMPortfolio(-1, -1, False, str(datetime.now()), portf.BuyingPower, stopValue, Risk.Med, holdings)
    return UDMportf